# دليل إضافة Apple Pay مع Paymob - Voltyks.API

> **آخر تحديث:** 2025-12-31
> **متوافق مع:** Voltyks.API Payment Flow v2.0

---

## نظرة عامة على الـ Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Apple Pay Integration Flow - Voltyks.API                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   الخطوة 1: إنشاء الشهادات (Certificates)                                   │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  • Process Certificate (ECC) - لمعالجة المعاملات                    │   │
│   │  • Merchant Certificate (RSA) - لإثبات هوية التاجر                  │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                        │
│   الخطوة 2: تسجيل الدومين على Apple Developer                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  • إنشاء Merchant ID                                                │   │
│   │  • رفع ملف التحقق على السيرفر                                       │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                        │
│   الخطوة 3: إرسال الشهادات لـ Paymob                                       │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  • certificate.pem + merchant_certificate.pem + private.key         │   │
│   │  • الحصول على Apple Pay Integration ID                              │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                        │
│   الخطوة 4: تحديث appsettings.json                                         │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  • إضافة Integration.ApplePay = <ID من Paymob>                      │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                    ↓                                        │
│   الخطوة 5: استخدام الـ API                                                │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  POST /api/payment/intention                                        │   │
│   │  paymentMethod: "ApplePay"                                          │   │
│   │  → clientSecret + publicKey → Paymob SDK                            │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## الخطوة 1: إنشاء الشهادات

### 1.1 Process Certificate (ECC) - شهادة المعالجة

هذه الشهادة تُستخدم لفك تشفير بيانات الدفع من Apple Pay.

#### أ) توليد المفتاح الخاص ECC:
```bash
openssl ecparam -name prime256v1 -genkey -noout -out yourdomain.key
```

#### ب) توليد CSR (Certificate Signing Request):
```bash
openssl req -new -key yourdomain.key -out yourdomain.csr
```

**المعلومات المطلوبة:**

| الحقل | المثال | الوصف |
|-------|--------|-------|
| Country Name | EG | رمز الدولة |
| State/Province | Cairo | المحافظة |
| Locality | Cairo | المدينة |
| Organization | Voltyks | اسم الشركة |
| Organizational Unit | IT | القسم |
| Common Name | voltyks.com | اسم الدومين |
| Email | admin@voltyks.com | البريد الإلكتروني |

#### ج) رفع CSR على Apple Developer Portal:
1. افتح [Apple Developer Portal](https://developer.apple.com/account/)
2. اذهب إلى **Certificates, Identifiers & Profiles**
3. اختر **Identifiers** → **Merchant IDs**
4. أنشئ Merchant ID جديد (مثل: `merchant.com.voltyks`)
5. اضغط **Create Certificate** تحت قسم "Apple Pay Payment Processing Certificate"
6. ارفع ملف `yourdomain.csr`
7. حمّل الشهادة الناتجة

#### د) تحويل الشهادة إلى PEM:
```bash
openssl x509 -inform DER -in apple_pay.cer -out certificate.pem
```

---

### 1.2 Merchant Certificate (RSA) - شهادة التاجر

هذه الشهادة تُستخدم لإثبات هوية التاجر.

#### أ) توليد المفتاح الخاص RSA:
```bash
openssl genpkey -algorithm RSA -out private.key
```

#### ب) توليد CSR:
```bash
openssl req -new -key private.key -out request.csr
```

#### ج) رفع CSR على Apple Developer:
1. في نفس الـ Merchant ID
2. اضغط **Create Certificate** تحت "Apple Pay Merchant Identity Certificate"
3. ارفع `request.csr`
4. حمّل الشهادة

#### د) تحويل الشهادة إلى PEM:
```bash
openssl x509 -inform DER -in merchant.cer -out merchant_certificate.pem
```

#### هـ) التحقق من CSR (اختياري):
```bash
openssl req -text -noout -verify -in request.csr
```

---

## الخطوة 2: تسجيل الدومين على Apple Developer

### 2.1 إنشاء Merchant ID
1. افتح [Apple Developer Portal](https://developer.apple.com/account/)
2. اذهب إلى **Certificates, Identifiers & Profiles**
3. اختر **Identifiers** من القائمة الجانبية
4. اضغط **+** لإضافة identifier جديد
5. اختر **Merchant IDs**
6. أدخل:
   - **Description:** Voltyks Apple Pay
   - **Identifier:** `merchant.com.voltyks`
7. اضغط **Continue** ثم **Register**

### 2.2 تسجيل الدومين
1. اختر الـ Merchant ID الذي أنشأته
2. تحت قسم **Merchant Domains** اضغط **Add Domain**
3. أدخل الدومين: `voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net`
4. حمّل ملف التحقق: `apple-developer-merchantid-domain-association`

### 2.3 رفع ملف التحقق على السيرفر

**المسار المطلوب:**
```
https://yourdomain.com/.well-known/apple-developer-merchantid-domain-association
```

**على Azure App Service:**
1. أنشئ مجلد `.well-known` في `wwwroot`
2. ضع الملف بداخله (بدون امتداد)

### 2.4 إتمام التحقق
1. ارجع لـ Apple Developer Portal
2. اضغط **Verify**
3. انتظر بضع دقائق

---

## الخطوة 3: إرسال الشهادات لـ Paymob

### 3.1 الملفات المطلوبة
أرسل هذه الملفات لفريق Paymob Back-office:

| الملف | الوصف |
|-------|-------|
| `certificate.pem` | Process Certificate |
| `merchant_certificate.pem` | Merchant Certificate |
| `yourdomain.key` | Private Key (ECC) |
| `private.key` | Private Key (RSA) |

### 3.2 ما ستحصل عليه من Paymob

| Key | الوصف |
|-----|-------|
| **Apple Pay Integration ID** | رقم مثل `5413128` |
| **تأكيد التفعيل** | بأن Apple Pay جاهز على حسابك |

---

## الخطوة 4: تحديث appsettings.json

### 4.1 الإعداد الحالي
```json
"Paymob": {
  "Integration": {
    "Card": 5413127,
    "Wallet": 5413126
  }
}
```

### 4.2 بعد إضافة Apple Pay
```json
"Paymob": {
  "Integration": {
    "Card": 5413127,
    "Wallet": 5413126,
    "ApplePay": 5413128
  }
}
```

### 4.3 ملاحظات
- الـ `IntegrationIds.ApplePay` **موجود بالفعل** في الكود
- فقط تحتاج إضافة القيمة في `appsettings.json`
- لا تحتاج أي تعديل في الـ Backend code

---

## الخطوة 5: استخدام الـ API

### 5.1 إنشاء Payment Intention

**نفس الـ Endpoint الحالي:**
```
POST /api/payment/intention
```

**Request Body:**
```json
{
  "amountCents": 10000,
  "billing": {
    "first_name": "Ahmed",
    "last_name": "Salem",
    "email": "ahmed@example.com",
    "phone_number": "01012345678"
  },
  "saveCard": false,
  "paymentMethod": "ApplePay"
}
```

**الفرق الوحيد:** `paymentMethod: "ApplePay"` بدلاً من `"Card"`

### 5.2 Response
```json
{
  "status": true,
  "message": "Intention created successfully.",
  "data": {
    "clientSecret": "ZXlKaGJHY2lPaUpJVX...",
    "publicKey": "egy_pk_live_...",
    "intentionId": "pi_xxx",
    "intentionOrderId": 123456789
  }
}
```

### 5.3 فتح Paymob SDK
استخدم `clientSecret` + `publicKey` لفتح Paymob SDK.
- الـ SDK سيعرض خيار Apple Pay تلقائياً إذا:
  - الجهاز iPhone/iPad/Mac
  - المستخدم أضاف كروت في Apple Wallet
  - الـ Integration مفعل

---

## الـ Webhook

### نفس الـ Flow الحالي
Apple Pay يستخدم **نفس webhook** الموجود:
```
POST /api/payment/webhook
```

### أنواع الـ Events
| Event | الوصف |
|-------|-------|
| `TRANSACTION` | إتمام/فشل الدفع |
| `CARD_TOKEN` | (لا يُستخدم مع Apple Pay عادةً) |

### التمييز في الـ Webhook
```json
{
  "obj": {
    "source_data": {
      "type": "apple_pay"
    }
  }
}
```

---

## قائمة التحقق النهائية

### الشهادات
- [ ] توليد ECC Private Key
- [ ] توليد ECC CSR
- [ ] رفع CSR على Apple Developer
- [ ] تحميل Process Certificate
- [ ] تحويل إلى PEM
- [ ] توليد RSA Private Key
- [ ] توليد RSA CSR
- [ ] تحميل Merchant Certificate
- [ ] تحويل إلى PEM

### Apple Developer
- [ ] إنشاء Merchant ID
- [ ] تسجيل الدومين
- [ ] رفع ملف التحقق
- [ ] التحقق من الدومين ✓

### Paymob
- [ ] إرسال الشهادات لـ Paymob
- [ ] استلام Apple Pay Integration ID
- [ ] تحديث `appsettings.json`

### الاختبار
- [ ] إنشاء intention مع `paymentMethod: "ApplePay"`
- [ ] فتح الدفع على iPhone
- [ ] التحقق من ظهور Apple Pay
- [ ] إتمام معاملة تجريبية
- [ ] التحقق من الـ Webhook

---

## الملفات المرتبطة في المشروع

| الملف | الوظيفة |
|-------|---------|
| `appsettings.json` | إضافة Integration.ApplePay |
| `IntegrationIds.cs` | تعريف ApplePay property (موجود) |
| `PaymobService.cs` | معالجة الـ intention (لا يحتاج تعديل) |
| `PaymentController.cs` | نفس الـ endpoints |

---

## الدعم

- **Apple Developer:** [developer.apple.com](https://developer.apple.com)
- **Paymob Docs:** [docs.paymob.com](https://docs.paymob.com)
- **Apple Pay on Web:** [Apple Pay Developer](https://developer.apple.com/apple-pay/)
