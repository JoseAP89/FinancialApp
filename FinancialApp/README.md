FinancialApp
============

Purpose
-------
FinancialApp is a small demo web app (Blazor/.NET MAUI based workspace) that demonstrates a simple financial calculator for computing the future value of an annuity. It lets a user enter an initial amount (present value), a periodic deposit, an annual interest rate and a period (in months), and shows the computed future value using the formula:

FV = PV * ((1 + r) ** n) + P * (((1 + r) ** n - 1) / r)

Where:
- `PV` is the initial amount (present value)
- `P` is the periodic deposit
- `r` is the interest rate per period (monthly rate = annualRate / 12)
- `n` is the number of periods (months)

Project structure (key files)
-----------------------------
- `Components/Pages/Home.razor` - UI for the calculator (inputs and result)
- `Components/Pages/Home.razor.cs` - form model (`LoanModel`), `EditContext` and calculation logic
- `Components/Pages/Home.razor.css` - local styles for the page

How to run
----------
1. Open the solution in Microsoft Visual Studio (2026) that targets .NET 10.
2. Set the appropriate startup project (the Blazor/MAUI host) and run (F5) or use the debugger.
3. Navigate to the root page (`/`) to use the calculator.

Notes
-----
- The app uses Blazor `EditForm` with an `EditContext` and data annotations for validation; this provides behavior similar to reactive forms.
- The `Period` input is expressed in months.
- If you want a different convention for deposit timing (beginning vs end of period) or additional features (export, charts, different compounding), open an issue or extend the `Home.razor.cs` logic.

License
-------
This repository contains example/demo code. Adapt and reuse as needed.
