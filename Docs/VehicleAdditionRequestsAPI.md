# Vehicle Addition Requests API
## Documentation for Mobile Team (iOS & Android — Native)

---

## Overview / نظرة عامة

ميزة تسمح للمستخدم بإرسال طلب لإضافة سيارته في حالة عدم وجود الـ Brand أو الـ Model في القوائم المتاحة في التطبيق. الأدمن بعد كده بيراجع الطلب ويقبله أو يرفضه.

### The Flow

1. **User** → بيدخل يضيف عربيته، مش لاقي الـ brand/model
2. **User** → بيضغط على "ما تجد سيارتك؟ أرسل طلب"
3. **User** → بيملأ form (brandName, modelName, capacity) ويبعت
4. **Admin** → بيشوف الطلبات في الـ dashboard ويقبل أو يرفض
5. **User** → بيستقبل notification بالنتيجة
6. **User (if accepted)** → يقدر يضيف عربيته من القوائم اللي بقت فيها الماركة الجديدة

### Important Notes

- **الـ Notifications بتتبعت على 3 مسارات:**
  - **FCM Push** — بيوصل حتى لو التطبيق مقفول (push على شاشة القفل)
  - **SignalR** — real-time لو المستخدم فاتح التطبيق
  - **DB Notifications** — المستخدم يشوفها في قسم Notifications في التطبيق (history)
- **Localization:** كل الـ notifications بتدعم `en` و `ar`. شوف `Docs/NotificationsLocalizationAPI.md` للتفاصيل.

---

## Base URL & Authentication

### Base URL
```
https://voltyks.runasp.net
```

### Required Headers
كل الـ requests محتاجة:
```
Authorization: Bearer {user_access_token}
Content-Type: application/json
```

---

## User Endpoints

### 1. Submit Vehicle Addition Request / إرسال طلب إضافة سيارة

يستخدمه المستخدم لما يحب يطلب إضافة سيارة جديدة للنظام.

```http
POST /api/vehicle-addition-requests
```

#### Request Body

```json
{
  "brandName": "Toyota",
  "modelName": "Corolla",
  "capacity": "50 kWh"
}
```

#### Fields

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| `brandName` | string | ✅ Yes | 100 | اسم الشركة المصنعة (مثلاً Toyota، Tesla) |
| `modelName` | string | ✅ Yes | 100 | اسم الموديل (مثلاً Corolla، Model Y) |
| `capacity` | string | ✅ Yes | 50 | سعة البطارية (مثلاً "50 kWh" أو "75") |

#### Success Response (200 OK)

```json
{
  "status": true,
  "message": "Request submitted successfully",
  "data": {
    "id": 12,
    "brandName": "Toyota",
    "modelName": "Corolla",
    "capacity": "50 kWh",
    "status": "pending",
    "createdAt": "2026-04-19T10:00:00Z",
    "updatedAt": null
  },
  "errors": null
}
```

#### Error Responses (400 Bad Request)

**حالة 1 — السيارة موجودة بالفعل:**
```json
{
  "status": false,
  "message": "This vehicle already exists in our system. Please check the vehicles list again.",
  "data": null,
  "errors": null
}
```

**حالة 2 — المستخدم عنده طلب pending (أي طلب، بغض النظر عن السيارة):**
> كل مستخدم مسموح له بـ **طلب pending واحد فقط في المرة**. لو عايز يبعت طلب تاني، لازم يستنى لحد ما الأدمن يراجع الطلب الحالي (accept أو decline).

```json
{
  "status": false,
  "message": "You already have a pending vehicle addition request. Please wait for admin review before submitting another one.",
  "data": null,
  "errors": null
}
```

**حالة 3 — Validation error (حقل فاضي):**
```json
{
  "status": false,
  "message": "BrandName, ModelName, and Capacity are required",
  "data": null,
  "errors": null
}
```

---

### 2. Get My Requests / جلب طلباتي

يستخدمه المستخدم لعرض تاريخ طلباته وحالة كل طلب.

```http
GET /api/vehicle-addition-requests/my
```

#### Request
مفيش body. الـ userId بيتقرأ تلقائيًا من الـ JWT token.

#### Success Response (200 OK)

```json
{
  "status": true,
  "message": "Retrieved 3 requests",
  "data": [
    {
      "id": 12,
      "brandName": "Toyota",
      "modelName": "Corolla",
      "capacity": "50 kWh",
      "status": "pending",
      "createdAt": "2026-04-19T10:00:00Z",
      "updatedAt": null
    },
    {
      "id": 10,
      "brandName": "Tesla",
      "modelName": "Model Y",
      "capacity": "75 kWh",
      "status": "accepted",
      "createdAt": "2026-04-15T08:30:00Z",
      "updatedAt": "2026-04-16T14:20:00Z"
    },
    {
      "id": 8,
      "brandName": "Honda",
      "modelName": "Civic",
      "capacity": "40 kWh",
      "status": "declined",
      "createdAt": "2026-04-10T12:00:00Z",
      "updatedAt": "2026-04-11T09:15:00Z"
    }
  ],
  "errors": null
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | رقم الطلب |
| `brandName` | string | الماركة اللي طلبها |
| `modelName` | string | الموديل اللي طلبه |
| `capacity` | string | السعة اللي طلبها |
| `status` | string | حالة الطلب — القيم الممكنة: `pending`, `accepted`, `declined` |
| `createdAt` | DateTime (ISO 8601 UTC) | تاريخ إنشاء الطلب |
| `updatedAt` | DateTime? (nullable) | تاريخ آخر تحديث (null لو الطلب لسه pending) |

الـ list مرتبة من الأحدث للأقدم (`CreatedAt DESC`).

---

## Status Values / قيم الحالة

| Value | Meaning | UI Display |
|-------|---------|------------|
| `pending` | قيد المراجعة | 🟡 أصفر — "قيد المراجعة" |
| `accepted` | تمت الموافقة وإضافة السيارة للنظام | 🟢 أخضر — "تمت الموافقة" |
| `declined` | مرفوض — السيارة موجودة بالفعل | 🔴 أحمر — "مرفوض" |

---

## Notifications / الإشعارات

بعد ما الأدمن يقبل أو يرفض الطلب، التطبيق بيستقبل إشعار عبر **3 مسارات في نفس الوقت**:

### (A) FCM Push Notification — بيوصل حتى لو التطبيق مقفول
الـ Push Notification بيتبعت لكل الـ device tokens المسجلة للمستخدم عبر Firebase. الـ data payload:
```json
{
  "requestId": "12",
  "NotificationType": "VehicleAdditionRequest_Accepted",
  "vehicleAdditionRequestId": "12",
  "userRole": "vehicle_owner"
}
```
> **ملاحظة:** `requestId` في الـ FCM payload = id الـ VehicleAdditionRequest (مش charging request).

### (B) SignalR — Real-time
لو المستخدم متصل بالـ SignalR hub، هيستقبل event اسمه `ReceiveNotification`.

**الـ Payload شكله:**
```json
{
  "title": "Request Accepted",
  "body": "Your vehicle is now available! You can add your vehicle now with us",
  "data": {
    "id": 45,
    "type": "VehicleAdditionRequest_Accepted",
    "vehicleAdditionRequestId": 12,
    "userRole": "vehicle_owner"
  },
  "timestamp": "2026-04-19T14:30:00Z"
}
```

### (C) DB Notifications — تقرأها من endpoint الـ notifications الموجود عندك
الـ notification بتتسجل في جدول الـ Notifications، والمستخدم يقدر يشوفها لما يفتح شاشة الـ Notifications في التطبيق (الـ endpoint بتاع الـ notifications موجود من قبل).

**الـ Record شكله:**
```json
{
  "id": 45,
  "title": "Request Accepted",
  "body": "Your vehicle is now available! You can add your vehicle now with us",
  "type": "VehicleAdditionRequest_Accepted",
  "originalId": 12,
  "isRead": false,
  "sentAt": "2026-04-19T14:30:00Z"
}
```

### Notification Types

| Type | Title | Body | متى بيتبعت |
|------|-------|------|-----------|
| `VehicleAdditionRequest_Accepted` | `Request Accepted` | `Your vehicle is now available! You can add your vehicle now with us` | لما الأدمن يقبل الطلب |
| `VehicleAdditionRequest_Declined` | `Request Declined` | `The vehicle you requested already exists. Please check again` | لما الأدمن يرفض الطلب |

### Notification Payload Fields

| Field | Type | Description |
|-------|------|-------------|
| `title` | string | عنوان الإشعار |
| `body` | string | نص الإشعار |
| `data.id` | int | id الـ Notification record في الـ DB |
| `data.type` | string | نوع الإشعار (زي الجدول فوق) |
| `data.vehicleAdditionRequestId` | int | id الطلب الأصلي (الـ `originalId`) |
| `data.userRole` | string | دايمًا `"vehicle_owner"` |
| `timestamp` | DateTime | توقيت الإشعار |

---

## UI/UX Recommendations / توصيات التصميم

### 1. نقطة دخول الفيتشر

في شاشة **"Add Vehicle"** (لما المستخدم بيختار الماركة والموديل)، ضيف زرار/رابط تحت القوائم:

> **"لا تجد سيارتك؟ أرسل طلب لإضافتها"**

### 2. شاشة إرسال الطلب

Form بسيط فيه:
- حقل `Brand Name` (نصي)
- حقل `Model Name` (نصي)
- حقل `Battery Capacity` (نصي، مع placeholder "مثال: 50 kWh")
- زر "إرسال الطلب"
- ملحوظة تحت الـ form: "سيتم مراجعة طلبك وإعلامك بالنتيجة."

### 3. شاشة "My Requests" (اختيارية بس مفضلة)

في قسم **"My Vehicles"** أو **"Settings"**، ضيف رابط **"طلبات إضافة السيارات"**:
- Empty state لو مفيش طلبات
- List بالطلبات مع تلوين status
- لو الطلب `accepted`، ضيف زرار "أضف السيارة الآن" يودي المستخدم لشاشة Add Vehicle

### 4. لما المستخدم يستقبل `VehicleAdditionRequest_Accepted`

**مهم جدًا:** لازم التطبيق يعمل refresh للقوائم التالية بعد القبول:
- قائمة الـ Brands (`GET /api/Brand`)
- قائمة الـ Models (`GET /api/Model`)

كده لما المستخدم يرجع لشاشة Add Vehicle، هيلاقي الماركة والموديل الجديدين.

### 5. Error Handling

اعرض الرسائل اللي بتجي من الـ backend في الـ `message` field مباشرة للمستخدم — الرسائل مكتوبة بطريقة واضحة للـ end-user.

### 6. Validation على الموبايل

قبل ما تبعت الـ request، اعمل validation محلي:
- الحقول الثلاثة مش فاضية (trim أول)
- `brandName` و `modelName` ≤ 100 حرف
- `capacity` ≤ 50 حرف
- يفضل إن الـ `capacity` يحتوي على رقم (regex `\d`)

---

## Status Flow / تدفق الحالات

```
           ┌──────────┐
           │ pending  │  ← user submits request
           └────┬─────┘
                │
        ┌───────┴────────┐
        │                │
   admin accepts    admin declines
        │                │
        ▼                ▼
  ┌──────────┐    ┌──────────┐
  │ accepted │    │ declined │
  └──────────┘    └──────────┘
```

**ملاحظات:**
- الحالة ما تقدرش تتغير بعد `accepted` أو `declined` (final states).
- لما الحالة تبقى `accepted`:
  - الـ Brand الجديد (لو مش موجود) بيتضاف لجدول `Brands`
  - الـ Model بيتضاف لجدول `Models` مرتبط بالـ Brand
  - الـ user يقدر يضيف عربيته من شاشة Add Vehicle
- لما الحالة تبقى `declined`:
  - السبب الوحيد للرفض: السيارة موجودة بالفعل
  - مفيش action مطلوب من المستخدم غير إنه يرجع يبحث في القوائم

---

## Business Rules / القواعد

### عند الإرسال
1. الـ user لازم يكون مسجّل دخول (authenticated).
2. الـ 3 حقول مطلوبة (brandName, modelName, capacity).
3. لو الـ Brand + Model موجودين بالفعل في النظام → الـ request بيترفض فوريًا بـ error.
4. **كل user مسموح له بـ طلب `pending` واحد فقط في المرة** (بغض النظر عن الـ brand/model). لازم يستنى لحد ما الأدمن يعمل accept أو decline للطلب الحالي قبل ما يبعت جديد.

### عند القبول (من الأدمن)
1. الـ Brand لو مش موجود، يتعمل جديد.
2. الـ Model بيتعمل جديد مرتبط بالـ Brand بـ الـ capacity (رقمية).
3. الـ user بيستقبل notification.

### عند الرفض (من الأدمن)
1. الحالة بتتحدث لـ `declined`.
2. الـ user بيستقبل notification بسبب الرفض.

---

## Summary for Mobile Dev / ملخص للـ Mobile Dev

### Endpoints to Implement

| # | Method | Endpoint | Purpose |
|---|--------|----------|---------|
| 1 | POST | `/api/vehicle-addition-requests` | إرسال طلب جديد |
| 2 | GET | `/api/vehicle-addition-requests/my` | جلب طلبات المستخدم |

### Screens to Build

1. زر "لا تجد سيارتك؟" في شاشة Add Vehicle
2. شاشة **Request Vehicle Addition** (form)
3. شاشة **My Vehicle Requests** (list) — اختيارية لكن مفضلة

### Notification Handling

- استقبل SignalR events من النوعين: `VehicleAdditionRequest_Accepted` و `VehicleAdditionRequest_Declined`
- لما يوصل `_Accepted` — اعمل refresh لقوائم Brands و Models
- لما يوصل `_Declined` — اعرض الرسالة وممكن تودي المستخدم للـ "My Requests"

### Important Reminders

- الـ Feature ده **بيستخدم FCM + SignalR + DB** للإشعارات — المستخدم هيستقبل الإشعار حتى لو التطبيق مقفول.
- كل الرسائل من الـ backend بـ English حاليًا (ممكن تترجمها في الـ UI).
- الـ DateTime في الـ responses بصيغة ISO 8601 UTC — حوّلها لـ local timezone للعرض.

---

## Contact

لأي استفسار عن الـ API، تواصل مع backend team.

**Backend Version:** 1.0
**Last Updated:** 2026-04-19
