# Vehicle Addition Requests — Admin API
## Documentation for Admin Dashboard Team

---

## Overview

الدليل ده مخصص لفريق الـ Dashboard لربط صفحة **"طلبات إضافة السيارات"** مع الـ backend. فيه 5 endpoints خاصة بالأدمن — كلها تحت الـ role `Admin`.

### فكرة الـ Accept Flow (مهم)

لأن المستخدم ممكن يكتب البيانات بطريقة غلط (typos، capitalization غلط، تسميات مختلفة)، الـ accept flow مصمم على مرحلتين:

1. **GET Preview** — الأدمن يفتح modal القبول → الـ dashboard يستدعي preview endpoint → يعرض تحذيرات + suggestions
2. **POST Accept (مع body اختياري)** — الأدمن يصحح البيانات من واجهة الـ dashboard → يبعت القيم المعدلة في الـ body

الـ admin يقدر:
- يختار brand موجود بدل ما يعمل واحد جديد (يمنع duplicates زي "Tesla" vs "tesla")
- يصحح اسم الموديل
- يحدد الـ capacity كرقم مباشر

---

## Base URL & Authentication

```
Base URL:  https://voltyks.runasp.net
Auth:      Bearer {admin_access_token}
Role:      Admin (required on all endpoints below)
```

---

## Endpoints

### 1. List Requests (Paginated)

```http
GET /api/admin/vehicle-addition-requests
```

**Query Parameters:**
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `status` | string (optional) | all | `pending` / `accepted` / `declined` |
| `pageNumber` | int | 1 | رقم الصفحة |
| `pageSize` | int | 20 | عدد العناصر (max 100) |

**Response (200):**
```json
{
  "status": true,
  "message": "Retrieved 12 requests",
  "data": {
    "items": [
      {
        "id": 12,
        "userId": "abc-123",
        "userFullName": "Ahmed Mohamed",
        "userEmail": "ahmed@example.com",
        "userPhone": "+201005151055",
        "brandName": "Teshla",
        "modelName": "modal 3",
        "capacity": "75 kwh",
        "status": "pending",
        "createdAt": "2026-04-20T10:00:00Z",
        "updatedAt": null,
        "processedBy": null
      }
    ],
    "totalCount": 12,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1,
    "hasPrevious": false,
    "hasNext": false
  }
}
```

---

### 2. Get Single Request

```http
GET /api/admin/vehicle-addition-requests/{id}
```

**Response (200):** بيانات الطلب كاملة (نفس شكل عنصر من `items` فوق).

---

### 3. Get Accept Preview ⭐ (جديد)

**أهم endpoint** — لازم يتستدعى **قبل** ما الأدمن يقدر يضغط Accept. بيرجع:
- البيانات الأصلية (للعرض read-only في الـ modal)
- Capacity المفسّرة رقميًا
- Exact brand match (لو موجود)
- Similar brands بترتيب تنازلي حسب التشابه (Levenshtein)
- Exact/similar models في نفس الـ brand المقترح
- Warnings نصية (لو فيه)

```http
GET /api/admin/vehicle-addition-requests/{id}/accept-preview
```

**Response (200) — User كتب Teshla/modal 3/75 kwh:**
```json
{
  "status": true,
  "message": "Preview generated",
  "data": {
    "original": {
      "brandName": "Teshla",
      "modelName": "modal 3",
      "capacity": "75 kwh"
    },
    "parsedCapacity": 75.0,
    "capacityParseSuccess": true,
    "exactBrandMatch": null,
    "similarBrands": [
      {
        "id": 30,
        "name": "Tesla",
        "similarity": 0.83,
        "modelsCount": 4
      }
    ],
    "exactModelMatch": null,
    "similarModels": [
      {
        "modelId": 145,
        "modelName": "Model 3 Long Range",
        "brandId": 30,
        "brandName": "Tesla",
        "similarity": 0.72
      }
    ],
    "warnings": [
      "Found 1 similar brand(s). User may have made a typo."
    ]
  }
}
```

**Response (200) — User كتب Tesla/Model 3/75 kWh (brand موجود، model جديد):**
```json
{
  "status": true,
  "data": {
    "original": { "brandName": "Tesla", "modelName": "Model 3", "capacity": "75 kWh" },
    "parsedCapacity": 75.0,
    "capacityParseSuccess": true,
    "exactBrandMatch": {
      "id": 30,
      "name": "Tesla",
      "similarity": 1.0,
      "modelsCount": 4
    },
    "similarBrands": [],
    "exactModelMatch": null,
    "similarModels": [],
    "warnings": []
  }
}
```

**Response (200) — Model duplicate موجود بالفعل:**
```json
{
  "status": true,
  "data": {
    "original": { "brandName": "Tesla", "modelName": "Model 3 Long Range", "capacity": "75 kWh" },
    "parsedCapacity": 75.0,
    "capacityParseSuccess": true,
    "exactBrandMatch": { "id": 30, "name": "Tesla", "similarity": 1.0, "modelsCount": 4 },
    "similarBrands": [],
    "exactModelMatch": {
      "modelId": 145,
      "modelName": "Model 3 Long Range",
      "brandId": 30,
      "brandName": "Tesla",
      "similarity": 1.0
    },
    "similarModels": [],
    "warnings": [
      "A model named 'Model 3 Long Range' already exists under 'Tesla'. Accepting would create a duplicate."
    ]
  }
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `original` | object | البيانات اللي المستخدم كتبها (للعرض) |
| `parsedCapacity` | number \| null | القيمة الرقمية المستخرجة من نص الـ capacity |
| `capacityParseSuccess` | bool | هل الـ parser نجح؟ |
| `exactBrandMatch` | object \| null | Brand بنفس الاسم case-insensitive |
| `similarBrands` | array | Brands مشابهة (similarity ≥ 0.6) — أعلى 5 |
| `exactModelMatch` | object \| null | Model موجود بالفعل → accepting هيعمل duplicate |
| `similarModels` | array | Models مشابهة — تحذير |
| `warnings` | array of strings | رسائل نصية للعرض في الـ UI |

---

### 4. Accept Request (مع body اختياري)

```http
POST /api/admin/vehicle-addition-requests/{id}/accept
Content-Type: application/json
```

**Body (اختياري):**
```json
{
  "useExistingBrandId": 30,
  "brandName": null,
  "modelName": "Model 3",
  "capacity": 75.0
}
```

**Body Fields (كلها nullable):**
| Field | Type | Behavior |
|-------|------|----------|
| `useExistingBrandId` | int? | لو معبّاه، الموديل يرتبط بالـ brand ده مباشرة (مش بيعمل brand جديد). أعلى أولوية. |
| `brandName` | string? | لو `useExistingBrandId` فاضي: يبحث case-insensitive عن brand بنفس الاسم، لو مش موجود يعمله جديد. |
| `modelName` | string? | لو معبّاه، يبقى اسم الموديل الجديد (بدل اللي كتبه المستخدم). |
| `capacity` | number? | لو معبّاه، يتستخدم مباشرة بدون parsing. لازم يكون > 0. |
| `lang` | string? | `"en"` أو `"ar"` للـ notification اللي هيوصل للمستخدم. default: English. |

**لو الـ body كله فاضي/null:** الـ endpoint يستخدم البيانات الأصلية زي ما كتبها المستخدم (backward compatible).

**Success Response (200):**
```json
{
  "status": true,
  "message": "Request accepted successfully. Vehicle added to the system.",
  "data": {
    "requestId": 12,
    "brandId": 30,
    "modelId": 146
  }
}
```

**Error Responses:**

- **Request مش موجود أو مش pending:**
```json
{ "status": false, "message": "Request is already accepted, cannot accept it." }
```

- **`useExistingBrandId` مش موجود:**
```json
{ "status": false, "message": "Brand with id 999 not found." }
```

- **Model duplicate:**
```json
{
  "status": false,
  "message": "Model 'Model 3' already exists under brand 'Tesla'. Please decline this request."
}
```

- **Capacity مش صالح:**
```json
{
  "status": false,
  "message": "Invalid capacity format: 'xyz'. Please provide a numeric capacity in the accept payload."
}
```
أو:
```json
{ "status": false, "message": "Capacity must be greater than zero." }
```

---

### 5. Decline Request

```http
POST /api/admin/vehicle-addition-requests/{id}/decline
```

**Body (اختياري):**
```json
{ "lang": "ar" }
```
- `lang`: `"en"` أو `"ar"` للـ notification. default: English.

**Response:**
```json
{
  "status": true,
  "message": "Request declined successfully.",
  "data": { "requestId": 12 }
}
```

الـ user بيستقبل notification: `"Request Declined"` / `"The vehicle you requested already exists. Please check again"`.

---

## الـ Flow المقترح في الـ Dashboard

### شاشة القبول (Accept Modal)

1. الأدمن يضغط "قبول" على طلب → يفتح modal.
2. الـ modal يستدعي `GET /accept-preview` فور الفتح → loading spinner.
3. لما الـ response يجي، الـ modal يعرض:

#### (أ) البيانات الأصلية (read-only)
من `data.original`:
- Brand: `"Teshla"`
- Model: `"modal 3"`
- Capacity: `"75 kwh"`
- + بيانات المستخدم من الـ GET بتاع الطلب نفسه

#### (ب) تحذيرات النظام
اعرض كل item في `data.warnings` كـ alert banner.
- لو `exactModelMatch != null` → Error red 🔴 ("Would create duplicate")
- لو `similarBrands` فيها items → Warning yellow 🟡 مع اقتراح
- لو `!capacityParseSuccess` → Warning يطلب من الأدمن يحدد capacity يدوي

#### (ج) البيانات النهائية (editable)
- **Brand picker:**
  - Radio 1: "Use existing brand" → dropdown بيحتوي على:
    - `exactBrandMatch` (لو موجود) — selected by default
    - `similarBrands` — كاقتراحات
    - كل الـ brands الموجودة في النظام (ممكن تجيبها من `/api/Brand/GetAllBrands`)
  - Radio 2: "Create new brand" → text input مبدئيًا فيه `original.brandName`

- **Model name:** text input مبدئيًا فيه `original.modelName`

- **Capacity:** number input مبدئيًا فيه `parsedCapacity` (لو `capacityParseSuccess`)، وإلا فاضي

#### (د) Preview قبل الحفظ
بناءً على اختيار الأدمن، اعرض نص واضح:
> "On accept, the system will use existing Brand **Tesla** (ID 30) and create new Model **Model 3** with 75.0 kWh."

#### (هـ) أزرار
- **Accept** → يبعت `POST /accept` بالـ body المعدّل
- **Cancel** → يقفل الـ modal

#### Edge cases
- لو `exactModelMatch != null`، الـ Accept button يفضل disabled ومعه tooltip "A model with this name already exists".
- لو الأدمن غيّر اسم الموديل بعد كده، يفضل يستدعي الـ preview تاني (optional, nice-to-have).

---

## Summary Table

| Method | Endpoint | الغرض |
|--------|----------|-------|
| GET | `/api/admin/vehicle-addition-requests` | قائمة الطلبات (paginated + filter) |
| GET | `/api/admin/vehicle-addition-requests/{id}` | تفاصيل طلب واحد |
| GET | `/api/admin/vehicle-addition-requests/{id}/accept-preview` | Suggestions + warnings للأدمن |
| POST | `/api/admin/vehicle-addition-requests/{id}/accept` | قبول (مع body اختياري للتعديل) |
| POST | `/api/admin/vehicle-addition-requests/{id}/decline` | رفض |

---

**Backend Version:** 2.0 (Adds accept-preview + editable accept)
**Last Updated:** 2026-04-20
