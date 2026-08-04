Entity Framework Core (SQLite) setup for FinancialApp

Quick notes and commands to manage migrations locally:

1. Install the dotnet-ef tool (if not already installed):

   dotnet tool install --global dotnet-ef

2. From the repository root, run migrations against the MAUI project using the design-time factory:

   cd FinancialApp
   dotnet ef migrations add InitialCreate --project . --startup-project .
   dotnet ef database update --project . --startup-project .

The DesignTimeDbContextFactory in FinancialApp/Data/ allows the EF tools to create the DbContext at design time and produce migration files.

Note: This project uses manual migrations only. The app does NOT call Database.Migrate() at startup. Run the EF commands above to create and apply migrations locally. The DesignTimeDbContextFactory is used by the tools to instantiate the DbContext.
