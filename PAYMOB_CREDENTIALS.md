# Paymob Payment Gateway Credentials

This document contains all Paymob credentials for both **TEST** and **LIVE** modes.

---

## Quick Reference Table

| Field | TEST Mode | LIVE Mode |
|-------|-----------|-----------|
| **ENV** | `test` | `live` |
| **SecretKey** | `egy_sk_test_65e4f2e04040c3729f9fa222a669e32662fdcb2614b8c278baf701b136ce7c80` | `egy_sk_live_6a3cd80006bc05db2ebc670f1176ebdb1d27b5d1aa4c7a3c488662310b709475` |
| **PublicKey** | `egy_pk_test_vsxZm3qA18nLhCjcEnIOGDdP92kBLDWa` | `egy_pk_live_rsNEP90gJW81yOPUm2MtkZPgb7hcvq6w` |
| **Integration Card** | `5229585` | `5413127` |
| **Integration Wallet** | `5252502` | `5413126` |

---

## Shared Credentials (Same for both modes)

| Field | Value |
|-------|-------|
| **ApiBase** | `https://accept.paymob.com/api` |
| **ApiKey** | `ZXlKaGJHY2lPaUpJVXpVeE1pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SmpiR0Z6Y3lJNklrMWxjbU5vWVc1MElpd2ljSEp2Wm1sc1pWOXdheUk2TVRBMk5qa3dNeXdpYm1GdFpTSTZJbWx1YVhScFlXd2lmUS41b1lTSjduQ3hUUWs1RkNiaUwwZ1F3VkFsWkFKWVYta0pCRFNfTWFlS3NReVI0M1lCUmZSZ0NOSGltZ1VaNjc4OTFxVjZ6SzVhRnpRWnI0alI5NVgwZw==` |
| **HmacSecret** | `C2DF5ABCCDACBD10B7CAF4ED98AF8770` |
| **IFrameId** | `947450` |
| **Currency** | `EGP` |
| **Intention URL** | `https://accept.paymob.com/v1/intention/` |

---

## Full appsettings.json Configuration

### TEST Mode Configuration
```json
"Paymob": {
  "ENV": "test",
  "ApiBase": "https://accept.paymob.com/api",
  "ApiKey": "ZXlKaGJHY2lPaUpJVXpVeE1pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SmpiR0Z6Y3lJNklrMWxjbU5vWVc1MElpd2ljSEp2Wm1sc1pWOXdheUk2TVRBMk5qa3dNeXdpYm1GdFpTSTZJbWx1YVhScFlXd2lmUS41b1lTSjduQ3hUUWs1RkNiaUwwZ1F3VkFsWkFKWVYta0pCRFNfTWFlS3NReVI0M1lCUmZSZ0NOSGltZ1VaNjc4OTFxVjZ6SzVhRnpRWnI0alI5NVgwZw==",
  "SecretKey": "egy_sk_test_65e4f2e04040c3729f9fa222a669e32662fdcb2614b8c278baf701b136ce7c80",
  "PublicKey": "egy_pk_test_vsxZm3qA18nLhCjcEnIOGDdP92kBLDWa",
  "HmacSecret": "C2DF5ABCCDACBD10B7CAF4ED98AF8770",
  "IFrameId": "947450",
  "Integration": {
    "Card": 5229585,
    "Wallet": 5252502
  },
  "Currency": "EGP",
  "Intention": {
    "Url": "https://accept.paymob.com/v1/intention/",
    "Path": ""
  }
}
```

### LIVE Mode Configuration
```json
"Paymob": {
  "ENV": "live",
  "ApiBase": "https://accept.paymob.com/api",
  "ApiKey": "ZXlKaGJHY2lPaUpJVXpVeE1pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SmpiR0Z6Y3lJNklrMWxjbU5vWVc1MElpd2ljSEp2Wm1sc1pWOXdheUk2TVRBMk5qa3dNeXdpYm1GdFpTSTZJbWx1YVhScFlXd2lmUS41b1lTSjduQ3hUUWs1RkNiaUwwZ1F3VkFsWkFKWVYta0pCRFNfTWFlS3NReVI0M1lCUmZSZ0NOSGltZ1VaNjc4OTFxVjZ6SzVhRnpRWnI0alI5NVgwZw==",
  "SecretKey": "egy_sk_live_6a3cd80006bc05db2ebc670f1176ebdb1d27b5d1aa4c7a3c488662310b709475",
  "PublicKey": "egy_pk_live_rsNEP90gJW81yOPUm2MtkZPgb7hcvq6w",
  "HmacSecret": "C2DF5ABCCDACBD10B7CAF4ED98AF8770",
  "IFrameId": "947450",
  "Integration": {
    "Card": 5413127,
    "Wallet": 5413126
  },
  "Currency": "EGP",
  "Intention": {
    "Url": "https://accept.paymob.com/v1/intention/",
    "Path": ""
  }
}
```

---

## How to Switch Modes

To switch between TEST and LIVE modes, update these 5 fields in `appsettings.json`:

1. **ENV**: Change to `"test"` or `"live"`
2. **SecretKey**: Use the corresponding secret key
3. **PublicKey**: Use the corresponding public key
4. **Integration.Card**: Use the corresponding card integration ID
5. **Integration.Wallet**: Use the corresponding wallet integration ID

---

## Test Card Numbers (for TEST mode)

| Card Type | Number | Expiry | CVV |
|-----------|--------|--------|-----|
| Visa (Success) | `4987654321098769` | Any future date | Any 3 digits |
| Mastercard (Success) | `5123456789012346` | Any future date | Any 3 digits |
| Visa (Declined) | `4111111111111111` | Any future date | Any 3 digits |

---

## Important Notes

- **Current Mode**: The application is currently in **LIVE** mode
- **Webhook URL**: `https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/payment/webhook`
- **Config File**: `Voltyks.API/appsettings.json`

---

## Security Warning

This file contains sensitive credentials. Ensure it is:
- Added to `.gitignore` if not already
- Not shared publicly
- Stored securely

---

*Last Updated: December 2025*
