# Subsidy & Grievance Portal

A single-window e-governance web application modelled on a **District Industries
Centre (DIC) / industrial promotion office**. Entrepreneurs apply for government
schemes and raise grievances; officers review and act on them; admins manage
schemes and view reports.

Built with **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core**,
**ASP.NET Core Identity** and **Bootstrap 5**. It runs out of the box on
**SQLite** for local development and switches to **PostgreSQL** for production
with a single configuration change.

---

## 1. What the app does

| Area | Description |
|------|-------------|
| **Scheme catalog** | Public list of active government schemes. Admins create / edit / activate / deactivate schemes. |
| **Entrepreneur profile** | An applicant fills in their business details once; they are reused on every application. |
| **Scheme application** | An applicant applies to a scheme and uploads a supporting **PDF** (validated for type and size; stored outside `wwwroot`). |
| **Application workflow** | `Draft → Submitted → Under Review → Approved / Rejected`. Officers change status and add remarks; applicants see live status. |
| **Grievances** | An applicant files a grievance and receives a unique **ticket number**. Officers assign and resolve; status is trackable by ticket (no login needed to track). |
| **Officer dashboard** | Pending applications and open grievances to act on. |
| **Admin dashboard** | User management (role assignment) + reports (counts by status) with **CSV export**. |
| **Audit trail** | Every status change is logged (who, what, when) and shown on the application / grievance detail pages. |

### User roles

| Role | Can do |
|------|--------|
| **Applicant** (self-registered) | Manage business profile, apply to schemes, upload documents, track applications, file & track grievances. |
| **Officer** | Review pending applications, change status with remarks, assign & resolve grievances. |
| **Admin** | Everything an officer can, plus manage schemes, assign roles to users, and view/export reports. |

Security highlights: role-based authorization on **every** controller action,
anti-forgery protection on **every** POST (global filter), server-side validation
on all forms, Identity's default password hashing, and uploads stored in a
protected folder that is never served statically.

---

## 2. Prerequisites

- **.NET 10 SDK** — check with:
  ```bash
  dotnet --version   # should print 10.x
  ```
- macOS (Monterey or later), Linux, or Windows. Nothing Windows-only is used.

---

## 3. Run locally on macOS (SQLite, zero setup)

```bash
cd GrievancePortal
dotnet restore
dotnet run
```

Then open the URL printed in the console (by default **http://localhost:5080**).

On first run the app automatically:
1. Creates the SQLite database file `grievanceportal.db` in the project folder.
2. Applies the included EF Core migration.
3. Seeds the roles, a sample set of schemes, and the **admin** account, plus
   demo **officer** and **applicant** accounts for quick testing.

### Development logins (seeded on first run)

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@adiseelan.tech` | `Admin@12345` |
| Officer | `officer@adiseelan.tech` | `Officer@12345` |
| Applicant | `applicant@adiseelan.tech` | `Applicant@12345` |

> ⚠️ **Change / disable these before any real deployment.** The admin credentials
> come from configuration (`SeedAdmin:Email` / `SeedAdmin:Password`) and should be
> overridden with environment variables in production (see §6 and §8). The demo
> officer and applicant accounts are seeded only when `SeedDemoUsers` is `true`
> (the default) — set it to `false` in `appsettings.Production.json` to skip them.

### Try the full flow

1. Sign in as the **applicant** above (or **register** a new account — new sign-ups
   are always applicants).
2. Fill in **My profile**.
3. Open **Schemes → Apply**, attach a PDF, and **Submit**.
4. Sign out, sign in as **officer** (or **admin**), open the **Officer desk**.
5. As a staff member, open the **Officer desk**, review the application, and move
   it to *Under Review* / *Approved* / *Rejected* with a remark.
6. File a **grievance** as the applicant and **Track** it by ticket number.

To stop the app, press `Ctrl+C`. To start fresh, delete `grievanceportal.db` and run again.

---

## 4. Project structure

```
GrievancePortal/
├── GrievancePortal.csproj              # net10.0, EF Core (Sqlite + Npgsql) + Identity
├── Program.cs                     # DI, provider switch, Identity, seeding, pipeline
├── appsettings.json               # base config (SQLite, dev defaults)
├── appsettings.Development.json   # dev logging
├── appsettings.Production.json    # production template (PostgreSQL)
├── Properties/launchSettings.json
├── Data/
│   ├── ApplicationDbContext.cs    # IdentityDbContext + domain sets & relationships
│   └── SeedData.cs                # migrate + roles + admin + sample schemes
├── Models/
│   ├── ApplicationUser.cs, Scheme.cs, EntrepreneurProfile.cs,
│   ├── SchemeApplication.cs, Grievance.cs, AuditLog.cs,
│   ├── ApplicationStatus.cs, GrievanceStatus.cs, Roles.cs
│   └── ViewModels/                # strongly-typed form/VM classes
├── Services/
│   ├── FileStorageService.cs      # PDF/size validation, protected storage, safe read
│   ├── AuditService.cs            # status-change logging
│   ├── EmailSender.cs             # NoOp (default) + SMTP (optional)
│   └── Settings.cs                # UploadSettings, EmailSettings
├── Controllers/                   # Home, Account, Schemes, Profile, Applications,
│                                  # Grievances, Officer, Admin
├── Views/                         # Razor views (Bootstrap 5), role-aware layout
├── Migrations/                    # EF Core InitialCreate (SQLite) + snapshot
├── wwwroot/                       # site.css, site.js (Bootstrap via CDN)
└── App_Data/uploads/              # uploaded PDFs (NOT served statically)
```

---

## 5. Migrations

The repo ships with an **InitialCreate** migration generated for **SQLite**, so
`dotnet run` works immediately and the schema is applied automatically at startup
(`db.Database.Migrate()` in `SeedData`).

Install the EF tools once if you don't have them:

```bash
dotnet tool install --global dotnet-ef
# then ensure ~/.dotnet/tools is on your PATH
```

Create and apply a new migration after changing the models:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update          # or just run the app, which auto-applies
```

> **Note — EF migrations are provider-specific.** The included migration targets
> SQLite. When you deploy to PostgreSQL you regenerate the migration for Npgsql
> (see §7). Pick the provider you deploy with and keep that provider's migration
> with the deployed build.

---

## 6. Configuration reference

All settings live in `appsettings*.json` and can be overridden by environment
variables. For nested keys, use a **double underscore** `__` in the variable name.

| Setting | Meaning |
|---------|---------|
| `DatabaseProvider` | `Sqlite` (dev) or `Postgres` (prod). |
| `ConnectionStrings:DefaultConnection` | DB connection string. |
| `SeedAdmin:Email` / `SeedAdmin:Password` / `SeedAdmin:FullName` | Seeded admin account. |
| `Uploads:MaxFileSizeMB` | Max upload size (default 5). |
| `Uploads:AllowedExtensions` | Allowed extensions (default `[".pdf"]`). |
| `Uploads:StorageFolder` | Protected storage folder (default `App_Data/uploads`). |
| `Email:Enabled` | `false` by default. When `true`, an SMTP sender is used. |
| `Email:SmtpHost` / `SmtpPort` / `UseSsl` / `UserName` / `Password` / `FromAddress` / `FromName` | SMTP settings. |

**Never commit real secrets.** In production, pass them as environment variables, e.g.:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=grievanceportal;Username=grievanceuser;Password=********"
export SeedAdmin__Password="a-strong-admin-password"
export Email__Password="smtp-app-password"
```

---

## 7. Switch to PostgreSQL for production

Switching provider is a **single configuration change** — the code already
supports both via `Program.cs`.

1. **Set the provider and connection string** (in `appsettings.Production.json`
   or, preferably, environment variables):
   ```
   DatabaseProvider = Postgres
   ConnectionStrings:DefaultConnection = Host=localhost;Port=5432;Database=grievanceportal;Username=grievanceuser;Password=********
   ```
   The `Npgsql.EntityFrameworkCore.PostgreSQL` package is already referenced, so no
   code changes are needed.

2. **Generate the PostgreSQL migration once** (because migrations are
   provider-specific). With the Postgres settings active:
   ```bash
   # from the project folder, pointing at your Postgres database
   export DatabaseProvider=Postgres
   export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=grievanceportal;Username=grievanceuser;Password=********"

   rm -rf Migrations                       # remove the SQLite-specific migration
   dotnet ef migrations add InitialCreate  # regenerates it for Npgsql
   ```
   Commit this regenerated migration with your production build. The application
   applies it automatically on first start.

   > Keeping **both** SQLite (dev) and Postgres (prod) migrations in one project is
   > not supported by EF out of the box — the simplest approach is to use the
   > provider you deploy with. If you want SQLite locally **and** Postgres in
   > production, regenerate the migration for whichever provider you're currently
   > targeting, or keep provider-specific migrations on separate branches.

---

## 8. Deploy to a Hostinger Ubuntu VPS (Kestrel + Nginx + systemd + SSL)

Target: `adiseelan.tech`, Kestrel behind Nginx, running as a **systemd** service,
with a free **Let's Encrypt** certificate. Commands assume Ubuntu 22.04 / 24.04.

### 8.1 Point DNS at the server
In your DNS provider, create **A records** for the VPS public IP:

```
@     A   <VPS_PUBLIC_IP>
www   A   <VPS_PUBLIC_IP>
```

### 8.2 Install the ASP.NET Core 10 runtime
```bash
# Ubuntu 24.04 (use .../ubuntu/22.04/... on 22.04)
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0
```
(The app is published **framework-dependent**, so only the runtime is needed on the server.)

### 8.3 Install PostgreSQL and create the database
```bash
sudo apt-get install -y postgresql
sudo -u postgres psql <<'SQL'
CREATE DATABASE grievanceportal;
CREATE USER grievanceuser WITH PASSWORD 'CHANGE_ME_STRONG';
GRANT ALL PRIVILEGES ON DATABASE grievanceportal TO grievanceuser;
SQL
# PostgreSQL 15+: also grant rights on the public schema
sudo -u postgres psql -d grievanceportal -c "GRANT ALL ON SCHEMA public TO grievanceuser;"
```

### 8.4 Publish on your Mac and copy to the server
First regenerate the migration for Postgres (see §7), then:
```bash
# on your Mac, in the project folder
dotnet publish -c Release -o publish

# copy to the server (create the folder first: sudo mkdir -p /var/www/grievanceportal)
rsync -avz --delete publish/ youruser@<VPS_PUBLIC_IP>:/var/www/grievanceportal/
```

On the server, make the app folder writable by the service account (uploads +
SQLite, if ever used, live under it):
```bash
sudo chown -R www-data:www-data /var/www/grievanceportal
sudo mkdir -p /var/www/grievanceportal/App_Data/uploads
sudo chown -R www-data:www-data /var/www/grievanceportal/App_Data
```

### 8.5 Store production secrets (environment file)
```bash
sudo mkdir -p /etc/grievanceportal
sudo tee /etc/grievanceportal/grievanceportal.env >/dev/null <<'ENV'
ASPNETCORE_ENVIRONMENT=Production
DatabaseProvider=Postgres
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=grievanceportal;Username=grievanceuser;Password=CHANGE_ME_STRONG
SeedAdmin__Email=admin@adiseelan.tech
SeedAdmin__Password=CHANGE_ME_STRONG_ADMIN
# Optional outbound email:
# Email__Enabled=true
# Email__SmtpHost=smtp.your-provider.com
# Email__SmtpPort=587
# Email__UseSsl=true
# Email__UserName=postmaster@adiseelan.tech
# Email__Password=your-smtp-password
# Email__FromAddress=no-reply@adiseelan.tech
ENV
sudo chmod 600 /etc/grievanceportal/grievanceportal.env
sudo chown root:root /etc/grievanceportal/grievanceportal.env
```

### 8.6 Create the systemd service (Kestrel)
```bash
sudo tee /etc/systemd/system/grievanceportal.service >/dev/null <<'UNIT'
[Unit]
Description=Subsidy & Grievance Portal
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/grievanceportal
ExecStart=/usr/bin/dotnet /var/www/grievanceportal/GrievancePortal.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=grievanceportal
User=www-data
EnvironmentFile=/etc/grievanceportal/grievanceportal.env
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_NOLOGO=1

[Install]
WantedBy=multi-user.target
UNIT

sudo systemctl daemon-reload
sudo systemctl enable --now grievanceportal
sudo systemctl status grievanceportal        # should be active (running)
```
The app listens only on `127.0.0.1:5000` and applies migrations + seeds on start.
Check logs with: `journalctl -u grievanceportal -f`.

### 8.7 Configure Nginx as a reverse proxy
```bash
sudo apt-get install -y nginx
sudo tee /etc/nginx/sites-available/grievanceportal >/dev/null <<'NGINX'
server {
    listen 80;
    server_name adiseelan.tech www.adiseelan.tech;

    # Allow PDF uploads (app limit is 5 MB; leave headroom).
    client_max_body_size 10M;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
NGINX

sudo ln -s /etc/nginx/sites-available/grievanceportal /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```
The app reads `X-Forwarded-Proto` (via forwarded-headers middleware in
`Program.cs`), so it knows requests are HTTPS once SSL is enabled.

### 8.8 Enable HTTPS with Let's Encrypt
```bash
sudo apt-get install -y certbot python3-certbot-nginx
sudo certbot --nginx -d adiseelan.tech -d www.adiseelan.tech
```
Certbot adds the `443` server block and an HTTP→HTTPS redirect, and sets up
automatic renewal. Verify renewal with `sudo certbot renew --dry-run`.

### 8.9 Firewall
```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

Your portal is now live at **https://adiseelan.tech**. Sign in with the admin
credentials from your environment file and change the password immediately.

### Updating later
```bash
# on your Mac
dotnet publish -c Release -o publish
rsync -avz --delete publish/ youruser@<VPS_PUBLIC_IP>:/var/www/grievanceportal/
# on the server
sudo chown -R www-data:www-data /var/www/grievanceportal
sudo systemctl restart grievanceportal
```

---

## 9. Security checklist before going live
- [ ] Change `SeedAdmin__Password` (and ideally the admin email) via the env file.
- [ ] Use a strong PostgreSQL password; don't expose Postgres to the internet.
- [ ] Keep `/etc/grievanceportal/grievanceportal.env` at `chmod 600`, owned by root.
- [ ] Confirm HTTPS works and HTTP redirects to HTTPS (certbot does this).
- [ ] Confirm uploaded PDFs are only reachable through the authorized
      `/Applications/Document/{id}` action, never by a direct URL.
- [ ] Back up the PostgreSQL database and the `App_Data/uploads` folder.

## 10. Troubleshooting
- **502 Bad Gateway** → the app isn't running or isn't on `127.0.0.1:5000`.
  Check `sudo systemctl status grievanceportal` and `journalctl -u grievanceportal -e`.
- **Can't log in after deploy** → the admin is seeded from `SeedAdmin__*`; verify
  the env file values and restart the service.
- **413 Request Entity Too Large** → raise `client_max_body_size` in Nginx.
- **DB errors on first start (Postgres)** → make sure you regenerated the migration
  for Npgsql (§7) before publishing, and that the connection string is correct.
