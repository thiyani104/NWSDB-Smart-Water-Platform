# NWSDB Smart Water Platform

A service-oriented REST API (plus a lightweight web portal) for a national water utility. It covers the whole billing lifecycle: field staff record meter readings, bills are generated automatically from a configurable tariff, customers pay online, partner banks settle bills through a B2B gateway, and every important action lands in an audit log.

**Stack:** ASP.NET Core 9 · Entity Framework Core 9 · SQL Server · JWT authentication · BCrypt · Swagger/OpenAPI · xUnit · Docker · GitHub Actions · Tailwind CSS + Chart.js (front end)

## Features

| Area | What it does |
|---|---|
| **Authentication** | Registration and login with BCrypt-hashed passwords and signed JWTs. Login accepts username or email. Auth endpoints are rate limited. |
| **Role-based access** | Four roles: Customer, Staff, Admin, Partner. Roles are assigned by the server only; public sign-up always creates a Customer. |
| **Metering and billing** | Staff record readings; the API rejects negative or decreasing values, computes consumption and generates a bill (fixed charge + units x rate). Tariffs live in the `SystemConfiguration` table. |
| **Payments** | Online settlement with business rules (no zero/negative amounts, no double payment, exact outstanding balance). Each payment produces a numbered receipt. |
| **Partner gateway** | Banks query pending bills and settle them with an `X-API-KEY` header (constant-time key comparison). |
| **Administration** | Staff provisioning, role assignment, activity (audit) log, revenue summary, tariff view, simulated backup/restore. |
| **Customer self-service** | Profile management, usage history, payment history, feedback to staff. |

## Architecture

```mermaid
flowchart LR
    UI[Web portal / Swagger / Partner banks] -->|HTTPS + JWT or API key| API
    subgraph API[ASP.NET Core API]
        C[Controllers<br/>Auth - Customer - Billing - Payment - Staff - Admin - Partner]
        S[Services<br/>Auth - Billing - Payment - Tariff - AccountAccess]
        C --> S
    end
    S --> DB[(SQL Server<br/>EF Core)]
