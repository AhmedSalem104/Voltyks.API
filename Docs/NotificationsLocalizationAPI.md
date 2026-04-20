# Notifications Language Preference (EN / AR)
## Documentation for Mobile & Dashboard Teams

---

## Overview

كل مستخدم عنده **preference محفوظة في حسابه** لغة الإشعارات (زي الـ Wallet):
- **Default**: `"en"` لأي user جديد
- **القيم المقبولة**: `"en"` أو `"ar"` (case-insensitive، أي قيمة تانية → `"en"`)

الـ backend بيستخدم القيمة المخزنة على الـ **receiver** لاختيار لغة الإشعار اللي هيوصله (FCM + SignalR + DB notification).

---

## الـ Flow المطلوب من الـ Frontend

1. **أول فتح للتطبيق لمستخدم مسجل** → ابعت الـ locale الحالية في الـ PUT endpoint.
2. **كل ما المستخدم يغير اللغة من settings** → ابعت تاني لنفس الـ endpoint.
3. **كل الإشعارات بعد كده** هتوصل تلقائيًا باللغة اللي اتحفظت.

الـ endpoint **idempotent** — لو بعت نفس القيمة المخزنة، مفيش DB write، بس بيرجع success.

---

## Endpoints

### 1. GET /api/auth/language
جلب اللغة الحالية المحفوظة للمستخدم.

**Headers:** `Authorization: Bearer {token}`

**Response (200):**
```json
{
  "status": true,
  "message": "Language fetched successfully",
  "data": "en"
}
```

---

### 2. PUT /api/auth/language
تحديث اللغة المفضلة للمستخدم.

**Headers:** `Authorization: Bearer {token}` + `Content-Type: application/json`

**Body:**
```json
{
  "language": "ar"
}
```

**Success Response (200) — قيمة جديدة:**
```json
{
  "status": true,
  "message": "Language updated to ar",
  "data": "ar"
}
```

**Success Response (200) — نفس القيمة المخزنة:**
```json
{
  "status": true,
  "message": "Language already set to ar",
  "data": "ar"
}
```

---

### 3. GET /api/auth/GetProfileDetails — بقى يرجع `preferredLanguage`

```json
{
  "status": true,
  "data": {
    "id": "...",
    "firstName": "...",
    ...
    "preferredLanguage": "ar"
  }
}
```

---

## إزاي الإشعارات بتتبعت

- الـ receiver (المستقبل) بيحصل على الإشعار **بلغته المحفوظة في الـ DB**.
- ده ينطبق على الـ FCM push + SignalR real-time + DB history.
- مفيش داعي لبعت أي `lang` في body أي endpoint — الـ `lang` field اللي كان في الـ body قديمًا **اتحذف تمامًا**.

## نصوص الـ Notifications المتاحة (EN / AR)

نفس القائمة اللي كانت قبل كده — بس اختيار اللغة بقى تلقائي حسب preference الـ receiver:

### 🚗 Charging Request Lifecycle
| Event | EN | AR |
|-------|----|----|
| Request sent | `New Charging Request 🚗` / `Driver requested...` | `طلب شحن جديد 🚗` / `طلب سائق الشحن في محطتك.` |
| Accepted | `Charging Request Accepted` / ... | `تم قبول طلب الشحن` / ... |
| Rejected | `Charging Request Rejected ❌` / ... | `تم رفض طلب الشحن ❌` / ... |
| Confirmed | `Charging Request Confirmed ✅` / ... | `تم تأكيد طلب الشحن ✅` / ... |
| Aborted (by charger) | `Charging session aborted` / ... | `تم إلغاء جلسة الشحن` / ... |
| Aborted (by vehicle) | `Request Aborted ❌` / ... | `تم إلغاء الطلب ❌` / ... |

### ⚡ Process Lifecycle
| Event | EN | AR |
|-------|----|----|
| Create process | `Process confirmation pending` / `Amount Charged... \| Amount Paid...` | `في انتظار تأكيد العملية` / ... |
| Update process | `Process updated` / ... | `تم تحديث العملية` / ... |
| Confirmed (by charger) | `Process confirmed` / ... | `تم تأكيد العملية` / ... |
| Confirmed (by vehicle) | `Process confirmed` / ... | `تم تأكيد العملية` / ... |
| Started | `Process started` / `{who} started the process.` | `بدأت العملية` / `{who} بدأ العملية.` |
| Terminated | `Process Terminated` / ... | `تم إنهاء العملية` / ... |
| Rating received | `New rating received ⭐` / `You received a {rating}★...` | `تم استلام تقييم جديد ⭐` / ... |

### 📋 Reports
| Event | EN | AR |
|-------|----|----|
| Report filed | `{name} filed a report against you` / ... | `قام {name} بالإبلاغ عنك` / ... |

### 🚙 Vehicle Addition
| Event | EN | AR |
|-------|----|----|
| Accepted | `Request Accepted` / `Your vehicle is now available!...` | `تم قبول الطلب` / `سيارتك متاحة الآن!...` |
| Declined | `Request Declined` / `The vehicle you requested already exists...` | `تم رفض الطلب` / `السيارة المطلوبة موجودة بالفعل...` |

---

## Suggested implementation for Frontend

```
on app launch (if user is authenticated):
  lang = currentAppLocale()  // "en" or "ar"
  PUT /api/auth/language  body: { "language": lang }

on user changes language in settings:
  newLang = "ar"  // or "en"
  PUT /api/auth/language  body: { "language": newLang }
  // (also update app UI locale)
```

---

## Migration Note
- كل المستخدمين القدامى default = `"en"`
- مفيش breaking change على الـ bodies — `lang` field القديم في DTOs اتشال، لكن لو الـ client لسه بعته، JSON deserializer بيتجاهله تلقائيًا (بدون errors).

---

**Version:** 2.0 — Per-User Stored Preference
**Last Updated:** 2026-04-20
