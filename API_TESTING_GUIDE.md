# WorkForceGov Citizen API - Complete Testing Guide

## Quick Start - How to Test All Endpoints

### Step 1: Start the API
```powershell
cd "C:\Users\2481439\OneDrive - Cognizant\Documents\Project\WorkForceGov"
dotnet run --project WorkForceGov.Citizen.API
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
```

### Step 2: Open Swagger UI
Go to: **https://localhost:7001**

---

## Testing Without Authorize Button (Manual Header Method)

Since the Authorize button is not available, you can still test all endpoints using one of these methods:

### **METHOD 1: Use X-User-Id Header (Easiest)**

Every endpoint accepts an `X-User-Id` header instead of a token.

**Steps:**
1. In Swagger, open any endpoint
2. Scroll down to see **"Headers"** section
3. Click **"Add parameter"** (if not visible, scroll down)
4. Enter:
   - Name: `X-User-Id`
   - Value: `1` (any valid user ID from the seeded database)

**Available User IDs:**
- `1` = John Citizen
- `2` = Jane Employer  
- `3` = Officer Smith
- `4` = Admin User
- `5` = Manager Davis
- `6` = Officer Compliance
- `7` = Auditor Brown

**Example:**
```
GET /api/citizen/dashboard
Headers:
  X-User-Id: 1
```

---

### **METHOD 2: Get JWT Token, Then Copy to Each Request**

**Step A: Get Token**

1. Find: **POST /api/auth/login**
2. Click **"Try it out"**
3. Enter body:
```json
{
  "email": "john.citizen@gov.local",
  "password": "password123"
}
```
4. Click **"Execute"**
5. Copy the `token` value from response

**Step B: Add Token to Request Headers**

For each endpoint you want to test:

1. Open the endpoint (e.g., GET /api/citizen/dashboard)
2. Click **"Try it out"**
3. Scroll to **"Headers"** section
4. Click **"Add parameter"**
5. Enter:
   - Name: `Authorization`
   - Value: `Bearer {paste_your_token_here}`
6. Click **"Execute"**

---

## Complete Test Sequence

### **1. Login & Get Token** ✅
```
POST /api/auth/login
Body:
{
  "email": "john.citizen@gov.local",
  "password": "password123"
}
Headers:
(none needed for login)
```

**Response includes `token` field**

---

### **2. Test All Citizen Endpoints**

Add header to each:
```
X-User-Id: 1
```

Or header:
```
Authorization: Bearer {token_from_step_1}
```

**A. Dashboard**
```
GET /api/citizen/dashboard
```

**B. Profile - Get**
```
GET /api/citizen/profile
```

**C. Profile - Update**
```
PUT /api/citizen/profile
Body:
{
  "fullName": "John Updated",
  "dob": "1990-05-15",
  "gender": "Male",
  "address": "123 Main Street",
  "phoneNumber": "+1-555-0101"
}
```

**D. Jobs - Search**
```
GET /api/citizen/jobs/search
Query Params:
  keyword: software
  location: New York, NY
  category: Technology
```

**E. Jobs - Apply**
```
POST /api/citizen/jobs/1/apply
Body:
"I am interested in this position because..."
```

**F. Applications - Get My Applications**
```
GET /api/citizen/applications
```

**G. Applications - Withdraw**
```
PUT /api/citizen/applications/1/withdraw
```

**H. Documents - Get**
```
GET /api/citizen/documents
```

**I. Documents - Upload**
```
POST /api/citizen/documents
Params:
  documentType: Resume
Files:
  file: (select a PDF or document file)
```

**J. Benefits - Get**
```
GET /api/citizen/benefits
```

**K. Benefits - Apply**
```
POST /api/citizen/benefits/1/apply
```

**L. Trainings - Get**
```
GET /api/citizen/trainings
```

**M. Trainings - Enroll**
```
POST /api/citizen/trainings/1/enroll
```

**N. Trainings - Unenroll**
```
POST /api/citizen/trainings/1/unenroll
```

**O. Complaints - Get**
```
GET /api/citizen/complaints
```

**P. Complaints - Raise**
```
POST /api/citizen/complaints
Query Params:
  employerId: 1
Body:
"Complaint description here..."
```

**Q. Notifications - Get & Mark Read**
```
GET /api/citizen/notifications
```

---

## Test Users (Pre-seeded in Database)

| ID | Email | Password | Role |
|---|---|---|---|
| 1 | john.citizen@gov.local | password123 | Citizen |
| 2 | jane.employer@company.com | password123 | Employer |
| 3 | officer.smith@gov.local | password123 | LaborOfficer |
| 4 | admin@gov.local | password123 | SystemAdmin |
| 5 | davis.manager@gov.local | password123 | ProgramManager |
| 6 | compliance@gov.local | password123 | ComplianceOfficer |
| 7 | auditor@gov.local | password123 | GovernmentAuditor |

---

## If Still Having Issues

### **Using Postman Instead of Swagger**

1. Download & Install Postman: https://www.postman.com/downloads/
2. Create new request
3. Set URL: `https://localhost:7001/api/auth/login`
4. Set method: `POST`
5. Go to **Body** tab → Select `raw` → Select `JSON`
6. Paste:
```json
{
  "email": "john.citizen@gov.local",
  "password": "password123"
}
```
7. Click **Send** → Copy token
8. Create new request for any endpoint
9. Go to **Headers** tab
10. Add: `Authorization` = `Bearer {token}`
11. Click **Send**

### **Using cURL (Command Line)**

```powershell
# Get Token
$token = (Invoke-WebRequest -Uri "https://localhost:7001/api/auth/login" `
  -Method POST `
  -Body '{"email":"john.citizen@gov.local","password":"password123"}' `
  -ContentType "application/json").Content | ConvertFrom-Json | Select -ExpandProperty token

# Test endpoint with token
Invoke-WebRequest -Uri "https://localhost:7001/api/citizen/dashboard" `
  -Method GET `
  -Headers @{"Authorization"="Bearer $token"}
```

---

## Architecture Summary

**Authentication Methods:**
1. **JWT Bearer Token** (Primary) - From POST /api/auth/login
2. **X-User-Id Header** (Fallback) - For testing without token

**All endpoints** require one of these two authentication methods.

**Security:** 
- Tokens expire after 8 hours
- X-User-Id must be a valid integer (user ID from database)
