# Budge

The project was generated using the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) version 10.8.0.

## Build

Run `dotnet build` to build the solution.

## Run

To run the application:

```bash
dotnet run --project .\src\AppHost
```

The Aspire dashboard will open automatically, showing the application URLs and logs.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

Create a new query:

```
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.8.0
```

## Test

The solution contains unit, integration, and functional tests.

To run the backend tests:

```bash
dotnet test
```

The React client tests live in `src/Web/ClientApp`:

```bash
cd src/Web/ClientApp
npm install
npm test
```

## Household ledger

Sign in and the home page becomes the ledger. It starts empty.

- Choose the currency in the ledger header. Amounts stay as entered; the currency changes how they are shown.
- Add people, then assign bills to the person who pays them.
- Add a credit card or other facility. Its set payment is added to that person's bills. The ledger shows this month's interest and how long the balance takes to pay off at that payment. Apply the set payment to charge interest and reduce the balance.
- Quick add a spend against a person and a category. That amount is subtracted from the category budget left for the month. Spending is also totaled by person.

Development creates a local sign-in account. It does not create people, bills, or spending.

## Database

Copy `src/Web/appsettings.Local.json.example` to `src/Web/appsettings.Local.json` and enter `Database:Username` and `Database:Password` there. That file is gitignored. The committed settings stay on SQLite and do not contain a password.

`Database:Provider` can be `Sqlite`, `MySql`, `PostgreSQL`, or `SqlServer`. The local file is set up for PostgreSQL on `localhost`. Migrations run when the API starts. Each provider has its own migration project under `src/Infrastructure.Migrations.*`.

```bash
dotnet run --project src/Web --no-launch-profile --urls http://localhost:5270
```

## Help
To learn more about the template go to the [project website](https://cleanarchitecture.jasontaylor.dev). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.
