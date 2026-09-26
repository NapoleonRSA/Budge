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
    }
}
