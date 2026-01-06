# Voltyks Payment System - Complete Integration Guide

> **Last Updated:** 2025-12-30
> **Version:** 2.0
> **Mode:** LIVE (Production)

---

## Table of Contents
1. [Configuration](#configuration)
2. [Payment Flows](#payment-flows)
   - [Flow 1: New Card Payment](#flow-1-new-card-payment-intention-api)
   - [Flow 2: Pay with Saved Card](#flow-2-pay-with-saved-card)
   - [Flow 3: Card Tokenization](#flow-3-card-tokenization-save-card-only)
3. [Card Management](#card-management-endpoints)
4. [Webhook System](#webhook-system)
5. [Error Handling](#error-handling)
6. [Code Examples](#complete-flutter-integration-example)
7. [Testing](#testing)

---

## Configuration

### Base URL
```
https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net
```

### Authentication
All endpoints (except webhook) require JWT Bearer token:
```
Authorization: Bearer <your_jwt_token>
```

### Paymob Credentials (LIVE)
| Key | Value |
|-----|-------|
| **PublicKey** | `egy_pk_live_rsNEP90gJW81yOPUm2MtkZPgb7hcvq6w` |
| **Integration Card** | `5413127` |
| **Integration Wallet** | `5413126` |
| **Currency** | `EGP` |

---

## Payment Flows

### Flow 1: New Card Payment (Intention API)

Main flow for paying with a new card using Paymob SDK.

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Mobile    │      │   Backend   │      │   Paymob    │      │   Webhook   │
│     App     │      │     API     │      │     SDK     │      │   Handler   │
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │                    │
       │ 1. POST /intention │                    │                    │
       │───────────────────>│                    │                    │
       │                    │ 2. Create Paymob   │                    │
       │                    │    Intention       │                    │
       │                    │───────────────────>│                    │
       │                    │                    │                    │
       │ 3. clientSecret +  │                    │                    │
       │    publicKey       │                    │                    │
       │<───────────────────│                    │                    │
       │                    │                    │                    │
       │ 4. Open Paymob SDK with clientSecret    │                    │
       │────────────────────────────────────────>│                    │
       │                    │                    │                    │
       │ 5. User enters card & completes payment │                    │
       │<────────────────────────────────────────│                    │
       │                    │                    │                    │
       │                    │ 6. Webhook (TRANSACTION)                │
       │                    │<────────────────────────────────────────│
       │                    │                    │                    │
       │                    │ 7. Webhook (CARD_TOKEN) - if saveCard   │
       │                    │<────────────────────────────────────────│
       │                    │                    │                    │
       │ 8. POST /getOrderStatus                 │                    │
       │───────────────────>│                    │                    │
       │                    │                    │                    │
       │ 9. Payment result  │                    │                    │
       │<───────────────────│                    │                    │
```

#### Step 1: Create Payment Intention

**Endpoint:** `POST /api/payment/intention`

**Request:**
```json
{
  "amountCents": 10000,
  "billing": {
    "first_name": "Ahmed",
    "last_name": "Salem",
    "email": "ahmed@example.com",
    "phone_number": "01012345678"
  },
  "saveCard": true,
  "paymentMethod": "Card"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `amountCents` | number | Yes | Amount in cents (100 = 1 EGP) |
| `billing.first_name` | string | Yes | Customer first name |
| `billing.last_name` | string | Yes | Customer last name |
| `billing.email` | string | Yes | Customer email |
| `billing.phone_number` | string | Yes | Customer phone (Egyptian format) |
| `saveCard` | boolean | No | Save card for future payments (default: false) |
| `paymentMethod` | string | No | "Card" or "Wallet" (default: Card) |

**Response:**
```json
{
  "status": true,
  "message": "Intention created successfully.",
  "data": {
    "clientSecret": "ZXlKaGJHY2lPaUpJVX...",
    "publicKey": "egy_pk_live_rsNEP90gJW81yOPUm2MtkZPgb7hcvq6w",
    "intentionId": "pi_xxxxxxxx",
    "intentionOrderId": 123456789,
    "redirectionUrl": null,
    "status": "pending",
    "paymentKeys": ["payment_key_here"]
  }
}
```

#### Step 2: Open Paymob SDK

Use `clientSecret` and `publicKey` to initialize Paymob SDK.

**Flutter:**
```dart
import 'package:paymob_payment/paymob_payment.dart';

PaymobPayment.instance.initialize(
  clientSecret: response.data.clientSecret,
  publicKey: response.data.publicKey,
);

final result = await PaymobPayment.instance.pay();
```

**React Native:**
```javascript
import { PaymobSDK } from 'paymob-react-native';

const paymentResult = await PaymobSDK.pay({
  clientSecret: response.data.clientSecret,
  publicKey: response.data.publicKey,
});
```

#### Step 3: Verify Payment Status

**Endpoint:** `POST /api/payment/getOrderStatus`

**Request:**
```json
{
  "paymobOrderId": 123456789
}
```

**Response:**
```json
{
  "status": true,
  "message": "Order status fetched from Paymob",
  "data": {
    "merchantOrderId": "uid:user123|ord:abc123",
    "orderStatus": "paid",
    "transactionStatus": "Paid",
    "isSuccess": true,
    "amountCents": 10000,
    "currency": "EGP",
    "paymobOrderId": 123456789,
    "paymobTransactionId": 987654321,
    "checkedAt": "2025-12-30T14:30:00"
  }
}
```

---

### Flow 2: Pay with Saved Card

For returning customers with saved cards.

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Mobile    │      │   Backend   │      │   Paymob    │
│     App     │      │     API     │      │             │
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │
       │ 1. GET /GetListOfCards                  │
       │───────────────────>│                    │
       │                    │                    │
       │ 2. List of saved cards                  │
       │<───────────────────│                    │
       │                    │                    │
       │ 3. User selects card                    │
       │                    │                    │
       │ 4. POST /payWithSavedCard               │
       │───────────────────>│                    │
       │                    │ 5. Token payment   │
       │                    │───────────────────>│
       │                    │                    │
       │                    │ 6. Payment result  │
       │                    │<───────────────────│
       │ 7. Response        │                    │
       │<───────────────────│                    │
```

#### Step 1: Get Saved Cards

**Endpoint:** `GET /api/payment/GetListOfCards`

**Response:**
```json
{
  "status": true,
  "message": "OK",
  "data": [
    {
      "id": 1,
      "brand": "visa",
      "last4": "4242",
      "expiryMonth": 12,
      "expiryYear": 2027,
      "isDefault": true
    },
    {
      "id": 2,
      "brand": "mastercard",
      "last4": "8888",
      "expiryMonth": 6,
      "expiryYear": 2026,
      "isDefault": false
    }
  ]
}
```

#### Step 2: Pay with Selected Card

**Endpoint:** `POST /api/payment/payWithSavedCard`

**Request:**
```json
{
  "cardId": 1,
  "amountCents": 10000
}
```

**Response:**
```json
{
  "status": true,
  "message": "Initiated",
  "data": {
    "merchantOrderId": "uid:user123|mid:12345|ord:abc123",
    "paymobOrderId": 123456789,
    "paymentKey": "ZXlKaGJHY2lPa...",
    "usedMerchantId": 12345,
    "pay_status": true,
    "pay_message": "Token payment initiated",
    "pay_data": { ... }
  }
}
```

---

### Flow 3: Card Tokenization (Save Card Only)

To save a card without making a payment.

**Endpoint:** `POST /api/payment/tokenization`

**Request:** No body required

**Response:**
```json
{
  "status": true,
  "message": "Tokenization started",
  "data": {
    "merchantOrderId": "abc123",
    "paymentKey": "ZXlKaGJHY2lPa..."
  }
}
```

Then open Paymob SDK with the payment key to let user enter card details.

---

## Card Management Endpoints

### Get All Saved Cards
**Endpoint:** `GET /api/payment/GetListOfCards`

### Set Default Card
**Endpoint:** `POST /api/payment/setDefault_Card`

```json
{ "cardId": 1 }
```

### Delete Card
**Endpoint:** `DELETE /api/payment/delete_Card`

```json
{ "cardId": 1 }
```

---

## Webhook System

### Overview

The backend automatically handles webhooks from Paymob at:
```
POST /api/payment/webhook
```

### Webhook Types

| Event Type | Description | HMAC Method |
|------------|-------------|-------------|
| `TRANSACTION` | Payment completed/failed | `BuildTransactionConcat` - specific fields order |
| `CARD_TOKEN` | Card saved for future use | `BuildTokenConcat` - alphabetically sorted values |

### Webhook Processing Flow

```
1. Webhook arrives at /api/payment/webhook
       ↓
2. Parse payload fields (query + body)
       ↓
3. Detect event type:
   - Has token keys → CARD_TOKEN
   - Has transaction id → TRANSACTION
       ↓
4. Verify HMAC signature with correct method
       ↓
5. Process based on type:
   - TRANSACTION → Update PaymentOrder status
   - CARD_TOKEN → Save card to UserSavedCard table
       ↓
6. Return HTTP 200 OK (always)
```

### CARD_TOKEN Webhook Processing

When `saveCard: true` and payment succeeds, Paymob sends CARD_TOKEN webhook:

```
1. Generate unique webhookId for idempotency
       ↓
2. Check if already processed (prevent duplicates)
       ↓
3. Validate HMAC signature
       ↓
4. Extract card token from:
   - obj.token
   - obj.saved_card_token
   - obj.card_token
   - obj.source_data.token
       ↓
5. Extract card details:
   - Last4: obj.masked_pan or obj.source_data.pan
   - Brand: obj.card_subtype or obj.source_data.type
   - Expiry: obj.expiry_month, obj.expiry_year
       ↓
6. Resolve userId from:
   - obj.metadata.user_id
   - merchant_order_id (uid:xxx pattern)
   - PaymentOrder lookup by paymobOrderId
       ↓
7. Save card to UserSavedCard table
       ↓
8. Log to CardTokenWebhookLog table
```

### Webhook Response Messages

| Message | Meaning |
|---------|---------|
| `Ignored (bad signature)` | HMAC verification failed |
| `Event acknowledged` | Unknown event type |
| `Transaction Paid` | Payment successful |
| `Card saved` | Card tokenization successful |
| `No token - logged` | No card token in payload |
| `No user - logged` | Could not resolve user ID |
| `Duplicate - logged` | Card already exists for user |
| `Already processed: {status}` | Duplicate webhook (idempotency) |

---

## Error Handling

### Error Response Format
```json
{
  "status": false,
  "message": "Error description",
  "data": null,
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

### HTTP Status Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Invalid/expired token |
| 502 | Bad Gateway - Paymob API error |

### Payment Status Values

| Status | Description |
|--------|-------------|
| `Pending` | Payment initiated, waiting for completion |
| `Paid` | Payment successful |
| `Failed` | Payment failed |
| `Voided` | Payment was voided |
| `Refunded` | Payment was refunded |

---

## Complete Flutter Integration Example

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:paymob_payment/paymob_payment.dart';

class PaymentService {
  static const String baseUrl = 'https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net';
  final String authToken;

  PaymentService(this.authToken);

  /// Pay with new card
  Future<PaymentResult> payWithNewCard({
    required int amountCents,
    required String firstName,
    required String lastName,
    required String email,
    required String phone,
    bool saveCard = false,
  }) async {
    // 1. Create intention
    final intentionResponse = await http.post(
      Uri.parse('$baseUrl/api/payment/intention'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $authToken',
      },
      body: jsonEncode({
        'amountCents': amountCents,
        'billing': {
          'first_name': firstName,
          'last_name': lastName,
          'email': email,
          'phone_number': phone,
        },
        'saveCard': saveCard,
        'paymentMethod': 'Card',
      }),
    );

    final intentionData = jsonDecode(intentionResponse.body);
    if (!intentionData['status']) {
      return PaymentResult(success: false, message: intentionData['message']);
    }

    // 2. Open Paymob SDK
    PaymobPayment.instance.initialize(
      clientSecret: intentionData['data']['clientSecret'],
      publicKey: intentionData['data']['publicKey'],
    );

    final sdkResult = await PaymobPayment.instance.pay();

    // 3. Verify payment status
    final statusResponse = await http.post(
      Uri.parse('$baseUrl/api/payment/getOrderStatus'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $authToken',
      },
      body: jsonEncode({
        'paymobOrderId': intentionData['data']['intentionOrderId'],
      }),
    );

    final statusData = jsonDecode(statusResponse.body);
    return PaymentResult(
      success: statusData['data']['isSuccess'] ?? false,
      message: statusData['data']['orderStatus'] ?? 'Unknown',
      orderId: statusData['data']['merchantOrderId'],
    );
  }

  /// Get saved cards
  Future<List<SavedCard>> getSavedCards() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/payment/GetListOfCards'),
      headers: {'Authorization': 'Bearer $authToken'},
    );

    final data = jsonDecode(response.body);
    if (!data['status']) return [];

    return (data['data'] as List)
        .map((card) => SavedCard.fromJson(card))
        .toList();
  }

  /// Pay with saved card
  Future<PaymentResult> payWithSavedCard({
    required int cardId,
    required int amountCents,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/payment/payWithSavedCard'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $authToken',
      },
      body: jsonEncode({
        'cardId': cardId,
        'amountCents': amountCents,
      }),
    );

    final data = jsonDecode(response.body);
    return PaymentResult(
      success: data['data']['pay_status'] ?? false,
      message: data['message'],
      orderId: data['data']['merchantOrderId'],
    );
  }

  /// Set default card
  Future<bool> setDefaultCard(int cardId) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/payment/setDefault_Card'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $authToken',
      },
      body: jsonEncode({'cardId': cardId}),
    );
    return jsonDecode(response.body)['status'] ?? false;
  }

  /// Delete card
  Future<bool> deleteCard(int cardId) async {
    final response = await http.delete(
      Uri.parse('$baseUrl/api/payment/delete_Card'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $authToken',
      },
      body: jsonEncode({'cardId': cardId}),
    );
    return jsonDecode(response.body)['status'] ?? false;
  }
}

class PaymentResult {
  final bool success;
  final String message;
  final String? orderId;

  PaymentResult({required this.success, required this.message, this.orderId});
}

class SavedCard {
  final int id;
  final String brand;
  final String last4;
  final int expiryMonth;
  final int expiryYear;
  final bool isDefault;

  SavedCard({
    required this.id,
    required this.brand,
    required this.last4,
    required this.expiryMonth,
    required this.expiryYear,
    required this.isDefault,
  });

  factory SavedCard.fromJson(Map<String, dynamic> json) => SavedCard(
    id: json['id'],
    brand: json['brand'] ?? '',
    last4: json['last4'] ?? '',
    expiryMonth: json['expiryMonth'] ?? 0,
    expiryYear: json['expiryYear'] ?? 0,
    isDefault: json['isDefault'] ?? false,
  );
}
```

---

## Testing

### Test Cards (Paymob Sandbox)

| Card Number | Expiry | CVV | Result |
|-------------|--------|-----|--------|
| 4987654321098769 | Any future date | 123 | Success |
| 5123456789012346 | Any future date | 123 | Success (Mastercard) |
| 4000000000000002 | Any future date | 123 | Declined |

### Test Flow
1. Use sandbox credentials in development
2. Switch to live credentials for production
3. Always verify payment status after SDK closes
4. Check saved cards after payment with `saveCard: true`

---

## API Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/payment/intention` | Create payment intention |
| `POST` | `/api/payment/getOrderStatus` | Check payment status |
| `POST` | `/api/payment/tokenization` | Start card tokenization |
| `GET` | `/api/payment/GetListOfCards` | Get user's saved cards |
| `POST` | `/api/payment/setDefault_Card` | Set default card |
| `DELETE` | `/api/payment/delete_Card` | Delete saved card |
| `POST` | `/api/payment/payWithSavedCard` | Pay with saved card |
| `POST` | `/api/payment/webhook` | Paymob webhook endpoint |

---

## Changelog

### v2.0 (2025-12-30)
- Fixed HMAC verification for CARD_TOKEN webhooks
- Removed `notification_url` from intention request (uses Paymob dashboard config)
- Added detailed webhook processing documentation
- Updated to LIVE mode credentials

### v1.0 (2025-12-25)
- Initial payment system implementation
- Intention API, saved cards, tokenization

---

## Support

- **API Issues:** Contact backend team
- **Paymob SDK Issues:** [Paymob Documentation](https://docs.paymob.com)
- **Webhook URL:** `https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/payment/webhook`
