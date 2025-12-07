# Pagination API Documentation
## For Frontend Team (iOS & Android)

---

## Overview

The Voltyks API now supports pagination for list endpoints. This document explains how to use pagination for fetching paginated data efficiently.

---

## How Pagination Works

### Query Parameters

All paginated endpoints accept these query parameters:

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `pageNumber` | int | 1 | - | The page number to retrieve (1-based) |
| `pageSize` | int | 20 | 100 | Number of items per page |

### Example Request

```http
GET /api/processes/my-activities?pageNumber=1&pageSize=10
Authorization: Bearer {access_token}
```

---

## Paginated Response Structure

All paginated endpoints return data in this format:

```json
{
    "data": {
        "items": [...],
        "totalCount": 150,
        "pageNumber": 1,
        "pageSize": 10,
        "totalPages": 15,
        "hasPrevious": false,
        "hasNext": true
    },
    "message": "Success",
    "status": true
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `items` | array | The list of items for the current page |
| `totalCount` | int | Total number of items across all pages |
| `pageNumber` | int | Current page number |
| `pageSize` | int | Number of items per page |
| `totalPages` | int | Total number of pages |
| `hasPrevious` | bool | `true` if there's a previous page |
| `hasNext` | bool | `true` if there's a next page |

---

## Paginated Endpoints

### 1. My Activities (Previous Processes)

```http
GET /api/processes/my-activities
Authorization: Bearer {access_token}
```

**Query Parameters:**
```
?pageNumber=1&pageSize=20
```

**Response:**
```json
{
    "data": {
        "items": [
            {
                "id": 123,
                "chargerRequestId": 456,
                "status": "Completed",
                "amountCharged": 25.50,
                "amountPaid": 30.00,
                "dateCreated": "2025-12-07T10:30:00Z",
                "dateCompleted": "2025-12-07T11:00:00Z",
                "isAsChargerOwner": false,
                "isAsVehicleOwner": true,
                "direction": "Outgoing",
                "counterpartyUserId": "user-id-here",
                "myRoleUserTypeId": 2,
                "vehicleOwnerRating": 4.5,
                "chargerOwnerRating": 5.0,
                "chargerProtocolName": "CCS2",
                "chargerCapacityKw": 50
            }
        ],
        "totalCount": 45,
        "pageNumber": 1,
        "pageSize": 20,
        "totalPages": 3,
        "hasPrevious": false,
        "hasNext": true
    },
    "message": "My activities fetched",
    "status": true
}
```

---

### 2. Current Requests (Pending Charging Requests)

```http
GET /api/auth/getUsersRequests
Authorization: Bearer {access_token}
```

**Query Parameters:**
```
?pageNumber=1&pageSize=10
```

**Response:**
```json
{
    "data": {
        "items": [
            {
                "id": 789,
                "requestedAt": "2025-12-07T14:00:00Z",
                "status": "Pending",
                "kwNeeded": 30,
                "distanceInKm": 2.5,
                "estimatedArrival": 5.0,
                "estimatedPrice": 45.00,
                "vehicleArea": "Nasr City",
                "vehicleStreet": "Makram Ebeid",
                "vehicleBrand": "Tesla",
                "vehicleModel": "Model 3",
                "vehicleColor": "White",
                "vehiclePlate": "ABC 123",
                "vehicleCapacity": 75
            }
        ],
        "totalCount": 3,
        "pageNumber": 1,
        "pageSize": 10,
        "totalPages": 1,
        "hasPrevious": false,
        "hasNext": false
    },
    "message": "Data retrieved successfully",
    "status": true
}
```

---

## Implementation Examples

### iOS (Swift)

```swift
// Pagination Parameters
struct PaginationParams {
    var pageNumber: Int = 1
    var pageSize: Int = 20

    var queryString: String {
        return "pageNumber=\(pageNumber)&pageSize=\(pageSize)"
    }
}

// Paginated Response Model
struct PagedResult<T: Decodable>: Decodable {
    let items: [T]
    let totalCount: Int
    let pageNumber: Int
    let pageSize: Int
    let totalPages: Int
    let hasPrevious: Bool
    let hasNext: Bool
}

struct ApiResponse<T: Decodable>: Decodable {
    let data: T?
    let message: String
    let status: Bool
}

// Usage Example
class ActivityService {
    func getMyActivities(params: PaginationParams) async throws -> PagedResult<MyActivity> {
        let url = URL(string: "https://api.voltyks.com/api/processes/my-activities?\(params.queryString)")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")

        let (data, _) = try await URLSession.shared.data(for: request)
        let response = try JSONDecoder().decode(ApiResponse<PagedResult<MyActivity>>.self, from: data)
        return response.data!
    }
}

// Pagination Controller Example
class ActivitiesViewController: UIViewController {
    var currentPage = 1
    var hasMore = true
    var isLoading = false
    var activities: [MyActivity] = []

    func loadMore() async {
        guard hasMore && !isLoading else { return }
        isLoading = true

        let params = PaginationParams(pageNumber: currentPage, pageSize: 20)
        let result = try await activityService.getMyActivities(params: params)

        activities.append(contentsOf: result.items)
        hasMore = result.hasNext
        currentPage += 1
        isLoading = false
    }
}
```

### Android (Kotlin)

```kotlin
// Pagination Parameters
data class PaginationParams(
    val pageNumber: Int = 1,
    val pageSize: Int = 20
)

// Paginated Response Model
data class PagedResult<T>(
    val items: List<T>,
    val totalCount: Int,
    val pageNumber: Int,
    val pageSize: Int,
    val totalPages: Int,
    val hasPrevious: Boolean,
    val hasNext: Boolean
)

data class ApiResponse<T>(
    val data: T?,
    val message: String,
    val status: Boolean
)

// Retrofit Interface
interface VoltyksApi {
    @GET("api/processes/my-activities")
    suspend fun getMyActivities(
        @Header("Authorization") token: String,
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 20
    ): ApiResponse<PagedResult<MyActivity>>

    @GET("api/auth/getUsersRequests")
    suspend fun getCurrentRequests(
        @Header("Authorization") token: String,
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 20
    ): ApiResponse<PagedResult<ChargingRequest>>
}

// ViewModel with Pagination
class ActivitiesViewModel : ViewModel() {
    private var currentPage = 1
    private var hasMore = true
    private var isLoading = false

    private val _activities = MutableLiveData<List<MyActivity>>()
    val activities: LiveData<List<MyActivity>> = _activities

    fun loadMore() {
        if (!hasMore || isLoading) return
        isLoading = true

        viewModelScope.launch {
            val result = api.getMyActivities(
                token = "Bearer $accessToken",
                pageNumber = currentPage,
                pageSize = 20
            )

            if (result.status && result.data != null) {
                val current = _activities.value.orEmpty()
                _activities.value = current + result.data.items
                hasMore = result.data.hasNext
                currentPage++
            }
            isLoading = false
        }
    }

    fun refresh() {
        currentPage = 1
        hasMore = true
        _activities.value = emptyList()
        loadMore()
    }
}
```

---

## Infinite Scroll Implementation

### iOS (SwiftUI)

```swift
struct ActivitiesListView: View {
    @StateObject var viewModel = ActivitiesViewModel()

    var body: some View {
        List {
            ForEach(viewModel.activities) { activity in
                ActivityRow(activity: activity)
            }

            if viewModel.hasMore {
                ProgressView()
                    .onAppear {
                        Task {
                            await viewModel.loadMore()
                        }
                    }
            }
        }
        .refreshable {
            await viewModel.refresh()
        }
    }
}
```

### Android (Compose)

```kotlin
@Composable
fun ActivitiesScreen(viewModel: ActivitiesViewModel) {
    val activities by viewModel.activities.observeAsState(emptyList())

    LazyColumn {
        items(activities) { activity ->
            ActivityItem(activity)
        }

        if (viewModel.hasMore) {
            item {
                LaunchedEffect(Unit) {
                    viewModel.loadMore()
                }
                CircularProgressIndicator(
                    modifier = Modifier.fillMaxWidth().padding(16.dp)
                )
            }
        }
    }
}
```

---

## Best Practices

### 1. Page Size Recommendations

| Use Case | Recommended Page Size |
|----------|----------------------|
| Activities List | 20 |
| Current Requests | 10 |
| Search Results | 15 |

### 2. Caching Strategy

- Cache the first page for offline access
- Invalidate cache on pull-to-refresh
- Store `totalCount` to show estimated list size

### 3. Error Handling

```swift
// iOS
if !response.status {
    // Show error message
    showError(response.message)
} else if response.data?.items.isEmpty == true && response.data?.pageNumber == 1 {
    // Show empty state
    showEmptyState()
}
```

```kotlin
// Android
when {
    !response.status -> showError(response.message)
    response.data?.items.isNullOrEmpty() && response.data?.pageNumber == 1 -> showEmptyState()
    else -> updateUI(response.data)
}
```

### 4. Loading States

- Show skeleton loading for first page
- Show footer spinner for subsequent pages
- Disable load more while loading

---

## Migration Guide

### Before (Without Pagination)

```http
GET /api/processes/my-activities
```

Response: `{ "data": [...all items...], "message": "...", "status": true }`

### After (With Pagination)

```http
GET /api/processes/my-activities?pageNumber=1&pageSize=20
```

Response: `{ "data": { "items": [...], "totalCount": 100, ... }, "message": "...", "status": true }`

**Note:** If you don't provide pagination parameters, defaults will be used (page 1, size 20).

---

## Testing Checklist

- [ ] Test first page load with default parameters
- [ ] Test with custom pageNumber and pageSize
- [ ] Test loading next pages (hasNext = true)
- [ ] Test last page (hasNext = false)
- [ ] Test empty results (items = [])
- [ ] Test pageSize > 100 (should cap at 100)
- [ ] Test pageNumber = 0 (should default to 1)
- [ ] Test pull-to-refresh resets to page 1

---

## Contact

For API issues or questions, contact the backend team.
