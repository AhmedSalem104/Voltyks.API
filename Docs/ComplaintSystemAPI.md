# Complaint System API Documentation
## For Frontend Team (iOS & Android)

---

## Overview

The Complaint System allows users to submit categorized complaints with rate limiting (1 complaint per 12 hours). The rate limit resets when a user creates a new charging process.

---

## User Endpoints

### 1. Submit Complaint

```http
POST /api/auth/general-complaints
Content-Type: application/json
Authorization: Bearer {access_token}
```

**Request Body:**
```json
{
    "categoryId": 1,
    "content": "Your complaint message here (max 2000 characters)"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `categoryId` | int | Yes | Category ID from complaint categories list |
| `content` | string | Yes | Complaint message (max 2000 chars) |
| `userId` | string | No | Optional - defaults to authenticated user |

**Success Response (200):**
```json
{
    "data": {
        "complaintId": 15,
        "categoryId": 1,
        "categoryName": "Technical Issue",
        "content": "Your complaint message",
        "createdAt": "2025-12-07T14:30:00Z"
    },
    "message": "Complaint submitted successfully",
    "status": true
}
```

**Rate Limit Error (200 with status: false):**
```json
{
    "data": null,
    "message": "يمكنك تقديم شكوى جديدة بعد 5 ساعة و 30 دقيقة",
    "status": false,
    "errors": ["Rate limit exceeded. Only 1 complaint per 12 hours allowed."]
}
```

**Validation Error:**
```json
{
    "data": null,
    "message": "Category not found or deleted",
    "status": false
}
```

---

### 2. Get Complaint Categories (for dropdown/picker)

```http
GET /api/admin/complaint-categories
Authorization: Bearer {access_token}
```

**Response:**
```json
{
    "data": [
        {
            "id": 1,
            "name": "Technical Issue",
            "description": "Problems with app functionality",
            "complaintsCount": 25
        },
        {
            "id": 2,
            "name": "Payment Problem",
            "description": "Issues related to payments and transactions",
            "complaintsCount": 12
        },
        {
            "id": 3,
            "name": "Charger Issue",
            "description": "Problems with charger equipment",
            "complaintsCount": 8
        },
        {
            "id": 4,
            "name": "User Complaint",
            "description": "Complaints about other users",
            "complaintsCount": 5
        }
    ],
    "message": "Success",
    "status": true
}
```

---

## Rate Limiting Rules

| Rule | Value |
|------|-------|
| **Limit** | 1 complaint per 12 hours |
| **Reset Trigger** | When user creates a new charging Process |
| **Error Message** | Shows remaining time in Arabic |

---

## UI/UX Recommendations

### Complaint Form Screen

1. **Category Picker**
   - Fetch categories from `/api/admin/complaint-categories`
   - Show category name and description
   - Required field

2. **Content Text Area**
   - Max 2000 characters
   - Show character counter
   - Required field

3. **Submit Button**
   - Disable while loading
   - Handle rate limit error gracefully

### Error Handling

```swift
// iOS Example
if response.status == false {
    if response.message.contains("ساعة") {
        // Rate limit error - show wait time
        showRateLimitAlert(message: response.message)
    } else {
        // Other error
        showErrorAlert(message: response.message)
    }
}
```

```kotlin
// Android Example
if (!response.status) {
    if (response.message.contains("ساعة")) {
        // Rate limit error - show wait time
        showRateLimitDialog(response.message)
    } else {
        // Other error
        showErrorDialog(response.message)
    }
}
```

---

## Data Models

### iOS (Swift)

```swift
// Request
struct CreateComplaintRequest: Codable {
    let categoryId: Int
    let content: String
}

// Response
struct ComplaintResponse: Codable {
    let complaintId: Int
    let categoryId: Int
    let categoryName: String
    let content: String
    let createdAt: String
}

// Category
struct ComplaintCategory: Codable {
    let id: Int
    let name: String
    let description: String?
    let complaintsCount: Int
}
```

### Android (Kotlin)

```kotlin
// Request
data class CreateComplaintRequest(
    val categoryId: Int,
    val content: String
)

// Response
data class ComplaintResponse(
    val complaintId: Int,
    val categoryId: Int,
    val categoryName: String,
    val content: String,
    val createdAt: String
)

// Category
data class ComplaintCategory(
    val id: Int,
    val name: String,
    val description: String?,
    val complaintsCount: Int
)
```

---

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    USER COMPLAINT FLOW                       │
└─────────────────────────────────────────────────────────────┘

1. User opens Complaint Screen
         │
         ▼
2. App fetches categories
   GET /api/admin/complaint-categories
         │
         ▼
3. User selects category & writes complaint
         │
         ▼
4. User taps Submit
         │
         ▼
5. POST /api/auth/general-complaints
         │
         ├─── status: true ──────► Show Success Message
         │                         "تم إرسال الشكوى بنجاح"
         │
         └─── status: false
                    │
                    ├── Rate Limit ──► Show Wait Time
                    │                  "يمكنك تقديم شكوى جديدة بعد X ساعة"
                    │
                    └── Other Error ──► Show Error Message
```

---

## Important Notes

1. **Authentication Required**: All endpoints require valid `Authorization: Bearer {token}` header

2. **Rate Limit Reset**: The 12-hour timer resets automatically when the user creates a new charging process (confirms a charging session)

3. **Category Validation**: Always validate that selected category exists before submission

4. **Character Limit**: Enforce 2000 character limit on frontend before API call

5. **Offline Handling**: Consider queuing complaints if user is offline

---

## Testing Checklist

- [ ] Submit complaint with valid category
- [ ] Submit complaint and try again within 12 hours (expect rate limit)
- [ ] Submit complaint with invalid category ID
- [ ] Submit complaint with empty content
- [ ] Submit complaint with content > 2000 chars
- [ ] Verify categories list loads correctly
- [ ] Test Arabic/English content submission

---

## Contact

For API issues or questions, contact the backend team.
