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

## The Core Architecture

# 🏛️ The Modern Accounting Database Blueprint

Welcome to your financial data headquarters! 🚀 This guide walks you through a clean, theory-driven approach to structuring your financial database. By treating your bank accounts and spending categories as equals, we build a system that's both powerful and beautifully simple.

---

## Table of Contents
- [The Core Architectural Shift](#1-core-architectural-shift)
- [Complete Financial ERD](#2-complete-financial-erd)
- [Comprehensive Table Structures](#3-comprehensive-table-structures)
- [Anticipating Your Reports](#4-anticipating-your-reports-how-data-maps)
- [Practical Ledger Examples](#5-practical-ledger-examples)
- [The Foundational Five](#the-foundational-five)

---

## 1. Core Architectural Shift

Forget the old way of having separate tables for **Accounts** and **Categories**. In our world, everything lives together in one master directory: **The Chart of Accounts (COA)**.

- Your **Bank Accounts** (Checking, Credit Card) and your **Spending Categories** (Groceries, Gas, Salary) are structurally identical.
- They are simply distinguished by their high-level **Accounting Type** (e.g., `ASSET`, `EXPENSE`).

This unification makes your database incredibly flexible and easier to manage.

---

## 2. Complete Financial ERD

Here is the big picture of how everything connects. It's simpler than you think!
+--------------------------------------------------+
| CHART_OF_ACCOUNTS |
+--------------------------------------------------+
| id (PK) |
| name (e.g., "Checking", "Groceries", "Salary") |
| class (ASSET, LIABILITY, EQUITY, INCOME, EXPENSE)|
| parent_id (FK -> CHART_OF_ACCOUNTS.id) |
+------------------------+-------------------------+
|
| 1
|
| Many
+------------------------+-------------------------+
| TRANSACTION_LINES |
+--------------------------------------------------+
| id (PK) |
| transaction_id (FK -> TRANSACTIONS.id) |
| account_id (FK -> CHART_OF_ACCOUNTS.id) |
| amount (NUMERIC) |
+------------------------+-------------------------+
|
| Many
|
| 1
+------------------------+-------------------------+
| TRANSACTIONS |
+--------------------------------------------------+
| id (PK) |
| date (DATE) |
| description (TEXT) |
+--------------------------------------------------+


---

## 3. Comprehensive Table Structures

Let's break down exactly what lives inside each table.

### Chart of Accounts Table
This is the master list. It contains *everything*—your assets, liabilities, income, expenses, and all the nested subcategories.

| Column Name | Data Type | Description |
| :--- | :--- | :--- |
| `id (PK)` | `UUID / INT` | Unique identifier for the account. |
| `name` | `VARCHAR` | A friendly name (e.g., "Chase Checking", "Food & Dining"). |
| `class` | `ENUM` | The root type: `ASSET`, `LIABILITY`, `EQUITY`, `INCOME`, `EXPENSE`. |
| `parent_id (FK)` | `UUID / INT` | References `id` in this table. `NULL` for top-level classes. Allows for an infinite nesting of subcategories. |

### Transactions Table
Think of this as the "header" or receipt for a financial event. It tells us *when* something happened.

| Column Name | Data Type | Description |
| :--- | :--- | :--- |
| `id (PK)` | `UUID / INT` | Unique identifier for the transaction. |
| `date` | `DATE` | The exact day the transaction occurred. |
| `description` | `TEXT` | A memo or note (e.g., "Weekly grocery run at Walmart"). |

### Transaction Lines Table (The Ledger Entries)
This is where the magic of double-entry accounting happens. Each transaction must have **at least two lines**.

- **The Golden Rule:** The `amount` for all lines in a single `transaction_id` must **always sum to exactly $0.00** (Debits = Credits).
- **Direction:** Positive numbers mean money is *entering* an account. Negative numbers mean money is *leaving*.

| Column Name | Data Type | Description |
| :--- | :--- | :--- |
| `id (PK)` | `UUID / INT` | Unique identifier for the line item. |
| `transaction_id (FK)` | `UUID / INT` | Links back to the parent `TRANSACTIONS` header. |
| `account_id (FK)` | `UUID / INT` | Links to the specific Account/Category in the `CHART_OF_ACCOUNTS`. |
| `amount` | `NUMERIC` | The signed financial amount (e.g., `150.00` or `-150.00`). |

---

## 4. Anticipating Your Reports (How Data Maps)

With this structure, generating financial statements is as easy as filtering by the `class` column. Your data is ready for analysis!

### Balance Sheet (Balance General)
- **Snapshot:** A picture of your financial health "As of Today."
- **Filter:** `WHERE class IN ('ASSET', 'LIABILITY', 'EQUITY')`.
    - **Assets:** Checking, Savings, Investments, Property.
    - **Liabilities:** Credit Card Balances, Student Loans, Mortgages.
    - **Equity:** Your calculated Net Worth (Assets - Liabilities).

### Income Statement (Estado de Resultados)
- **Performance:** Tracks your financial activity over a period (e.g., "January 1 to January 31").
- **Filter:** `WHERE class IN ('INCOME', 'EXPENSE')`.
    - **Income:** Salary, Side hustle revenue, Dividends.
    - **Expenses:** Your nested categories (Groceries, Auto, Gas).
    - **Net Income:** Income minus Expenses. This is the exact amount you saved during that timeframe!

---

## 5. Practical Ledger Examples

Here is how everyday money moves route through this schema to ensure your books always balance.

### Buying Gas ($50)
- **Line 1:** `account_id` = Checking (`ASSET`), `amount` = `-50.00`
- **Line 2:** `account_id` = Gas (`EXPENSE`), `amount` = `50.00`
- **Result:** Assets drop by $50, Expenses increase by $50.

### Moving Money to Savings ($200)
- **Line 1:** `account_id` = Checking (`ASSET`), `amount` = `-200.00`
- **Line 2:** `account_id` = Savings (`ASSET`), `amount` = `200.00`
- **Result:** Total Assets remain completely unchanged. The Income Statement is never touched.

### Receiving Paycheck ($3,000)
- **Line 1:** `account_id` = Checking (`ASSET`), `amount` = `3000.00`
- **Line 2:** `account_id` = Salary (`INCOME`), `amount` = `-3000.00`
- **Result:** Assets increase by $3,000, Income increases by $3,000.

---

## The Foundational Five

All of this is built on the core framework of financial accounting. These five categories are the universal building blocks.

```
                  ┌───────────────────────────────┐
                  │ Elements of Financial State.  │
                  └───────────────┬───────────────┘
                                  │
         ┌────────────────────────┴────────────────────────┐
         │                                                 │
         ▼                                                 ▼
┌─────────────────┐                               ┌─────────────────┐
│  Balance Sheet  │                               │Income Statement │
└────────┬────────┘                               └────────┬────────┘
         │                                                 │
         ├─► Asset                                         ├─► Revenue
         ├─► Liability                                     └─► Expense
         └─► Equity
```


### Balance Sheet Elements (Financial Position)

**Assets:** Economic resources owned or controlled by a business that will provide future economic benefits.

**Liabilities:** Present obligations of a business arising from past events, requiring a future outflow of resources.

**Equity:** The residual interest in the assets of the business after deducting all of its liabilities.

### Income Statement Elements (Financial Performance)

**Revenue (Income):** Increases in economic benefits during the accounting period, resulting in an increase in equity (other than contributions from equity participants).

**Expenses:** Decreases in economic benefits during the accounting period, resulting in a decrease in equity (other than distributions to equity participants).

### Minor Structural Exceptions

Depending on the accounting standard used, such as IFRS or US GAAP, standard setters occasionally highlight a few specific subsets of the main five elements:

- **Gains and Losses:** US GAAP lists "Gains" and "Losses" as distinct elements. However, they are conceptually identical to Revenue and Expenses, representing peripheral or incidental transactions rather than core operations.
- **Investments by / Distributions to Owners:** These are often separated out into their own categories to track equity changes, but they fundamentally remain sub-components of the Equity parent element.



Notes
-----
- The app uses Blazor `EditForm` with an `EditContext` and data annotations for validation; this provides behavior similar to reactive forms.
- The `Period` input is expressed in months.
- If you want a different convention for deposit timing (beginning vs end of period) or additional features (export, charts, different compounding), open an issue or extend the `Home.razor.cs` logic.

License
-------
This repository contains example/demo code. Adapt and reuse as needed.
