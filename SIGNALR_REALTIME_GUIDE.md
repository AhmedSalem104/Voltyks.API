# دليل SignalR Real-time الكامل

## الفهرس
1. [ما تم في Backend](#1-ما-تم-في-backend)
2. [Hub Endpoints](#2-hub-endpoints)
3. [Events المرسلة](#3-events-المرسلة)
4. [iOS Implementation](#4-ios-implementation-swift)
5. [Android Implementation](#5-android-implementation-kotlin)
6. [Data Models](#6-data-models)
7. [Usage Examples](#7-usage-examples)
8. [Events Reference Table](#8-events-reference-table)
9. [Frontend Checklist](#9-frontend-checklist)

---

## 1. ما تم في Backend

### الملفات الجديدة

| الملف | المسار | الوظيفة |
|-------|--------|---------|
| `ChargingRequestHub.cs` | `Voltyks.API/Hubs/` | Hub لطلبات الشحن |
| `ProcessHub.cs` | `Voltyks.API/Hubs/` | Hub للدفع/العمليات |
| `NotificationHub.cs` | `Voltyks.API/Hubs/` | Hub للإشعارات العامة |
| `ISignalRService.cs` | `Voltyks.Application/Interfaces/SignalR/` | Interface |
| `SignalRService.cs` | `Voltyks.API/Services/` | Implementation |

### الملفات المعدلة

| الملف | التعديل |
|-------|---------|
| `Extensions.cs` | إضافة `services.AddSignalR()` + CORS + JWT support + Hub mapping |
| `ServiceManager.cs` | إضافة `ISignalRService` للـ constructor |
| `ChargingRequestService.cs` | إضافة SignalR calls بعد كل تغيير حالة |
| `ProcessesService.cs` | إضافة SignalR calls بعد كل تغيير حالة |

### ملاحظة مهمة
- **لم يتم إضافة أي REST API endpoints جديدة**
- SignalR يعمل **بجانب** Firebase FCM (لم يتم استبداله)
- **لم يتم تغيير أي logic موجود** - فقط إضافة سطور SignalR

---

## 2. Hub Endpoints

```
Base URL: https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net

Hub 1: /hubs/charging-request   ← لطلبات الشحن
Hub 2: /hubs/process            ← للدفع والعمليات
Hub 3: /hubs/notification       ← للإشعارات
```

### Full URLs:
```
wss://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/hubs/charging-request
wss://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/hubs/process
wss://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net/hubs/notification
```

---

## 3. Events المرسلة

### ChargingRequest Events

| Event Name | متى يُرسل | المُستقبل | Data |
|------------|----------|----------|------|
| `NewRequest` | طلب جديد (pending) | Charger Owner | `{ requestId, chargerId, kwNeeded, status }` |
| `RequestAccepted` | قبول الطلب | Vehicle Owner | `{ requestId, chargerOwnerName, status }` |
| `RequestRejected` | رفض الطلب | Vehicle Owner | `{ requestId, stationOwnerName, status }` |
| `RequestConfirmed` | تأكيد الطلب | Vehicle Owner | `{ requestId, chargerOwnerName, status }` |
| `RequestAborted` | إلغاء الطلب | الطرف الآخر | `{ requestId, abortedBy, status }` |

### Process Events

| Event Name | متى يُرسل | المُستقبل | Data |
|------------|----------|----------|------|
| `ProcessCreated` | إنشاء عملية | Charger Owner | `{ processId, requestId, estimatedPrice, amountCharged, amountPaid, status }` |
| `ProcessStarted` | بدء الجلسة | الطرف الآخر | `{ processId, status, startedBy }` |
| `PaymentCompleted` | اكتمال العملية | الطرف الآخر | `{ processId, status, confirmedBy }` |
| `PaymentAborted` | إلغاء العملية | الطرف الآخر | `{ processId, status, abortedBy }` |
| `PaymentStatusChanged` | تحديث البيانات | Charger Owner | `{ processId, estimatedPrice, amountCharged, amountPaid }` |

### Notification Events

| Event Name | الوظيفة | Data |
|------------|---------|------|
| `ReceiveNotification` | إشعار شخصي لمستخدم معين | `{ title, body, timestamp, data }` |
| `ReceiveBroadcast` | إشعار عام لكل المستخدمين | `{ title, body, timestamp, data }` |

---

## 4. iOS Implementation (Swift)

### إضافة المكتبة
```ruby
# Podfile
pod 'SignalRClient', '~> 0.9.0'
```
```bash
cd ios && pod install
```

### SignalRManager.swift

```swift
import Foundation
import SignalRClient

class SignalRManager {

    // ========== Singleton ==========
    static let shared = SignalRManager()
    private init() {}

    // ========== Configuration ==========
    private let baseURL = "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net"

    // ========== Hub Connections ==========
    private var chargingRequestHub: HubConnection?
    private var processHub: HubConnection?
    private var notificationHub: HubConnection?

    // ========== JWT Token ==========
    var accessToken: String? {
        didSet {
            if accessToken != nil {
                connectAll()
            }
        }
    }

    // ========== Connection Status ==========
    var isConnected: Bool {
        return chargingRequestHub?.state == .connected
    }

    // ========== Callbacks ==========
    // Charging Request Events
    var onNewRequest: ((ChargingRequestEvent) -> Void)?
    var onRequestAccepted: ((ChargingRequestEvent) -> Void)?
    var onRequestRejected: ((ChargingRequestEvent) -> Void)?
    var onRequestConfirmed: ((ChargingRequestEvent) -> Void)?
    var onRequestAborted: ((ChargingRequestEvent) -> Void)?

    // Process Events
    var onProcessCreated: ((ProcessEvent) -> Void)?
    var onProcessStarted: ((ProcessEvent) -> Void)?
    var onPaymentCompleted: ((ProcessEvent) -> Void)?
    var onPaymentAborted: ((ProcessEvent) -> Void)?
    var onPaymentStatusChanged: ((ProcessEvent) -> Void)?

    // Notification Events
    var onNotificationReceived: ((NotificationEvent) -> Void)?
    var onBroadcastReceived: ((NotificationEvent) -> Void)?

    // ========================================
    // MARK: - Connect All Hubs
    // ========================================
    func connectAll() {
        guard let token = accessToken, !token.isEmpty else {
            print("❌ SignalR: No access token")
            return
        }

        connectChargingRequestHub(token: token)
        connectProcessHub(token: token)
        connectNotificationHub(token: token)
    }

    // ========================================
    // MARK: - Charging Request Hub
    // ========================================
    private func connectChargingRequestHub(token: String) {
        let url = URL(string: "\(baseURL)/hubs/charging-request")!

        chargingRequestHub = HubConnectionBuilder(url: url)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = { token }
            }
            .withAutoReconnect()
            .withLogging(minLogLevel: .error)
            .build()

        // Event: NewRequest
        chargingRequestHub?.on(method: "NewRequest") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ChargingRequestEvent.self)
                DispatchQueue.main.async {
                    self?.onNewRequest?(data)
                }
            } catch {
                print("❌ Error parsing NewRequest: \(error)")
            }
        }

        // Event: RequestAccepted
        chargingRequestHub?.on(method: "RequestAccepted") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ChargingRequestEvent.self)
                DispatchQueue.main.async {
                    self?.onRequestAccepted?(data)
                }
            } catch {
                print("❌ Error parsing RequestAccepted: \(error)")
            }
        }

        // Event: RequestRejected
        chargingRequestHub?.on(method: "RequestRejected") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ChargingRequestEvent.self)
                DispatchQueue.main.async {
                    self?.onRequestRejected?(data)
                }
            } catch {
                print("❌ Error parsing RequestRejected: \(error)")
            }
        }

        // Event: RequestConfirmed
        chargingRequestHub?.on(method: "RequestConfirmed") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ChargingRequestEvent.self)
                DispatchQueue.main.async {
                    self?.onRequestConfirmed?(data)
                }
            } catch {
                print("❌ Error parsing RequestConfirmed: \(error)")
            }
        }

        // Event: RequestAborted
        chargingRequestHub?.on(method: "RequestAborted") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ChargingRequestEvent.self)
                DispatchQueue.main.async {
                    self?.onRequestAborted?(data)
                }
            } catch {
                print("❌ Error parsing RequestAborted: \(error)")
            }
        }

        chargingRequestHub?.delegate = self
        chargingRequestHub?.start()
        print("🔌 ChargingRequestHub: Connecting...")
    }

    // ========================================
    // MARK: - Process Hub
    // ========================================
    private func connectProcessHub(token: String) {
        let url = URL(string: "\(baseURL)/hubs/process")!

        processHub = HubConnectionBuilder(url: url)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = { token }
            }
            .withAutoReconnect()
            .withLogging(minLogLevel: .error)
            .build()

        // Event: ProcessCreated
        processHub?.on(method: "ProcessCreated") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ProcessEvent.self)
                DispatchQueue.main.async {
                    self?.onProcessCreated?(data)
                }
            } catch {
                print("❌ Error parsing ProcessCreated: \(error)")
            }
        }

        // Event: ProcessStarted
        processHub?.on(method: "ProcessStarted") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ProcessEvent.self)
                DispatchQueue.main.async {
                    self?.onProcessStarted?(data)
                }
            } catch {
                print("❌ Error parsing ProcessStarted: \(error)")
            }
        }

        // Event: PaymentCompleted
        processHub?.on(method: "PaymentCompleted") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ProcessEvent.self)
                DispatchQueue.main.async {
                    self?.onPaymentCompleted?(data)
                }
            } catch {
                print("❌ Error parsing PaymentCompleted: \(error)")
            }
        }

        // Event: PaymentAborted
        processHub?.on(method: "PaymentAborted") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ProcessEvent.self)
                DispatchQueue.main.async {
                    self?.onPaymentAborted?(data)
                }
            } catch {
                print("❌ Error parsing PaymentAborted: \(error)")
            }
        }

        // Event: PaymentStatusChanged
        processHub?.on(method: "PaymentStatusChanged") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: ProcessEvent.self)
                DispatchQueue.main.async {
                    self?.onPaymentStatusChanged?(data)
                }
            } catch {
                print("❌ Error parsing PaymentStatusChanged: \(error)")
            }
        }

        processHub?.start()
        print("🔌 ProcessHub: Connecting...")
    }

    // ========================================
    // MARK: - Notification Hub
    // ========================================
    private func connectNotificationHub(token: String) {
        let url = URL(string: "\(baseURL)/hubs/notification")!

        notificationHub = HubConnectionBuilder(url: url)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = { token }
            }
            .withAutoReconnect()
            .withLogging(minLogLevel: .error)
            .build()

        // Event: ReceiveNotification
        notificationHub?.on(method: "ReceiveNotification") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: NotificationEvent.self)
                DispatchQueue.main.async {
                    self?.onNotificationReceived?(data)
                }
            } catch {
                print("❌ Error parsing ReceiveNotification: \(error)")
            }
        }

        // Event: ReceiveBroadcast
        notificationHub?.on(method: "ReceiveBroadcast") { [weak self] (args: ArgumentExtractor) in
            do {
                let data = try args.getArgument(type: NotificationEvent.self)
                DispatchQueue.main.async {
                    self?.onBroadcastReceived?(data)
                }
            } catch {
                print("❌ Error parsing ReceiveBroadcast: \(error)")
            }
        }

        notificationHub?.start()
        print("🔌 NotificationHub: Connecting...")
    }

    // ========================================
    // MARK: - Disconnect
    // ========================================
    func disconnectAll() {
        chargingRequestHub?.stop()
        processHub?.stop()
        notificationHub?.stop()

        chargingRequestHub = nil
        processHub = nil
        notificationHub = nil

        print("🔌 SignalR: Disconnected all hubs")
    }
}

// ========================================
// MARK: - HubConnectionDelegate
// ========================================
extension SignalRManager: HubConnectionDelegate {
    func connectionDidOpen(hubConnection: HubConnection) {
        print("✅ SignalR: Hub connected")
    }

    func connectionDidFailToOpen(error: Error) {
        print("❌ SignalR: Failed to connect - \(error.localizedDescription)")
    }

    func connectionDidClose(error: Error?) {
        print("⚠️ SignalR: Connection closed - \(error?.localizedDescription ?? "unknown")")
    }

    func connectionWillReconnect(error: Error) {
        print("🔄 SignalR: Reconnecting...")
    }

    func connectionDidReconnect() {
        print("✅ SignalR: Reconnected")
    }
}
```

---

## 5. Android Implementation (Kotlin)

### إضافة المكتبة
```gradle
// app/build.gradle
dependencies {
    implementation 'com.microsoft.signalr:signalr:7.0.0'
}
```

### SignalRManager.kt

```kotlin
package com.voltyks.app.signalr

import android.util.Log
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState

object SignalRManager {

    private const val TAG = "SignalR"
    private const val BASE_URL = "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net"

    // ========== Hub Connections ==========
    private var chargingRequestHub: HubConnection? = null
    private var processHub: HubConnection? = null
    private var notificationHub: HubConnection? = null

    // ========== JWT Token ==========
    var accessToken: String? = null
        set(value) {
            field = value
            if (value != null) {
                connectAll()
            }
        }

    // ========== Connection Status ==========
    val isConnected: Boolean
        get() = chargingRequestHub?.connectionState == HubConnectionState.CONNECTED

    // ========== Callbacks ==========
    var onNewRequest: ((ChargingRequestEvent) -> Unit)? = null
    var onRequestAccepted: ((ChargingRequestEvent) -> Unit)? = null
    var onRequestRejected: ((ChargingRequestEvent) -> Unit)? = null
    var onRequestConfirmed: ((ChargingRequestEvent) -> Unit)? = null
    var onRequestAborted: ((ChargingRequestEvent) -> Unit)? = null
    var onProcessCreated: ((ProcessEvent) -> Unit)? = null
    var onProcessStarted: ((ProcessEvent) -> Unit)? = null
    var onPaymentCompleted: ((ProcessEvent) -> Unit)? = null
    var onPaymentAborted: ((ProcessEvent) -> Unit)? = null
    var onPaymentStatusChanged: ((ProcessEvent) -> Unit)? = null
    var onNotificationReceived: ((NotificationEvent) -> Unit)? = null
    var onBroadcastReceived: ((NotificationEvent) -> Unit)? = null

    // ========================================
    // Connect All Hubs
    // ========================================
    fun connectAll() {
        val token = accessToken
        if (token.isNullOrEmpty()) {
            Log.e(TAG, "❌ No access token")
            return
        }

        connectChargingRequestHub(token)
        connectProcessHub(token)
        connectNotificationHub(token)
    }

    // ========================================
    // Charging Request Hub
    // ========================================
    private fun connectChargingRequestHub(token: String) {
        chargingRequestHub = HubConnectionBuilder
            .create("$BASE_URL/hubs/charging-request")
            .withAccessTokenProvider { token }
            .build()

        chargingRequestHub?.on("NewRequest", { data: Any ->
            Log.d(TAG, "📥 NewRequest: $data")
            val event = parseChargingRequestEvent(data)
            onNewRequest?.invoke(event)
        }, Any::class.java)

        chargingRequestHub?.on("RequestAccepted", { data: Any ->
            Log.d(TAG, "✅ RequestAccepted: $data")
            val event = parseChargingRequestEvent(data)
            onRequestAccepted?.invoke(event)
        }, Any::class.java)

        chargingRequestHub?.on("RequestRejected", { data: Any ->
            Log.d(TAG, "❌ RequestRejected: $data")
            val event = parseChargingRequestEvent(data)
            onRequestRejected?.invoke(event)
        }, Any::class.java)

        chargingRequestHub?.on("RequestConfirmed", { data: Any ->
            Log.d(TAG, "✅ RequestConfirmed: $data")
            val event = parseChargingRequestEvent(data)
            onRequestConfirmed?.invoke(event)
        }, Any::class.java)

        chargingRequestHub?.on("RequestAborted", { data: Any ->
            Log.d(TAG, "⚠️ RequestAborted: $data")
            val event = parseChargingRequestEvent(data)
            onRequestAborted?.invoke(event)
        }, Any::class.java)

        chargingRequestHub?.onClosed { error ->
            Log.e(TAG, "❌ ChargingRequestHub closed: ${error?.message}")
        }

        chargingRequestHub?.start()?.subscribe(
            { Log.d(TAG, "✅ ChargingRequestHub connected") },
            { error -> Log.e(TAG, "❌ ChargingRequestHub error: ${error.message}") }
        )
    }

    // ========================================
    // Process Hub
    // ========================================
    private fun connectProcessHub(token: String) {
        processHub = HubConnectionBuilder
            .create("$BASE_URL/hubs/process")
            .withAccessTokenProvider { token }
            .build()

        processHub?.on("ProcessCreated", { data: Any ->
            Log.d(TAG, "💰 ProcessCreated: $data")
            val event = parseProcessEvent(data)
            onProcessCreated?.invoke(event)
        }, Any::class.java)

        processHub?.on("ProcessStarted", { data: Any ->
            Log.d(TAG, "▶️ ProcessStarted: $data")
            val event = parseProcessEvent(data)
            onProcessStarted?.invoke(event)
        }, Any::class.java)

        processHub?.on("PaymentCompleted", { data: Any ->
            Log.d(TAG, "✅ PaymentCompleted: $data")
            val event = parseProcessEvent(data)
            onPaymentCompleted?.invoke(event)
        }, Any::class.java)

        processHub?.on("PaymentAborted", { data: Any ->
            Log.d(TAG, "❌ PaymentAborted: $data")
            val event = parseProcessEvent(data)
            onPaymentAborted?.invoke(event)
        }, Any::class.java)

        processHub?.on("PaymentStatusChanged", { data: Any ->
            Log.d(TAG, "📊 PaymentStatusChanged: $data")
            val event = parseProcessEvent(data)
            onPaymentStatusChanged?.invoke(event)
        }, Any::class.java)

        processHub?.start()?.subscribe(
            { Log.d(TAG, "✅ ProcessHub connected") },
            { error -> Log.e(TAG, "❌ ProcessHub error: ${error.message}") }
        )
    }

    // ========================================
    // Notification Hub
    // ========================================
    private fun connectNotificationHub(token: String) {
        notificationHub = HubConnectionBuilder
            .create("$BASE_URL/hubs/notification")
            .withAccessTokenProvider { token }
            .build()

        notificationHub?.on("ReceiveNotification", { data: Any ->
            Log.d(TAG, "🔔 Notification: $data")
            val event = parseNotificationEvent(data)
            onNotificationReceived?.invoke(event)
        }, Any::class.java)

        notificationHub?.on("ReceiveBroadcast", { data: Any ->
            Log.d(TAG, "📢 Broadcast: $data")
            val event = parseNotificationEvent(data)
            onBroadcastReceived?.invoke(event)
        }, Any::class.java)

        notificationHub?.start()?.subscribe(
            { Log.d(TAG, "✅ NotificationHub connected") },
            { error -> Log.e(TAG, "❌ NotificationHub error: ${error.message}") }
        )
    }

    // ========================================
    // Disconnect
    // ========================================
    fun disconnectAll() {
        chargingRequestHub?.stop()
        processHub?.stop()
        notificationHub?.stop()

        chargingRequestHub = null
        processHub = null
        notificationHub = null

        Log.d(TAG, "🔌 Disconnected all hubs")
    }

    // ========================================
    // Parsers
    // ========================================
    private fun parseChargingRequestEvent(data: Any): ChargingRequestEvent {
        val map = data as? Map<*, *> ?: return ChargingRequestEvent(0, "unknown")
        return ChargingRequestEvent(
            requestId = (map["requestId"] as? Number)?.toInt() ?: 0,
            status = map["status"] as? String ?: "unknown",
            chargerId = (map["chargerId"] as? Number)?.toInt(),
            kwNeeded = (map["kwNeeded"] as? Number)?.toDouble(),
            chargerOwnerName = map["chargerOwnerName"] as? String,
            stationOwnerName = map["stationOwnerName"] as? String,
            abortedBy = map["abortedBy"] as? String
        )
    }

    private fun parseProcessEvent(data: Any): ProcessEvent {
        val map = data as? Map<*, *> ?: return ProcessEvent(0, "unknown")
        return ProcessEvent(
            processId = (map["processId"] as? Number)?.toInt() ?: 0,
            status = map["status"] as? String ?: "unknown",
            requestId = (map["requestId"] as? Number)?.toInt(),
            estimatedPrice = (map["estimatedPrice"] as? Number)?.toDouble(),
            amountCharged = (map["amountCharged"] as? Number)?.toDouble(),
            amountPaid = (map["amountPaid"] as? Number)?.toDouble(),
            startedBy = map["startedBy"] as? String,
            confirmedBy = map["confirmedBy"] as? String,
            abortedBy = map["abortedBy"] as? String
        )
    }

    private fun parseNotificationEvent(data: Any): NotificationEvent {
        val map = data as? Map<*, *> ?: return NotificationEvent("", "", "")
        return NotificationEvent(
            title = map["title"] as? String ?: "",
            body = map["body"] as? String ?: "",
            timestamp = map["timestamp"] as? String ?: ""
        )
    }
}
```

---

## 6. Data Models

### iOS (Swift)

```swift
struct ChargingRequestEvent: Codable {
    let requestId: Int
    let status: String
    let chargerId: Int?
    let kwNeeded: Double?
    let chargerOwnerName: String?
    let stationOwnerName: String?
    let abortedBy: String?  // "charger_owner" or "vehicle_owner"
}

struct ProcessEvent: Codable {
    let processId: Int
    let status: String
    let requestId: Int?
    let estimatedPrice: Double?
    let amountCharged: Double?
    let amountPaid: Double?
    let startedBy: String?    // "charger_owner" or "vehicle_owner"
    let confirmedBy: String?  // "charger_owner" or "vehicle_owner"
    let abortedBy: String?    // "charger_owner" or "vehicle_owner"
}

struct NotificationEvent: Codable {
    let title: String
    let body: String
    let timestamp: String
}
```

### Android (Kotlin)

```kotlin
data class ChargingRequestEvent(
    val requestId: Int,
    val status: String,
    val chargerId: Int? = null,
    val kwNeeded: Double? = null,
    val chargerOwnerName: String? = null,
    val stationOwnerName: String? = null,
    val abortedBy: String? = null
)

data class ProcessEvent(
    val processId: Int,
    val status: String,
    val requestId: Int? = null,
    val estimatedPrice: Double? = null,
    val amountCharged: Double? = null,
    val amountPaid: Double? = null,
    val startedBy: String? = null,
    val confirmedBy: String? = null,
    val abortedBy: String? = null
)

data class NotificationEvent(
    val title: String,
    val body: String,
    val timestamp: String
)
```

---

## 7. Usage Examples

### iOS - Login

```swift
func onLoginSuccess(token: String) {
    // 1. Save token
    UserDefaults.standard.set(token, forKey: "accessToken")

    // 2. Start SignalR
    SignalRManager.shared.accessToken = token

    // 3. Navigate to home
    navigateToHome()
}
```

### iOS - Setup Callbacks

```swift
func setupSignalRCallbacks() {
    let signalR = SignalRManager.shared

    signalR.onNewRequest = { event in
        print("📥 New request: \(event.requestId)")
    }

    signalR.onRequestAccepted = { event in
        print("✅ Accepted: \(event.requestId)")
        // Update UI
    }

    signalR.onRequestRejected = { event in
        print("❌ Rejected: \(event.requestId)")
    }

    signalR.onPaymentCompleted = { event in
        print("✅ Completed: \(event.processId)")
    }
}
```

### iOS - Logout

```swift
func logout() {
    SignalRManager.shared.disconnectAll()
    UserDefaults.standard.removeObject(forKey: "accessToken")
    navigateToLogin()
}
```

### Android - Login

```kotlin
fun onLoginSuccess(token: String) {
    // 1. Save token
    getSharedPreferences("app", MODE_PRIVATE)
        .edit()
        .putString("accessToken", token)
        .apply()

    // 2. Start SignalR
    SignalRManager.accessToken = token

    // 3. Navigate to home
    startActivity(Intent(this, MainActivity::class.java))
}
```

### Android - Setup Callbacks

```kotlin
fun setupSignalRCallbacks() {
    SignalRManager.onNewRequest = { event ->
        Log.d("App", "📥 New request: ${event.requestId}")
    }

    SignalRManager.onRequestAccepted = { event ->
        runOnUiThread {
            Toast.makeText(this, "تم قبول طلبك!", Toast.LENGTH_SHORT).show()
        }
    }

    SignalRManager.onPaymentCompleted = { event ->
        runOnUiThread {
            // Update UI
        }
    }
}
```

### Android - Logout

```kotlin
fun logout() {
    SignalRManager.disconnectAll()
    getSharedPreferences("app", MODE_PRIVATE).edit().clear().apply()
    startActivity(Intent(this, LoginActivity::class.java))
    finish()
}
```

---

## 8. Events Reference Table

| Event | Hub | المُرسل | المُستقبل | متى؟ |
|-------|-----|--------|----------|------|
| `NewRequest` | charging-request | Vehicle Owner | Charger Owner | طلب شحن جديد |
| `RequestAccepted` | charging-request | Charger Owner | Vehicle Owner | قبول الطلب |
| `RequestRejected` | charging-request | Charger Owner | Vehicle Owner | رفض الطلب |
| `RequestConfirmed` | charging-request | Charger Owner | Vehicle Owner | تأكيد الجلسة |
| `RequestAborted` | charging-request | Any | الطرف الآخر | إلغاء |
| `ProcessCreated` | process | Vehicle Owner | Charger Owner | إنشاء عملية |
| `ProcessStarted` | process | Any | الطرف الآخر | بدء الجلسة |
| `PaymentCompleted` | process | Any | الطرف الآخر | اكتمال |
| `PaymentAborted` | process | Any | الطرف الآخر | إلغاء |
| `PaymentStatusChanged` | process | Vehicle Owner | Charger Owner | تحديث بيانات |
| `ReceiveNotification` | notification | Server | User معين | إشعار شخصي |
| `ReceiveBroadcast` | notification | Server | الكل | إشعار عام |

---

## 9. Frontend Checklist

```
┌──────────────────────────────────────────────────────────────┐
│                    Frontend Checklist                         │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  1. [ ] أضف مكتبة SignalR                                    │
│         iOS: pod 'SignalRClient'                             │
│         Android: com.microsoft.signalr:signalr               │
│                                                               │
│  2. [ ] أنشئ SignalRManager singleton                        │
│                                                               │
│  3. [ ] بعد Login → SignalRManager.accessToken = token       │
│                                                               │
│  4. [ ] اربط الـ callbacks بالـ UI                           │
│                                                               │
│  5. [ ] عند Logout → SignalRManager.disconnectAll()          │
│                                                               │
│  6. [ ] اختبر كل event منفصل                                 │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## متى يتصل؟ متى يقطع؟

```
App Lifecycle:

[App Launch]
     ↓
[Login Screen]
     ↓
[Login Success] ──→ ✅ SignalRManager.accessToken = token
     ↓
[Main App] ←── يستقبل real-time events
     ↓
[Logout] ──→ ❌ SignalRManager.disconnectAll()
```

---

## ملاحظات مهمة

1. **التوكن مطلوب** - جميع الـ Hubs تتطلب JWT Token
2. **Auto Reconnect مفعّل** - إذا انقطع الاتصال يعيد الاتصال تلقائياً
3. **Firebase لا يزال يعمل** - SignalR يعمل بجانب FCM وليس بديلاً عنه
4. **Thread Safety** - استخدم `DispatchQueue.main.async` (iOS) أو `runOnUiThread` (Android) لتحديث UI

---

## تاريخ التحديث
- **2024**: Initial SignalR implementation
- تم إنشاء هذا الملف تلقائياً من Claude Code
