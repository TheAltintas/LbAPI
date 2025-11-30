# LittleBeaconAPI - Endpoints Testing Guide

## Base URL
- Development: `http://localhost:5119`

## Authentication Endpoints

### 1. Login
```powershell
$response = Invoke-RestMethod -Uri 'http://localhost:5119/api/Auth/login' -Method Post -ContentType 'application/json' -Body '{"username":"Admin","password":"Admin"}'
$token = $response.token
```

### 2. Register New User
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Auth/register' -Method Post -ContentType 'application/json' -Body '{"username":"newuser","password":"password123"}'
```

### 3. Get All Users (Admin Only)
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Auth/users' -Method Get -Headers @{ Authorization = "Bearer $token" }
```

### 4. Get User by ID (Admin Only)
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Auth/users/1' -Method Get -Headers @{ Authorization = "Bearer $token" }
```

### 5. Delete User (Admin Only)
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Auth/users/2' -Method Delete -Headers @{ Authorization = "Bearer $token" }
```

## Shift Management Endpoints

### 1. Get All Shifts
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts' -Method Get
```

### 2. Get Shift by ID
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts/1' -Method Get
```

### 3. Get Shifts by User ID
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts/user/1' -Method Get
```

### 4. Get Upcoming Shifts for User
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts/upcoming/1' -Method Get
```

### 5. Create New Shift (Admin Only)
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts' -Method Post -Headers @{ Authorization = "Bearer $token" } -ContentType 'application/json' -Body '{"date":"30. November","actualDate":"2025-11-30T00:00:00","time":"08:00-16:00","location":"Kontoret","userId":1,"hours":8,"tag":"Vagt","isCompleted":false,"weekOffset":0,"borderColor":"#FF0000"}'
```

### 6. Update Shift
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts/1' -Method Put -ContentType 'application/json' -Body '{"id":1,"date":"1. November","actualDate":"2025-11-01T00:00:00","time":"08:00-16:00","location":"Hjemme","userId":1,"hours":8,"tag":"Ferie","isCompleted":true,"weekOffset":0}'
```

### 7. Delete Shift (Admin Only)
```powershell
Invoke-RestMethod -Uri 'http://localhost:5119/api/Shifts/4' -Method Delete -Headers @{ Authorization = "Bearer $token" }
```

## Response Format

All endpoints return consistent JSON responses:

### Success Response
```json
{
  "success": true,
  "data": [...],
  "message": "Operation successful"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description"
}
```

## Shift Model Properties
- `id` (int) - Auto-generated
- `date` (string) - Display date (e.g., "30. November")
- `actualDate` (DateTime) - Actual date for sorting/filtering
- `time` (string) - Time range (e.g., "08:00-16:00")
- `location` (string) - Location name
- `tag` (string, optional) - Shift tag/category
- `userId` (int) - User ID who owns the shift
- `isCompleted` (bool) - Completion status
- `hours` (int) - Number of hours
- `status` (string, optional) - Status (default: "Vagt")
- `notes` (string, optional) - Additional notes
- `borderColor` (string, optional) - Color code for UI
- `weekOffset` (int) - Week offset for scheduling
- `createdAt` (DateTime) - Creation timestamp
- `sickReportId` (int, optional) - Related sick report ID

## Admin Authorization
To access admin-only endpoints, include the Authorization header:
```
Authorization: Bearer {token}
```

The token is obtained from the login endpoint.

## CORS Configuration
The API allows requests from:
- `http://localhost:4200`
- `http://localhost:4201`

## Database
- SQLite database
- Location: Root directory
- Migrations are applied automatically on startup
