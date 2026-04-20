# Notifications Localization (EN / AR)
## Documentation for Mobile & Dashboard Teams

---

## Overview

كل الـ notifications في التطبيق (FCM push + SignalR real-time + DB history) بتدعم **لغتين**:
- **English** (`en`) — default
- **Arabic** (`ar`)

الـ frontend بيتحكم في لغة الـ notification عن طريق field اسمه `lang` يتبعت **اختياري** في الـ body لأي endpoint بيعمل trigger لـ notification.

---

## القواعد

1. **`lang` اختياري** — لو مش مبعوت، default English.
2. **القيم المقبولة**: `"en"` أو `"ar"` (case-insensitive). أي قيمة تانية → fallback إلى English.
3. **لغة الـ notification = لغة الـ sender** (الـ user اللي بيعمل الـ API call). لو User A بعت request بـ `lang=ar` والإشعار رايح لـ User B، User B هيستقبل الإشعار بالعربي.
4. **Background notifications** (زي timeout من rating window، expiry من stale cleanup) → **دايمًا English**. مفيش طريقة يتبعت معاها lang.

---

## Endpoints اللي بتقبل `lang`

| Endpoint | Notification Type |
|----------|-------------------|
| `POST /api/ChargingRequest/sendChargingRequest` | VehicleOwner_RequestCharger |
| `POST /api/ChargingRequest/AcceptRequest` | ChargerOwner_AcceptRequest |
| `POST /api/ChargingRequest/RejectRequest` | ChargerOwner_RejectRequest |
| `POST /api/ChargingRequest/ConfirmRequest` | ChargerOwner_ConfirmedProcessSuccessfully |
| `POST /api/ChargingRequest/abortRequest` | ChargerOwner_ProcessAborted / VehicleOwner_ProcessAbortedAfterPaymentSuccessfully |
| Processes — ConfirmByVehicleOwner | VehicleOwner_CreateProcess |
| Processes — Update | VehicleOwner_UpdateProcess |
| Processes — OwnerDecision | ChargerOwner_ConfirmProcess / VehicleOwner_ConfirmProcess / Process_Started / Process_Terminated |
| Processes — SubmitRating | VehicleOwner_SubmitRating / ChargerOwner_SubmitRating |
| `POST /api/UserReport/create` | Report_VehicleOwnerToChargerOwner / Report_ChargerOwnerToVehicleOwner |
| `POST /api/admin/vehicle-addition-requests/{id}/accept` | VehicleAdditionRequest_Accepted |
| `POST /api/admin/vehicle-addition-requests/{id}/decline` | VehicleAdditionRequest_Declined |

---

## كيفية الاستخدام

### مثال: إرسال طلب شحن بالعربي

```http
POST /api/ChargingRequest/sendChargingRequest
Content-Type: application/json
Authorization: Bearer {token}

{
  "chargerId": 123,
  "kwNeeded": 30,
  "currentBatteryPercentage": 45,
  "latitude": 30.01,
  "longitude": 31.2,
  "lang": "ar"
}
```

صاحب الشاحن هيستقبل:
- **Title:** `طلب شحن جديد 🚗`
- **Body:** `طلب سائق الشحن في محطتك.`

نفس الـ request بدون `"lang": "ar"` أو بـ `"lang": "en"`:
- **Title:** `New Charging Request 🚗`
- **Body:** `Driver requested to charge at your station.`

---

## نصوص كل الـ notifications (EN / AR)

### Vehicle Addition Requests
| Type | EN Title / Body | AR Title / Body |
|------|----------------|------------------|
| Accepted | `Request Accepted` / `Your vehicle is now available! You can add your vehicle now with us` | `تم قبول الطلب` / `سيارتك متاحة الآن! يمكنك إضافة سيارتك معنا` |
| Declined | `Request Declined` / `The vehicle you requested already exists. Please check again` | `تم رفض الطلب` / `السيارة المطلوبة موجودة بالفعل. يرجى البحث مرة أخرى` |

### Charging Request Lifecycle
| Type | EN | AR |
|------|----|----|
| VehicleOwner_RequestCharger | `New Charging Request 🚗` / `Driver requested to charge at your station.` | `طلب شحن جديد 🚗` / `طلب سائق الشحن في محطتك.` |
| ChargerOwner_AcceptRequest | `Charging Request Accepted` / `Your request to charge at {ownerName}'s station has been accepted.` | `تم قبول طلب الشحن` / `تم قبول طلبك للشحن في محطة {ownerName}.` |
| ChargerOwner_RejectRequest | `Charging Request Rejected ❌` / `Your request to charge at {ownerName}'s station was rejected.` | `تم رفض طلب الشحن ❌` / `تم رفض طلبك للشحن في محطة {ownerName}.` |
| ChargerOwner_ConfirmedProcessSuccessfully | `Charging Request Confirmed ✅` / `The charger {ownerName} confirmed the charging session for your vehicle.` | `تم تأكيد طلب الشحن ✅` / `أكد صاحب الشاحن {ownerName} جلسة الشحن لسيارتك.` |
| ChargerOwner_ProcessAborted | `Charging session aborted` / `The station owner aborted your charging request.` | `تم إلغاء جلسة الشحن` / `قام صاحب المحطة بإلغاء طلب الشحن الخاص بك.` |
| VehicleOwner_ProcessAbortedAfterPaymentSuccessfully | `Request Aborted ❌` / `The driver {driverName} aborted the charging session at your station after payment.` | `تم إلغاء الطلب ❌` / `قام السائق {driverName} بإلغاء جلسة الشحن في محطتك بعد الدفع.` |

### Process Lifecycle
| Type | EN | AR |
|------|----|----|
| VehicleOwner_CreateProcess | `Process confirmation pending` / `Amount Charged: X | Amount Paid: Y` | `في انتظار تأكيد العملية` / `المبلغ المحصل: X | المبلغ المدفوع: Y` |
| VehicleOwner_UpdateProcess | `Process updated` / `The vehicle owner updated process details.` أو `Updated fields → ...` | `تم تحديث العملية` / `قام صاحب السيارة بتحديث تفاصيل العملية.` أو `الحقول المحدثة → ...` |
| ChargerOwner_ConfirmProcess | `Process confirmed` / `Charger owner confirmed your session. Please submit your rating.` | `تم تأكيد العملية` / `أكد صاحب الشاحن جلستك. يرجى إرسال التقييم.` |
| VehicleOwner_ConfirmProcess | `Process confirmed` / `Vehicle owner confirmed the session completion.` | `تم تأكيد العملية` / `أكد صاحب السيارة اكتمال الجلسة.` |
| Process_Started | `Process started` / `{whoStarted} started the process.` | `بدأت العملية` / `{whoStarted} بدأ العملية.` |
| Process_Terminated | `Process Terminated` / `The process has been terminated.` | `تم إنهاء العملية` / `تم إنهاء العملية.` |
| Process_Terminated (expiry, background) | `Process Terminated` / `The request has expired.` | (English only — background) |
| SubmitRating | `New rating received ⭐` / `You received a {rating}★ rating for process #{id}.` | `تم استلام تقييم جديد ⭐` / `حصلت على تقييم {rating}★ للعملية رقم #{id}.` |
| DefaultRating_Applied (background) | `Default rating applied` / `A default {rating}★ rating was applied for process #{id}.` | (English only — background) |

### Reports
| Type | EN | AR |
|------|----|----|
| Report_* | `{reporterName} filed a report against you` / `Open the process to review the report details.` | `قام {reporterName} بالإبلاغ عنك` / `افتح العملية لمراجعة تفاصيل البلاغ.` |

---

## Response

الـ response من كل endpoint (JSON body اللي بيرجع) لسه **English دايمًا** — الـ localization تطبق على الـ **notification text فقط** (title/body اللي بيوصل للمستخدم).

---

**Version:** 1.0 — Notifications Localization
**Last Updated:** 2026-04-20
