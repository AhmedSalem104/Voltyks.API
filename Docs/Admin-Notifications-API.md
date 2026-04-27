# Admin Notification Center — Frontend Integration Guide

دليل شامل لتنفيذ الـ Admin Notification Center في الداشبورد.

**الـ feature بتغطي 3 قدرات للـ admin:**
1. تعديل نصوص الإشعارات الموجودة (19 template)
2. إرسال إشعار لمستخدم معين (template أو custom)
3. Broadcast لكل المستخدمين أو فئة محددة

---

## Auth & Base

```http
Authorization: Bearer <admin-jwt>
Base URL: https://voltyks.runasp.net
```

كل الـ endpoints تحت `/api/admin/notifications` ومحمية بـ `Role=Admin`.

---

## 1. Template Management

### 1.1 List all templates

```http
GET /api/admin/notifications/templates
```

**Response 200:**
```json
{
  "status": true,
  "message": "Templates fetched",
  "data": [
    {
      "key": "ChargerOwnerConfirmed",
      "titleEn": "Charging Request Confirmed ✅",
      "titleAr": "تم تأكيد طلب الشحن ✅",
      "bodyEn": "The charger {stationOwnerName} confirmed the charging session for your vehicle.",
      "bodyAr": "أكد صاحب الشاحن {stationOwnerName} جلسة الشحن لسيارتك.",
      "requiredParams": ["stationOwnerName"],
      "isCustomized": false,
      "updatedAt": null,
      "updatedBy": null
    }
  ]
}
```

**حقل بحقل:**
| Field | Type | Notes |
|---|---|---|
| `key` | string | معرف ثابت من الكود |
| `titleEn`/`titleAr` | string | النص الحالي (DB لو معدّل، hardcoded لو مش معدّل) |
| `bodyEn`/`bodyAr` | string | الـ body مع `{placeholders}` |
| `requiredParams` | string[] | الـ placeholders اللازم وجودها في الـ body |
| `isCustomized` | bool | `true` لو في DB row override |
| `updatedAt`/`updatedBy` | datetime/userId | متى ومن آخر تعديل (null لو hardcoded) |

**UI hint:** اعرض chip أصفر "Customized" لو `isCustomized=true`. اعرض الـ `requiredParams` كـ tags تحت كل template.

---

### 1.2 Get single template

```http
GET /api/admin/notifications/templates/{key}
```

**Response:** نفس عنصر واحد من الـ list أعلاه.

**Errors:**
```json
{ "status": false, "message": "Unknown template key: Foo", "data": null }
```

---

### 1.3 Update template

```http
PUT /api/admin/notifications/templates/{key}
Content-Type: application/json
```

**Body:**
```json
{
  "titleEn": "New title ✅",
  "titleAr": "عنوان جديد ✅",
  "bodyEn": "Hello {stationOwnerName}, your session is confirmed.",
  "bodyAr": "أهلًا {stationOwnerName}، جلستك مؤكدة."
}
```

**⚠️ Server-side validation:**
- كل `requiredParams` لازم تظهر بصيغة `{paramName}` في **EN body+title** **و** **AR body+title** (أي مكان فيهم).
- ما تقدرش تضيف placeholder غير معروف.

**Possible errors:**
```json
{ "status": false, "message": "Required placeholder(s) not preserved. EN missing: stationOwnerName" }
{ "status": false, "message": "Unknown placeholder(s): foo, bar" }
{ "status": false, "message": "Unknown template key: NotARealKey" }
```

**Client-side validation قبل الإرسال (recommended):**
```js
function validateUpdate(required, dto) {
  const allText = [dto.titleEn, dto.titleAr, dto.bodyEn, dto.bodyAr].join(" ");
  const missing = required.filter(p => !allText.includes(`{${p}}`));
  if (missing.length) {
    return `Add placeholder {${missing.join("}, {")}} to body or title`;
  }
  return null;
}
```

**Response (نفس الـ GET):** الـ template المحدّث بـ `isCustomized=true` و `updatedAt`/`updatedBy` ممتلئين.

---

### 1.4 Preview before save

```http
POST /api/admin/notifications/templates/{key}/preview
```

**Body:**
```json
{
  "lang": "ar",
  "params": { "stationOwnerName": "Abdelrahman" }
}
```

**Response:**
```json
{
  "status": true,
  "data": {
    "title": "تم تأكيد طلب الشحن ✅",
    "body": "أكد صاحب الشاحن Abdelrahman جلسة الشحن لسيارتك.",
    "fromDb": false
  }
}
```

`fromDb`: `true` لو الـ source DB row، `false` لو hardcoded fallback.

**UI hint:** زر "Preview" يفتح modal يعرض الـ rendered output. خلي debounced auto-preview كل ما الادمن يكتب في الـ form.

---

### 1.5 Reset to hardcoded default

```http
DELETE /api/admin/notifications/templates/{key}
```

**Response:**
```json
{ "status": true, "message": "Template reset to hardcoded default", "data": { "key": "...", "reset": true } }
```

يحذف DB row → الـ resolver يرجع للـ hardcoded fallback. مفيد لو الادمن غلط.

---

## 2. Send to a Single User

```http
POST /api/admin/notifications/send-to-user
```

### Mode A: Template موجود

```json
{
  "userId": "eea2a716-3b7a-4db1-8463-d2819150f686",
  "mode": "template",
  "template": {
    "key": "ChargerOwnerConfirmed",
    "params": { "stationOwnerName": "Abdo" }
  }
}
```

### Mode B: Custom message

```json
{
  "userId": "eea2a716-3b7a-4db1-8463-d2819150f686",
  "mode": "custom",
  "custom": {
    "titleEn": "Hello from admin",
    "titleAr": "مرحبًا من الإدارة",
    "bodyEn": "Your wallet balance is low",
    "bodyAr": "رصيدك منخفض"
  }
}
```

**Response (للحالتين):**
```json
{
  "status": true,
  "message": "Notification sent",
  "data": {
    "notificationId": 2891,
    "userId": "eea2a716-...",
    "title": "...",
    "body": "...",
    "pushSent": 1
  }
}
```

| Field | Notes |
|---|---|
| `notificationId` | Row في `Notifications` table (history) |
| `pushSent` | عدد الـ device tokens اللي رجعت 200 من FCM (0 لو اليوزر مش مسجل أي device) |

**Possible errors:**
```json
{ "status": false, "message": "userId is required" }
{ "status": false, "message": "User not found" }
{ "status": false, "message": "template.key is required when mode=template" }
{ "status": false, "message": "Unknown template key: Foo" }
{ "status": false, "message": "Missing template params: stationOwnerName" }
{ "status": false, "message": "custom.titleEn and custom.bodyEn are required" }
```

**ملاحظات سلوكية:**
- اللغة بتتحدد من `user.PreferredLanguage` (en/ar) في الـ DB.
- في `mode=custom`: لو `titleAr`/`bodyAr` فاضيين، الـ EN يُستخدم للـ AR speakers.
- DB row mandatory دايمًا — حتى لو FCM فشل.

**UI form layout:**
```
┌─────────────────────────────────────────────┐
│ User picker  [search by phone/email/name]  │
├─────────────────────────────────────────────┤
│ ◯ Use template     ◯ Custom message        │
├─────────────────────────────────────────────┤
│ [if template]                               │
│   Template: [ dropdown of 19 templates ]   │
│   Required params:                          │
│     • stationOwnerName: [____________]     │
│ [if custom]                                 │
│   Title EN:  [_________________]            │
│   Title AR:  [_________________]            │
│   Body EN:   [_________________]            │
│   Body AR:   [_________________]            │
├─────────────────────────────────────────────┤
│              [ Send notification ]          │
└─────────────────────────────────────────────┘
```

---

## 3. Broadcast

```http
POST /api/admin/notifications/broadcast
```

### Audience options

#### A. كل المستخدمين
```json
{
  "audience": { "type": "all" },
  "mode": "template",
  "template": { "key": "ProcessTerminated", "params": {} }
}
```

#### B. حسب الدور
```json
{
  "audience": { "type": "role", "role": "vehicle_owner" },
  "mode": "custom",
  "custom": {
    "titleEn": "Promotion",
    "titleAr": "عرض",
    "bodyEn": "20% off this week",
    "bodyAr": "خصم 20% هذا الأسبوع"
  }
}
```
الـ role: `"vehicle_owner"` أو `"charger_owner"`.
**ChargerOwner** = اليوزر اللي ربط شاحن واحد على الأقل.
**VehicleOwner** = اللي ما عندوش شواحن.

#### C. قائمة محددة
```json
{
  "audience": { "type": "users", "userIds": ["uuid-1", "uuid-2"] },
  "mode": "template",
  "template": { "key": "VehicleAdditionAccepted", "params": {} }
}
```

#### D. حسب المدينة (NOT IMPLEMENTED YET)
```json
{ "audience": { "type": "city", "city": "Cairo" }, ... }
```
**يرجع 200 + `status:false`** بـ `"audience.type=city is not supported yet"`. هتتفعّل لما `Address.City` يكون منسّق على كل المستخدمين.

### Response

```json
{
  "status": true,
  "message": "Broadcast dispatched",
  "data": {
    "broadcastId": 17,
    "recipientCount": 1240,
    "dbPersistedCount": 1240,
    "fcmAttemptedCount": 980,
    "fcmSucceededCount": 956
  }
}
```

| Counter | Meaning |
|---|---|
| `recipientCount` | عدد المستخدمين المطابقين للـ audience filter (excluding deleted/banned) |
| `dbPersistedCount` | Notification rows اتكتبت في DB (المفروض = recipientCount) |
| `fcmAttemptedCount` | عدد device tokens اللي حاولنا نبعتلها FCM (< recipientCount لو فيه users بدون tokens) |
| `fcmSucceededCount` | FCM رجعت 200 OK (< fcmAttemptedCount لو فيه dead tokens) |

**Audit trail:** كل broadcast بيتسجل في `NotificationBroadcasts` table. الـ admin يقدر يراجع تاريخ الـ broadcasts بـ `broadcastId` و `sentAt`.

### UI form layout

```
┌─────────────────────────────────────────────┐
│ Audience                                    │
│  ◯ All users                                │
│  ◯ By role:  [ Vehicle Owners | Charger Owners ] │
│  ◯ Specific users:  [ multi-select picker ] │
├─────────────────────────────────────────────┤
│ ◯ Use template     ◯ Custom message        │
│  ... (نفس send-to-user)                    │
├─────────────────────────────────────────────┤
│ ⚠️ This will notify ~{estimate} users.     │
│              [ Send broadcast ]             │
└─────────────────────────────────────────────┘
```

**Confirmation modal mandatory** قبل الإرسال (broadcasts ما بترجعش).

**بعد الإرسال:** اعرض success toast بـ counters + لينك للـ audit page (إذا اتنفذ).

### Possible errors

```json
{ "status": false, "message": "audience.role must be vehicle_owner or charger_owner" }
{ "status": false, "message": "audience.userIds is required when type=users" }
{ "status": false, "message": "audience.type=city is not supported yet" }
{ "status": false, "message": "Unknown audience.type: foo" }
```

---

## 4. Error Response Format (موحّد)

كل الـ endpoints بترجع 200 status code مع shape:
```json
{ "status": true|false, "message": "...", "data": {...}|null, "errors": null }
```

`status:false` بيشير للـ business validation error. الـ HTTP 4xx/5xx بيكون لـ auth/permission/server failures فقط.

---

## 5. UI Recommendations

### Sidebar / Menu

```
Admin Panel
├── ...
└── Notifications
    ├── 📥 Inbox          (existing — admin's received notifications)
    ├── 📝 Templates       ← Section 1
    ├── 👤 Send to User    ← Section 2
    ├── 📢 Broadcast       ← Section 3
    └── 📜 History         (broadcasts audit log — optional, future)
```

### Templates page

- **Search/filter:** by `key` (substring match)
- **Group by category:** Charging Lifecycle (5), Process (8), Reports (1), Vehicle Addition (2), Other (3)
- **Card layout:** EN/AR side-by-side، edit-in-place
- **Inline preview:** debounced POST لـ `/preview` كل ما الادمن يكتب
- **Required params panel:** chips قابلة للنسخ بـ click، الادمن يقدر يضيفهم في الـ textarea بسرعة
- **Reset button:** بـ confirmation "This will revert to factory default"

### Common helpers

- **Placeholder helper component:** input field يعرض الـ `requiredParams` كـ chips، click → يضيف `{paramName}` للـ cursor position
- **Lang switcher:** زر يقلب بين EN/AR في الـ preview pane
- **Live char counter:** Title 255 max, Body 2000 max

---

## 6. Performance & Caching

- الـ resolver في الـ backend عنده `IMemoryCache` بـ TTL 5 دقائق
- `PUT`/`DELETE` بيعمل invalidation فورية → الادمن يشوف التعديل **في next notification immediately**
- Monster ASP بـ instance واحد → الـ cache consistent
- لو app pool recycled، الـ cache يتفضى ويعاد ملؤه من DB (شفافة للأدمن)

---

## 7. Quick QA Sequence

```bash
TOKEN=<your-admin-jwt>
ABDO_ID="0019e8fc-a4c2-4ea1-8df2-560a8a2ac616"
BASE=https://voltyks.runasp.net

# 1. List templates (expect 19)
curl -s "$BASE/api/admin/notifications/templates" -H "Authorization: Bearer $TOKEN"

# 2. Preview before any edits
curl -s -X POST "$BASE/api/admin/notifications/templates/ChargerOwnerConfirmed/preview" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"lang":"ar","params":{"stationOwnerName":"Abdo"}}'

# 3. Update template
curl -s -X PUT "$BASE/api/admin/notifications/templates/ChargerOwnerConfirmed" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"titleEn":"TEST EN","titleAr":"اختبار","bodyEn":"Hello {stationOwnerName}","bodyAr":"أهلًا {stationOwnerName}"}'

# 4. Validation error: missing placeholder
curl -s -X PUT "$BASE/api/admin/notifications/templates/ChargerOwnerConfirmed" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"titleEn":"X","titleAr":"X","bodyEn":"No placeholder","bodyAr":"بدون"}' \
# → "Required placeholder(s) not preserved. EN missing: stationOwnerName"

# 5. Send to user (custom)
curl -s -X POST "$BASE/api/admin/notifications/send-to-user" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d "{\"userId\":\"$ABDO_ID\",\"mode\":\"custom\",\"custom\":{\"titleEn\":\"Hi\",\"titleAr\":\"اهلا\",\"bodyEn\":\"Test\",\"bodyAr\":\"اختبار\"}}"

# 6. Broadcast to specific users
curl -s -X POST "$BASE/api/admin/notifications/broadcast" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d "{\"audience\":{\"type\":\"users\",\"userIds\":[\"$ABDO_ID\"]},\"mode\":\"template\",\"template\":{\"key\":\"ProcessTerminated\",\"params\":{}}}"

# 7. Reset
curl -s -X DELETE "$BASE/api/admin/notifications/templates/ChargerOwnerConfirmed" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 8. Template Catalogue (19 keys)

| Key | Required Params | Original EN Title |
|---|---|---|
| `VehicleAdditionAccepted` | — | Request Accepted |
| `VehicleAdditionDeclined` | — | Request Declined |
| `VehicleOwnerRequestCharger` | — | New Charging Request 🚗 |
| `ChargerOwnerAccepted` | stationOwnerName | Charging Request Accepted |
| `ChargerOwnerRejected` | stationOwnerName | Charging Request Rejected ❌ |
| `ChargerOwnerConfirmed` | stationOwnerName | Charging Request Confirmed ✅ |
| `ChargerOwnerAborted` | — | Charging session aborted |
| `VehicleOwnerAbortedAfterPayment` | driverName | Request Aborted ❌ |
| `ProcessConfirmationPending` | amountCharged, amountPaid | Process confirmation pending |
| `VehicleOwnerUpdateProcess` | — | Process updated |
| `VehicleOwnerUpdateProcessWithFields` | fieldsCsv | Process updated |
| `SubmitRating` | rating, processId | New rating received ⭐ |
| `ProcessTerminated` | — | Process Terminated |
| `ProcessExpired` | — | Process Terminated |
| `ProcessConfirmedByCharger` | — | Process confirmed |
| `ProcessConfirmedByVehicle` | — | Process confirmed |
| `ProcessStarted` | whoStarted | Process started |
| `DefaultRatingApplied` | rating, processId | Default rating applied |
| `ReportFiled` | reporterName | {reporterName} filed a report against you |

---

## 9. What to Build Next (Future)

- **Audit log page:** `GET /api/admin/notifications/broadcasts` — list past broadcasts بـ counters و filters
- **Scheduled broadcasts:** "send tomorrow at 10am"
- **City audience:** بعد ما `Address.City` يكون مظبوط على كل المستخدمين
- **Open-rate analytics:** يحتاج FCM delivery callbacks من الموبايل
- **Template categories/grouping:** metadata في الـ registry

---

## 10. Migration Note

الـ feature بتعتمد على table جديد `NotificationTemplates` و `NotificationBroadcasts`. الـ migration `AddNotificationCenter` لازم تتطبق على production DB. لو الـ Templates endpoint بيرجع 500 بـ DB error، يبقى محتاج:

```bash
dotnet ef database update --project Voltyks.Persistence --startup-project Voltyks.API
```

أو الـ SQL migration يدوي على الـ Monster ASP DB.
