# ChargingMode Feature - دليل الـ Frontend

## Base URL
```
https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net
```

---

## ما هو ChargingMode؟

`ChargingMode` هو Feature Flag للتحكم في سلوك الشواحن في التطبيق:

| الحالة | الوصف |
|--------|-------|
| `chargingModeEnabled = false` | وضع الإعداد - الشواحن الجديدة تكون غير نشطة |
| `chargingModeEnabled = true` | وضع التشغيل الكامل - الشواحن تعمل بشكل طبيعي |

---

## الـ Endpoints

### 1. Public Endpoint (بدون Authentication)

#### معرفة حالة ChargingMode
```http
GET /api/v1/app-config/charging-mode-status
```

**Response عندما `chargingModeEnabled = false`:**
```json
{
  "status": true,
  "message": "Success",
  "data": {
    "chargingModeEnabled": false,
    "enabledAt": null,
    "updatedBy": null,
    "updatedAt": "2025-12-29T16:05:43",
    "message": "Charging is in setup mode. New chargers will be activated by admin."
  },
  "errors": null
}
```

**Response عندما `chargingModeEnabled = true`:**
```json
{
  "status": true,
  "message": "Success",
  "data": {
    "chargingModeEnabled": true,
    "enabledAt": "2025-12-29T18:00:00",
    "updatedBy": "a54c6277-5a35-48b3-ab13-c626f367189c",
    "updatedAt": "2025-12-29T18:00:00",
    "message": "Charging is fully operational"
  },
  "errors": null
}
```

---

### 2. Admin Endpoints (يتطلب Admin Token)

#### أ) عرض حالة ChargingMode
```http
GET /api/admin/settings/charging-mode
Authorization: Bearer {ADMIN_TOKEN}
```

**Response:**
```json
{
  "status": true,
  "message": "Success",
  "data": {
    "chargingModeEnabled": false,
    "enabledAt": null,
    "updatedBy": null,
    "updatedAt": "2025-12-29T16:05:43",
    "message": "Charging is in setup mode. New chargers will be activated by admin."
  },
  "errors": null
}
```

---

#### ب) تفعيل/تعطيل ChargingMode
```http
PATCH /api/admin/settings/charging-mode
Authorization: Bearer {ADMIN_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "enabled": true
}
```

**Response (نجاح):**
```json
{
  "status": true,
  "message": "Charging mode enabled successfully",
  "data": true,
  "errors": null
}
```

---

#### ج) تفعيل كل الشواحن غير النشطة دفعة واحدة
```http
POST /api/admin/settings/activate-all-chargers
Authorization: Bearer {ADMIN_TOKEN}
```

**Response:**
```json
{
  "status": true,
  "message": "15 chargers activated successfully",
  "data": 15,
  "errors": null
}
```

---

## السلوك حسب الحالة

### عندما `chargingModeEnabled = false`

| الحدث | السلوك |
|-------|--------|
| إضافة شاحن جديد | يُنشأ كـ `Inactive` تلقائياً |
| محاولة تفعيل شاحن Inactive | **مرفوض** - يرجع خطأ |
| تعطيل شاحن Active | **مسموح** |

**خطأ محاولة تفعيل شاحن:**
```json
{
  "status": false,
  "message": "Cannot activate charger. Charging mode is not enabled yet. Please wait for admin activation.",
  "data": false,
  "errors": null
}
```

---

### عندما `chargingModeEnabled = true`

| الحدث | السلوك |
|-------|--------|
| إضافة شاحن جديد | يُنشأ كـ `Active` تلقائياً |
| محاولة تفعيل شاحن Inactive | **مسموح** |
| تعطيل شاحن Active | **مسموح** |

---

## ملاحظة مهمة

**تغيير قيمة `chargingModeEnabled` لا يؤثر على الشواحن الموجودة!**

- الشواحن الـ Inactive تظل Inactive
- الشواحن الـ Active تظل Active
- التغيير يؤثر فقط على:
  1. الشواحن الجديدة
  2. إمكانية التفعيل اليدوي

لتفعيل الشواحن القديمة، استخدم:
```http
POST /api/admin/settings/activate-all-chargers
```

---

## تطبيق الـ Frontend

### 1. عند بدء التطبيق

```javascript
// جلب حالة ChargingMode عند فتح التطبيق
const fetchChargingModeStatus = async () => {
  const response = await fetch(
    'https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/v1/app-config/charging-mode-status'
  );
  const data = await response.json();

  // تخزين الحالة في الـ State
  return data.data.chargingModeEnabled;
};

// في App.js أو main entry point
useEffect(() => {
  fetchChargingModeStatus().then(enabled => {
    setChargingModeEnabled(enabled);
    // أو استخدام Redux/Context
    dispatch(setChargingMode(enabled));
  });
}, []);
```

---

### 2. شاشة إضافة شاحن جديد

```javascript
const AddChargerScreen = () => {
  const { chargingModeEnabled } = useAppState();

  const handleAddCharger = async (chargerData) => {
    // إضافة الشاحن عادي
    const result = await addCharger(chargerData);

    if (result.status) {
      // عرض رسالة مختلفة حسب الحالة
      if (!chargingModeEnabled) {
        showAlert({
          title: "تم إضافة الشاحن",
          message: "الشاحن في وضع الانتظار وسيتم تفعيله من قِبل الإدارة.",
          type: "info"
        });
      } else {
        showAlert({
          title: "تم إضافة الشاحن",
          message: "الشاحن جاهز للاستخدام.",
          type: "success"
        });
      }
    }
  };

  return (
    <View>
      {!chargingModeEnabled && (
        <InfoBanner>
          ملاحظة: الشاحن الجديد سيكون في وضع الانتظار حتى يتم تفعيله من الإدارة.
        </InfoBanner>
      )}
      {/* باقي الفورم */}
    </View>
  );
};
```

---

### 3. شاشة تفاصيل الشاحن (Charger Card)

```javascript
const ChargerCard = ({ charger }) => {
  const { chargingModeEnabled } = useAppState();

  // تحديد هل الزر يعمل أم لا
  const canToggle = chargingModeEnabled || charger.isActive;

  const handleToggleStatus = async () => {
    if (!canToggle) {
      showAlert({
        title: "غير متاح",
        message: "لا يمكن تفعيل الشاحن حالياً. يرجى انتظار تفعيل الإدارة.",
        type: "warning"
      });
      return;
    }

    const response = await toggleChargerStatus(charger.id);

    if (!response.status) {
      // الـ API رجّع خطأ
      showAlert({
        title: "خطأ",
        message: response.message,
        type: "error"
      });
      return;
    }

    // نجاح - تحديث الـ UI
    refreshChargerList();
  };

  return (
    <Card>
      <Text>{charger.name}</Text>

      <StatusBadge active={charger.isActive}>
        {charger.isActive ? "نشط" : "غير نشط"}
      </StatusBadge>

      <Button
        onPress={handleToggleStatus}
        disabled={!canToggle}
        style={!canToggle && styles.disabledButton}
      >
        {charger.isActive ? "تعطيل" : "تفعيل"}
      </Button>

      {/* رسالة توضيحية لما الزر معطّل */}
      {!canToggle && (
        <HintText>
          سيتم تفعيل الشاحن من قِبل الإدارة
        </HintText>
      )}
    </Card>
  );
};
```

---

### 4. Admin Dashboard - إدارة ChargingMode

```javascript
const AdminSettingsScreen = () => {
  const [chargingMode, setChargingMode] = useState(false);
  const [loading, setLoading] = useState(false);
  const { adminToken } = useAuth();

  // جلب الحالة الحالية
  useEffect(() => {
    fetchChargingModeAdmin();
  }, []);

  const fetchChargingModeAdmin = async () => {
    const response = await fetch(
      'https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/admin/settings/charging-mode',
      {
        headers: {
          'Authorization': `Bearer ${adminToken}`
        }
      }
    );
    const data = await response.json();
    setChargingMode(data.data.chargingModeEnabled);
  };

  // تفعيل/تعطيل
  const toggleChargingMode = async () => {
    setLoading(true);

    const response = await fetch(
      'https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/admin/settings/charging-mode',
      {
        method: 'PATCH',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ enabled: !chargingMode })
      }
    );

    const data = await response.json();

    if (data.status) {
      setChargingMode(!chargingMode);
      showToast(data.message);
    }

    setLoading(false);
  };

  // تفعيل كل الشواحن
  const activateAllChargers = async () => {
    const confirmed = await showConfirm(
      "تأكيد",
      "هل تريد تفعيل جميع الشواحن غير النشطة؟"
    );

    if (!confirmed) return;

    setLoading(true);

    const response = await fetch(
      'https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/admin/settings/activate-all-chargers',
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${adminToken}`
        }
      }
    );

    const data = await response.json();

    if (data.status) {
      showToast(`تم تفعيل ${data.data} شاحن بنجاح`);
    }

    setLoading(false);
  };

  return (
    <View>
      <SettingRow>
        <Text>وضع الشحن</Text>
        <Switch
          value={chargingMode}
          onValueChange={toggleChargingMode}
          disabled={loading}
        />
      </SettingRow>

      <Text style={styles.hint}>
        {chargingMode
          ? "الشواحن الجديدة ستكون نشطة تلقائياً"
          : "الشواحن الجديدة ستحتاج تفعيل يدوي"
        }
      </Text>

      <Button
        title="تفعيل جميع الشواحن غير النشطة"
        onPress={activateAllChargers}
        disabled={loading}
      />
    </View>
  );
};
```

---

## جدول حالات الـ UI

| ChargingMode | حالة الشاحن | زر التفعيل/التعطيل | الرسالة |
|--------------|-------------|-------------------|---------|
| `false` | Inactive | **معطّل** (disabled) | "سيتم التفعيل من الإدارة" |
| `false` | Active | **يعمل** (تعطيل فقط) | - |
| `true` | Inactive | **يعمل** | - |
| `true` | Active | **يعمل** | - |

---

## أمثلة cURL

### 1. جلب حالة ChargingMode (Public)
```bash
curl -X GET "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/v1/app-config/charging-mode-status"
```

### 2. تفعيل ChargingMode (Admin)
```bash
curl -X PATCH "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/admin/settings/charging-mode" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"enabled": true}'
```

### 3. تفعيل كل الشواحن (Admin)
```bash
curl -X POST "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/api/admin/settings/activate-all-chargers" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

## ملخص سريع

| Endpoint | Method | Auth | الوظيفة |
|----------|--------|------|---------|
| `/api/v1/app-config/charging-mode-status` | GET | No | حالة ChargingMode (للمستخدم) |
| `/api/admin/settings/charging-mode` | GET | Admin | حالة ChargingMode (للأدمن) |
| `/api/admin/settings/charging-mode` | PATCH | Admin | تفعيل/تعطيل ChargingMode |
| `/api/admin/settings/activate-all-chargers` | POST | Admin | تفعيل كل الشواحن غير النشطة |

---

## الحالة الحالية

```
ChargingModeEnabled = false
```

الشواحن الجديدة ستُنشأ كـ **Inactive** ولن يتمكن المستخدمون من تفعيلها حتى يقوم الأدمن بتفعيل ChargingMode أو تفعيل الشواحن يدوياً.
