# Voltyks API - Payment System Documentation

> **Complete Guide to the Payment System Architecture**

---

## Table of Contents
1. [Overview](#overview)
2. [Payment Flow](#payment-flow)
3. [Paymob Integration](#paymob-integration)
4. [API Endpoints](#api-endpoints)
5. [Database Models](#database-models)
6. [Saved Cards System](#saved-cards-system)
7. [Wallet System](#wallet-system)
8. [Webhook Processing](#webhook-processing)
9. [DTOs Reference](#dtos-reference)
10. [Configuration](#configuration)
11. [Security](#security)
12. [Testing](#testing)

---

## Overview

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT (Mobile/Web)                         │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       PaymentController                              │
│  /api/payment                                                        │
│  ├── POST /intention          → Create payment intention             │
│  ├── POST /tokenization       → Start card tokenization              │
│  ├── POST /payWithSavedCard   → Charge saved card                    │
│  ├── GET  /GetListOfCards     → List user's saved cards              │
│  ├── POST /setDefault_Card    → Set default card                     │
│  ├── DELETE /delete_Card      → Delete saved card                    │
│  ├── POST /getOrderStatus     → Get order status                     │
│  └── POST /webhook            → Receive Paymob notifications         │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         IPaymobService                               │
│  (PaymobService.cs)                                                  │
│  └── Business Logic for all payment operations                       │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
           ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
           │    Redis     │ │   Database   │ │  Paymob API  │
           │ (Auth Token  │ │ (Orders,     │ │ (Payment     │
           │   Cache)     │ │  Cards)      │ │  Gateway)    │
           └──────────────┘ └──────────────┘ └──────────────┘
```

### Key Components

| Component | File | Description |
|-----------|------|-------------|
| **Controller** | `PaymentController.cs` | API endpoints |
| **Service** | `PaymobService.cs` | Payment business logic |
| **Auth Provider** | `PaymobAuthTokenProviderRedis.cs` | Token caching |
| **Entities** | `PaymentOrder.cs`, `PaymentTransaction.cs` | Database models |
| **Config** | `PaymobOptions.cs` | Settings model |

---

## Payment Flow

### Flow 1: New Card Payment (Intention API)

```
Step 1: Client Request
┌─────────────────────────────────────────────────────────────┐
│ POST /api/payment/intention                                  │
│ {                                                            │
│   "amount": 10000,          // Amount in cents (100 EGP)    │
│   "paymentMethod": "card",  // card, wallet, apple_pay      │
│   "billingData": { ... }                                    │
│ }                                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 2: Server Processing
┌─────────────────────────────────────────────────────────────┐
│ 1. Get/Create Auth Token (Redis cached, 50 min TTL)         │
│ 2. Create PaymentOrder in database (Status: Pending)        │
│ 3. Call Paymob API: POST /ecommerce/orders                  │
│ 4. Store PaymobOrderId                                      │
│ 5. Call Paymob: POST /v1/intention/                         │
│ 6. Return ClientSecret to client                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 3: Client SDK
┌─────────────────────────────────────────────────────────────┐
│ Client uses Paymob SDK with ClientSecret                    │
│ → User enters card details securely                         │
│ → Payment processed by Paymob                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 4: Webhook Notification
┌─────────────────────────────────────────────────────────────┐
│ Paymob sends POST to /api/payment/webhook                   │
│ → Server verifies HMAC signature                            │
│ → Updates PaymentOrder status                               │
│ → Creates PaymentTransaction record                         │
│ → Saves card token (if tokenization requested)              │
└─────────────────────────────────────────────────────────────┘
```

### Flow 2: Saved Card Payment

```
Step 1: List Saved Cards
┌─────────────────────────────────────────────────────────────┐
│ GET /api/payment/GetListOfCards                              │
│ Response: [                                                  │
│   { "cardId": 1, "last4": "4242", "brand": "Visa" },        │
│   { "cardId": 2, "last4": "5555", "brand": "Mastercard" }   │
│ ]                                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 2: Charge Saved Card
┌─────────────────────────────────────────────────────────────┐
│ POST /api/payment/payWithSavedCard                           │
│ { "cardId": 1, "amountCents": 10000 }                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 3: Server Processing
┌─────────────────────────────────────────────────────────────┐
│ 1. Retrieve saved card token from database                  │
│ 2. Create new PaymentOrder                                  │
│ 3. Generate payment key for saved card                      │
│ 4. Call Paymob with card token                              │
│ 5. Return payment result                                    │
└─────────────────────────────────────────────────────────────┘
```

### Flow 3: Card Tokenization (Save Card)

```
Step 1: Start Tokenization
┌─────────────────────────────────────────────────────────────┐
│ POST /api/payment/tokenization                               │
│ Response: { "paymentKey": "...", "iframeUrl": "..." }        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 2: Client SDK
┌─────────────────────────────────────────────────────────────┐
│ User enters card details in Paymob iframe                   │
│ Card is tokenized (no actual charge)                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 3: Webhook - CARD_TOKEN Event
┌─────────────────────────────────────────────────────────────┐
│ Paymob sends webhook with card token:                       │
│ {                                                            │
│   "type": "CARD_TOKEN",                                      │
│   "obj": {                                                   │
│     "token": "abc123...",                                    │
│     "masked_pan": "xxxx-xxxx-xxxx-4242",                    │
│     "card_subtype": "VISA",                                 │
│     "card_expiry_month": 12,                                │
│     "card_expiry_year": 2027                                │
│   }                                                          │
│ }                                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 4: Save Card
┌─────────────────────────────────────────────────────────────┐
│ Server saves to UserSavedCard table:                        │
│ - Token (encrypted by Paymob)                               │
│ - Last4: "4242"                                             │
│ - Brand: "Visa"                                             │
│ - ExpiryMonth: 12, ExpiryYear: 2027                         │
│ - UserId: (from JWT claims)                                 │
└─────────────────────────────────────────────────────────────┘
```

---

## Paymob Integration

### Configuration

**File**: `appsettings.json`

```json
{
  "Paymob": {
    "ENV": "test",
    "ApiBase": "https://accept.paymob.com/api",
    "ApiKey": "[Your API Key - Base64 Encoded]",
    "SecretKey": "egy_sk_test_...",
    "PublicKey": "egy_pk_test_...",
    "HmacSecret": "C2DF5ABCCDACBD10B7CAF4ED98AF8770",
    "IFrameId": "947450",
    "Integration": {
      "Card": 5229585,
      "Wallet": 5252502
    },
    "Currency": "EGP",
    "Intention": {
      "Url": "https://accept.paymob.com/v1/intention/"
    }
  }
}
```

### Configuration Properties

| Property | Description | Example |
|----------|-------------|---------|
| `ENV` | Environment (test/live) | `"test"` |
| `ApiBase` | Paymob API base URL | `"https://accept.paymob.com/api"` |
| `ApiKey` | Authentication API key | Base64 encoded string |
| `SecretKey` | Secret key for Intention API | `"egy_sk_test_..."` |
| `PublicKey` | Public key for client SDK | `"egy_pk_test_..."` |
| `HmacSecret` | Webhook signature verification | 32-char hex string |
| `IFrameId` | Paymob iframe ID for card entry | `"947450"` |
| `Integration.Card` | Card payment integration ID | `5229585` |
| `Integration.Wallet` | Wallet payment integration ID | `5252502` |
| `Currency` | Default currency | `"EGP"` |

### Paymob API Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/auth/tokens` | POST | Get authentication token |
| `/ecommerce/orders` | POST | Create payment order |
| `/acceptance/payment_keys` | POST | Generate payment key |
| `/v1/intention/` | POST | Create payment intention |
| `/acceptance/transactions/{id}` | GET | Get transaction details |
| `/ecommerce/orders/{id}` | GET | Get order status |

### Authentication Token Caching

**File**: `PaymobAuthTokenProviderRedis.cs`

```
┌─────────────────────────────────────────────────────────────┐
│                    Token Request Flow                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Request Token                                               │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────────┐     Hit      ┌──────────────────┐          │
│  │ Check Redis │─────────────▶│ Return Cached    │          │
│  │   Cache     │              │ Token            │          │
│  └─────────────┘              └──────────────────┘          │
│       │ Miss                                                 │
│       ▼                                                      │
│  ┌─────────────┐                                             │
│  │ Acquire Lock│ (15 sec TTL)                               │
│  └─────────────┘                                             │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────────┐                                             │
│  │ Call Paymob │ POST /auth/tokens                          │
│  │    API      │                                             │
│  └─────────────┘                                             │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────────┐                                             │
│  │ Cache Token │ (50 min TTL)                               │
│  │ in Redis    │                                             │
│  └─────────────┘                                             │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────────┐                                             │
│  │ Release     │                                             │
│  │ Lock        │                                             │
│  └─────────────┘                                             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## API Endpoints

### Base URL
```
https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/payment
```

### Endpoints Reference

#### 1. Create Payment Intention
```http
POST /api/payment/intention
Authorization: Bearer {token}
Content-Type: application/json

{
  "amount": 10000,
  "paymentMethod": "card",
  "saveCard": false,
  "billingData": {
    "first_name": "Ahmed",
    "last_name": "Salem",
    "email": "ahmed@example.com",
    "phone_number": "+201111111111",
    "country": "EG",
    "city": "Cairo",
    "street": "123 Main St",
    "building": "10",
    "floor": "2",
    "apartment": "5"
  }
}

Response:
{
  "status": true,
  "message": "Success",
  "data": {
    "clientSecret": "pi_...",
    "paymentKeys": { ... }
  }
}
```

#### 2. Start Card Tokenization
```http
POST /api/payment/tokenization
Authorization: Bearer {token}

Response:
{
  "status": true,
  "data": {
    "paymentKey": "ZXlK...",
    "iframeUrl": "https://accept.paymob.com/api/acceptance/iframes/947450?payment_token=ZXlK..."
  }
}
```

#### 3. List Saved Cards
```http
GET /api/payment/GetListOfCards
Authorization: Bearer {token}

Response:
{
  "status": true,
  "data": [
    {
      "cardId": 1,
      "last4": "4242",
      "brand": "Visa",
      "expiryMonth": 12,
      "expiryYear": 2027,
      "isDefault": true
    }
  ]
}
```

#### 4. Pay with Saved Card
```http
POST /api/payment/payWithSavedCard
Authorization: Bearer {token}
Content-Type: application/json

{
  "cardId": 1,
  "amountCents": 10000
}

Response:
{
  "status": true,
  "message": "Payment successful",
  "data": {
    "orderId": "ORD_123456",
    "transactionId": "TXN_789"
  }
}
```

#### 5. Set Default Card
```http
POST /api/payment/setDefault_Card
Authorization: Bearer {token}
Content-Type: application/json

{
  "cardId": 1
}

Response:
{
  "status": true,
  "message": "Default card updated"
}
```

#### 6. Delete Card
```http
DELETE /api/payment/delete_Card
Authorization: Bearer {token}
Content-Type: application/json

{
  "cardId": 1
}

Response:
{
  "status": true,
  "message": "Card deleted successfully"
}
```

#### 7. Get Order Status
```http
POST /api/payment/getOrderStatus
Authorization: Bearer {token}
Content-Type: application/json

{
  "paymobOrderId": 123456789
}

Response:
{
  "status": true,
  "data": {
    "orderId": 123456789,
    "status": "Paid",
    "amountCents": 10000,
    "currency": "EGP",
    "transactions": [...]
  }
}
```

#### 8. Webhook (Paymob Notifications)
```http
POST /api/payment/webhook
Content-Type: application/json

{
  "type": "TRANSACTION",
  "obj": {
    "id": 123456,
    "order": { "id": 789 },
    "success": true,
    "amount_cents": 10000,
    ...
  }
}

Response: 200 OK (always)
```

---

## Database Models

### Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                            AppUser                                   │
│  (Identity User)                                                     │
│  ├── Id (string)                                                     │
│  ├── Email                                                           │
│  ├── PhoneNumber                                                     │
│  └── Wallet (double?) ◄─── User's wallet balance                    │
└─────────────────────────────────────────────────────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         PaymentOrder                                 │
│  ├── Id (int, PK)                                                    │
│  ├── MerchantOrderId (string, unique)                               │
│  ├── PaymobOrderId (long)                                           │
│  ├── UserId (string, FK → AppUser)                                  │
│  ├── AmountCents (long)                                             │
│  ├── Currency (string, default: "EGP")                              │
│  ├── Status (string: Pending, Paid, Failed, Refunded)               │
│  ├── LastPaymentKey (string)                                        │
│  ├── PaymentKeyExpiresAt (DateTime)                                 │
│  ├── CreatedAt (DateTime)                                           │
│  └── UpdatedAt (DateTime)                                           │
└─────────────────────────────────────────────────────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      PaymentTransaction                              │
│  ├── Id (int, PK)                                                    │
│  ├── MerchantOrderId (string, FK → PaymentOrder)                    │
│  ├── PaymobOrderId (long)                                           │
│  ├── PaymobTransactionId (long)                                     │
│  ├── IntegrationType (string: Card, Wallet, CardToken)              │
│  ├── AmountCents (long)                                             │
│  ├── Currency (string)                                              │
│  ├── Status (string: Initiated, Pending, Paid, Failed)              │
│  ├── IsSuccess (bool)                                               │
│  ├── CapturedAmountCents (long)                                     │
│  ├── RefundedAmountCents (long)                                     │
│  ├── GatewayResponseCode (string)                                   │
│  ├── GatewayResponseMessage (string)                                │
│  ├── PaymentMethodMasked (string)                                   │
│  ├── CardBrand (string: Visa, Mastercard)                           │
│  ├── HmacVerified (bool)                                            │
│  ├── CreatedAt (DateTime)                                           │
│  └── UpdatedAt (DateTime)                                           │
└─────────────────────────────────────────────────────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        PaymentAction                                 │
│  (Refunds, Voids, Captures)                                          │
│  ├── Id (int, PK)                                                    │
│  ├── PaymobTransactionId (long, FK)                                 │
│  ├── ActionType (string: Refund, Void, Capture)                     │
│  ├── RequestedAmountCents (long)                                    │
│  ├── ProcessedAmountCents (long)                                    │
│  ├── Status (string: Requested, Processing, Completed)              │
│  ├── GatewayResponseCode (string)                                   │
│  ├── GatewayResponseMessage (string)                                │
│  ├── CreatedAt (DateTime)                                           │
│  └── UpdatedAt (DateTime)                                           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                        UserSavedCard                                 │
│  ├── Id (int, PK)                                                    │
│  ├── UserId (string, FK → AppUser)                                  │
│  ├── Token (string) ◄─── Paymob card token (PCI compliant)         │
│  ├── Last4 (string) ◄─── "4242"                                     │
│  ├── Brand (string) ◄─── "Visa", "Mastercard"                       │
│  ├── ExpiryMonth (int)                                              │
│  ├── ExpiryYear (int)                                               │
│  ├── PaymobTokenId (string)                                         │
│  ├── IsDefault (bool)                                               │
│  ├── MerchantId (long)                                              │
│  └── CreatedAt (DateTime)                                           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                         WebhookLog                                   │
│  (Audit trail for all webhooks)                                      │
│  ├── Id (int, PK)                                                    │
│  ├── EventType (string: TRANSACTION, CARD_TOKEN)                    │
│  ├── MerchantOrderId (string)                                       │
│  ├── PaymobOrderId (long)                                           │
│  ├── PaymobTransactionId (long)                                     │
│  ├── IsHmacValid (bool)                                             │
│  ├── HttpStatus (int)                                               │
│  ├── HeadersJson (string)                                           │
│  ├── RawPayload (string) ◄─── Full webhook body for debugging      │
│  ├── ReceivedAt (DateTime)                                          │
│  ├── IsValid (bool)                                                 │
│  └── MerchantId (long)                                              │
└─────────────────────────────────────────────────────────────────────┘
```

### Database Files

| Entity | File Path |
|--------|-----------|
| PaymentOrder | `Voltyks.Persistence/Entities/Main/Paymob/PaymentOrder.cs` |
| PaymentTransaction | `Voltyks.Persistence/Entities/Main/Paymob/PaymentTransaction.cs` |
| PaymentAction | `Voltyks.Persistence/Entities/Main/Paymob/PaymentAction.cs` |
| UserSavedCard | `Voltyks.Persistence/Entities/Main/Paymob/UserSavedCard.cs` |
| WebhookLog | `Voltyks.Persistence/Entities/Main/Paymob/WebhookLog.cs` |

### Order Statuses

| Status | Description |
|--------|-------------|
| `Pending` | Order created, awaiting payment |
| `OrderCreated` | Paymob order created |
| `Paid` | Payment successful |
| `Failed` | Payment failed |
| `Refunded` | Full/partial refund processed |
| `Voided` | Order cancelled/voided |

### Transaction Statuses

| Status | Description |
|--------|-------------|
| `Initiated` | Transaction started |
| `Pending` | Awaiting confirmation |
| `Paid` | Successfully completed |
| `Failed` | Transaction failed |

---

## Saved Cards System

### How Card Tokenization Works

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Card Tokenization Process                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. User clicks "Add New Card"                                       │
│       │                                                              │
│       ▼                                                              │
│  2. Client calls POST /api/payment/tokenization                      │
│       │                                                              │
│       ▼                                                              │
│  3. Server returns payment key + iframe URL                          │
│       │                                                              │
│       ▼                                                              │
│  4. Client opens Paymob iframe/SDK                                   │
│     ┌──────────────────────────────────────┐                        │
│     │  ┌────────────────────────────────┐  │                        │
│     │  │ Card Number: 4242424242424242  │  │                        │
│     │  │ Expiry: 12/27    CVV: 123      │  │                        │
│     │  │         [Save Card]            │  │                        │
│     │  └────────────────────────────────┘  │                        │
│     │         Paymob Secure Iframe         │                        │
│     └──────────────────────────────────────┘                        │
│       │                                                              │
│       ▼                                                              │
│  5. Paymob processes card (NO actual charge)                         │
│       │                                                              │
│       ▼                                                              │
│  6. Paymob sends CARD_TOKEN webhook to server                        │
│       │                                                              │
│       ▼                                                              │
│  7. Server saves card token to database:                             │
│     - Token: "tok_abc123..." (Paymob token)                         │
│     - Last4: "4242"                                                 │
│     - Brand: "Visa"                                                 │
│     - Expiry: 12/2027                                               │
│       │                                                              │
│       ▼                                                              │
│  8. User sees card in "My Cards" list                                │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Duplicate Card Detection

The system prevents saving the same card twice:

```csharp
// Unique combination check
UserId + Last4 + Brand + ExpiryMonth + ExpiryYear

// Example: If user already has:
// - Visa **** 4242 (12/27)
// Adding same card again will be skipped
```

### Charging with Saved Card

```
┌─────────────────────────────────────────────────────────────────────┐
│                   Saved Card Payment Flow                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. POST /api/payment/payWithSavedCard                               │
│     { "cardId": 1, "amountCents": 10000 }                           │
│       │                                                              │
│       ▼                                                              │
│  2. Server retrieves card from database                              │
│     SELECT * FROM UserSavedCards WHERE Id = 1 AND UserId = @userId  │
│       │                                                              │
│       ▼                                                              │
│  3. Create new PaymentOrder                                          │
│       │                                                              │
│       ▼                                                              │
│  4. Generate payment key for saved card token                        │
│       │                                                              │
│       ▼                                                              │
│  5. Call Paymob API with token                                       │
│     POST /acceptance/payments/pay                                    │
│     {                                                                │
│       "payment_token": "ZXlK...",                                   │
│       "source": {                                                   │
│         "identifier": "tok_abc123...",                              │
│         "subtype": "TOKEN"                                          │
│       }                                                              │
│     }                                                                │
│       │                                                              │
│       ▼                                                              │
│  6. Paymob charges the card                                          │
│       │                                                              │
│       ▼                                                              │
│  7. Return result to client                                          │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Wallet System

### User Wallet Balance

Each user has a wallet balance stored in `AppUser.Wallet`:

```csharp
public class AppUser : IdentityUser
{
    // ... other properties
    public double? Wallet { get; set; }  // Wallet balance in EGP
}
```

### Wallet Payment Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Mobile Wallet Payment                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. POST /api/payment/intention                                      │
│     {                                                                │
│       "amount": 10000,                                              │
│       "paymentMethod": "wallet",                                    │
│       "billingData": { "phone_number": "01XXXXXXXXX" }              │
│     }                                                                │
│       │                                                              │
│       ▼                                                              │
│  2. Server generates wallet payment key                              │
│     Integration ID: 5252502 (Wallet)                                │
│       │                                                              │
│       ▼                                                              │
│  3. Call Paymob wallet endpoint                                      │
│     POST /acceptance/payments/pay                                    │
│     {                                                                │
│       "payment_token": "...",                                       │
│       "source": {                                                   │
│         "identifier": "01XXXXXXXXX",  // Must start with 01         │
│         "subtype": "WALLET"                                         │
│       }                                                              │
│     }                                                                │
│       │                                                              │
│       ▼                                                              │
│  4. Paymob returns redirect URL                                      │
│     User is redirected to mobile wallet app                          │
│       │                                                              │
│       ▼                                                              │
│  5. User approves payment in wallet app                              │
│     (Vodafone Cash, Orange Money, etc.)                             │
│       │                                                              │
│       ▼                                                              │
│  6. Webhook notification received                                    │
│       │                                                              │
│       ▼                                                              │
│  7. Payment confirmed                                                │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Supported Mobile Wallets (Egypt)

| Wallet | Phone Prefix |
|--------|--------------|
| Vodafone Cash | 010 |
| Etisalat Cash | 011 |
| Orange Money | 012 |
| WE Pay | 015 |

---

## Webhook Processing

### Webhook Endpoint

```http
POST /api/payment/webhook
Content-Type: application/json (or application/x-www-form-urlencoded)
```

### Webhook Types

| Type | Description | Action |
|------|-------------|--------|
| `TRANSACTION` | Payment completed/failed | Update order & transaction status |
| `CARD_TOKEN` | Card tokenization complete | Save card to database |
| `VOID` | Transaction voided | Update status to Voided |
| `REFUND` | Refund processed | Update refund amount |

### Webhook Processing Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Webhook Processing                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. Receive POST from Paymob                                         │
│       │                                                              │
│       ▼                                                              │
│  2. Enable request buffering (for re-reading body)                   │
│       │                                                              │
│       ▼                                                              │
│  3. Verify HMAC signature                                            │
│     HMAC = SHA512(concatenated_params, HmacSecret)                  │
│       │                                                              │
│       ▼                                                              │
│  4. Parse webhook payload (JSON or Form)                             │
│       │                                                              │
│       ▼                                                              │
│  5. Log webhook to WebhookLog table                                  │
│       │                                                              │
│       ▼                                                              │
│  6. Process based on event type:                                     │
│     ├── TRANSACTION → Update PaymentOrder & PaymentTransaction       │
│     ├── CARD_TOKEN → Save to UserSavedCard                          │
│     └── REFUND/VOID → Update PaymentAction                          │
│       │                                                              │
│       ▼                                                              │
│  7. Return 200 OK (always, to acknowledge receipt)                   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### HMAC Verification

```csharp
// Paymob sends these fields for HMAC calculation:
var fields = new[]
{
    "amount_cents",
    "created_at",
    "currency",
    "error_occured",
    "has_parent_transaction",
    "id",
    "integration_id",
    "is_3d_secure",
    "is_auth",
    "is_capture",
    "is_refunded",
    "is_standalone_payment",
    "is_voided",
    "order.id",
    "owner",
    "pending",
    "source_data.pan",
    "source_data.sub_type",
    "source_data.type",
    "success"
};

// Concatenate values and calculate HMAC-SHA512
var hmac = HMACSHA512(concatenatedValues, HmacSecret);
// Compare with received hmac parameter
```

---

## DTOs Reference

### Request DTOs

| DTO | Purpose | File |
|-----|---------|------|
| `CardCheckoutRequest` | Card payment with billing | `CardCheckoutRequest.cs` |
| `WalletCheckoutRequest` | Wallet payment | `WalletCheckoutServiceDto.cs` |
| `ChargeWithSavedCardReq` | Saved card charge | `ChargeWithSavedCardReq.cs` |
| `SetDefaultCardRequestDto` | Set default card | `SetDefaultCardRequestDto.cs` |
| `DeleteCardRequestDto` | Delete card | `DeleteCardRequestDto.cs` |
| `PaymobOrderRequestDto` | Get order status | `PaymobOrderRequestDto.cs` |

### Response DTOs

| DTO | Purpose | File |
|-----|---------|------|
| `CardCheckoutResponse` | Payment key + iframe | `CardCheckoutResponse.cs` |
| `CreateIntentResponse` | Intention client secret | `CreateIntentResponse.cs` |
| `OrderStatusDto` | Order status details | `OrderStatusDto.cs` |
| `SavedCardViewDto` | Card display info | `SavedCardViewDto.cs` |
| `SavedCardPaymentResponse` | Saved card charge result | `SavedCardPaymentResponse.cs` |

### Billing Data Structure

```csharp
public class BillingDataDto
{
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public string phone_number { get; set; }
    public string country { get; set; }      // "EG"
    public string city { get; set; }
    public string street { get; set; }
    public string building { get; set; }
    public string floor { get; set; }
    public string apartment { get; set; }
}
```

---

## Configuration

### PaymobOptions Class

**File**: `Voltyks.Core/DTOs/Paymob/Options/PaymobOptions.cs`

```csharp
public class PaymobOptions
{
    public string ApiBase { get; set; }           // API base URL
    public string ApiKey { get; set; }            // Auth API key
    public string HmacSecret { get; set; }        // Webhook signature
    public string SecretKey { get; set; }         // Intention API secret
    public string IframeId { get; set; }          // Embed iframe ID
    public string Currency { get; set; }          // Default: "EGP"
    public IntegrationIds Integration { get; set; }
    public string PublicKey { get; set; }         // Client SDK key
    public IntentionOptions Intention { get; set; }
}

public class IntegrationIds
{
    public int Card { get; set; }                 // Card integration ID
    public int Wallet { get; set; }               // Wallet integration ID
}
```

### Dependency Injection Setup

```csharp
// In Program.cs or Startup.cs
services.Configure<PaymobOptions>(configuration.GetSection("Paymob"));
services.AddScoped<IPaymobService, PaymobService>();
services.AddSingleton<IPaymobAuthTokenProvider, PaymobAuthTokenProviderRedis>();
```

---

## Security

### PCI DSS Compliance

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PCI DSS Compliance                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ✅ Card numbers NEVER touch our servers                            │
│     - Card entry via Paymob iframe/SDK only                         │
│     - We only store tokens (not actual card data)                   │
│                                                                      │
│  ✅ CVV is never stored                                              │
│     - CVV entered directly in Paymob form                           │
│     - Not transmitted to our backend                                │
│                                                                      │
│  ✅ Tokenization                                                     │
│     - Card data replaced with secure tokens                         │
│     - Tokens useless without our merchant credentials               │
│                                                                      │
│  ✅ Masked display                                                   │
│     - Only last 4 digits shown: **** **** **** 4242                 │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Security Measures

| Measure | Implementation |
|---------|----------------|
| **Authentication** | JWT Bearer tokens on all payment endpoints |
| **Webhook Verification** | HMAC-SHA512 signature validation |
| **Token Storage** | Paymob tokens only, no raw card data |
| **User Isolation** | Cards filtered by authenticated UserId |
| **HTTPS Only** | All API calls over TLS |
| **Concurrency Control** | Semaphore locks prevent race conditions |

### Authorization

```csharp
[Authorize]  // All payment endpoints require authentication
public class PaymentController : ControllerBase
{
    [AllowAnonymous]  // Exception: webhook must be accessible by Paymob
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook() { ... }
}
```

---

## Testing

### Test Cards (Paymob Sandbox)

| Card Number | Brand | Result |
|-------------|-------|--------|
| `4242424242424242` | Visa | Success |
| `5123456789012346` | Mastercard | Success |
| `4000000000000002` | Visa | Declined |

### Test Credentials

```
CVV: Any 3 digits (e.g., 123)
Expiry: Any future date (e.g., 12/27)
```

### Testing with cURL

```bash
# 1. Login to get token
curl -X POST "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"EmailOrPhone": "Admin@gmail.com", "password": "Voltyks1041998@"}'

# 2. Create payment intention
curl -X POST "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/payment/intention" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 10000,
    "paymentMethod": "card",
    "billingData": {
      "first_name": "Test",
      "last_name": "User",
      "email": "test@example.com",
      "phone_number": "+201111111111"
    }
  }'

# 3. List saved cards
curl -X GET "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/payment/GetListOfCards" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## File Structure

```
Voltyks.API/
├── Controllers/
│   └── PaymentController.cs              # API endpoints
│
├── Voltyks.Application/
│   ├── Interfaces/Paymob/
│   │   ├── IPaymobService.cs             # Service interface
│   │   └── IPaymobAuthTokenProvider.cs   # Token provider interface
│   │
│   └── Services/Paymob/
│       ├── PaymobService.cs              # Main payment logic
│       └── PaymobAuthTokenProviderRedis.cs  # Token caching
│
├── Voltyks.Core/
│   └── DTOs/Paymob/
│       ├── Options/
│       │   └── PaymobOptions.cs          # Configuration model
│       ├── Requests/
│       │   ├── CardCheckoutRequest.cs
│       │   ├── ChargeWithSavedCardReq.cs
│       │   └── ...
│       └── Responses/
│           ├── CardCheckoutResponse.cs
│           ├── CreateIntentResponse.cs
│           └── ...
│
└── Voltyks.Persistence/
    ├── Entities/Main/Paymob/
    │   ├── PaymentOrder.cs
    │   ├── PaymentTransaction.cs
    │   ├── PaymentAction.cs
    │   ├── UserSavedCard.cs
    │   └── WebhookLog.cs
    │
    └── Data/Configurations/
        ├── PaymentOrderConfiguration.cs
        ├── PaymentTransactionConfigrations.cs
        └── UserSavedCardConfiguration.cs
```

---

*Last Updated: December 9, 2025*
