# Sliding Refresh Token - Mobile App Integration Guide

## Overview

The `/api/Auth/RefreshToken` endpoint now implements **Sliding Refresh Token** mechanism. Each time you refresh your tokens, you receive a **NEW refresh token** that must be saved and used for the next refresh call.

---

## API Endpoint

```
POST /api/Auth/RefreshToken
Authorization: Bearer {refreshToken}
```

---

## Response Format

```json
{
  "status": true,
  "message": "TokenRefreshedSuccessfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "555dce3b-198f-426d-b0be-92d234747cc3",
    "accessTokenExpiresAt": "2026-01-06T14:18:40",
    "refreshTokenExpiresAt": "2026-01-13T13:48:40"
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `accessToken` | string | New JWT access token for API calls |
| `refreshToken` | string | **NEW** refresh token for next refresh |
| `accessTokenExpiresAt` | datetime | Access token expiry time |
| `refreshTokenExpiresAt` | datetime | Refresh token expiry time |

---

## Token Validity

| Token | Duration | Notes |
|-------|----------|-------|
| Access Token | **30 minutes** | Used for API authentication |
| Refresh Token | **7 days** | Resets with each use (sliding) |

---

## Important: One-Time Use

Each refresh token can only be used **ONCE**. After using it, you must save and use the new refresh token from the response.

```
Call 1: Token "abc-123" → Success → New token "def-456"
Call 2: Token "abc-123" → FAIL (Unauthorized)
Call 2: Token "def-456" → Success → New token "ghi-789"
```

---

# iOS Implementation (Swift)

## Response Model

```swift
struct TokenResponse: Codable {
    let status: Bool
    let message: String
    let data: TokenData?
}

struct TokenData: Codable {
    let accessToken: String
    let refreshToken: String
    let accessTokenExpiresAt: String
    let refreshTokenExpiresAt: String
}
```

## Keychain Helper

```swift
import Security

class KeychainHelper {

    static func save(_ value: String, forKey key: String) {
        guard let data = value.data(using: .utf8) else { return }

        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: key,
            kSecValueData as String: data
        ]

        SecItemDelete(query as CFDictionary)
        SecItemAdd(query as CFDictionary, nil)
    }

    static func get(_ key: String) -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: key,
            kSecReturnData as String: true
        ]

        var result: AnyObject?
        SecItemCopyMatching(query as CFDictionary, &result)

        guard let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }

    static func delete(_ key: String) {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: key
        ]
        SecItemDelete(query as CFDictionary)
    }

    static func clearAll() {
        delete("accessToken")
        delete("refreshToken")
        delete("accessTokenExpiry")
        delete("refreshTokenExpiry")
    }
}
```

## Auth Service

```swift
import Foundation

class AuthService {

    static let shared = AuthService()
    private let baseURL = "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net"

    func refreshTokens(completion: @escaping (Result<String, Error>) -> Void) {

        guard let refreshToken = KeychainHelper.get("refreshToken") else {
            completion(.failure(AuthError.noRefreshToken))
            return
        }

        guard let url = URL(string: "\(baseURL)/api/Auth/RefreshToken") else { return }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(refreshToken)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        URLSession.shared.dataTask(with: request) { data, response, error in

            if let error = error {
                completion(.failure(error))
                return
            }

            guard let data = data else {
                completion(.failure(AuthError.noData))
                return
            }

            do {
                let tokenResponse = try JSONDecoder().decode(TokenResponse.self, from: data)

                if tokenResponse.status, let tokenData = tokenResponse.data {

                    // IMPORTANT: Save the NEW tokens
                    KeychainHelper.save(tokenData.accessToken, forKey: "accessToken")
                    KeychainHelper.save(tokenData.refreshToken, forKey: "refreshToken")
                    KeychainHelper.save(tokenData.accessTokenExpiresAt, forKey: "accessTokenExpiry")
                    KeychainHelper.save(tokenData.refreshTokenExpiresAt, forKey: "refreshTokenExpiry")

                    completion(.success(tokenData.accessToken))

                } else {
                    KeychainHelper.clearAll()
                    completion(.failure(AuthError.sessionExpired))
                }

            } catch {
                completion(.failure(error))
            }

        }.resume()
    }
}

enum AuthError: Error, LocalizedError {
    case noRefreshToken
    case noData
    case sessionExpired

    var errorDescription: String? {
        switch self {
        case .noRefreshToken: return "No refresh token found"
        case .noData: return "No data received"
        case .sessionExpired: return "Session expired, please login again"
        }
    }
}
```

## API Client with Auto Refresh

```swift
class APIClient {

    static let shared = APIClient()
    private let baseURL = "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net"

    func request<T: Codable>(
        endpoint: String,
        method: String = "GET",
        body: Encodable? = nil,
        completion: @escaping (Result<T, Error>) -> Void
    ) {
        executeRequest(endpoint: endpoint, method: method, body: body, isRetry: false, completion: completion)
    }

    private func executeRequest<T: Codable>(
        endpoint: String,
        method: String,
        body: Encodable?,
        isRetry: Bool,
        completion: @escaping (Result<T, Error>) -> Void
    ) {
        guard let accessToken = KeychainHelper.get("accessToken") else {
            completion(.failure(AuthError.noRefreshToken))
            return
        }

        guard let url = URL(string: "\(baseURL)\(endpoint)") else { return }

        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        if let body = body {
            request.httpBody = try? JSONEncoder().encode(body)
        }

        URLSession.shared.dataTask(with: request) { data, response, error in

            if let httpResponse = response as? HTTPURLResponse, httpResponse.statusCode == 401 {

                // Token expired - try refresh (only once)
                if !isRetry {
                    AuthService.shared.refreshTokens { result in
                        switch result {
                        case .success:
                            self.executeRequest(endpoint: endpoint, method: method, body: body, isRetry: true, completion: completion)
                        case .failure(let error):
                            completion(.failure(error))
                        }
                    }
                    return
                }
            }

            // Handle response...
            guard let data = data else {
                completion(.failure(AuthError.noData))
                return
            }

            do {
                let result = try JSONDecoder().decode(T.self, from: data)
                completion(.success(result))
            } catch {
                completion(.failure(error))
            }

        }.resume()
    }
}
```

## Usage Example (iOS)

```swift
// Login - save initial tokens
func login(email: String, password: String) {
    // After successful login response:
    KeychainHelper.save(response.data.token, forKey: "accessToken")
    KeychainHelper.save(response.data.refreshToken, forKey: "refreshToken")
}

// Make API call - auto refresh if needed
APIClient.shared.request(endpoint: "/api/chargers") { (result: Result<ChargersResponse, Error>) in
    switch result {
    case .success(let chargers):
        print("Chargers: \(chargers)")
    case .failure(let error):
        if case AuthError.sessionExpired = error {
            // Navigate to login screen
            self.navigateToLogin()
        }
    }
}
```

---

# Android Implementation (Kotlin)

## Response Model

```kotlin
data class TokenResponse(
    val status: Boolean,
    val message: String,
    val data: TokenData?
)

data class TokenData(
    val accessToken: String,
    val refreshToken: String,
    val accessTokenExpiresAt: String,
    val refreshTokenExpiresAt: String
)
```

## Gradle Dependencies

```gradle
dependencies {
    // Encrypted SharedPreferences
    implementation "androidx.security:security-crypto:1.1.0-alpha06"

    // Networking
    implementation "com.squareup.okhttp3:okhttp:4.12.0"
    implementation "com.squareup.retrofit2:retrofit:2.9.0"
    implementation "com.squareup.retrofit2:converter-gson:2.9.0"
    implementation "com.google.code.gson:gson:2.10.1"

    // Coroutines
    implementation "org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3"
}
```

## Secure Storage

```kotlin
import android.content.Context
import android.content.SharedPreferences
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey

class SecurePrefs(context: Context) {

    private val masterKey = MasterKey.Builder(context)
        .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
        .build()

    private val prefs: SharedPreferences = EncryptedSharedPreferences.create(
        context,
        "secure_prefs",
        masterKey,
        EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
        EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
    )

    fun saveTokens(tokenData: TokenData) {
        prefs.edit()
            .putString("accessToken", tokenData.accessToken)
            .putString("refreshToken", tokenData.refreshToken)
            .putString("accessTokenExpiry", tokenData.accessTokenExpiresAt)
            .putString("refreshTokenExpiry", tokenData.refreshTokenExpiresAt)
            .apply()
    }

    fun getAccessToken(): String? = prefs.getString("accessToken", null)

    fun getRefreshToken(): String? = prefs.getString("refreshToken", null)

    fun clearAll() {
        prefs.edit().clear().apply()
    }
}
```

## Auth Repository

```kotlin
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import com.google.gson.Gson

class AuthRepository(
    private val securePrefs: SecurePrefs
) {

    private val client = OkHttpClient()
    private val gson = Gson()
    private val baseUrl = "https://voltyks-dqh6fzgwdndrdng7.canadacentral-01.azurewebsites.net"

    suspend fun refreshTokens(): Result<String> = withContext(Dispatchers.IO) {

        val refreshToken = securePrefs.getRefreshToken()
            ?: return@withContext Result.failure(Exception("No refresh token"))

        val request = Request.Builder()
            .url("$baseUrl/api/Auth/RefreshToken")
            .post("".toRequestBody(null))
            .addHeader("Authorization", "Bearer $refreshToken")
            .addHeader("Content-Type", "application/json")
            .build()

        try {
            val response = client.newCall(request).execute()
            val body = response.body?.string()

            val tokenResponse = gson.fromJson(body, TokenResponse::class.java)

            if (tokenResponse.status && tokenResponse.data != null) {

                // IMPORTANT: Save the NEW tokens
                securePrefs.saveTokens(tokenResponse.data)

                Result.success(tokenResponse.data.accessToken)

            } else {
                securePrefs.clearAll()
                Result.failure(Exception("Session expired"))
            }

        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
```

## Token Authenticator (Auto Refresh)

```kotlin
import okhttp3.Authenticator
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import kotlinx.coroutines.runBlocking

class TokenAuthenticator(
    private val authRepository: AuthRepository,
    private val securePrefs: SecurePrefs,
    private val onSessionExpired: () -> Unit
) : Authenticator {

    override fun authenticate(route: Route?, response: Response): Request? {

        // Avoid infinite loop - only retry once
        if (response.request.header("Authorization-Retry") != null) {
            // Session truly expired
            runBlocking {
                securePrefs.clearAll()
                onSessionExpired()
            }
            return null
        }

        // Try to refresh tokens
        val result = runBlocking { authRepository.refreshTokens() }

        return result.getOrNull()?.let { newAccessToken ->
            response.request.newBuilder()
                .header("Authorization", "Bearer $newAccessToken")
                .header("Authorization-Retry", "true")
                .build()
        }
    }
}
```

## OkHttp Client Setup

```kotlin
class NetworkModule(
    private val securePrefs: SecurePrefs,
    private val authRepository: AuthRepository,
    private val onSessionExpired: () -> Unit
) {

    val okHttpClient: OkHttpClient by lazy {
        OkHttpClient.Builder()
            .authenticator(TokenAuthenticator(authRepository, securePrefs, onSessionExpired))
            .addInterceptor { chain ->
                val token = securePrefs.getAccessToken()
                val request = chain.request().newBuilder()
                    .apply {
                        if (token != null) {
                            addHeader("Authorization", "Bearer $token")
                        }
                    }
                    .addHeader("Content-Type", "application/json")
                    .build()
                chain.proceed(request)
            }
            .build()
    }
}
```

## Usage Example (Android)

```kotlin
// Login - save initial tokens
fun login(email: String, password: String) {
    viewModelScope.launch {
        val response = authApi.login(LoginRequest(email, password))
        if (response.status) {
            securePrefs.saveTokens(TokenData(
                accessToken = response.data.token,
                refreshToken = response.data.refreshToken,
                accessTokenExpiresAt = "",
                refreshTokenExpiresAt = ""
            ))
        }
    }
}

// Make API call - auto refresh handled by Authenticator
fun getChargers() {
    viewModelScope.launch {
        try {
            val chargers = api.getChargers()
            _chargersState.value = chargers
        } catch (e: Exception) {
            // Handle error
        }
    }
}
```

---

# Error Handling

## Error Response

```json
{
  "status": false,
  "message": "RefreshTokenMismatch",
  "data": null,
  "errors": null
}
```

## Common Errors

| Error Message | Cause | Action |
|---------------|-------|--------|
| `RefreshTokenMismatch` | Token already used or invalid | Redirect to login |
| `Refresh token not found` | No Authorization header | Check request headers |
| `Invalid refresh token format` | Malformed token | Redirect to login |

---

# Testing Checklist

- [ ] Login and save both `accessToken` and `refreshToken`
- [ ] Call RefreshToken endpoint and verify new tokens are saved
- [ ] Call RefreshToken again with NEW token - should succeed
- [ ] Call RefreshToken with OLD token - should fail with `RefreshTokenMismatch`
- [ ] Test auto-refresh when access token expires
- [ ] Test redirect to login when refresh token expires

---

# Security Best Practices

1. **Always use secure storage** (Keychain for iOS, EncryptedSharedPreferences for Android)
2. **Never log tokens** in production
3. **Clear tokens on logout**
4. **Handle 401 errors** by attempting refresh, then logout if refresh fails
5. **Use HTTPS** for all API calls

---

# Summary

| Before | After |
|--------|-------|
| `data` = string (access token only) | `data.accessToken` = access token |
| | `data.refreshToken` = **NEW refresh token** |
| | `data.accessTokenExpiresAt` = expiry time |
| | `data.refreshTokenExpiresAt` = expiry time |

**Key Point:** After each refresh call, you MUST save and use the new `refreshToken` for the next refresh.
