# BiometricPushServer — How to Use

## Overview

BiometricPushServer is an ASP.NET Core 8 application that acts as a **central attendance server** for biometric time-and-attendance hardware (ZKTeco, eSSL, Hikvision, and any device that supports the IClock push protocol). Devices push attendance records to the server in real time; administrators monitor everything through a web dashboard or REST API.

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0 or later |
| SQL Server | 2019 / 2022 (or SQL Server Express / LocalDB for development) |
| A biometric device | Any device that supports the **IClock / ADMS push protocol** |

---

## 1. Initial Setup

### 1.1 Clone and build

```bash
git clone https://github.com/Shikshaware/BiometricPushServer.git
cd BiometricPushServer
dotnet build BiometricPushServer.slnx
```

### 1.2 Configure the database connection

Edit `BiometricPushServer.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=YOUR_SERVER;Database=BiometricPushServer;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 1.3 Configure authentication

The default admin credentials are set in `appsettings.json`. **Change these before deploying to production.**

```json
{
  "Auth": {
    "AdminUsername": "admin",
    "AdminPassword": "Admin@123",
    "JwtSecret": "Replace_This_With_A_Long_Random_Secret_At_Least_32_Chars"
  }
}
```

### 1.4 Run database migrations

In **Development** mode the app auto-migrates on startup. For production, run migrations explicitly:

```bash
cd BiometricPushServer.Web
dotnet ef database update
```

### 1.5 Start the application

```bash
dotnet run --project BiometricPushServer.Web
```

The server starts on `https://localhost:5001` (HTTPS) and `http://localhost:5000` (HTTP) by default.

If you do not explicitly set `ASPNETCORE_URLS` or Kestrel endpoints, the app uses `DeviceCompatibility:DefaultHttpUrl` from configuration so already-configured biometric devices can continue to reach HTTP port `5000`. In production, set your own host bindings if you need a different interface or port exposure policy.

---

## 2. Web Dashboard

Navigate to `https://localhost:5001` in your browser.

### Login

| Field | Default value |
|---|---|
| Username | `admin` |
| Password | `Admin@123` |

Provider admins can still login with the default admin credentials.  
Client owners login with `username + password + clientId` and are automatically scoped to their own client data.

After login you are redirected to the **Dashboard** (`/Dashboard`).

### Dashboard pages

| URL | Description |
|---|---|
| `/Dashboard` | Provider/client dashboard. Provider selects a client, then sees that client's live stats + attendance reporting |
| `/Device` | List all registered devices |
| `/Device/Details/{id}` | Device detail — approve, lock, unlock, restart, sync time, clear logs |
| `/Attendance` | Paginated attendance log (all time) |
| `/Attendance/Today` | Today's punches only |
| `/User` | Registered users (paginated) |

### Provider and client dashboards

- **Provider users** (no `client_id` claim) land on a provider dashboard where they can select a client and open a client-scoped dashboard.
- **Client users** (with `client_id` claim) are automatically restricted to their own client dashboard/report data.
- Dashboard report section supports **Daily / Weekly / Monthly** attendance reporting with:
  - First In
  - Last Out
  - Punch count
- Reports are shown in UI and can be downloaded as **Excel (.xlsx)**.

### Filtering by client

All list pages accept an optional `clientId` query parameter to scope results to a specific tenant/client:

```
/Attendance?clientId=5
/Device?clientId=5
```

---

## 3. Connecting a Biometric Device (IClock Protocol)

Biometric devices that support **ADMS / IClock push** (ZKTeco, eSSL, etc.) need to be pointed at this server.

### Device configuration

In the device's network settings, set the **ADMS / Push server** address to:

```
http://YOUR_SERVER_IP:5000/iclock/
```

For compatibility with devices that are already configured for plain HTTP push, `/iclock/*` requests can be accepted on that HTTP port without HTTPS redirection when `DeviceCompatibility:AllowHttpIClock` is enabled. Deploy these device callbacks only on a trusted LAN/VPN or behind network controls that fit your environment.

The server exposes the following IClock endpoints automatically — no additional configuration is needed:

| Endpoint | Method | Purpose |
|---|---|---|
| `/iclock/cdata?SN={serial}` | GET | Device keep-alive / registration |
| `/iclock/cdata?SN={serial}&table=ATTLOG` | POST | Device pushes attendance records |
| `/iclock/getrequest?SN={serial}` | GET | Device polls for remote commands |
| `/iclock/devicecmd?SN={serial}` | POST | Device reports command execution result |

### Device registration flow

1. The device sends its first `GET /iclock/cdata` heartbeat.
2. The server **auto-registers** the device in the database with `IsApproved = false`.
3. An administrator **approves** the device from the web dashboard (`/Device`) or via the API.
4. Once approved, attendance records pushed by the device are stored and appear in real time on the dashboard.

> **Note:** A device that is not yet approved still registers and sends heartbeats, but its attendance data is recorded against it only after the device record exists in the database.

---

## 4. REST API

All API endpoints return JSON in the following envelope:

```json
{
  "success": true,
  "message": "Success",
  "statusCode": 200,
  "data": { ... }
}
```

### 4.1 Obtain a JWT token

```http
POST /api/auth/token
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123",
  "clientId": null
}
```

Response:

```json
{
  "success": true,
  "data": { "token": "<JWT>" }
}
```

Use the token in all subsequent API requests:

```
Authorization: ******
```

Owner logins must include `clientId` in the token request payload.

### 4.1.1 Owner invite and registration

Provider admin can create owner invite tokens:

```http
POST /api/auth/create-owner-invite
Authorization: ******
Content-Type: application/json

{
  "clientId": 5,
  "username": "owner1",
  "timeZoneId": "Asia/Kolkata"
}
```

Owner registration with invite token:

```http
POST /api/auth/register-owner
Content-Type: application/json

{
  "clientId": 5,
  "username": "owner1",
  "password": "StrongPass!123",
  "inviteToken": "<token>",
  "timeZoneId": "Asia/Kolkata"
}
```

### 4.2 Devices

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/device` | List all devices (optional `?clientId=&locationId=`) |
| GET | `/api/device/{id}` | Get a single device |
| POST | `/api/device/{id}/approve` | Approve a device |
| POST | `/api/device/{id}/lock` | Lock device + queue LOCK command |
| POST | `/api/device/{id}/unlock` | Unlock device + queue UNLOCK command |
| POST | `/api/device/{id}/restart` | Queue RESTART command |
| POST | `/api/device/{id}/synctime` | Queue SYNCTIME command |
| POST | `/api/device/{id}/syncattendancelogs` | Queue full attendance-log sync from device |
| POST | `/api/device/{id}/clearattendance` | Queue CLEAR ATT LOG command |
| POST | `/api/device/{id}/clearusers` | Queue CLEAR DATA command |
| POST | `/api/device/bulk-assign-location` | Assign many devices to one location |

Approved devices that reconnect after being offline longer than the configured offline threshold are also automatically queued for a full `DATA QUERY ATTLOG` sync, unless the same sync command is already pending.

### 4.3 Attendance

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/attendance` | Paginated attendance (`?pageNumber=1&pageSize=50&clientId=&locationId=`) |
| GET | `/api/attendance/today` | Today's punches (`?clientId=&locationId=`) |
| GET | `/api/attendance/device/{deviceSN}` | Logs for a device (`?from=&to=`) |
| GET | `/api/attendance/report` | Attendance report for `daily`, `weekly`, or `monthly` (`?period=&clientId=&referenceDate=&locationId=`) |
| GET | `/api/attendance/report/export` | Download the report as `.xlsx` with first-in/last-out |
| POST | `/api/attendance/push` | Push attendance from a device via REST (no auth required) |

> Report aggregation and displayed/exported timestamps use the client timezone (`BioPortalUsers.TimeZoneId`) for day/week/month boundaries and first-in/last-out windows.

**Push attendance via REST** (alternative to IClock for HTTP-capable devices):

```http
POST /api/attendance/push?clientId=1
Content-Type: application/json

{
  "deviceSN": "ABC123",
  "records": [
    {
      "userCode": "EMP001",
      "punchTime": "2024-06-01T09:00:00",
      "attendanceState": 0,
      "verifyMode": 1,
      "workCode": ""
    }
  ]
}
```

### 4.4 Users

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/user` | Paginated users (`?pageNumber=&pageSize=&clientId=`) |
| GET | `/api/user/{userCode}` | Get a user (`?clientId=`) |
| POST | `/api/user` | Create or update a user (upsert) |
| DELETE | `/api/user/{userCode}` | Delete a user (`?clientId=`) |

**User upsert payload:**

```json
{
  "userCode": "EMP001",
  "name": "Jane Smith",
  "cardNumber": "0123456789",
  "privilege": 0,
  "isEnabled": true,
  "clientId": 1
}
```

### 4.5 Commands

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/command` | Enqueue a command for a device |
| GET | `/api/command/pending/{deviceSN}` | List pending commands for a device |

**Enqueue a command:**

```json
{
  "deviceSN": "ABC123",
  "commandType": "RESTART"
}
```

Supported `commandType` values: `LOCK`, `UNLOCK`, `RESTART`, `SYNCTIME`, `DATA QUERY ATTLOG`, `CLEAR ATT LOG`, `CLEAR DATA`.

### 4.6 Dashboard stats

```http
GET /api/dashboard/stats?clientId=1
Authorization: ******
```

---

## 5. Real-Time Attendance (SignalR)

The server broadcasts live attendance updates via SignalR at:

```
wss://YOUR_SERVER/hubs/attendance
```

Subscribe to the `AttendanceUpdated` event from any JavaScript client:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/attendance")
    .build();

connection.on("AttendanceUpdated", (logs) => {
    console.log("New attendance data:", logs);
});

await connection.start();
```

---

## 6. Background Jobs (Hangfire)

Hangfire runs three recurring jobs automatically:

| Job | Schedule | Description |
|---|---|---|
| `detect-offline-devices` | Every minute | Marks devices offline when heartbeats stop |
| `expire-stale-commands` | Every 5 minutes | Marks commands with 3+ retries as failed |
| `cleanup-heartbeats` | Daily at 03:00 UTC | Purges heartbeat records older than 7 days |

### Hangfire dashboard

Available at `/hangfire` — requires an authenticated admin session. Use it to inspect, retry, or delete background jobs.

---

## 7. Logging

Logs are written to:

- **Console** — visible when running interactively
- **Rolling daily files** — `logs/biometric-YYYYMMDD.log` in the application directory

Log level can be adjusted in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 8. Swagger / API Explorer

In Development mode, interactive API documentation is available at:

```
https://localhost:5001/swagger
```

Click **Authorize**, enter `****** then try any endpoint directly from the browser.

---

## 9. Production Deployment Checklist

- [ ] Update `Auth:AdminPassword` and `Auth:JwtSecret` in environment-specific configuration or secrets manager
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production` (disables auto-migration and Swagger)
- [ ] Run `dotnet ef database update` before first launch
- [ ] Point biometric devices to the publicly reachable server URL over HTTP (most devices do not support HTTPS for push)
- [ ] Place a reverse proxy (nginx / IIS / Caddy) in front of the app for HTTPS termination
- [ ] Ensure SQL Server firewall rules allow the application host
