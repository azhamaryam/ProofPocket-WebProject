# ProofPocket – Deliverable 2 (ASP.NET Core MVC + Identity)

Warranty tracker. The D1 static pages are now dynamic MVC views backed by SQL Server and ASP.NET Core Identity.

## Requirements
- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio) – or change the connection string in `appsettings.json`

## Run
```
dotnet restore
dotnet run
```
Open the URL printed in the console. On first start the app creates the database `ProofPocketDb`,
the roles (`Admin`, `User`), the admin account and four starter categories.

### Default admin
| Email | Password |
|-------|----------|
| admin@proofpocket.com | Admin@123 |

(Change it under `SeedAdmin` in `appsettings.json` before the first run.)

## What is included
| Area | Pages |
|------|-------|
| Public | Home |
| Identity (`AccountController`) | Login, Register, Logout, Forgot Password (+ confirmation), Reset Password (+ confirmation), Change Password, Access Denied, Lockout |
| User (`DashboardController`, role `User`) | Dashboard (live stats, expiring soon, my products, add product), Edit product, Delete product |
| Admin (`AdminController`, role `Admin`) | Dashboard (live stats), registered users + delete, categories add / edit / delete |

## Notes
- A warranty is **Expiring Soon** when it ends within 30 days (`Product.ExpiringSoonDays`).
  **Active Warranties** counts every warranty that has not expired.
- Forgot Password: no email service is configured, so in Development the reset link is shown on the
  confirmation page. For production, send the link by email in `AccountController.ForgotPassword`.
- The database uses EF Core migrations. In Visual Studio open Tools > NuGet Package Manager >
  Package Manager Console and run `Add-Migration InitialCreate`, then `Update-Database`, then press F5.
- Backgrounds in `wwwroot/css/style.css` are a little darker than D1 (lavender and yellow tones).
