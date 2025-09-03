# Educate Platform API Documentation

## Overview
Complete educational platform API with authentication, course management, subscriptions, payments, and administrative features. Built with Clean Architecture principles using .NET 9 and PostgreSQL.

## Base URL
```
https://localhost:5001/api
```

## Authentication
All protected endpoints require JWT Bearer token in the Authorization header:
```
Authorization: Bearer {jwt-token}
```

---

## Authentication Endpoints

### 1. User Registration
**POST** `/auth/register`

Registers a new user account with email confirmation required.

**Request:**
```json
{
  "firstName": "John",
  "lastName": "Doe", 
  "username": "johndoe",
  "email": "john.doe@example.com",
  "password": "SecurePass123",
  "confirmPassword": "SecurePass123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Registration successful. Your email is not yet verified. Some features may be restricted until verification is complete.",
  "userId": "user-id-guid",
  "emailConfirmationRequired": true
}
```

### 2. Email Confirmation
**GET** `/auth/confirm-email?token={confirmationToken}`

Confirms user email address using token from confirmation email.

### 3. User Login
**POST** `/auth/login`

Authenticates user with email/username and password. Returns JWT and refresh token.

**Request:**
```json
{
  "emailOrUsername": "john.doe@example.com",
  "password": "SecurePass123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "expiresAt": "2024-01-02T12:00:00Z"
}
```

### 4. Refresh Token
**POST** `/auth/refresh-token`

Refreshes expired JWT token using refresh token.

### 5. Forgot Password
**POST** `/auth/forgot-password`

Initiates password reset process.

### 6. Reset Password
**POST** `/auth/reset-password`

Resets user password using JWT token from email.

### 7. Set Password (OAuth Users)
**POST** `/auth/set-password`

Allows OAuth users to set a password for standard login. Requires authentication.

### 8. Google OAuth Login
**GET** `/auth/google-login`

Initiates Google OAuth flow. Redirects to Google for authentication.

### 9. Google OAuth Callback
**GET** `/auth/google-callback`

Handles Google OAuth callback and account creation/login.

---

## Course Management Endpoints (Public)

### 1. Get All Courses
**GET** `/course`

Returns all available courses with their levels and subject counts.

**Response:**
```json
[
  {
    "courseId": "guid",
    "name": "ATS Examination",
    "description": "Associate Technician Scheme examination preparation",
    "createdAt": "2024-01-01T00:00:00Z",
    "levels": [
      {
        "levelId": "guid",
        "name": "ATS1",
        "order": 1,
        "subjectCount": 5
      }
    ]
  }
]
```

---

## Student Endpoints (Requires Authentication)

### 1. Get Student Profile
**GET** `/student/profile`

Returns authenticated student's profile information.

**Response:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "studentId": "STU001",
  "subscriptionStatus": "Active"
}
```

### 2. Get Student Subscriptions
**GET** `/student/subscriptions`

Returns student's active subscriptions.

### 3. Get Practice Materials
**GET** `/student/materials/{courseId}/{levelId}`

Returns practice materials for subscribed course/level.

**Response:**
```json
[
  {
    "id": "guid",
    "title": "Mathematics Fundamentals",
    "content": "Practice material content...",
    "isFree": false,
    "hasAccess": true
  }
]
```

### 4. Get Test Results
**GET** `/student/test-results`

Returns student's test history and results.

---

## Admin Endpoints (Requires Admin Role)

### Course Management

#### 1. Create Course
**POST** `/admin/courses`

Creates a new course.

**Request:**
```json
{
  "name": "ICAN Examination",
  "description": "Institute of Chartered Accountants examination preparation"
}
```

**Response:**
```json
{
  "courseId": "guid",
  "name": "ICAN Examination",
  "description": "Institute of Chartered Accountants examination preparation",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

#### 2. Get Course by ID
**GET** `/admin/courses/{id}`

Returns detailed course information with levels and subjects.

#### 3. Get All Courses (Admin)
**GET** `/admin/courses?page=1&pageSize=10`

Returns paginated list of all courses with full details.

#### 4. Update Course
**PUT** `/admin/courses/{id}`

Updates course information.

#### 5. Delete Course
**DELETE** `/admin/courses/{id}`

Deletes a course (with validation for dependent data).

### Level Management

#### 1. Create Level
**POST** `/admin/courses/{courseId}/levels`

Creates a new level within a course.

**Request:**
```json
{
  "courseId": "guid",
  "name": "Foundation Level",
  "order": 1
}
```

#### 2. Create Level (Direct)
**POST** `/admin/levels`

Creates a level with explicit course ID.

#### 3. Get Level by ID
**GET** `/admin/levels/{id}`

Returns level details with subjects.

#### 4. Update Level
**PUT** `/admin/levels/{id}`

Updates level information.

#### 5. Delete Level
**DELETE** `/admin/levels/{id}`

Deletes a level (with dependency validation).

### Subject Management

#### 1. Create Subject
**POST** `/admin/levels/{levelId}/subjects`

Creates a new subject within a level.

**Request:**
```json
{
  "levelId": "guid",
  "name": "Mathematics"
}
```

#### 2. Create Subject (Direct)
**POST** `/admin/subjects`

Creates a subject with explicit level ID.

#### 3. Get Subject by ID
**GET** `/admin/subjects/{id}`

Returns subject details.

#### 4. Update Subject
**PUT** `/admin/subjects/{id}`

Updates subject information.

#### 5. Delete Subject
**DELETE** `/admin/subjects/{id}`

Deletes a subject.

### Analytics

#### 1. Subscription Analytics
**GET** `/admin/analytics/subscriptions`

Returns subscription analytics by course.

**Response:**
```json
[
  {
    "courseName": "ATS Examination",
    "activeSubscriptions": 150,
    "totalRevenue": 750000
  }
]
```

#### 2. Engagement Analytics
**GET** `/admin/analytics/engagement`

Returns test engagement analytics.

**Response:**
```json
[
  {
    "courseName": "ATS Examination",
    "testsCreated": 25,
    "averageDuration": 45.5
  }
]
```

---

## Payment Endpoints

### 1. Initialize Payment
**POST** `/payment/initialize`

Initializes payment for course subscription.

**Request:**
```json
{
  "courseId": "guid",
  "levelId": "guid",
  "paymentProvider": "paystack",
  "callbackUrl": "https://educate.com/payment/success"
}
```

**Response:**
```json
{
  "success": true,
  "paymentUrl": "https://checkout.paystack.com/xyz",
  "reference": "EDU_20240101120000_ABC123",
  "message": "Payment initialized successfully"
}
```

### 2. Paystack Webhook
**POST** `/payment/paystack/webhook`

Handles Paystack payment notifications. IP and signature validation included.

### 3. Monnify Webhook
**POST** `/payment/monnify/webhook`

Handles Monnify payment notifications. IP whitelisting and signature validation included.

---

## Data Models

### User Entity
```csharp
{
  "id": "string", // IdentityUser ID
  "firstName": "string",
  "lastName": "string",
  "studentId": "string?",
  "createdAt": "datetime",
  "lastLoginAt": "datetime?",
  "emailConfirmedAt": "datetime?",
  "oAuthProvider": "string?"
}
```

### Course Hierarchy
```csharp
Course {
  "courseId": "guid",
  "name": "string",
  "description": "string",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "levels": "Level[]"
}

Level {
  "levelId": "guid",
  "courseId": "guid",
  "name": "string",
  "order": "int",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "subjects": "Subject[]"
}

Subject {
  "subjectId": "guid",
  "levelId": "guid",
  "name": "string",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Payment Entity
```csharp
Payment {
  "paymentId": "guid",
  "userId": "string",
  "courseId": "guid?",
  "levelId": "guid?",
  "amount": "decimal",
  "provider": "string", // "Paystack" | "Monnify"
  "reference": "string",
  "status": "string", // "Pending" | "Success" | "Failed"
  "createdAt": "datetime"
}
```

### Pagination Response
```csharp
PagedResult<T> {
  "items": "T[]",
  "totalCount": "int",
  "pageNumber": "int",
  "pageSize": "int",
  "totalPages": "int",
  "hasNextPage": "boolean",
  "hasPreviousPage": "boolean"
}
```

---

## Authentication & Authorization

### Roles
- **Student**: Default role for registered users
- **Admin**: Full platform management access

### Policies
- **StudentOnly**: Requires authenticated student role
- **AdminOnly**: Requires authenticated admin role

### JWT Claims
```json
{
  "sub": "user-id",
  "email": "user@example.com", 
  "role": "Student|Admin",
  "name": "Full Name",
  "exp": 1234567890
}
```

---

## Security Features

### Password Requirements
- Minimum 8 characters
- Mixed case letters, numbers, and symbols
- ASP.NET Core Identity validation

### Rate Limiting
- 100 requests per minute per IP
- Configurable per endpoint
- Failed login protection

### Data Protection
- Personal data encryption
- GDPR compliance ready
- Audit logging for all actions

### Payment Security
- Webhook signature verification
- IP whitelisting for providers
- Secure reference generation
- Idempotent payment processing

---

## Error Responses

### Standard Error Format
```json
{
  "success": false,
  "message": "Error description"
}
```

### Common HTTP Status Codes
- **200**: Success
- **201**: Created
- **204**: No Content (successful updates/deletes)
- **400**: Bad Request (validation errors)
- **401**: Unauthorized (authentication required)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found
- **409**: Conflict (duplicate data)
- **429**: Too Many Requests (rate limited)
- **500**: Internal Server Error

---

## Environment Configuration

### Required Settings
- **ConnectionString**: PostgreSQL database (port 5435)
- **JWT**: Secret key, issuer, audience
- **SendGrid**: API key for email services
- **Google OAuth**: Client ID and secret
- **Paystack**: API keys and webhook settings
- **Monnify**: API credentials and configuration

### Development Tools
- **Swagger UI**: Available at `/swagger`
- **Static Files**: OAuth test pages in `/wwwroot`
- **Serilog**: File and console logging to `/logs`

---

## Testing

### HTTP Client Testing
Use `Educate.API.http` file with REST Client extension for VS Code.

### Authentication Flow Testing
1. Register new user
2. Confirm email
3. Login and get JWT
4. Test protected endpoints
5. Test admin functionalities
6. Test payment flow
7. Test OAuth integration

### Payment Testing
- Use Paystack/Monnify test keys
- Test webhook endpoints with provider test data
- Verify subscription creation and activation

### OAuth Testing
Navigate to `/test-oauth.html` for Google authentication testing.