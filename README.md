<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Azure-SQL-0078D4?style=for-the-badge&logo=microsoftazure&logoColor=white" alt="Azure SQL" />
  <img src="https://img.shields.io/badge/Redis-Upstash-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
  <img src="https://img.shields.io/badge/Firebase-FCM-FFCA28?style=for-the-badge&logo=firebase&logoColor=black" alt="Firebase" />
  <img src="https://img.shields.io/badge/Paymob-Payment-00C853?style=for-the-badge&logo=stripe&logoColor=white" alt="Paymob" />
</p>

<h1 align="center">Voltyks EV Charging API</h1>

<p align="center">
  <strong>A comprehensive backend solution for Electric Vehicle charging services</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#api-endpoints">API Endpoints</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#deployment">Deployment</a>
</p>

---

## Overview

**Voltyks** is a full-featured ASP.NET Core Web API that connects Electric Vehicle (EV) owners with charging station providers. The platform handles the complete charging workflow — from discovering nearby chargers to processing payments and managing ratings.

### What Voltyks Offers

- **For EV Owners**: Find nearby chargers, send charging requests, track sessions, and pay securely
- **For Charger Owners**: List chargers, accept requests, earn money, and build reputation
- **For Admins**: Manage users, configure fees, handle complaints, and monitor platform activity

---

## Features

### Core Features

| Feature | Description |
|---------|-------------|
| **Charger Discovery** | Find nearby chargers using geolocation with filtering by protocol and capacity |
| **Charging Requests** | Real-time request system with accept/reject workflow |
| **Payment Processing** | Integrated Paymob for cards and mobile wallets (Vodafone Cash, etc.) |
| **Push Notifications** | Firebase Cloud Messaging for instant updates |
| **Rating System** | Two-way ratings for both vehicle owners and charger providers |
| **Wallet System** | Built-in wallet for fee management and transactions |

### Authentication & Security

- **JWT Authentication** with refresh token rotation
- **OAuth Integration** — Google & Facebook sign-in
- **Phone Verification** — OTP via multiple SMS providers
- **Email Change** — Secure 3-step verification flow
- **Role-Based Access** — User and Admin roles
- **Rate Limiting** — Protection against abuse

### Payment Integration (Paymob)

- Credit/Debit card payments
- Mobile wallet payments (Vodafone Cash, Orange Cash, Etisalat Cash)
- Card tokenization — save cards for quick checkout
- Webhook handling with HMAC verification
- Order status tracking and refunds

### Admin Dashboard

- User management with ban/unban controls
- Fee configuration (minimum + percentage)
- Terms & conditions versioning (multi-language)
- Complaint management system
- Platform analytics and monitoring

---

## Architecture

Voltyks follows **Clean Architecture** principles with clear separation of concerns:

```
Voltyks.API/
├── Voltyks.API/                    # Presentation Layer
│   ├── Controllers/                # API Controllers
│   ├── Middleware/                 # Custom Middleware
│   └── Extensions/                 # Service Registration
│
├── Voltyks.Application/            # Business Logic Layer
│   ├── Services/                   # Core Services
│   └── Interfaces/                 # Service Contracts
│
├── Voltyks.Core/                   # Domain Layer
│   ├── DTOs/                       # Data Transfer Objects
│   ├── Enums/                      # Enumerations
│   └── Exceptions/                 # Custom Exceptions
│
├── Voltyks.Persistence/            # Data Access Layer
│   ├── Entities/                   # Database Entities
│   ├── Configurations/             # EF Core Configurations
│   └── Migrations/                 # Database Migrations
│
├── Voltyks.Infrastructure/         # Infrastructure Layer
│   ├── Repositories/               # Repository Pattern
│   └── UnitOfWork/                 # Unit of Work
│
└── Voltyks.AdminControlDashboard/  # Admin Services
    └── Services/                   # Admin-specific Services
```

### Design Patterns

- **Repository Pattern** with Generic Repository
- **Unit of Work** for transaction management
- **Service Manager** for service orchestration
- **Dependency Injection** throughout the application
- **Options Pattern** for configuration

---

## Technology Stack

| Category | Technology |
|----------|------------|
| **Framework** | ASP.NET Core 8.0 |
| **Database** | Azure SQL Server |
| **ORM** | Entity Framework Core 8 |
| **Caching** | Redis (Upstash) |
| **Authentication** | JWT + ASP.NET Identity |
| **Payments** | Paymob API |
| **SMS Providers** | Twilio, SmsEgypt, BeOn |
| **Push Notifications** | Firebase Cloud Messaging |
| **API Documentation** | Swagger / OpenAPI |
| **Deployment** | Azure App Service (Linux) |

---

## API Endpoints

### Authentication (`/api/Auth`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/Login` | User login |
| POST | `/register` | User registration |
| POST | `/RefreshToken` | Refresh JWT token |
| POST | `/forget-password` | Request password reset |
| POST | `/reset-password` | Reset password with OTP |
| GET | `/GetProfileDetails` | Get user profile |
| PUT | `/toggle-availability` | Toggle charging availability |
| GET | `/wallet` | Get wallet balance |
| POST | `/request-email-change` | Request email change |

### Chargers (`/api/Charger`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/GetCapacity` | Get all charger capacities |
| GET | `/GetProtocol` | Get charging protocols |
| GET | `/GetPrices` | Get price list |
| POST | `/AddCharger` | Register new charger |
| GET | `/GetChargersByUser` | Get user's chargers |
| POST | `/GetNearChargers` | Find nearby chargers |
| PUT | `/ToggleStatus` | Toggle charger status |

### Charging Requests (`/api/ChargingRequest`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/sendChargingRequest` | Send charging request |
| PUT | `/AcceptRequest` | Accept request |
| PUT | `/RejectRequest` | Reject request |
| PUT | `/ConfirmRequest` | Confirm charging started |
| PUT | `/abortRequest` | Abort session |
| POST | `/GetRequestDetailsById` | Get request details |

### Payments (`/api/payment`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/intention` | Create payment intention |
| POST | `/webhook` | Paymob webhook handler |
| GET | `/GetListOfCards` | Get saved cards |
| POST | `/payWithSavedCard` | Pay with saved card |
| POST | `/setDefault_Card` | Set default card |

### Vehicles (`/api/Vehicles`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/CreateVehicle` | Add vehicle |
| PUT | `/UpdateVehicle` | Update vehicle |
| GET | `/GetVehiclesByUser` | Get user's vehicles |
| DELETE | `/DeleteVehicle` | Delete vehicle |

### Processes (`/api/processes`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/confirm-by-vehicle-owner` | Confirm charging complete |
| POST | `/submit-rating` | Submit rating |
| GET | `/my-activities` | Get activity history |

> **Full API Documentation**: Available at `/swagger` when running the application

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) or Azure SQL
- [Redis](https://redis.io/) or Upstash account
- [Firebase Project](https://firebase.google.com/) for push notifications
- [Paymob Account](https://paymob.com/) for payment processing

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/Voltyks.API.git
   cd Voltyks.API
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update configuration**

   Copy `appsettings.json` and configure your settings (see [Configuration](#configuration))

4. **Apply database migrations**
   ```bash
   cd Voltyks.API
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access Swagger UI**
   ```
   https://localhost:5001/swagger
   ```

---

## Configuration

Configure the following in `appsettings.json`:

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=VoltyksDB;User ID=admin;Password=***;",
    "Redis": "your-redis-url:6379,password=***,ssl=True"
  }
}
```

### JWT Settings
```json
{
  "JwtOptions": {
    "Issuer": "https://your-domain.com",
    "Audience": "YourAudience",
    "SecurityKey": "your-super-secret-key-min-32-chars",
    "ExpiresInMinutes": 43200
  }
}
```

### Payment (Paymob)
```json
{
  "Paymob": {
    "ENV": "test",
    "ApiKey": "your-api-key",
    "SecretKey": "your-secret-key",
    "PublicKey": "your-public-key",
    "HmacSecret": "your-hmac-secret",
    "Integration": {
      "Card": 123456,
      "Wallet": 789012
    }
  }
}
```

### Firebase
```json
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "ServiceAccountFile": "Firebase/service-account-key.json"
  }
}
```

---

## API Response Format

All endpoints return a consistent response structure:

### Success Response
```json
{
  "data": { ... },
  "message": "Operation completed successfully",
  "status": true,
  "errors": []
}
```

### Paginated Response
```json
{
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 5,
    "totalCount": 100,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": "Data fetched successfully",
  "status": true
}
```

### Error Response
```json
{
  "data": null,
  "message": "Error description",
  "status": false,
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

---

## Charging Workflow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   EV Owner      │     │    Voltyks      │     │  Charger Owner  │
│                 │     │    Platform     │     │                 │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │  1. Search Chargers   │                       │
         │──────────────────────>│                       │
         │                       │                       │
         │  2. Send Request      │                       │
         │──────────────────────>│  3. Push Notification │
         │                       │──────────────────────>│
         │                       │                       │
         │                       │  4. Accept/Reject     │
         │  5. Status Update     │<──────────────────────│
         │<──────────────────────│                       │
         │                       │                       │
         │  6. Confirm Start     │                       │
         │──────────────────────>│  7. Notify Started    │
         │                       │──────────────────────>│
         │                       │                       │
         │  8. Complete Session  │                       │
         │──────────────────────>│  9. Process Payment   │
         │                       │──────────────────────>│
         │                       │                       │
         │  10. Rate Charger     │  11. Rate User        │
         │──────────────────────>│<──────────────────────│
         │                       │                       │
```

---

## Fee Structure

The platform uses a flexible fee model:

- **Base Amount**: Set by charger owner (per kWh or per session)
- **Platform Fee**: Higher of (Minimum Fee) OR (Percentage of Base Amount)
- **Total**: Base Amount + Platform Fee

Example:
```
Base Amount: 100 EGP
Minimum Fee: 5 EGP
Percentage: 10%

Calculated Fee: max(5, 100 × 0.10) = 10 EGP
Total Charged: 110 EGP
```

---

## Deployment

### Azure App Service

1. **Build for production**
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained false
   ```

2. **Deploy via Visual Studio**
   - Right-click project → Publish
   - Select Azure App Service
   - Follow the deployment wizard

3. **Configure App Settings in Azure Portal**
   - Add all connection strings
   - Configure environment variables
   - Enable managed identity if using Key Vault

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` |
| `ConnectionStrings__DefaultConnection` | SQL Server connection |
| `ConnectionStrings__Redis` | Redis connection |

---

## Documentation

Additional documentation is available in the `/Docs` folder:

- **[PaginationAPI.md](./Docs/PaginationAPI.md)** — Pagination implementation guide
- **[ComplaintSystemAPI.md](./Docs/ComplaintSystemAPI.md)** — Complaint system documentation

---

## Security Considerations

- All sensitive data is stored securely (not in code)
- JWT tokens have configurable expiration
- Refresh tokens are stored in Redis with expiration
- Payment webhooks verified via HMAC
- OTP attempts are rate-limited and blocked after failures
- User banning system for abuse prevention
- HTTPS enforced in production

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is proprietary software. All rights reserved.

---

<p align="center">
  <strong>Voltyks</strong> — Powering the Future of EV Charging
</p>

<p align="center">
  Built with ASP.NET Core 8.0 | Deployed on Azure
</p>
