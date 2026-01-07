# Voltyks.API - Project Structure & Architecture

## Overview

Voltyks.API is a **.NET 8 Web API** for an Electric Vehicle (EV) charging station platform. The architecture follows **Clean Architecture** principles with clear separation of concerns.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PRESENTATION LAYER                              │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         Voltyks.API                                  │   │
│  │  • Controllers (API Endpoints)                                       │   │
│  │  • Middlewares (Error Handling, Rate Limiting)                       │   │
│  │  • Extensions (DI Configuration)                                     │   │
│  │  • Hubs (SignalR Real-time)                                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                   Voltyks.AdminControlDashboard                      │   │
│  │  • Admin-specific Services                                           │   │
│  │  • Admin DTOs                                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              APPLICATION LAYER                               │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                       Voltyks.Application                            │   │
│  │  • Services (Business Logic)                                         │   │
│  │  • Interfaces (Contracts)                                            │   │
│  │  • ServiceManager (Dependency Aggregation)                           │   │
│  │  • Utilities (Helper Classes)                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                                DOMAIN LAYER                                  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          Voltyks.Core                                │   │
│  │  • DTOs (Data Transfer Objects)                                      │   │
│  │  • Enums                                                             │   │
│  │  • Error Models                                                      │   │
│  │  • Exceptions                                                        │   │
│  │  • Mapping (AutoMapper Profiles)                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            INFRASTRUCTURE LAYER                              │
│                                                                             │
│  ┌──────────────────────────────┐  ┌──────────────────────────────────┐   │
│  │    Voltyks.Infrastructure    │  │      Voltyks.Persistence         │   │
│  │  • Generic Repository        │  │  • DbContext                     │   │
│  │  • Unit of Work              │  │  • Entities                      │   │
│  │  • Repository Interfaces     │  │  • Configurations                │   │
│  └──────────────────────────────┘  │  • Migrations                    │   │
│                                    │  • Seeding                       │   │
│                                    └──────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              EXTERNAL SERVICES                               │
│                                                                             │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────┐ │
│  │ SQL Server │ │   Redis    │ │  Firebase  │ │  Paymob    │ │ SMS Egypt│ │
│  │ (Azure)    │ │  (Cache)   │ │   (FCM)    │ │ (Payments) │ │  (OTP)   │ │
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘ └──────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
Voltyks.API/
│
├── 📁 Voltyks.API/                    # Presentation Layer (Entry Point)
│   ├── 📁 Controllers/                # API Controllers
│   │   ├── 📁 Admin/                  # Admin-only endpoints
│   │   │   ├── AdminBrandsController.cs
│   │   │   ├── AdminCapacityController.cs
│   │   │   ├── AdminChargersController.cs
│   │   │   ├── AdminComplaintsController.cs
│   │   │   ├── AdminComplaintCategoriesController.cs
│   │   │   ├── AdminFeesController.cs
│   │   │   ├── AdminNotificationsController.cs
│   │   │   ├── AdminPaymentController.cs
│   │   │   ├── AdminProcessController.cs
│   │   │   ├── AdminProtocolController.cs
│   │   │   ├── AdminReportsController.cs
│   │   │   ├── AdminStoreController.cs
│   │   │   ├── AdminTermsController.cs
│   │   │   ├── AdminUsersController.cs
│   │   │   └── AdminVehiclesController.cs
│   │   │
│   │   ├── AuthController.cs          # Authentication (Login, Register, etc.)
│   │   ├── ChargerController.cs       # Charger stations
│   │   ├── ChargingRequestController.cs # Charging sessions
│   │   ├── PaymentController.cs       # Paymob integration
│   │   ├── StoreController.cs         # E-commerce store
│   │   └── ...
│   │
│   ├── 📁 Extentions/                 # DI & Configuration
│   │   └── Extentions.cs              # Service registration
│   │
│   ├── 📁 Hubs/                       # SignalR Real-time
│   │   └── ChargingHub.cs             # Real-time charging updates
│   │
│   ├── 📁 Middelwares/                # Custom Middlewares
│   │   └── ErrorHandlerMiddleware.cs  # Global error handling
│   │
│   ├── 📁 Firebase/                   # Firebase configuration
│   │   └── voltyks-firebase.json
│   │
│   ├── Program.cs                     # Application entry point
│   └── appsettings.json               # Configuration
│
├── 📁 Voltyks.AdminControlDashboard/  # Admin Module
│   ├── 📁 Dtos/                       # Admin-specific DTOs
│   │   ├── Brands/
│   │   ├── Chargers/
│   │   ├── Complaints/
│   │   ├── Users/
│   │   └── ...
│   │
│   ├── 📁 Interfaces/                 # Admin service interfaces
│   │   ├── Complaints/
│   │   │   ├── IAdminComplaintsService.cs
│   │   │   └── IAdminComplaintCategoriesService.cs
│   │   └── Notifications/
│   │       └── IAdminNotificationsService.cs
│   │
│   ├── 📁 Services/                   # Admin service implementations
│   │   ├── Complaints/
│   │   │   ├── AdminComplaintsService.cs
│   │   │   └── AdminComplaintCategoriesService.cs
│   │   └── Notifications/
│   │       └── AdminNotificationsService.cs
│   │
│   ├── IAdminServiceManager.cs        # Admin service aggregator interface
│   └── AdminServiceManager.cs         # Admin service aggregator
│
├── 📁 Voltyks.Application/            # Application Layer (Business Logic)
│   ├── 📁 Interfaces/                 # Service contracts
│   │   ├── Auth/
│   │   │   └── IAuthService.cs
│   │   ├── ChargerStation/
│   │   │   └── IChargerService.cs
│   │   ├── ChargingRequest/
│   │   │   └── IChargingRequestService.cs
│   │   ├── Paymob/
│   │   │   └── IPaymobService.cs
│   │   ├── Redis/
│   │   │   └── IRedisService.cs
│   │   ├── Firebase/
│   │   │   └── IFirebaseService.cs
│   │   ├── Store/
│   │   │   └── IStoreService.cs
│   │   └── ...
│   │
│   ├── 📁 Services/                   # Service implementations
│   │   ├── Auth/
│   │   │   └── AuthService.cs         # Login, Register, JWT, Refresh Token
│   │   ├── ChargerStation/
│   │   │   └── ChargerService.cs      # Charger CRUD
│   │   ├── ChargingRequest/
│   │   │   ├── ChargingRequestService.cs
│   │   │   └── Interceptor/           # Charging interceptor logic
│   │   ├── Paymob/
│   │   │   └── PaymobService.cs       # Payment processing
│   │   ├── Redis/
│   │   │   └── RedisService.cs        # Caching & token storage
│   │   ├── Firebase/
│   │   │   └── FirebaseService.cs     # Push notifications
│   │   ├── Store/
│   │   │   └── StoreService.cs        # E-commerce
│   │   └── ...
│   │
│   ├── 📁 ServiceManager/             # Service aggregation
│   │   ├── IServiceManager.cs
│   │   └── ServiceManager.cs
│   │
│   └── 📁 Utilities/                  # Helper classes
│
├── 📁 Voltyks.Core/                   # Domain Layer (Core Models)
│   ├── 📁 DTOs/                       # Data Transfer Objects
│   │   ├── AuthDTOs/
│   │   │   ├── LoginDTO.cs
│   │   │   ├── RegisterDTO.cs
│   │   │   ├── TokensResponseDto.cs   # JWT + Refresh Token
│   │   │   └── ...
│   │   ├── Charger/
│   │   │   └── ChargerDto.cs
│   │   ├── Paymob/
│   │   │   ├── CardsDTOs/
│   │   │   ├── ApplePay/
│   │   │   └── ...
│   │   ├── Store/
│   │   │   ├── Products/
│   │   │   ├── Categories/
│   │   │   └── Reservations/
│   │   ├── Common/
│   │   │   ├── ApiResponse.cs         # Standard API response wrapper
│   │   │   └── PaginationParams.cs
│   │   └── ...
│   │
│   ├── 📁 Enums/                      # Enumerations
│   │   ├── ChargingStatus.cs
│   │   ├── PaymentStatus.cs
│   │   └── ...
│   │
│   ├── 📁 ErrorModels/                # Error response models
│   │   └── ErrorMessages.cs
│   │
│   ├── 📁 Exceptions/                 # Custom exceptions
│   │
│   └── 📁 Mapping/                    # AutoMapper profiles
│       └── MappingProfile.cs
│
├── 📁 Voltyks.Infrastructure/         # Infrastructure Layer
│   ├── 📁 Interfaces/                 # Repository interfaces
│   │   └── IGenericRepository.cs
│   │
│   ├── 📁 Repositories/               # Repository implementations
│   │   └── GenericRepository.cs
│   │
│   └── 📁 UnitOfWork/                 # Unit of Work pattern
│       ├── IUnitOfWork.cs
│       └── UnitOfWork.cs
│
├── 📁 Voltyks.Persistence/            # Data Layer
│   ├── 📁 Data/
│   │   ├── VoltyksDbContext.cs        # EF Core DbContext
│   │   │
│   │   ├── 📁 Configurations/         # Entity configurations
│   │   │   ├── AppUserConfiguration.cs
│   │   │   ├── ChargerConfiguration.cs
│   │   │   ├── Store/
│   │   │   │   ├── ProductConfiguration.cs
│   │   │   │   └── CategoryConfiguration.cs
│   │   │   └── ...
│   │   │
│   │   ├── 📁 Migrations/             # EF Core migrations
│   │   │
│   │   └── 📁 Seeding/                # Seed data
│   │       └── SeedData.cs
│   │
│   └── 📁 Entities/                   # Database entities
│       ├── 📁 Identity/               # User & Auth entities
│       │   ├── AppUser.cs             # Application user
│       │   └── Address.cs
│       │
│       └── 📁 Main/                   # Domain entities
│           ├── Charger.cs
│           ├── ChargingRequest.cs
│           ├── Brand.cs
│           ├── Model.cs
│           ├── Vehicle.cs
│           ├── Complaint.cs
│           ├── ComplaintCategory.cs
│           ├── 📁 Paymob/             # Payment entities
│           │   ├── CardToken.cs
│           │   └── ProcessedWebhook.cs
│           └── 📁 Store/              # E-commerce entities
│               ├── Product.cs
│               ├── Category.cs
│               └── Reservation.cs
│
└── 📁 Voltyks.Web/                    # (Optional) Web UI
```

---

## Layer Responsibilities

### 1. Voltyks.API (Presentation Layer)

| Component | Responsibility |
|-----------|----------------|
| **Controllers** | Handle HTTP requests, validate input, return responses |
| **Middlewares** | Cross-cutting concerns (error handling, logging) |
| **Extensions** | Dependency Injection configuration |
| **Hubs** | SignalR real-time communication |

### 2. Voltyks.AdminControlDashboard (Admin Module)

| Component | Responsibility |
|-----------|----------------|
| **Dtos** | Admin-specific data transfer objects |
| **Services** | Admin business logic (user management, reports) |
| **ServiceManager** | Aggregates all admin services |

### 3. Voltyks.Application (Application Layer)

| Component | Responsibility |
|-----------|----------------|
| **Services** | Business logic implementation |
| **Interfaces** | Service contracts (abstraction) |
| **ServiceManager** | Aggregates all services for DI |
| **Utilities** | Helper methods and utilities |

### 4. Voltyks.Core (Domain Layer)

| Component | Responsibility |
|-----------|----------------|
| **DTOs** | Data transfer between layers |
| **Enums** | Domain enumerations |
| **Mapping** | AutoMapper profiles |
| **Exceptions** | Custom domain exceptions |

### 5. Voltyks.Infrastructure (Infrastructure Layer)

| Component | Responsibility |
|-----------|----------------|
| **Repositories** | Data access abstraction |
| **UnitOfWork** | Transaction management |

### 6. Voltyks.Persistence (Data Layer)

| Component | Responsibility |
|-----------|----------------|
| **DbContext** | EF Core database context |
| **Entities** | Database models |
| **Configurations** | Fluent API configurations |
| **Migrations** | Database schema versioning |
| **Seeding** | Initial data population |

---

## Key Features

### Authentication & Authorization
- JWT Bearer tokens (30-minute expiry)
- Sliding Refresh Tokens (7-day expiry, one-time use)
- Role-based authorization (Admin, User)
- Redis for token storage

### External Integrations
- **Paymob** - Payment processing (Cards, Apple Pay)
- **Firebase** - Push notifications (FCM)
- **SMS Egypt** - OTP verification
- **Redis** - Caching & session management

### Real-time Features
- **SignalR** - Live charging status updates

### E-commerce
- Product catalog
- Categories
- Reservations system

---

## API Endpoints Overview

### Public Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/Login` | User login |
| POST | `/api/auth/Register` | User registration |
| POST | `/api/auth/RefreshToken` | Refresh JWT token |

### User Endpoints (Requires Authentication)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/chargers` | Get charger stations |
| POST | `/api/charging-requests` | Start charging session |
| GET | `/api/store/products` | Browse products |

### Admin Endpoints (Requires Admin Role)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/users` | Manage users |
| GET | `/api/admin/chargers` | Manage chargers |
| GET | `/api/admin/reports` | View reports |

---

## Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Main configuration |
| `appsettings.Development.json` | Development overrides |
| `voltyks-firebase.json` | Firebase credentials |

---

## Database

- **Provider**: SQL Server (Azure SQL Database)
- **ORM**: Entity Framework Core 8
- **Connection**: Defined in `appsettings.json`

---

## Deployment

- **Platform**: Azure App Service
- **CI/CD**: GitHub Actions
- **URL**: `https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net`
