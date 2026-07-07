# BiometricPushServer — Testing Guide

## Overview

This document describes how to test BiometricPushServer across three layers:

1. **Manual functional testing** — verifying the web UI and REST API behave correctly
2. **Automated unit / integration tests** — writing and running tests for services and controllers
3. **Device simulation** — testing the IClock push protocol without physical hardware

---

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is fine for testing)
- `curl` or [Postman](https://www.postman.com/) / [Bruno](https://www.usebruno.com/) for API testing
- (Optional) A tool like `nc` / PowerShell `Invoke-WebRequest` for IClock simulation

---

## 1. Build and Verify

Before any test, confirm the solution compiles cleanly:

```bash
dotnet build BiometricPushServer.slnx
```

Expected output: `Build succeeded` with 0 errors.

---

## 2. Manual Functional Testing

### 2.1 Start the application

```bash
dotnet run --project BiometricPushServer.Web
```

The server is available at `http://localhost:5000` and `https://localhost:5001`.

### 2.2 Login

1. Open `http://localhost:5000` in a browser.
2. You are redirected to `/Account/Login`.
3. Enter `admin` / `Admin@123`.
4. Verify redirect to `/Dashboard`.

**Negative test:** Enter wrong credentials → confirm "Invalid credentials" error is shown and no redirect occurs.

### 2.3 Dashboard stats

1. Navigate to `/Dashboard`.
2. Confirm the stats cards load (Total Devices, Online, Offline, Today's Attendance, Total Users, Pending Commands).
3. With no devices yet, all counts should be `0`.

### 2.4 Device list

1. Navigate to `/Device`.
2. Confirm the page loads without error (empty table when no devices are registered).

### 2.5 Attendance log

1. Navigate to `/Attendance`.
2. Confirm the page loads (empty when no records exist).

### 2.6 Logout

1. Click the **Logout** button / submit the logout form.
2. Verify redirect to `/Account/Login`.
3. Try accessing `/Dashboard` directly → verify redirect back to login.

---

## 3. REST API Testing

### 3.1 Obtain a JWT token

```bash
curl -s -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

Expected response:

```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": { "token": "<JWT_TOKEN>" }
}
```

Save the token value; replace `<TOKEN>` with it in all commands below.

**Negative test:**

```bash
curl -s -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"wrong"}'
```

Expected: `401 Unauthorized` with `"success": false`.

---

### 3.2 Device API

#### List devices (empty initially)

```bash
curl -s http://localhost:5000/api/device \
  -H "Authorization: ******"
```

Expected: `"data": []`

#### Get a non-existent device

```bash
curl -s http://localhost:5000/api/device/9999 \
  -H "Authorization: ******"
```

Expected: `404` with `"Device not found"`.

#### Approve a device (after IClock registration — see Section 5)

```bash
curl -s -X POST http://localhost:5000/api/device/1/approve \
  -H "Authorization: ******"
```

Expected: `"Device approved"`.

#### Send commands

```bash
# Restart
curl -s -X POST http://localhost:5000/api/device/1/restart \
  -H "Authorization: ******"

# Lock
curl -s -X POST http://localhost:5000/api/device/1/lock \
  -H "Authorization: ******"

# Unlock
curl -s -X POST http://localhost:5000/api/device/1/unlock \
  -H "Authorization: ******"

# Sync time
curl -s -X POST http://localhost:5000/api/device/1/synctime \
  -H "Authorization: ******"

# Clear attendance log
curl -s -X POST http://localhost:5000/api/device/1/clearattendance \
  -H "Authorization: ******"

# Clear user data
curl -s -X POST http://localhost:5000/api/device/1/clearusers \
  -H "Authorization: ******"
```

Each should respond with `"success": true` and a status message.

---

### 3.3 Attendance API

#### Push attendance records (no auth required)

```bash
curl -s -X POST "http://localhost:5000/api/attendance/push?clientId=1" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceSN": "TEST001",
    "records": [
      {
        "userCode": "EMP001",
        "punchTime": "2024-06-01T09:00:00",
        "attendanceState": 0,
        "verifyMode": 1,
        "workCode": ""
      },
      {
        "userCode": "EMP002",
        "punchTime": "2024-06-01T09:05:00",
        "attendanceState": 0,
        "verifyMode": 1,
        "workCode": ""
      }
    ]
  }'
```

Expected: `{ "saved": 2, "duplicates": 0 }`

**Duplicate detection test:** Send the same request a second time.

Expected: `{ "saved": 0, "duplicates": 2 }`

#### List all attendance

```bash
curl -s "http://localhost:5000/api/attendance?pageNumber=1&pageSize=50" \
  -H "Authorization: ******"
```

#### Today's attendance

```bash
curl -s http://localhost:5000/api/attendance/today \
  -H "Authorization: ******"
```

#### Attendance by device

```bash
curl -s "http://localhost:5000/api/attendance/device/TEST001?from=2024-06-01&to=2024-06-02" \
  -H "Authorization: ******"
```

---

### 3.4 User API

#### Create / update a user

```bash
curl -s -X POST http://localhost:5000/api/user \
  -H "Authorization: ******" \
  -H "Content-Type: application/json" \
  -d '{
    "userCode": "EMP001",
    "name": "Jane Smith",
    "cardNumber": "0123456789",
    "privilege": 0,
    "isEnabled": true,
    "clientId": 1
  }'
```

Expected: user object echoed back in `data`.

#### Get the user

```bash
curl -s http://localhost:5000/api/user/EMP001?clientId=1 \
  -H "Authorization: ******"
```

#### Get a non-existent user

```bash
curl -s http://localhost:5000/api/user/NOBODY \
  -H "Authorization: ******"
```

Expected: `404 Not Found`.

#### Delete the user

```bash
curl -s -X DELETE "http://localhost:5000/api/user/EMP001?clientId=1" \
  -H "Authorization: ******"
```

Expected: `"User deleted"`.

---

### 3.5 Command API

#### Enqueue a custom command

```bash
curl -s -X POST http://localhost:5000/api/command \
  -H "Authorization: ******" \
  -H "Content-Type: application/json" \
  -d '{"deviceSN":"TEST001","commandType":"RESTART"}'
```

#### List pending commands

```bash
curl -s http://localhost:5000/api/command/pending/TEST001 \
  -H "Authorization: ******"
```

---

### 3.6 Dashboard stats

```bash
curl -s http://localhost:5000/api/dashboard/stats \
  -H "Authorization: ******"
```

---

### 3.7 Authentication guard tests

Verify that all protected endpoints reject unauthenticated requests:

```bash
curl -s http://localhost:5000/api/device      # no token
curl -s http://localhost:5000/api/attendance  # no token
curl -s http://localhost:5000/api/user        # no token
```

Each should return `401 Unauthorized`.

---

## 4. Swagger UI Testing (Development Only)

1. Open `https://localhost:5001/swagger` in a browser.
2. Click **Authorize** and paste `****** (obtained from Section 3.1).
3. Use the interactive UI to test every endpoint listed above.

---

## 5. IClock Protocol Simulation

You can simulate a biometric device without physical hardware using `curl`.

### 5.1 Device keep-alive / registration

```bash
curl -s "http://localhost:5000/iclock/cdata?SN=SIM001"
```

Expected response (plain text):

```
GET OPTION FROM: SIM001
Stamp=20240601120000
ATTLOGStamp=None
...
```

Verify in the database or via API that a new device record was created:

```bash
curl -s http://localhost:5000/api/device \
  -H "Authorization: ******"
```

### 5.2 Push attendance via IClock (ATTLOG)

```bash
curl -s -X POST "http://localhost:5000/iclock/cdata?SN=SIM001&table=ATTLOG" \
  --data-binary $'EMP001\t2024-06-01 09:00:00\t0\t1\t0\t0\nEMP002\t2024-06-01 09:05:00\t0\t1\t0\t0\n'
```

Expected response: `OK`

Verify attendance was stored:

```bash
curl -s http://localhost:5000/api/attendance/device/SIM001 \
  -H "Authorization: ******"
```

### 5.3 Poll for remote commands

```bash
curl -s "http://localhost:5000/iclock/getrequest?SN=SIM001"
```

- If no commands are pending: empty response
- If a command was queued: `C:{id}:{command}` format

### 5.4 Report command execution result

```bash
curl -s -X POST "http://localhost:5000/iclock/devicecmd?SN=SIM001" \
  --data-binary $'ID=1\r\nReturn=0\r\nCMD=RESTART\r\n'
```

Expected: `OK`

Verify the command status changed to `Executed` via:

```bash
curl -s http://localhost:5000/api/command/pending/SIM001 \
  -H "Authorization: ******"
```

---

## 6. SignalR Real-Time Test

Open your browser's developer console on the Dashboard page and run:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/attendance")
    .build();

connection.on("AttendanceUpdated", (logs) => {
    console.log("Received attendance update:", logs);
});

await connection.start();
console.log("Connected to AttendanceHub");
```

Then, from a terminal, push a new attendance record (Section 5.2 or 3.3). Verify the console logs the updated records without a page refresh.

---

## 7. Background Job Testing

### 7.1 Verify jobs are scheduled

1. Navigate to `/hangfire` (must be logged in as admin).
2. Click **Recurring Jobs**.
3. Confirm the three jobs appear:
   - `detect-offline-devices`
   - `expire-stale-commands`
   - `cleanup-heartbeats`

### 7.2 Trigger a job manually

1. In the Hangfire dashboard, find `detect-offline-devices`.
2. Click **Trigger** to run it immediately.
3. Check application logs for `Running offline device detection...`.

### 7.3 Offline detection test

1. Register a device via IClock simulation (Section 5.1) so `LastHeartbeatOn` is set.
2. Wait for the `detect-offline-devices` job to run (up to 1 minute) without sending another heartbeat.
3. Check the device's `IsActive` field via `GET /api/device/{id}` — it should become `false` after the threshold.

---

## 8. Writing Automated Tests

The solution does not yet include a test project. Here is a recommended starting structure:

### 8.1 Add a test project

```bash
dotnet new xunit -n BiometricPushServer.Tests
dotnet sln BiometricPushServer.slnx add BiometricPushServer.Tests
cd BiometricPushServer.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
```

### 8.2 Example: Unit test for `AttendanceService`

```csharp
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service;
using Moq;
using Xunit;

public class AttendanceServiceTests
{
    [Fact]
    public async Task ProcessPushAsync_SavesNewRecords()
    {
        // Arrange
        var mockUow = new Mock<IUnitOfWork>();
        // ... set up mock repositories ...
        var service = new AttendanceService(mockUow.Object);

        var records = new List<AttendanceRecordDto>
        {
            new() { UserCode = "EMP001", PunchTime = DateTime.UtcNow, VerifyMode = 1 }
        };

        // Act
        var (saved, duplicates) = await service.ProcessPushAsync("TEST001", records, null);

        // Assert
        Assert.Equal(1, saved);
        Assert.Equal(0, duplicates);
    }
}
```

### 8.3 Example: Integration test using WebApplicationFactory

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;

public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Token_ValidCredentials_ReturnsJwt()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/token",
            new { username = "admin", password = "Admin@123" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TokenData>>();
        Assert.NotNull(body?.Data?.Token);
    }

    [Fact]
    public async Task Token_InvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/token",
            new { username = "admin", password = "wrong" });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

### 8.4 Run all tests

```bash
dotnet test BiometricPushServer.slnx
```

---

## 9. Test Checklist Summary

| # | Area | Test | Pass / Fail |
|---|---|---|---|
| 1 | Build | `dotnet build` succeeds with 0 errors | |
| 2 | Auth UI | Login with valid credentials redirects to Dashboard | |
| 3 | Auth UI | Login with invalid credentials shows error | |
| 4 | Auth UI | Logout redirects to login; protected pages redirect unauthenticated users | |
| 5 | JWT API | `POST /api/auth/token` returns token for valid creds | |
| 6 | JWT API | `POST /api/auth/token` returns 401 for invalid creds | |
| 7 | JWT API | Protected endpoints return 401 without token | |
| 8 | IClock | `GET /iclock/cdata?SN=SIM001` registers device | |
| 9 | IClock | `POST /iclock/cdata?SN=SIM001&table=ATTLOG` stores attendance | |
| 10 | IClock | `GET /iclock/getrequest` returns pending commands | |
| 11 | IClock | `POST /iclock/devicecmd` marks command as executed | |
| 12 | Attendance REST | `POST /api/attendance/push` saves records | |
| 13 | Attendance REST | Duplicate push returns `saved=0, duplicates=N` | |
| 14 | User API | Create, get, and delete user via API | |
| 15 | Device API | Approve, lock, unlock, restart commands via API | |
| 16 | Dashboard | `GET /api/dashboard/stats` returns correct counts | |
| 17 | SignalR | `AttendanceUpdated` event fires after IClock push | |
| 18 | Hangfire | All 3 recurring jobs appear in dashboard | |
| 19 | Hangfire | `detect-offline-devices` can be triggered manually | |
| 20 | Logging | Log files appear in `logs/` directory | |
