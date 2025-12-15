# Voltyks Payment System - Frontend Integration Guide

## Base URL
```
https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net
```

## Authentication
All endpoints (except webhook) require JWT Bearer token in header:
```
Authorization: Bearer <your_jwt_token>
```

---

## Payment Flows

### Flow 1: New Card Payment (Intention API)

This is the main flow for paying with a new card using Paymob SDK.

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Mobile    │      │   Backend   │      │   Paymob    │      │   Webhook   │
│     App     │      │     API     │      │     SDK     │      │   Handler   │
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │                    │
       │ 1. POST /intention │                    │                    │
       │───────────────────>│                    │                    │
       │                    │                    │                    │
       │ 2. client_secret   │                    │                    │
       │<───────────────────│                    │                    │
       │                    │                    │                    │
       │ 3. Open Paymob SDK │                    │                    │
       │    with client_secret                   │                    │
       │────────────────────────────────────────>│                    │
       │                    │                    │                    │
       │ 4. User enters card details             │                    │
       │    & completes payment                  │                    │
       │<────────────────────────────────────────│                    │
       │                    │                    │                    │
       │                    │ 5. Webhook (TRANSACTION)                │
       │                    │<────────────────────────────────────────│
       │                    │                    │                    │
       │ 6. Check order status                   │                    │
       │───────────────────>│                    │                    │
       │                    │                    │                    │
       │ 7. Payment result  │                    │                    │
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
| amountCents | number | Yes | Amount in cents (100 = 1 EGP) |
| billing.first_name | string | Yes | Customer first name |
| billing.last_name | string | Yes | Customer last name |
| billing.email | string | Yes | Customer email |
| billing.phone_number | string | Yes | Customer phone (Egyptian format) |
| saveCard | boolean | No | Save card for future payments |
| paymentMethod | string | No | "Card" or "Wallet" (default: Card) |

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

Use the `clientSecret` and `publicKey` from the response to initialize Paymob SDK.

**Flutter Example:**
```dart
import 'package:paymob_payment/paymob_payment.dart';

PaymobPayment.instance.initialize(
  clientSecret: response.data.clientSecret,
  publicKey: response.data.publicKey,
);

final result = await PaymobPayment.instance.pay();
if (result.success) {
  // Payment successful
} else {
  // Payment failed
}
```

**React Native Example:**
```javascript
import { PaymobSDK } from 'paymob-react-native';

const paymentResult = await PaymobSDK.pay({
  clientSecret: response.data.clientSecret,
  publicKey: response.data.publicKey,
});
```

#### Step 3: Check Payment Status

After SDK closes, verify payment status:

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
    "merchantOrderId": "abc123",
    "orderStatus": "paid",
    "transactionStatus": "Paid",
    "isSuccess": true,
    "amountCents": 10000,
    "currency": "EGP",
    "paymobOrderId": 123456789,
    "paymobTransactionId": 987654321,
    "checkedAt": "2025-12-15T14:30:00"
  }
}
```

---

### Flow 2: Pay with Saved Card

For returning customers who have saved cards.

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Mobile    │      │   Backend   │      │   Paymob    │
│     App     │      │     API     │      │             │
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │
       │ 1. GET /GetListOfCards                  │
       │───────────────────>│                    │
       │                    │                    │
       │ 2. List of cards   │                    │
       │<───────────────────│                    │
       │                    │                    │
       │ 3. User selects card                    │
       │                    │                    │
       │ 4. POST /payWithSavedCard               │
       │───────────────────>│                    │
       │                    │ 5. Charge card     │
       │                    │───────────────────>│
       │                    │                    │
       │                    │ 6. Result          │
       │                    │<───────────────────│
       │ 7. Payment result  │                    │
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

**Request:** (no body required)

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

### Set Default Card

**Endpoint:** `POST /api/payment/setDefault_Card`

**Request:**
```json
{
  "cardId": 1
}
```

**Response:**
```json
{
  "status": true,
  "message": "Default set",
  "data": true
}
```

### Delete Card

**Endpoint:** `DELETE /api/payment/delete_Card`

**Request:**
```json
{
  "cardId": 1
}
```

**Response:**
```json
{
  "status": true,
  "message": "Deleted",
  "data": true
}
```

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

### Common Error Codes

| HTTP Status | Meaning |
|-------------|---------|
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

## Webhook Events (Backend Handles Automatically)

The backend automatically handles these webhook events from Paymob:

| Event Type | Description |
|------------|-------------|
| `TRANSACTION` | Payment completed/failed - updates order status |
| `CARD_TOKEN` | Card saved - stores card for future use |

**Note:** Frontend doesn't need to handle webhooks. Just check order status after SDK closes.

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

  // Pay with new card
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

  // Get saved cards
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

  // Pay with saved card
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
| 4000000000000002 | Any future date | 123 | Declined |

### Test Flow
1. Use sandbox credentials in development
2. Switch to live credentials for production
3. Always verify payment status after SDK closes

---

## Support

For API issues, contact backend team.
For Paymob SDK issues, refer to [Paymob Documentation](https://docs.paymob.com).
