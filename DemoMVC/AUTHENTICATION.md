# Authentication and authorization setup

The application uses ASP.NET Core Identity in the existing SQLite `ApplicationDbContext`.
The `Admin` and `User` roles are created at startup. Student and catalog management endpoints
require the `Admin` role; the Home and lesson pages remain public. Self-registration always
assigns only the `User` role.

Identity accounts are separate from student records. The current domain has no student-to-account
relationship, so `User` accounts do not receive access to student data. Add a student-specific
role only together with an explicit, validated account-to-student mapping.

## Configure the initial administrator

Set all three values in User Secrets for local development:

```powershell
dotnet user-secrets set "AdminSeed:Username" "admin" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "AdminSeed:Email" "admin@example.com" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "AdminSeed:Password" "<choose-a-unique-strong-password>" --project DemoMVC\DemoMVC.csproj
```

In deployment, configure `AdminSeed__Username`, `AdminSeed__Email`, and
`AdminSeed__Password` as environment variables or through a secret manager. Do not put
the password in source control. If no AdminSeed values are configured, no administrator
account is created; the two roles are still initialized.

## Configure SMTP

For Gmail, use `smtp.gmail.com`, port `587`, and STARTTLS (`EnableSsl=true` with the
project's `SmtpClient`). `UserName` and `SenderEmail` must be the same Gmail address.
`Password` must be a Google App Password (not the Gmail sign-in password); Google
requires 2-Step Verification to create an App Password. Do not commit the App Password.

Set SMTP values in User Secrets (enter the App Password without display spaces):

```powershell
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "Smtp:Port" "587" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "Smtp:UserName" "your-gmail-address@gmail.com" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "Smtp:Password" "<your-google-app-password>" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "Smtp:SenderEmail" "your-gmail-address@gmail.com" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "Smtp:SenderName" "PTPMQL Hải Anh" --project DemoMVC\DemoMVC.csproj
dotnet user-secrets set "Smtp:EnableSsl" "true" --project DemoMVC\DemoMVC.csproj
```

To ensure emailed links use the real public host (especially behind a reverse proxy), set
the app origin, including a non-default port if used:

```powershell
dotnet user-secrets set "Application:PublicBaseUrl" "https://your-domain.example" --project DemoMVC\DemoMVC.csproj
```

In production, use environment variables such as `Smtp__Host`, `Smtp__Port`,
`Smtp__UserName`, `Smtp__Password`, `Smtp__SenderEmail`, and `Application__PublicBaseUrl`.
The app only reports an email as sent after SMTP completes successfully; this means the
SMTP server accepted it, not that Gmail guarantees inbox delivery. Delivery failures are
logged server-side without logging SMTP credentials or tokens. Email confirmation and
password reset tokens expire after two hours.

## Database

`AddIdentity` adds only Identity tables to the existing SQLite database. The application
applies pending EF migrations at startup; to apply them explicitly before deployment:

```powershell
dotnet ef database update --project DemoMVC\DemoMVC.csproj
```

Back up the SQLite database before deploying schema changes. The migration does not
alter or remove business tables or their relationships.
