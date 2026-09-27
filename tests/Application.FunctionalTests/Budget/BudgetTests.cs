using Budge.Application.Budget.Commands.UpdateCurrency;
using Budge.Application.Budget.Queries.GetBudgetDashboard;
using Budge.Application.Common.Exceptions;
using Budge.Application.CreditFacilities.Commands.ApplyCreditPayment;
using Budge.Application.CreditFacilities.Commands.CreateCreditFacility;
using Budge.Application.CreditFacilities.Commands.DeleteCreditFacility;
using Budge.Application.Expenses.Commands.CreateExpense;
using Budge.Application.People.Commands.CreatePerson;
using Budge.Application.Categories.Commands.CreateCategory;
using Budge.Application.Bills.Commands.CreateBill;
using Budge.Domain.Entities;
using Budge.Domain.Enums;
using Budge.Domain.Services;

namespace Budge.Application.FunctionalTests.Budget;

public class BudgetTests : TestBase
{
    [Test]
    public async Task ShouldRequireAPersonName()
    {
        await TestApp.RunAsDefaultUserAsync();

        await Should.ThrowAsync<ValidationException>(() => TestApp.SendAsync(new CreatePersonCommand()));
    }

    [Test]
    public async Task ShouldAssignABillToAPerson()
    {
        await TestApp.RunAsDefaultUserAsync();

        var personId = await TestApp.SendAsync(new CreatePersonCommand { Name = "Alex" });

        var billId = await TestApp.SendAsync(new CreateBillCommand
        {
            PersonId = personId,
            Name = "Rent",
            Amount = 1450m,
            DueDay = 1
        });

        var bill = await TestApp.FindAsync<Bill>(billId);

        bill.ShouldNotBeNull();
        bill.PersonId.ShouldBe(personId);
        bill.Amount.ShouldBe(1450m);

        var dashboard = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));

        dashboard.BillsPayable.ShouldBe(1450m);
        dashboard.Bills.Single().PersonName.ShouldBe("Alex");
    }

    [Test]
    public async Task ShouldSubtractQuickAddSpendFromTheCategoryBudget()
    {
        await TestApp.RunAsDefaultUserAsync();

        var personId = await TestApp.SendAsync(new CreatePersonCommand { Name = "Alex" });
        var categoryId = await TestApp.SendAsync(new CreateCategoryCommand
        {
            Name = "Groceries",
            MonthlyBudget = 100m
        });

        await TestApp.SendAsync(new CreateExpenseCommand
        {
            PersonId = personId,
            CategoryId = categoryId,
            Amount = 25.50m,
            SpentOn = new DateOnly(2026, 9, 2),
            Note = "Milk"
        });

        var september = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        var category = september.Categories.Single();

        category.Spent.ShouldBe(25.50m);
        category.Remaining.ShouldBe(74.50m);
        september.TotalRemaining.ShouldBe(74.50m);
        september.SpendByPerson.Single().Name.ShouldBe("Alex");
        september.SpendByPerson.Single().Spent.ShouldBe(25.50m);
        september.Expenses.Single().Note.ShouldBe("Milk");

        var october = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 10));

        october.Categories.Single().Spent.ShouldBe(0);
        october.Categories.Single().Remaining.ShouldBe(100m);
    }

    [Test]
    public async Task ShouldAddCreditPaymentAsABillAndProjectPayoff()
    {
        await TestApp.RunAsDefaultUserAsync();

        var personId = await TestApp.SendAsync(new CreatePersonCommand { Name = "Alex" });
        var facilityId = await TestApp.SendAsync(new CreateCreditFacilityCommand
        {
            PersonId = personId,
            Name = "Visa",
            Balance = 1000m,
            AnnualInterestRate = 0m,
            MonthlyPayment = 100m,
            DueDay = 12
        });

        var dashboard = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        var facility = dashboard.CreditFacilities.Single();
        var bill = dashboard.Bills.Single();

        facility.Id.ShouldBe(facilityId);
        facility.PersonName.ShouldBe("Alex");
        facility.WillPayOff.ShouldBeTrue();
        facility.MonthsToPayoff.ShouldBe(10);
        facility.FirstMonthInterest.ShouldBe(0);
        facility.TotalInterest.ShouldBe(0);
        bill.Name.ShouldBe("Visa payment");
        bill.PersonId.ShouldBe(personId);
        bill.Amount.ShouldBe(100m);
        bill.CreditFacilityId.ShouldBe(facilityId);
        dashboard.BillsPayable.ShouldBe(100m);

        await TestApp.SendAsync(new ApplyCreditPaymentCommand(facilityId));

        var afterPayment = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));

        afterPayment.CreditFacilities.Single().Balance.ShouldBe(900m);
        afterPayment.CreditFacilities.Single().MonthsToPayoff.ShouldBe(9);

        await TestApp.SendAsync(new DeleteCreditFacilityCommand(facilityId));

        var afterDelete = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));

        afterDelete.CreditFacilities.ShouldBeEmpty();
        afterDelete.Bills.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportWhenTheSetPaymentCannotCoverInterest()
    {
        await TestApp.RunAsDefaultUserAsync();

        var personId = await TestApp.SendAsync(new CreatePersonCommand { Name = "Alex" });

        await TestApp.SendAsync(new CreateCreditFacilityCommand
        {
            PersonId = personId,
            Name = "Store card",
            Balance = 1000m,
            AnnualInterestRate = 12m,
            MonthlyPayment = 10m,
            DueDay = 5
        });

        var dashboard = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        var facility = dashboard.CreditFacilities.Single();

        facility.WillPayOff.ShouldBeFalse();
        facility.MonthsToPayoff.ShouldBeNull();
        facility.FirstMonthInterest.ShouldBe(10m);
        dashboard.Bills.Single().Amount.ShouldBe(10m);
        facility.Schedule.Count.ShouldBe(24);
        facility.Schedule.Last().Balance.ShouldBe(1000m);
    }

    [Test]
    public async Task ShouldAmortizeAHomeLoanDownToZero()
    {
        await TestApp.RunAsDefaultUserAsync();

        var personId = await TestApp.SendAsync(new CreatePersonCommand { Name = "Alex" });
        var payment = CreditPayoffCalculator.PaymentForTerm(12000m, 0m, 12);

        await TestApp.SendAsync(new CreateCreditFacilityCommand
        {
            PersonId = personId,
            Name = "Home loan",
            Kind = FacilityKind.Installment,
            TermMonths = 12,
            Balance = 12000m,
            AnnualInterestRate = 0m,
            MonthlyPayment = payment,
            DueDay = 1
        });

        var dashboard = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        var loan = dashboard.CreditFacilities.Single();

        loan.Kind.ShouldBe(FacilityKind.Installment);
        loan.Type.ShouldBe(FacilityType.OtherInstallment);
        loan.TermMonths.ShouldBe(12);
        loan.WillPayOff.ShouldBeTrue();
        loan.MonthsToPayoff.ShouldBe(12);
        loan.Schedule.Count.ShouldBe(12);
        loan.Schedule.Last().Balance.ShouldBe(0);
        dashboard.Bills.Single().Name.ShouldBe("Home loan payment");
        dashboard.Bills.Single().Amount.ShouldBe(payment);
    }

    [Test]
    public async Task ShouldStoreLoanTypeAndIncludeAdminFeesInPayoffProjectionAndPayments()
    {
        await TestApp.RunAsDefaultUserAsync();

        var personId = await TestApp.SendAsync(new CreatePersonCommand { Name = "Alex" });
        var facilityId = await TestApp.SendAsync(new CreateCreditFacilityCommand
        {
            PersonId = personId,
            Name = "Car finance",
            Type = FacilityType.CarLoan,
            TermMonths = 10,
            Balance = 100m,
            AnnualInterestRate = 0m,
            MonthlyPayment = 20m,
            MonthlyAdminFee = 5m,
            DueDay = 1
        });

        var beforePayment = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        var loan = beforePayment.CreditFacilities.Single();

        loan.Type.ShouldBe(FacilityType.CarLoan);
        loan.Kind.ShouldBe(FacilityKind.Installment);
        loan.TotalFees.ShouldBe(35m);
        loan.Schedule.First().Fee.ShouldBe(5m);
        loan.Schedule.First().Balance.ShouldBe(85m);

        await TestApp.SendAsync(new ApplyCreditPaymentCommand(facilityId));

        var afterPayment = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        afterPayment.CreditFacilities.Single().Balance.ShouldBe(85m);
    }

    [Test]
    public async Task ShouldChangeTheCurrencyWithoutAlteringAmounts()
    {
        await TestApp.RunAsDefaultUserAsync();

        var before = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        before.Currency.ShouldBe("USD");
        before.People.ShouldBeEmpty();
        before.Bills.ShouldBeEmpty();
        before.CreditFacilities.ShouldBeEmpty();

        await TestApp.SendAsync(new UpdateCurrencyCommand { CurrencyCode = "EUR" });

        var after = await TestApp.SendAsync(new GetBudgetDashboardQuery(2026, 9));
        after.Currency.ShouldBe("EUR");
        after.TotalBudget.ShouldBe(before.TotalBudget);
    }
}
