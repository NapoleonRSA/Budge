using Budge.Application.Common.Interfaces;
using Budge.Application.Common.Security;
using Budge.Domain.Services;

namespace Budge.Application.Budget.Queries.GetBudgetDashboard;

[Authorize]
public record GetBudgetDashboardQuery(int Year, int Month) : IRequest<BudgetDashboardDto>;

public class GetBudgetDashboardQueryValidator : AbstractValidator<GetBudgetDashboardQuery>
{
    public GetBudgetDashboardQueryValidator()
    {
        RuleFor(v => v.Year).InclusiveBetween(2000, 2100);
        RuleFor(v => v.Month).InclusiveBetween(1, 12);
    }
}

public class GetBudgetDashboardQueryHandler : IRequestHandler<GetBudgetDashboardQuery, BudgetDashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetBudgetDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BudgetDashboardDto> Handle(GetBudgetDashboardQuery request, CancellationToken cancellationToken)
    {
        var start = new DateOnly(request.Year, request.Month, 1);
        var end = start.AddMonths(1);
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var people = await _context.People
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PersonDto { Id = p.Id, Name = p.Name })
            .ToListAsync(cancellationToken);

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryBudgetDto
            {
                Id = c.Id,
                Name = c.Name,
                MonthlyBudget = c.MonthlyBudget
            })
            .ToListAsync(cancellationToken);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.SpentOn >= start && e.SpentOn < end)
            .OrderByDescending(e => e.SpentOn)
            .ThenByDescending(e => e.Id)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                PersonId = e.PersonId,
                PersonName = e.Person.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                Amount = e.Amount,
                SpentOn = e.SpentOn,
                Note = e.Note
            })
            .ToListAsync(cancellationToken);

        var bills = await _context.Bills
            .AsNoTracking()
            .OrderBy(b => b.Person.Name)
            .ThenBy(b => b.DueDay)
            .ThenBy(b => b.Name)
            .Select(b => new BillDto
            {
                Id = b.Id,
                PersonId = b.PersonId,
                PersonName = b.Person.Name,
                Name = b.Name,
                Amount = b.Amount,
                DueDay = b.DueDay,
                CreditFacilityId = b.CreditFacilityId
            })
            .ToListAsync(cancellationToken);

        var facilities = await _context.CreditFacilities
            .AsNoTracking()
            .OrderBy(f => f.Person.Name)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);

        foreach (var category in categories)
        {
            var spent = expenses.Where(e => e.CategoryId == category.Id).Sum(e => e.Amount);
            category.Spent = spent;
            category.Remaining = category.MonthlyBudget - spent;
        }

        var spendByPerson = people
            .Select(person => new PersonSpendDto
            {
                PersonId = person.Id,
                Name = person.Name,
                Spent = expenses.Where(e => e.PersonId == person.Id).Sum(e => e.Amount)
            })
            .ToList();

        var credit = facilities
            .Select(facility =>
            {
                var payoff = CreditPayoffCalculator.Calculate(
                    facility.Balance,
                    facility.AnnualInterestRate,
                    facility.MonthlyPayment,
                    asOf);

                return new CreditFacilityDto
                {
                    Id = facility.Id,
                    PersonId = facility.PersonId,
                    PersonName = people.First(p => p.Id == facility.PersonId).Name,
                    Name = facility.Name,
                    Balance = facility.Balance,
                    AnnualInterestRate = facility.AnnualInterestRate,
                    MonthlyPayment = facility.MonthlyPayment,
                    DueDay = facility.DueDay,
                    WillPayOff = payoff.WillPayOff,
                    MonthsToPayoff = payoff.Months,
                    PayoffDate = payoff.PayoffDate,
                    TotalInterest = payoff.TotalInterest,
                    TotalPaid = payoff.TotalPaid,
                    FirstMonthInterest = payoff.FirstMonthInterest
                };
            })
            .ToList();

        var totalBudget = categories.Sum(c => c.MonthlyBudget);
        var totalSpent = expenses.Sum(e => e.Amount);

        return new BudgetDashboardDto
        {
            Year = request.Year,
            Month = request.Month,
            TotalBudget = totalBudget,
            TotalSpent = totalSpent,
            TotalRemaining = totalBudget - totalSpent,
            BillsPayable = bills.Sum(b => b.Amount),
            People = people,
            Categories = categories,
            SpendByPerson = spendByPerson,
            Bills = bills,
            CreditFacilities = credit,
            Expenses = expenses
        };
    }
}

public class BudgetDashboardDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public decimal TotalBudget { get; init; }

    public decimal TotalSpent { get; init; }

    public decimal TotalRemaining { get; init; }

    public decimal BillsPayable { get; init; }

    public IReadOnlyCollection<PersonDto> People { get; init; } = [];

    public IReadOnlyCollection<CategoryBudgetDto> Categories { get; init; } = [];

    public IReadOnlyCollection<PersonSpendDto> SpendByPerson { get; init; } = [];

    public IReadOnlyCollection<BillDto> Bills { get; init; } = [];

    public IReadOnlyCollection<CreditFacilityDto> CreditFacilities { get; init; } = [];

    public IReadOnlyCollection<ExpenseDto> Expenses { get; init; } = [];
}

public class PersonDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public class CategoryBudgetDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal MonthlyBudget { get; init; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }
}

public class PersonSpendDto
{
    public int PersonId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Spent { get; init; }
}

public class BillDto
{
    public int Id { get; init; }

    public int PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public int DueDay { get; init; }

    public int? CreditFacilityId { get; init; }
}

public class CreditFacilityDto
{
    public int Id { get; init; }

    public int PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal Balance { get; init; }

    public decimal AnnualInterestRate { get; init; }

    public decimal MonthlyPayment { get; init; }

    public int DueDay { get; init; }

    public bool WillPayOff { get; init; }

    public int? MonthsToPayoff { get; init; }

    public DateOnly? PayoffDate { get; init; }

    public decimal? TotalInterest { get; init; }

    public decimal? TotalPaid { get; init; }

    public decimal FirstMonthInterest { get; init; }
}

public class ExpenseDto
{
    public int Id { get; init; }

    public int PersonId { get; init; }

    public string PersonName { get; init; } = string.Empty;

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public DateOnly SpentOn { get; init; }

    public string? Note { get; init; }
}
