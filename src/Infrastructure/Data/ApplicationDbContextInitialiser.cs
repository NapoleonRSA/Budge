using Budge.Domain.Constants;
using Budge.Domain.Entities;
using Budge.Domain.Services;
using Budge.Domain.ValueObjects;
using Budge.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Budge.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole(Roles.Administrator);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new [] { administratorRole.Name });
            }
        }

        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            _context.TodoLists.Add(new TodoList
            {
                Title = "Tasks",
                Colour = Colour.Green,
                Items =
                {
                    new TodoItem { Title = "Make a todo list 📃" },
                    new TodoItem { Title = "Check off the first item ✅" },
                    new TodoItem { Title = "Realise you've already done two things on the list! 🤯"},
                    new TodoItem { Title = "Reward yourself with a nice, long nap 🏆" },
                }
            });

            await _context.SaveChangesAsync();
        }

        if (!_context.People.Any())
        {
            var alex = new Person { Name = "Alex" };
            var jordan = new Person { Name = "Jordan" };
            var groceries = new Category { Name = "Groceries", MonthlyBudget = 800m };
            var transport = new Category { Name = "Transport", MonthlyBudget = 300m };
            var dining = new Category { Name = "Dining", MonthlyBudget = 200m };
            var visa = new CreditFacility
            {
                Person = alex,
                Name = "Visa",
                Balance = 4200m,
                AnnualInterestRate = 21.9m,
                MonthlyPayment = 250m,
                DueDay = 20
            };

            _context.People.AddRange(alex, jordan);
            _context.Categories.AddRange(groceries, transport, dining);
            _context.CreditFacilities.Add(visa);
            _context.Bills.AddRange(
                new Bill
                {
                    Person = alex,
                    Name = "Rent",
                    Amount = 1450m,
                    DueDay = 1
                },
                new Bill
                {
                    Person = jordan,
                    Name = "Internet",
                    Amount = 79m,
                    DueDay = 15
                },
                new Bill
                {
                    Person = alex,
                    Name = CreditPayoffCalculator.PaymentBillName(visa.Name),
                    Amount = visa.MonthlyPayment,
                    DueDay = visa.DueDay,
                    CreditFacility = visa
                });

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            _context.Expenses.AddRange(
                new Expense { Person = alex, Category = groceries, Amount = 86.40m, SpentOn = today, Note = "Market" },
                new Expense { Person = jordan, Category = dining, Amount = 42m, SpentOn = today, Note = "Lunch" },
                new Expense { Person = jordan, Category = transport, Amount = 18.50m, SpentOn = today, Note = "Train" });

            await _context.SaveChangesAsync();
        }
    }
}
