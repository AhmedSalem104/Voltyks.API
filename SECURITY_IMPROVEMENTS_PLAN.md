# Security Improvements Plan - Voltyks.API

## Overview

This document outlines the security improvements implemented in the Voltyks.API project to enhance data validation, error handling, and overall API security.

---

## 1. DTO Validation Improvements

### 1.1 LoginDTO
**File:** `Voltyks.Core/DTOs/AuthDTOs/LoginDTO.cs`

| Property | Validation | Description |
|----------|------------|-------------|
| `EmailOrPhone` | `[Required]` | Must not be empty |
| `Password` | `[Required]`, `[MinLength(6)]` | Must be at least 6 characters |

### 1.2 SendChargingRequestDto
**File:** `Voltyks.Core/DTOs/ChargerRequest/SendChargingRequestDto.cs`

| Property | Validation | Description |
|----------|------------|-------------|
| `ChargerId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `KwNeeded` | `[Required]`, `[Range(0.1, 1000.0)]` | Must be between 0.1 and 1000 |
| `CurrentBatteryPercentage` | `[Required]`, `[Range(0, 100)]` | Must be 0-100% |
| `Latitude` | `[Required]`, `[Range(-90.0, 90.0)]` | Valid latitude range |
| `Longitude` | `[Required]`, `[Range(-180.0, 180.0)]` | Valid longitude range |

### 1.3 AddChargerDto / UpdateChargerDto
**Files:**
- `Voltyks.Core/DTOs/Charger/AddChargerDto.cs`
- `Voltyks.Core/DTOs/Charger/UpdateChargerDto.cs`

| Property | Validation | Description |
|----------|------------|-------------|
| `ProtocolId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `CapacityId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `PriceOptionId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `Area` | `[StringLength(100)]` | Max 100 characters |
| `Street` | `[StringLength(200)]` | Max 200 characters |
| `BuildingNumber` | `[StringLength(50)]` | Max 50 characters |
| `Latitude` | `[Required]`, `[Range(-90.0, 90.0)]` | Valid latitude range |
| `Longitude` | `[Required]`, `[Range(-180.0, 180.0)]` | Valid longitude range |

### 1.4 CreateAndUpdateVehicleDto
**File:** `Voltyks.Core/DTOs/VehicleDTOs/CreateVehicleDto.cs`

| Property | Validation | Description |
|----------|------------|-------------|
| `Color` | `[Required]`, `[StringLength(50)]` | Max 50 characters |
| `Plate` | `[Required]`, `[StringLength(20, MinimumLength=2)]` | 2-20 characters |
| `BrandId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `ModelId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `Year` | `[Required]`, `[Range(1900, 2100)]` | Valid year range |

### 1.5 SubmitRatingDto
**File:** `Voltyks.Core/DTOs/Process/SubmitRatingDto.cs`

| Property | Validation | Description |
|----------|------------|-------------|
| `ProcessId` | `[Required]`, `[Range(1, int.MaxValue)]` | Must be positive integer |
| `RatingForOther` | `[Required]`, `[Range(1.0, 5.0)]` | Rating must be 1-5 |

---

## 2. Error Handling Improvements

### 2.1 AuthService - Nominatim API
**File:** `Voltyks.Application/Services/Auth/AuthService.cs`
**Method:** `GetAddressFromLatLongNominatimAsync`

```csharp
try
{
    // Nominatim API call with 10-second timeout
    client.Timeout = TimeSpan.FromSeconds(10);
    // ... existing logic ...
}
catch (HttpRequestException) { return ("N/A", "N/A"); }
catch (TaskCanceledException) { return ("N/A", "N/A"); }
catch (Exception) { return ("N/A", "N/A"); }
```

**Benefits:**
- Prevents API crashes from external service failures
- Graceful degradation with fallback values
- 10-second timeout prevents hanging requests

### 2.2 ChargingRequestService - Nominatim API
**File:** `Voltyks.Application/Services/ChargingRequest/ChargingRequestService.cs`
**Method:** `GetAddressFromLatLongNominatimAsync`

Same pattern as AuthService with:
- Try-catch for `HttpRequestException`
- Try-catch for `TaskCanceledException` (timeout)
- Try-catch for generic `Exception`
- 10-second timeout

---

## 3. Security Architecture

### 3.1 Existing Security (Already Implemented)

| Component | Status | Location |
|-----------|--------|----------|
| Global Error Handler | ✅ | `Voltyks.API/Middelwares/GlobalErrorHandlingMiddelwares.cs` |
| JWT Authentication | ✅ | `Voltyks.API/Extentions/Extentions.cs` |
| Custom Exceptions | ✅ | `Voltyks.Core/Exceptions/` |
| SQL Injection Protection | ✅ | Entity Framework Core LINQ |

### 3.2 New Improvements (This Update)

| Component | Status | Description |
|-----------|--------|-------------|
| DTO Validation | ✅ | DataAnnotations on critical DTOs |
| External API Try-Catch | ✅ | Nominatim API calls wrapped |
| Request Timeout | ✅ | 10-second timeout on HTTP calls |
| Graceful Degradation | ✅ | Fallback values on API failures |

---

## 4. Validation Response Format

When validation fails, the API returns:

```json
{
  "statusCode": 400,
  "errors": [
    {
      "field": "EmailOrPhone",
      "errors": ["Email or phone is required"]
    },
    {
      "field": "Password",
      "errors": ["Password must be at least 6 characters"]
    }
  ]
}
```

---

## 5. Protected Against

| Attack/Issue | Protection |
|--------------|------------|
| Empty/Null Input | `[Required]` validation |
| Invalid Coordinates | `[Range]` validation (-90 to 90, -180 to 180) |
| Invalid Battery % | `[Range(0, 100)]` validation |
| String Overflow | `[StringLength]` validation |
| Negative IDs | `[Range(1, int.MaxValue)]` validation |
| External API Failures | Try-catch with fallback |
| Hanging Requests | 10-second timeout |
| Invalid Ratings | `[Range(1.0, 5.0)]` validation |

---

## 6. Files Modified

### DTOs (6 files):
1. `Voltyks.Core/DTOs/AuthDTOs/LoginDTO.cs`
2. `Voltyks.Core/DTOs/ChargerRequest/SendChargingRequestDto.cs`
3. `Voltyks.Core/DTOs/Charger/AddChargerDto.cs`
4. `Voltyks.Core/DTOs/Charger/UpdateChargerDto.cs`
5. `Voltyks.Core/DTOs/VehicleDTOs/CreateVehicleDto.cs`
6. `Voltyks.Core/DTOs/Process/SubmitRatingDto.cs`

### Services (2 files):
1. `Voltyks.Application/Services/Auth/AuthService.cs`
2. `Voltyks.Application/Services/ChargingRequest/ChargingRequestService.cs`

---

## 7. Future Recommendations

| Priority | Recommendation |
|----------|----------------|
| High | Add validation to remaining DTOs (BillingData, etc.) |
| High | Implement Rate Limiting middleware |
| Medium | Add FluentValidation for complex rules |
| Medium | Restrict CORS to specific origins |
| Low | Add security headers middleware |
| Low | Implement request/response logging |

---

## 8. Testing Checklist

- [ ] Test login with empty credentials
- [ ] Test charging request with invalid coordinates (lat > 90)
- [ ] Test charging request with battery > 100%
- [ ] Test adding charger with negative IDs
- [ ] Test rating submission with rating > 5
- [ ] Test API when Nominatim is unavailable
- [ ] Verify existing functionality still works

---

**Last Updated:** 2026-01-03
**Commit:** `cfb1424`
