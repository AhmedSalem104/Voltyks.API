<p align="center">
  <img src="https://capsule-render.vercel.app/api?type=waving&color=gradient&customColorList=6,11,20&height=200&section=header&text=Voltyks%20API&fontSize=80&fontAlignY=35&animation=twinkling&fontColor=fff&desc=Electric%20Vehicle%20Charging%20Platform&descAlignY=55&descSize=20" alt="Header" />
</p>

<p align="center">
  <a href="https://git.io/typing-svg">
    <img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=600&size=22&pause=1000&color=6C63FF&center=true&vCenter=true&multiline=true&repeat=false&width=600&height=100&lines=Powering+the+Future+of+EV+Charging;Built+with+.NET+8+%7C+Clean+Architecture+%7C+Azure" alt="Typing SVG" />
  </a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-2.2.0-blue?style=for-the-badge&logo=semantic-release" alt="Version" />
  <img src="https://img.shields.io/badge/license-Proprietary-red?style=for-the-badge" alt="License" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Azure-Deployed-0078D4?style=for-the-badge&logo=microsoftazure&logoColor=white" alt="Azure" />
  <img src="https://img.shields.io/badge/Build-Passing-brightgreen?style=for-the-badge" alt="Build" />
</p>

<p align="center">
  <a href="#-features"><img src="https://img.shields.io/badge/Features-6C63FF?style=for-the-badge" alt="Features" /></a>
  <a href="#-tech-stack"><img src="https://img.shields.io/badge/Tech_Stack-FF6B6B?style=for-the-badge" alt="Tech Stack" /></a>
  <a href="#-architecture"><img src="https://img.shields.io/badge/Architecture-4ECDC4?style=for-the-badge" alt="Architecture" /></a>
  <a href="#-api-reference"><img src="https://img.shields.io/badge/API_Reference-45B7D1?style=for-the-badge" alt="API" /></a>
  <a href="#-getting-started"><img src="https://img.shields.io/badge/Get_Started-96CEB4?style=for-the-badge" alt="Get Started" /></a>
</p>

---

## Overview

**Voltyks** is an enterprise-grade API platform that revolutionizes how Electric Vehicle owners connect with charging station providers. Our platform orchestrates the complete charging lifecycle — from discovering nearby chargers to seamless payment processing.

### What Makes Voltyks Special?

| For EV Owners | For Charger Owners |
|:--------------|:-------------------|
| Find nearby chargers instantly | List and manage your chargers |
| Send real-time charging requests | Get instant request notifications |
| Pay securely with cards or wallets | Earn money from your chargers |
| Rate your charging experience | Build your reputation |
| Shop from our EV Store | Manage reservations |

---

## What's New in v2.2.0

| Category | Updates |
|:---------|:--------|
| **Store Module** | Complete e-commerce with categories, products, reservations |
| **Product Reservation** | IsReserved flag for real-time availability tracking |
| **Payment** | Paymob Intention API, TRANSACTION webhook, Apple Pay |
| **Security** | HMAC-SHA512 verification, PCI-compliant logging |
| **Admin** | Complete store management, wallet transactions |

### Recent Changes

```diff
+ Added IsReserved field to product endpoints for reservation status
+ Implemented Store module with categories, products, and reservations
+ Added product image management with Azure Blob Storage
+ Query parameter conversion for StoreController endpoints
+ Added TRANSACTION webhook handling for automatic payment updates
+ Implemented HMAC-SHA512 webhook signature verification
+ Added wallet transaction history with notes tracking
+ Created full CRUD endpoints for Protocols
+ Removed sensitive data from logs (PCI compliance)
```

---

## Features

<table>
<tr>
<td width="20%" align="center"><b>Core</b></td>
<td width="20%" align="center"><b>Security</b></td>
<td width="20%" align="center"><b>Payments</b></td>
<td width="20%" align="center"><b>Mobile</b></td>
<td width="20%" align="center"><b>Admin</b></td>
</tr>
<tr>
<td>Geolocation Search</td>
<td>JWT + Refresh Tokens</td>
<td>Paymob Intention API</td>
<td>Push Notifications</td>
<td>User Management</td>
</tr>
<tr>
<td>Real-time Requests</td>
<td>OAuth (Google/FB)</td>
<td>Card Tokenization</td>
<td>Device Management</td>
<td>Wallet Control</td>
</tr>
<tr>
<td>Two-way Ratings</td>
<td>OTP Verification</td>
<td>Saved Cards</td>
<td>Deep Linking</td>
<td>Complaints System</td>
</tr>
<tr>
<td>Wallet System</td>
<td>HMAC Webhook Verify</td>
<td>Apple Pay</td>
<td>Multi-language</td>
<td>Store Management</td>
</tr>
<tr>
<td>E-commerce Store</td>
<td>Rate Limiting</td>
<td>Mobile Wallets</td>
<td>SignalR Real-time</td>
<td>Reservations</td>
</tr>
</table>

### Feature Architecture

```
                                    VOLTYKS PLATFORM
    ┌─────────────────────────────────────────────────────────────────┐
    │                                                                 │
    │   ┌─────────────┐   ┌─────────────┐   ┌─────────────────────┐  │
    │   │  CHARGING   │   │  PAYMENTS   │   │      SECURITY       │  │
    │   │             │   │             │   │                     │  │
    │   │ • Find      │   │ • Cards     │   │ • JWT Auth          │  │
    │   │ • Request   │   │ • Wallets   │   │ • OAuth 2.0         │  │
    │   │ • Track     │   │ • Apple Pay │   │ • OTP SMS           │  │
    │   │ • Rate      │   │ • Saved     │   │ • Rate Limiting     │  │
    │   └─────────────┘   └─────────────┘   └─────────────────────┘  │
    │                                                                 │
    │   ┌─────────────┐   ┌─────────────┐   ┌─────────────────────┐  │
    │   │    STORE    │   │   ADMIN     │   │    NOTIFICATIONS    │  │
    │   │             │   │             │   │                     │  │
    │   │ • Products  │   │ • Users     │   │ • Push (FCM)        │  │
    │   │ • Categories│   │ • Wallets   │   │ • SignalR           │  │
    │   │ • Reserve   │   │ • Fees      │   │ • SMS               │  │
    │   │ • Images    │   │ • Reports   │   │ • In-App            │  │
    │   └─────────────┘   └─────────────┘   └─────────────────────┘  │
    │                                                                 │
    └─────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

<p align="center">
  <a href="https://dotnet.microsoft.com/"><img src="https://skillicons.dev/icons?i=dotnet" alt=".NET" /></a>
  <a href="https://docs.microsoft.com/en-us/dotnet/csharp/"><img src="https://skillicons.dev/icons?i=cs" alt="C#" /></a>
  <a href="https://azure.microsoft.com/"><img src="https://skillicons.dev/icons?i=azure" alt="Azure" /></a>
  <a href="https://redis.io/"><img src="https://skillicons.dev/icons?i=redis" alt="Redis" /></a>
  <a href="https://firebase.google.com/"><img src="https://skillicons.dev/icons?i=firebase" alt="Firebase" /></a>
  <a href="https://git-scm.com/"><img src="https://skillicons.dev/icons?i=git" alt="Git" /></a>
  <a href="https://github.com/"><img src="https://skillicons.dev/icons?i=github" alt="GitHub" /></a>
  <a href="https://www.postman.com/"><img src="https://skillicons.dev/icons?i=postman" alt="Postman" /></a>
</p>

### Backend & Framework

| Technology | Version | Purpose |
|:-----------|:--------|:--------|
| .NET | 8.0 | Runtime & SDK |
| ASP.NET Core | 8.0 | Web API Framework |
| Entity Framework Core | 8.0 | ORM & Migrations |
| SignalR | 8.0 | Real-time Communication |
| AutoMapper | 14.0.0 | Object Mapping |

### Database & Caching

| Technology | Purpose |
|:-----------|:--------|
| Azure SQL Server | Primary Database |
| Redis (Upstash) | Distributed Caching |
| Dapper | High-performance Queries |

### Authentication & Security

| Technology | Purpose |
|:-----------|:--------|
| JWT Bearer | Token Authentication |
| ASP.NET Identity | User Management |
| BCrypt | Password Hashing |
| Google OAuth2 | Social Login |
| Facebook OAuth2 | Social Login |

### External Services

| Service | Purpose |
|:--------|:--------|
| Paymob | Payment Gateway (Cards, Wallets, Apple Pay) |
| Firebase (FCM) | Push Notifications |
| SMS Egypt | OTP & SMS |
| Twilio | SMS Fallback |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Controllers │  │ Middlewares │  │ SignalR Hubs            │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                     APPLICATION LAYER                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Services   │  │ Interfaces  │  │ Business Logic          │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                       DOMAIN LAYER                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │    DTOs     │  │    Enums    │  │ Mapping Profiles        │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                   INFRASTRUCTURE LAYER                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Repositories│  │ Unit of Work│  │ External Services       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                    PERSISTENCE LAYER                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  DbContext  │  │  Entities   │  │ Migrations & Seeding    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
Voltyks.API/
│
├── Voltyks.API/                       # Presentation Layer
│   ├── Controllers/                   # API Endpoints
│   │   ├── Admin/                     # Admin-specific endpoints
│   │   ├── AuthController.cs          # Authentication
│   │   ├── ChargerController.cs       # Charger management
│   │   ├── ChargingRequestController.cs
│   │   ├── PaymentController.cs       # Payment processing
│   │   ├── StoreController.cs         # E-commerce store
│   │   └── ...
│   ├── Hubs/                          # SignalR Hubs
│   ├── Middlewares/                   # Custom Middleware
│   └── Program.cs                     # Application Entry Point
│
├── Voltyks.Application/               # Application Layer
│   ├── Interfaces/                    # Service Contracts
│   └── Services/                      # Business Logic
│       ├── Auth/                      # Authentication Services
│       ├── Payment/                   # Payment Processing
│       ├── Store/                     # E-commerce Services
│       └── ...
│
├── Voltyks.Core/                      # Domain Layer
│   ├── DTOs/                          # Data Transfer Objects
│   │   ├── Store/                     # Store DTOs
│   │   │   ├── Products/
│   │   │   ├── Categories/
│   │   │   └── Reservations/
│   │   └── ...
│   ├── Enums/                         # Enumerations
│   └── MappingProfiles/               # AutoMapper Profiles
│
├── Voltyks.Infrastructure/            # Infrastructure Layer
│   ├── Repositories/                  # Data Access
│   └── UnitOfWork/                    # Transaction Management
│
├── Voltyks.Persistence/               # Persistence Layer
│   ├── Data/                          # DbContext & Seeding
│   ├── Entities/                      # Database Entities
│   │   ├── Main/
│   │   │   ├── Store/                 # Store entities
│   │   │   ├── Paymob/                # Payment entities
│   │   │   └── ...
│   └── Migrations/                    # EF Migrations
│
└── Voltyks.AdminControlDashboard/     # Admin Module
    ├── DTOs/                          # Admin-specific DTOs
    └── Services/                      # Admin Services
```

---

## Database Schema

### Entity Overview (40+ Entities)

```
┌─────────────────────────────────────────────────────────────────┐
│                      DATABASE ENTITIES                          │
├──────────────────┬──────────────────┬───────────────────────────┤
│   IDENTITY (4)   │   VEHICLES (5)   │      CHARGING (5+)        │
├──────────────────┼──────────────────┼───────────────────────────┤
│ • AppUser        │ • Vehicle        │ • ChargingRequest         │
│ • Address        │ • Brand          │ • Process                 │
│ • UserType       │ • Model          │ • Charger                 │
│ • UsersBanned    │ • Capacity       │ • ChargerAddress          │
│                  │ • Protocol       │ • RatingsHistory          │
├──────────────────┼──────────────────┼───────────────────────────┤
│   PAYMENT (8)    │    STORE (3)     │      SUPPORT (4+)         │
├──────────────────┼──────────────────┼───────────────────────────┤
│ • PaymentOrder   │ • StoreCategory  │ • Notification            │
│ • PaymentTransaction│ • StoreProduct│ • UserGeneralComplaint    │
│ • PaymentAction  │ • StoreReservation│ • ComplaintCategory      │
│ • UserSavedCard  │                  │ • UserReport              │
│ • WebhookLog     │                  │ • TermsDocument           │
│ • CardTokenWebhookLog│              │ • DeviceToken             │
│ • RevokedCardToken│                 │                           │
│ • ProcessedWebhook│                 │                           │
└──────────────────┴──────────────────┴───────────────────────────┘
```

### Key Relationships

```
AppUser ─────┬───── Vehicle (1:N)
             ├───── Charger (1:N)
             ├───── ChargingRequest (1:N)
             ├───── UserSavedCard (1:N)
             ├───── StoreReservation (1:N)
             └───── Notification (1:N)

Brand ───── Model ───── Vehicle

StoreCategory ───── StoreProduct ───── StoreReservation

ChargingRequest ───── Process ───── RatingsHistory
```

---

## API Reference

### Base URL
```
Production: https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net
```

### Authentication Endpoints `/api/auth`

| Method | Endpoint | Description | Auth |
|:------:|:---------|:------------|:----:|
| `POST` | `/Login` | User login with JWT | - |
| `POST` | `/register` | Create new account | - |
| `POST` | `/RefreshToken` | Refresh JWT token | - |
| `POST` | `/forget-password` | Request password reset | - |
| `POST` | `/verify-forget-password-otp` | Verify OTP | - |
| `POST` | `/reset-password` | Reset with OTP | - |
| `GET` | `/GetProfileDetails` | Get user profile | Required |
| `PUT` | `/toggle-availability` | Toggle availability | Required |
| `GET` | `/wallet` | Get wallet balance | Required |
| `POST` | `/general-complaints` | Submit complaint | Required |

### Charger Endpoints `/api/charger`

| Method | Endpoint | Description | Auth |
|:------:|:---------|:------------|:----:|
| `GET` | `/GetCapacity` | Get capacities (cached 1hr) | - |
| `GET` | `/GetProtocol` | Get protocols (cached 1hr) | - |
| `GET` | `/GetPrices` | Get price list (cached 30min) | - |
| `POST` | `/AddCharger` | Register charger | Required |
| `GET` | `/GetChargersByUser` | My chargers | Required |
| `POST` | `/GetNearChargers` | Find nearby | Required |
| `PUT` | `/ToggleStatus` | Toggle status | Required |
| `PUT` | `/UpdateCharger` | Update details | Required |
| `DELETE` | `/DeleteCharger` | Delete charger | Required |

### Charging Request Endpoints `/api/chargingrequest`

| Method | Endpoint | Description | Auth |
|:------:|:---------|:------------|:----:|
| `POST` | `/sendChargingRequest` | Send request | Required |
| `PUT` | `/AcceptRequest` | Accept | Required |
| `PUT` | `/RejectRequest` | Reject | Required |
| `PUT` | `/ConfirmRequest` | Confirm start | Required |
| `PUT` | `/abortRequest` | Abort session | Required |
| `POST` | `/registerDeviceToken` | Register FCM token | Required |

### Payment Endpoints `/api/payment`

| Method | Endpoint | Description | Auth |
|:------:|:---------|:------------|:----:|
| `POST` | `/intention` | Create payment intention | Required |
| `POST` | `/webhook` | Paymob webhook (TRANSACTION/TOKEN) | - |
| `POST` | `/getOrderStatus` | Check payment status | Required |
| `POST` | `/tokenization` | Start card tokenization | Required |
| `GET` | `/GetListOfCards` | List saved cards | Required |
| `POST` | `/setDefault_Card` | Set default card | Required |
| `POST` | `/payWithSavedCard` | Pay with saved card | Required |
| `DELETE` | `/delete_Card` | Delete saved card | Required |
| `POST` | `/applepay/process` | Process Apple Pay | Required |

### Store Endpoints `/api/store`

| Method | Endpoint | Description | Auth |
|:------:|:---------|:------------|:----:|
| `GET` | `/categories` | List all categories | - |
| `GET` | `/products` | List products with pagination | - |
| `GET` | `/GetProductById?id={id}` | Get product by ID | - |
| `GET` | `/GetProductBySlug?slug={slug}` | Get product by slug | - |
| `POST` | `/reservations` | Create reservation | Required |
| `GET` | `/my-reservations` | User's reservations | Required |
| `PUT` | `/UpdateMyReservation?id={id}` | Update reservation | Required |
| `DELETE` | `/CancelMyReservation?id={id}` | Cancel reservation | Required |

#### Product Response with IsReserved

```json
{
  "data": {
    "id": 1,
    "categoryId": 2,
    "categoryName": "EV Accessories",
    "name": "Portable Charger",
    "slug": "portable-charger",
    "description": "High-quality portable EV charger",
    "price": 2500.00,
    "currency": "EGP",
    "images": ["url1", "url2"],
    "status": "active",
    "isReservable": true,
    "isReserved": false
  },
  "message": "Product retrieved successfully",
  "status": true
}
```

### Admin Store Endpoints `/api/admin/store`

| Method | Endpoint | Description |
|:------:|:---------|:------------|
| `GET` | `/categories` | List categories (with soft-delete filter) |
| `GET` | `/categories/{id}` | Get category by ID |
| `POST` | `/categories` | Create category |
| `PUT` | `/categories/{id}` | Update category |
| `DELETE` | `/categories/{id}` | Soft delete category |
| `POST` | `/categories/{id}/restore` | Restore category |
| `DELETE` | `/categories/{id}/force` | Hard delete category |
| `GET` | `/products` | List products |
| `GET` | `/products/{id}` | Get product by ID |
| `POST` | `/products` | Create product |
| `PUT` | `/products/{id}` | Update product |
| `DELETE` | `/products/{id}` | Soft delete product |
| `POST` | `/products/{id}/restore` | Restore product |
| `DELETE` | `/products/{id}/force` | Hard delete product |
| `POST` | `/products/{id}/images` | Upload images |
| `DELETE` | `/products/{id}/images` | Delete image |
| `DELETE` | `/products/{id}/images/all` | Delete all images |
| `GET` | `/reservations` | List reservations |
| `GET` | `/reservations/{id}` | Get reservation details |
| `PUT` | `/reservations/{id}/contact` | Record contact |
| `PUT` | `/reservations/{id}/payment` | Record payment |
| `PUT` | `/reservations/{id}/delivery` | Record delivery |
| `PUT` | `/reservations/{id}/complete` | Complete reservation |
| `PUT` | `/reservations/{id}/cancel` | Cancel reservation |

### Admin Endpoints `/api/admin/*`

| Method | Endpoint | Description |
|:------:|:---------|:------------|
| `GET` | `/users` | List users with search |
| `GET` | `/users/{id}` | Get user details |
| `POST` | `/users/{id}/ban-toggle` | Toggle user ban |
| `DELETE` | `/users/{id}` | Soft delete user |
| `DELETE` | `/users/{id}/hard` | Hard delete user |
| `GET` | `/fees/wallet-transactions` | Wallet history |
| `POST` | `/fees/transfer` | Add/Deduct balance |
| `GET` | `/complaints` | List complaints |
| `PATCH` | `/complaints/{id}/status` | Update status |
| `GET` | `/notifications` | List notifications |
| `PATCH` | `/notifications/{id}/read` | Mark as read |

---

## API Response Format

### Success Response
```json
{
  "data": { ... },
  "message": "Operation successful",
  "status": true,
  "errors": []
}
```

### Error Response
```json
{
  "data": null,
  "message": "Error description",
  "status": false,
  "errors": ["Error 1", "Error 2"]
}
```

### Paginated Response
```json
{
  "data": {
    "items": [...],
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

---

## Charging Workflow

```
    EV Owner                    Voltyks API               Charger Owner
       │                            │                           │
       │  1. Search nearby          │                           │
       │ ─────────────────────────► │                           │
       │                            │                           │
       │  ◄──────────────────────── │                           │
       │     Available chargers     │                           │
       │                            │                           │
       │  2. Send charging request  │                           │
       │ ─────────────────────────► │  3. Push notification     │
       │                            │ ─────────────────────────►│
       │                            │                           │
       │                            │  4. Accept request        │
       │                            │ ◄─────────────────────────│
       │  5. Request accepted       │                           │
       │ ◄──────────────────────────│                           │
       │                            │                           │
       │  6. Confirm charging start │                           │
       │ ─────────────────────────► │  7. Notify started        │
       │                            │ ─────────────────────────►│
       │                            │                           │
       │         ⚡ CHARGING IN PROGRESS ⚡                      │
       │                            │                           │
       │  8. Complete session       │                           │
       │ ─────────────────────────► │                           │
       │                            │  9. Process payment       │
       │                            │ ───────► Paymob ──────────│
       │                            │                           │
       │  10. Submit rating         │  11. Submit rating        │
       │ ─────────────────────────► │ ◄─────────────────────────│
       │                            │                           │
       │  ✅ Session completed      │  ✅ Payment received      │
       │ ◄──────────────────────────│ ─────────────────────────►│
```

---

## Quick Start

### Prerequisites

- .NET SDK 8.0+
- SQL Server 2019+ or Azure SQL
- Redis 7.0+ (optional, for caching)

### Installation

```bash
# Clone the repository
git clone https://bitbucket.org/voltyks-global/voltyks-backend.git

# Navigate to project
cd voltyks-backend

# Restore dependencies
dotnet restore

# Apply database migrations
cd Voltyks.API
dotnet ef database update

# Run the application
dotnet run
```

### Configuration

Create `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=VoltyksDB;...",
    "Redis": "your-redis-url:6379,password=***,ssl=True"
  },
  "JwtOptions": {
    "Issuer": "https://your-domain.com",
    "Audience": "YourAudience",
    "SecurityKey": "your-super-secret-key-min-32-chars",
    "ExpiresInMinutes": 43200
  },
  "Paymob": {
    "ENV": "live",
    "ApiKey": "your-api-key",
    "SecretKey": "your-secret-key",
    "PublicKey": "your-public-key",
    "Integration": {
      "Card": 5413127,
      "Wallet": 5413126,
      "ApplePay": 5458488
    },
    "HmacSecret": "your-hmac-secret"
  },
  "Firebase": {
    "ProjectId": "your-project-id",
    "ServiceAccountFile": "Firebase/service-account-key.json"
  }
}
```

---

## Security

| Feature | Implementation |
|:--------|:---------------|
| JWT tokens | Configurable expiration (30 days default) |
| Refresh tokens | Stored in Redis with secure rotation |
| HMAC-SHA512 | Webhook verification (Paymob) |
| Rate limiting | OTP attempts (5 max) |
| User banning | With reason tracking |
| HTTPS | Enforced in production |
| OAuth 2.0 | Google, Facebook integration |
| Card tokenization | PCI-compliant (no card data stored) |
| Request limits | 1MB on webhook endpoints |

### Payment Security

```
✅ HMAC-SHA512 Webhook Signature Verification
✅ Time-safe signature comparison (prevents timing attacks)
✅ Card tokens stored instead of card numbers
✅ No sensitive data in logs (PCI compliance)
✅ Request size limits to prevent DoS attacks
✅ HTTP response validation before processing
✅ Bounded retry loops with exponential backoff
```

---

## Deployment

### CI/CD with GitHub Actions

```yaml
Triggers:
  - Push to master branch
  - Manual workflow dispatch

Steps:
  1. Checkout code
  2. Setup .NET 8
  3. Restore dependencies
  4. Fix database schema
  5. Apply database migrations
  6. Build & Publish
  7. Deploy to Azure App Service
```

### Manual Deployment

```bash
# Build for production
dotnet publish -c Release -o ./publish

# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip

# Deploy via Azure CLI
az webapp deploy --resource-group Voltyks --name VoltyksApp --src-path deploy.zip
```

---

## CORS Configuration

```csharp
AllowedOrigins:
  - https://voltyks.com
  - https://www.voltyks.com
  - https://admin.voltyks.com
  - https://voltyks.vercel.app
  - http://localhost:3000 (dev)
  - http://localhost:5173 (dev)
```

---

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

This project is proprietary software. All rights reserved by **Voltyks Global**.

---

<p align="center">
  <img src="https://capsule-render.vercel.app/api?type=waving&color=gradient&customColorList=6,11,20&height=100&section=footer" alt="Footer" />
</p>

<p align="center">
  <strong>Voltyks</strong> — Powering the Future of EV Charging
</p>

<p align="center">
  Built with .NET 8 | Deployed on Microsoft Azure
</p>

<p align="center">
  <a href="https://voltyks.com">Website</a> •
  <a href="https://admin.voltyks.com">Admin Dashboard</a> •
  <a href="mailto:support@voltyks.com">Support</a>
</p>
