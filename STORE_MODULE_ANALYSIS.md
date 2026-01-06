# Store Module - Technical Analysis Document

## Overview

نظام متجر بسيط يتيح للمستخدمين تصفح المنتجات وحجزها مجانا، ثم يتواصل فريق التطبيق معهم لاتمام عملية البيع.

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  User يتصفح  │ ──► │  User يحجز   │ ──► │ فريقك يتواصل │
│   المنتجات   │     │   المنتج     │     │    معاه      │
└──────────────┘     └──────────────┘     └──────────────┘
```

### خارج النطاق (Out of Scope)
- لا يوجد دفع أونلاين
- لا يوجد سلة مشتريات
- لا يوجد نظام شحن

---

## Database Schema

### 1. Categories Table

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | BIGINT | PK, AUTO_INCREMENT | المعرف الفريد |
| name | VARCHAR(255) | NOT NULL | اسم القسم |
| slug | VARCHAR(255) | UNIQUE, NOT NULL | الرابط المختصر |
| status | ENUM | NOT NULL | active, coming_soon, hidden |
| sort_order | INT | DEFAULT 0 | ترتيب العرض |
| icon | VARCHAR(100) | NULLABLE | أيقونة القسم |
| placeholder_message | TEXT | NULLABLE | رسالة Coming Soon |
| created_at | TIMESTAMP | NOT NULL | تاريخ الانشاء |
| updated_at | TIMESTAMP | NOT NULL | تاريخ التحديث |
| deleted_at | TIMESTAMP | NULLABLE | Soft Delete |

### 2. Products Table

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | BIGINT | PK, AUTO_INCREMENT | المعرف الفريد |
| category_id | BIGINT | FK → categories.id | القسم التابع له |
| name | VARCHAR(255) | NOT NULL | اسم المنتج |
| slug | VARCHAR(255) | UNIQUE | الرابط المختصر |
| description | TEXT | NULLABLE | وصف المنتج |
| price | DECIMAL(10,2) | NOT NULL | السعر |
| currency | VARCHAR(3) | DEFAULT 'EGP' | العملة |
| images | JSON | NULLABLE | صور المنتج |
| specifications | JSON | NULLABLE | المواصفات الفنية |
| status | ENUM | NOT NULL | active, out_of_stock, hidden |
| is_reservable | BOOLEAN | DEFAULT true | قابل للحجز |
| created_at | TIMESTAMP | NOT NULL | تاريخ الانشاء |
| updated_at | TIMESTAMP | NOT NULL | تاريخ التحديث |
| deleted_at | TIMESTAMP | NULLABLE | Soft Delete |

### 3. Reservations Table

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | BIGINT | PK, AUTO_INCREMENT | المعرف الفريد |
| user_id | BIGINT | FK → users.id | المستخدم |
| product_id | BIGINT | FK → products.id | المنتج المحجوز |
| quantity | INT | NOT NULL, DEFAULT 1 | الكمية المطلوبة |
| unit_price | DECIMAL(10,2) | NOT NULL | سعر الوحدة وقت الحجز |
| total_price | DECIMAL(10,2) | NOT NULL | السعر الاجمالي |
| status | ENUM | NOT NULL | pending, contacted, completed, cancelled |
| **payment_status** | ENUM | DEFAULT 'unpaid' | unpaid, paid |
| **payment_method** | VARCHAR(50) | NULLABLE | cash, bank_transfer, instapay, vodafone_cash, etc. |
| **payment_reference** | VARCHAR(100) | NULLABLE | رقم مرجعي للدفع |
| **paid_amount** | DECIMAL(10,2) | NULLABLE | المبلغ المدفوع فعليا |
| **paid_at** | TIMESTAMP | NULLABLE | تاريخ الدفع |
| **delivery_status** | ENUM | DEFAULT 'pending' | pending, delivered |
| **delivered_at** | TIMESTAMP | NULLABLE | تاريخ الاستلام |
| **delivery_notes** | TEXT | NULLABLE | ملاحظات الاستلام |
| admin_notes | TEXT | NULLABLE | ملاحظات الفريق |
| contacted_at | TIMESTAMP | NULLABLE | تاريخ التواصل |
| created_at | TIMESTAMP | NOT NULL | تاريخ الحجز |
| updated_at | TIMESTAMP | NOT NULL | تاريخ التحديث |

### 4. Payment Methods (Reference Table - Optional)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | BIGINT | PK, AUTO_INCREMENT | المعرف الفريد |
| code | VARCHAR(50) | UNIQUE, NOT NULL | كود الطريقة |
| name | VARCHAR(100) | NOT NULL | اسم الطريقة |
| is_active | BOOLEAN | DEFAULT true | مفعلة أم لا |
| sort_order | INT | DEFAULT 0 | ترتيب العرض |

**Default Payment Methods:**
| Code | Name |
|------|------|
| cash | كاش |
| bank_transfer | تحويل بنكي |
| instapay | انستاباي |
| vodafone_cash | فودافون كاش |
| fawry | فوري |
| other | أخرى |

---

## User Flow

```
1. يفتح Store
       ↓
2. يختار Category (Chargers مثلا)
       ↓
3. يشوف قائمة المنتجات + يقدر يعمل Search
       ↓
4. يضغط على منتج ← صفحة التفاصيل
       ↓
5. يحدد الكمية المطلوبة (Quantity Selector)
       ↓
6. يضغط "Reserve For Free"
       ↓
7. ✓ Thank You + "فريقنا هيتواصل معاك"
       ↓
8. المنتج يظهرله "Already Reserved"
       ↓
9. (اختياري) يقدر يعدل الكمية أو يلغي من My Reservations
```

---

## Category States

| Status | السلوك | Search |
|--------|--------|--------|
| `active` | يعرض المنتجات | ✅ يعمل |
| `coming_soon` | يعرض Placeholder Message | ⚠️ يعمل بدون نتائج |
| `hidden` | لا يظهر للمستخدم | ❌ |

### UI States

**Active Category:**
```
┌─────────────────────────────────┐
│ 🔍 Search bar (enabled)         │
│ 📦 Products Grid/List           │
└─────────────────────────────────┘
```

**Coming Soon Category:**
```
┌─────────────────────────────────┐
│ 🔍 Search bar (enabled)         │
│                                 │
│     🎉 Coming Very Soon         │
│        Stay Tuned!              │
└─────────────────────────────────┘
```

**Empty Search Results:**
```
┌─────────────────────────────────┐
│ "No results for what you're     │
│  searching for"                 │
└─────────────────────────────────┘
```

---

## API Endpoints

### Public APIs (للمستخدم)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/store/categories` | جلب الأقسام المتاحة |
| GET | `/store/products` | جلب المنتجات مع الفلترة |
| GET | `/store/products/{id}` | تفاصيل منتج |
| POST | `/store/reservations` | انشاء حجز جديد |
| GET | `/store/my-reservations` | حجوزات المستخدم |
| PUT | `/store/my-reservations/{id}` | تعديل كمية الحجز |
| DELETE | `/store/my-reservations/{id}` | الغاء الحجز |

### Admin APIs (للوحة التحكم)

#### Categories CRUD

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/categories` | جلب كل الأقسام |
| GET | `/admin/categories/{id}` | تفاصيل قسم |
| POST | `/admin/categories` | انشاء قسم |
| PUT | `/admin/categories/{id}` | تعديل قسم |
| DELETE | `/admin/categories/{id}` | Soft Delete |
| POST | `/admin/categories/{id}/restore` | استرجاع محذوف |
| DELETE | `/admin/categories/{id}/force` | حذف نهائي |

#### Products CRUD

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/products` | جلب كل المنتجات |
| GET | `/admin/products/{id}` | تفاصيل منتج |
| POST | `/admin/products` | انشاء منتج |
| PUT | `/admin/products/{id}` | تعديل منتج |
| DELETE | `/admin/products/{id}` | Soft Delete |
| POST | `/admin/products/{id}/restore` | استرجاع محذوف |
| DELETE | `/admin/products/{id}/force` | حذف نهائي |

#### Reservations Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/admin/reservations` | جلب كل الحجوزات |
| GET | `/admin/reservations/{id}` | تفاصيل حجز |
| PUT | `/admin/reservations/{id}` | تحديث حالة الحجز |
| PUT | `/admin/reservations/{id}/contact` | تسجيل التواصل مع العميل |
| PUT | `/admin/reservations/{id}/payment` | تسجيل الدفع |
| PUT | `/admin/reservations/{id}/delivery` | تسجيل الاستلام |
| PUT | `/admin/reservations/{id}/complete` | اتمام العملية |
| PUT | `/admin/reservations/{id}/cancel` | الغاء الحجز |

---

## API Request/Response Examples

### GET `/store/categories`

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Chargers",
      "slug": "chargers",
      "status": "active",
      "sort_order": 1,
      "icon": "bolt"
    },
    {
      "id": 2,
      "name": "Scooters",
      "slug": "scooters",
      "status": "coming_soon",
      "sort_order": 2,
      "icon": "scooter",
      "placeholder_message": "Coming Very Soon Stay Tuned!"
    }
  ]
}
```

### GET `/store/products?category_id=1&search=voltyks`

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Voltyks 16A 3Phase 11KW",
      "slug": "voltyks-16a-3phase-11kw",
      "price": 6000,
      "currency": "EGP",
      "image": "https://...",
      "status": "active"
    }
  ],
  "meta": {
    "total": 1,
    "page": 1,
    "per_page": 10
  }
}
```

### GET `/store/products/{id}`

**Response:**
```json
{
  "data": {
    "id": 1,
    "name": "Voltyks 16A 3Phase 11KW",
    "slug": "voltyks-16a-3phase-11kw",
    "description": "Full description here...",
    "price": 6000,
    "currency": "EGP",
    "images": [
      "https://...",
      "https://..."
    ],
    "specifications": {
      "power": "11KW",
      "phase": "3Phase",
      "ampere": "16A"
    },
    "status": "active",
    "is_reservable": true,
    "is_reserved_by_user": false,
    "category": {
      "id": 1,
      "name": "Chargers",
      "slug": "chargers"
    }
  }
}
```

### POST `/store/reservations`

**Request:**
```json
{
  "product_id": 1,
  "quantity": 2
}
```

**Response:**
```json
{
  "data": {
    "id": 1,
    "product_id": 1,
    "quantity": 2,
    "unit_price": 6000,
    "total_price": 12000,
    "currency": "EGP",
    "status": "pending",
    "created_at": "2024-01-15T10:30:00Z"
  },
  "message": "Reservation created successfully. Our team will contact you soon."
}
```

### GET `/store/my-reservations`

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "product": {
        "id": 1,
        "name": "Voltyks 16A 3Phase 11KW",
        "image": "https://..."
      },
      "quantity": 2,
      "unit_price": 6000,
      "total_price": 12000,
      "currency": "EGP",
      "status": "pending",
      "created_at": "2024-01-15T10:30:00Z"
    }
  ]
}
```

### PUT `/store/my-reservations/{id}`

**Request:**
```json
{
  "quantity": 3
}
```

**Response:**
```json
{
  "data": {
    "id": 1,
    "quantity": 3,
    "unit_price": 6000,
    "total_price": 18000,
    "status": "pending"
  },
  "message": "Reservation updated successfully"
}
```

**Note:** لا يمكن تعديل الحجز اذا كانت الحالة `contacted` أو `completed`

### DELETE `/store/my-reservations/{id}`

**Response:**
```json
{
  "message": "Reservation cancelled successfully"
}
```

**Note:** لا يمكن الغاء الحجز اذا كانت الحالة `completed`

---

### Admin APIs Examples

#### PUT `/admin/reservations/{id}/contact`

تسجيل التواصل مع العميل

**Request:**
```json
{
  "admin_notes": "تم التواصل عبر الهاتف، العميل موافق"
}
```

**Response:**
```json
{
  "data": {
    "id": 1,
    "status": "contacted",
    "contacted_at": "2024-01-15T14:30:00Z",
    "admin_notes": "تم التواصل عبر الهاتف، العميل موافق"
  },
  "message": "Contact recorded successfully"
}
```

---

#### PUT `/admin/reservations/{id}/payment`

تسجيل الدفع

**Request:**
```json
{
  "payment_method": "instapay",
  "paid_amount": 12000,
  "payment_reference": "INS-123456789"
}
```

**Response:**
```json
{
  "data": {
    "id": 1,
    "payment_status": "paid",
    "payment_method": "instapay",
    "paid_amount": 12000,
    "payment_reference": "INS-123456789",
    "paid_at": "2024-01-15T16:00:00Z"
  },
  "message": "Payment recorded successfully"
}
```

---

#### PUT `/admin/reservations/{id}/delivery`

تسجيل الاستلام

**Request:**
```json
{
  "delivery_notes": "تم التسليم في فرع مدينة نصر"
}
```

**Response:**
```json
{
  "data": {
    "id": 1,
    "delivery_status": "delivered",
    "delivered_at": "2024-01-16T10:00:00Z",
    "delivery_notes": "تم التسليم في فرع مدينة نصر"
  },
  "message": "Delivery recorded successfully"
}
```

---

#### PUT `/admin/reservations/{id}/complete`

اتمام العملية بالكامل

**Response:**
```json
{
  "data": {
    "id": 1,
    "status": "completed",
    "payment_status": "paid",
    "delivery_status": "delivered"
  },
  "message": "Reservation completed successfully"
}
```

**Validation:** لا يمكن اتمام العملية الا اذا:
- `payment_status = paid`
- `delivery_status = delivered`

---

#### GET `/admin/reservations?status=contacted&payment_status=unpaid`

**Query Parameters:**
- `status` → فلترة بحالة الحجز
- `payment_status` → فلترة بحالة الدفع
- `delivery_status` → فلترة بحالة الاستلام
- `from_date` → من تاريخ
- `to_date` → الى تاريخ
- `search` → بحث باسم العميل أو المنتج

### POST `/admin/categories`

**Request:**
```json
{
  "name": "Batteries",
  "slug": "batteries",
  "status": "coming_soon",
  "sort_order": 4,
  "icon": "battery",
  "placeholder_message": "Coming Very Soon Stay Tuned!"
}
```

**Response:**
```json
{
  "data": {
    "id": 4,
    "name": "Batteries",
    "slug": "batteries",
    "status": "coming_soon",
    "sort_order": 4,
    "icon": "battery",
    "placeholder_message": "Coming Very Soon Stay Tuned!",
    "created_at": "2024-01-15T10:30:00Z",
    "updated_at": "2024-01-15T10:30:00Z",
    "deleted_at": null
  },
  "message": "Category created successfully"
}
```

### GET `/admin/categories?with_trashed=true`

**Query Parameters:**
- `with_trashed=true` → يجيب المحذوف كمان
- `only_trashed=true` → يجيب المحذوف بس
- `status=active` → فلترة بالحالة

---

## Validation Rules

### Category Validation

| Field | Rules |
|-------|-------|
| name | required, string, max:255, unique (not deleted) |
| slug | optional (auto-generate), unique, lowercase |
| status | required, in: [active, coming_soon, hidden] |
| sort_order | required, integer, min:0 |
| placeholder_message | required_if: status=coming_soon, string, max:500 |

### Product Validation

| Field | Rules |
|-------|-------|
| category_id | required, exists:categories,id |
| name | required, string, max:255 |
| slug | optional (auto-generate), unique |
| description | nullable, string |
| price | required, numeric, min:0 |
| currency | required, string, size:3 |
| images | nullable, array |
| images.* | url or file |
| specifications | nullable, json |
| status | required, in: [active, out_of_stock, hidden] |
| is_reservable | boolean |

### Reservation Validation

| Field | Rules |
|-------|-------|
| product_id | required, exists:products,id |
| quantity | required, integer, min:1, max:100 |

**Business Validation:**
- المستخدم يقدر يحجز نفس المنتج مرة واحدة بس (unique: user_id + product_id)
- لو عايز يغير الكمية، يعدل الحجز الموجود أو يلغيه ويحجز تاني

### Payment Recording Validation (Admin)

| Field | Rules |
|-------|-------|
| payment_method | required, in: [cash, bank_transfer, instapay, vodafone_cash, fawry, other] |
| paid_amount | required, numeric, min:0 |
| payment_reference | nullable, string, max:100 |

**Business Validation:**
- لا يمكن تسجيل الدفع الا اذا كانت حالة الحجز `contacted`
- لا يمكن تعديل بيانات الدفع بعد تسجيلها (يجب الغاء واعادة)

### Delivery Recording Validation (Admin)

| Field | Rules |
|-------|-------|
| delivery_notes | nullable, string, max:500 |

**Business Validation:**
- لا يمكن تسجيل الاستلام الا اذا كان الدفع `paid`

---

## Soft Delete Logic

### عند الحذف (Soft Delete)
```
├── deleted_at = now()
├── البيانات المرتبطة تفضل موجودة
└── لا تظهر للمستخدم في الـ Public APIs
```

### عند الاسترجاع (Restore)
```
├── deleted_at = null
└── كل حاجة ترجع تظهر طبيعي
```

### الحذف النهائي (Force Delete)
```
├── لازم يكون soft deleted أولا
├── أو مفيش بيانات مرتبطة
└── يتم حذف السجل نهائيا من قاعدة البيانات
```

### Force Delete Constraints

**Category:**
- لا يمكن حذف category نهائيا اذا كان يحتوي على منتجات
- يجب نقل المنتجات أو حذفها أولا

**Product:**
- لا يمكن حذف product نهائيا اذا كان له حجوزات
- يجب الغاء أو اكمال الحجوزات أولا

---

## Reservation Statuses

### Main Status (حالة الحجز الرئيسية)
```
pending ──────► contacted ──────► completed
    │               │
    └───────────────┴──────────► cancelled
```

| Status | Description |
|--------|-------------|
| `pending` | حجز جديد - منتظر التواصل |
| `contacted` | تم التواصل مع العميل |
| `completed` | تمت العملية بالكامل |
| `cancelled` | تم الالغاء |

---

### Payment Status (حالة الدفع)
```
unpaid ──────► paid
```

| Status | Description |
|--------|-------------|
| `unpaid` | لم يتم الدفع بعد |
| `paid` | تم الدفع |

### Payment Methods (طرق الدفع)
| Method | Description |
|--------|-------------|
| `cash` | كاش |
| `bank_transfer` | تحويل بنكي |
| `instapay` | انستاباي |
| `vodafone_cash` | فودافون كاش |
| `fawry` | فوري |
| `other` | طريقة أخرى |

---

### Delivery Status (حالة الاستلام)
```
pending ──────► delivered
```

| Status | Description |
|--------|-------------|
| `pending` | لم يتم الاستلام بعد |
| `delivered` | تم الاستلام |

---

### Complete Flow
```
┌─────────────────────────────────────────────────────────────────┐
│                    RESERVATION LIFECYCLE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. User creates reservation                                    │
│     status: pending                                             │
│     payment_status: unpaid                                      │
│     delivery_status: pending                                    │
│                    ↓                                            │
│  2. Admin contacts user                                         │
│     status: contacted ✓                                         │
│                    ↓                                            │
│  3. User pays (offline)                                         │
│     payment_status: paid ✓                                      │
│     payment_method: cash/bank/instapay...                       │
│     paid_amount: 12000                                          │
│     paid_at: 2024-01-15                                         │
│                    ↓                                            │
│  4. User receives product                                       │
│     delivery_status: delivered ✓                                │
│     delivered_at: 2024-01-16                                    │
│                    ↓                                            │
│  5. Process complete                                            │
│     status: completed ✓                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Business Rules

### Categories
- ✅ Categories ديناميكية (تتجاب من الـ API)
- ✅ Category ممكن تكون active أو coming_soon أو hidden
- ✅ الترتيب بـ sort_order
- ✅ Soft Delete مدعوم

### Products
- ✅ كل منتج تابع لـ Category واحد
- ✅ المنتج يظهر فقط اذا الـ Category كانت active
- ✅ Soft Delete مدعوم

### Reservations
- ✅ المستخدم يحجز مرة واحدة فقط لكل منتج
- ✅ المستخدم يقدر يحدد الكمية المطلوبة
- ✅ المستخدم يقدر يعدل الكمية (لو الحالة pending)
- ✅ المستخدم يقدر يلغي الحجز (لو الحالة مش completed)
- ✅ الحجز مجاني - الفريق يتواصل لاحقا
- ✅ بعد الحجز المنتج يظهر "Already Reserved"
- ✅ المستخدم يقدر يشوف حجوزاته
- ✅ السعر يتحسب تلقائيا (unit_price × quantity)

### Payment & Delivery Rules
- ✅ الدفع يتم خارج التطبيق (offline)
- ✅ الأدمن يسجل الدفع بالطريقة والمبلغ والرقم المرجعي
- ✅ الأدمن يسجل الاستلام مع ملاحظات
- ✅ لا يمكن اتمام العملية الا بعد الدفع والاستلام
- ✅ لا يمكن تعديل الحجز بعد تسجيل الدفع
- ✅ لا يمكن الغاء الحجز بعد الاستلام

### Search
- ✅ Search يشتغل في الـ active categories فقط
- ✅ يبحث في اسم المنتج والوصف

---

## Notifications

| Event | للمستخدم | للأدمن |
|-------|----------|--------|
| حجز جديد | ✅ "تم الحجز بنجاح" | ✅ "حجز جديد من [اسم]" |
| تم التواصل | ✅ "فريقنا تواصل معاك" | - |
| تم الدفع | ✅ "تم تأكيد الدفع بنجاح" | - |
| تم الاستلام | ✅ "تم تأكيد استلام المنتج" | - |
| اكتملت العملية | ✅ "شكرا لك! تمت العملية بنجاح" | ✅ "تمت عملية [اسم]" |
| الغاء الحجز | ✅ "تم الغاء الحجز" | ✅ "تم الغاء حجز [اسم]" |

---

## Admin Panel Screens

### Categories Management
```
┌──────────────────────────────────────────────────────────────┐
│  Categories                              [+ Add Category]    │
├──────────────────────────────────────────────────────────────┤
│  Filter: [All ▾] [Active] [Coming Soon] [Hidden] [Deleted]  │
├──────────────────────────────────────────────────────────────┤
│  │ # │ Name      │ Status      │ Products │ Actions        │
│  ├───┼───────────┼─────────────┼──────────┼────────────────┤
│  │ 1 │ Chargers  │ 🟢 Active   │ 5        │ [✏️] [🗑️]      │
│  │ 2 │ Scooters  │ 🟡 Coming   │ 0        │ [✏️] [🗑️]      │
│  │ 3 │ Access... │ 🟡 Coming   │ 0        │ [✏️] [🗑️]      │
│  │ 4 │ Batteries │ 🔴 Deleted  │ 2        │ [↩️] [❌]      │
└──────────────────────────────────────────────────────────────┘
```

### Reservations Management
```
┌────────────────────────────────────────────────────────────────────────────────────────────┐
│  Reservations                                                      [Export] [Filter ▾]    │
├────────────────────────────────────────────────────────────────────────────────────────────┤
│  Filter: [All] [Pending] [Contacted] [Paid] [Delivered] [Completed] [Cancelled]           │
├────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                            │
│ │ # │العميل│ المنتج  │ الكمية│ الاجمالي  │ الحالة    │ الدفع    │ الاستلام │ Actions     │
│ ├───┼──────┼─────────┼───────┼───────────┼───────────┼──────────┼──────────┼─────────────┤
│ │ 1 │ أحمد │ Voltyks │ 2     │ 12,000 EGP│ 🟡 pending│ ⏳ unpaid│ ⏳ pending│[📞][💳][📦]│
│ │ 2 │ محمد │ Voltyks │ 1     │ 6,000 EGP │ 🟢contacted│ ✅ paid  │ ⏳ pending│[📦][✓]     │
│ │ 3 │ علي  │ Voltyks │ 3     │ 18,000 EGP│ ✅completed│ ✅ paid  │ ✅delivered│[👁️]        │
│ │ 4 │ سارة │ Voltyks │ 1     │ 6,000 EGP │ 🔴cancelled│ ⏳ unpaid│ ⏳ pending│[👁️]        │
│                                                                                            │
└────────────────────────────────────────────────────────────────────────────────────────────┘

Actions Legend:
[📞] = تسجيل التواصل
[💳] = تسجيل الدفع
[📦] = تسجيل الاستلام
[✓]  = اتمام العملية
[👁️] = عرض التفاصيل
```

### Reservation Detail View (Admin)
```
┌─────────────────────────────────────────────────────────────┐
│  Reservation #1                                    [Back]   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  👤 العميل: أحمد محمد                                       │
│  📱 الهاتف: 01012345678                                     │
│  📧 الايميل: ahmed@email.com                                │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📦 المنتج: Voltyks 16A 3Phase 11KW                        │
│  🔢 الكمية: 2                                               │
│  💰 سعر الوحدة: 6,000 EGP                                   │
│  💵 الاجمالي: 12,000 EGP                                    │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📊 Status Timeline:                                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ✅ Created      │ 15 Jan 2024, 10:30 AM            │   │
│  │ ✅ Contacted    │ 15 Jan 2024, 02:30 PM            │   │
│  │ ✅ Paid         │ 15 Jan 2024, 04:00 PM            │   │
│  │    (InstaPay - INS-123456789)                      │   │
│  │ ⏳ Delivered    │ Pending                          │   │
│  │ ⏳ Completed    │ Pending                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📝 Admin Notes:                                            │
│  "تم التواصل عبر الهاتف، العميل موافق"                      │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [📦 تسجيل الاستلام]  [❌ الغاء الحجز]                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Future Scalability

هذا التصميم يدعم التوسع المستقبلي لاضافة:

| Feature | Ready? | Notes |
|---------|--------|-------|
| Multi-Currency | ✅ | حقل currency موجود |
| Product Variants | 🔄 | يحتاج جدول variants |
| Online Payment | 🔄 | يحتاج جدول orders |
| Inventory Management | 🔄 | يحتاج حقل stock |
| Reviews & Ratings | 🔄 | يحتاج جدول reviews |
| Wishlist | 🔄 | يحتاج جدول wishlists |

---

## Summary

```
✅ نظام متجر بسيط
✅ عرض منتجات حسب الأقسام
✅ حجز مجاني مع تحديد الكمية
✅ تسجيل الحجوزات
✅ الفريق يتواصل offline
✅ تتبع حالة الدفع (خارج التطبيق)
✅ تتبع حالة الاستلام
✅ CRUD كامل للأقسام والمنتجات
✅ Soft Delete مدعوم
✅ قابل للتوسع مستقبلا

❌ بدون دفع أونلاين (الدفع خارج التطبيق)
❌ بدون سلة
❌ بدون شحن
```
