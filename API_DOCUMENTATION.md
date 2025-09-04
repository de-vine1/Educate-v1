# Educate Platform API Documentation

## Overview

Complete educational platform API with authentication, course management, subscriptions, payments, and administrative features. Built with Clean Architecture principles using .NET 9 and PostgreSQL.

## Base URL

```
https://localhost:7111/api
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
  "password": "SecurePass123!"
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
  "password": "SecurePass123!"
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

**Request:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here"
}
```

### 5. Forgot Password

**POST** `/auth/forgot-password`

Initiates password reset process.

**Request:**

```json
{
  "email": "john.doe@example.com"
}
```

### 6. Reset Password

**POST** `/auth/reset-password`

Resets user password using JWT token from email.

**Request:**

```json
{
  "token": "reset_token_from_email",
  "newPassword": "NewSecurePass123!"
}
```

### 7. Set Password (OAuth Users)

**POST** `/auth/set-password`

Allows OAuth users to set a password for standard login. Requires authentication.

**Request:**

```json
{
  "newPassword": "NewSecurePass123!"
}
```

### 8. Google OAuth Login

**GET** `/auth/google-login`

Initiates Google OAuth flow. Redirects to Google for authentication.

### 9. Google OAuth Callback

**GET** `/auth/google-callback`

Handles Google OAuth callback and account creation/login.

### 10. Logout

**POST** `/auth/logout`

Logs out user and revokes refresh token. Requires authentication.

**Request:**

```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response:**

```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

## Course Management Endpoints (Public)

### 1. Get All Courses

**GET** `/course`

Returns all available courses with their levels and subject counts.

**Response:**

```json
[
  {
    "courseId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "ATS Examination",
    "description": "Associate Technician Scheme examination preparation",
    "createdAt": "2024-01-01T00:00:00Z",
    "levels": [
      {
        "levelId": "550e8400-e29b-41d4-a716-446655440001",
        "name": "ATS1",
        "order": 1,
        "subjectCount": 5
      }
    ]
  }
]
```

---

## Enrollment Endpoints (Requires Authentication)

### 1. Enroll in Course

**POST** `/enrollment/enroll`

Enrolls user in a course level with payment initialization.

**Request:**

```json
{
  "courseId": "550e8400-e29b-41d4-a716-446655440000",
  "levelId": "550e8400-e29b-41d4-a716-446655440001"
}
```

**Response:**

```json
{
  "success": true,
  "message": "Enrollment created. Complete payment to activate subscription.",
  "userCourseId": "guid",
  "paymentId": "guid",
  "paymentUrl": "https://paystack.com/pay/reference",
  "amount": 50000
}
```

### 2. Payment Callback

**POST** `/enrollment/payment-callback`

Handles payment status updates and activates subscriptions.

**Request:**

```json
{
  "reference": "payment_reference",
  "status": "success"
}
```

### 3. Get My Enrollments

**GET** `/enrollment/my-enrollments`

Returns user's enrollment history and status.

**Response:**

```json
[
  {
    "userCourseId": "guid",
    "courseName": "ATS Examination",
    "levelName": "ATS1",
    "status": "Active",
    "subscriptionStartDate": "2024-01-01T00:00:00Z",
    "subscriptionEndDate": "2024-07-01T00:00:00Z",
    "isActive": true,
    "daysRemaining": 180
  }
]
```

### 4. Renew Subscription

**POST** `/enrollment/renew/{userCourseId}`

Renews an existing subscription with payment.

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

## Test System Endpoints (Requires Authentication)

### 1. Start Practice Test

**POST** `/test/practice/start`

Starts a new practice test session with instant feedback.

**Request:**

```json
{
  "courseId": "550e8400-e29b-41d4-a716-446655440000",
  "levelId": "550e8400-e29b-41d4-a716-446655440001",
  "subjectId": "550e8400-e29b-41d4-a716-446655440002",
  "questionCount": 20,
  "difficultyLevel": "Medium"
}
```

**Response:**

```json
{
  "sessionId": "guid",
  "questions": [
    {
      "questionId": "guid",
      "questionText": "What is 2 + 2?",
      "options": ["2", "3", "4", "5"],
      "questionNumber": 1
    }
  ],
  "totalQuestions": 20,
  "timeLimit": null
}
```

### 2. Start Mock Exam

**POST** `/test/mock/start`

Starts a timed mock exam session.

**Request:**

```json
{
  "courseId": "550e8400-e29b-41d4-a716-446655440000",
  "levelId": "550e8400-e29b-41d4-a716-446655440001",
  "questionCount": 50,
  "timeLimitMinutes": 90
}
```

**Response:**

```json
{
  "sessionId": "guid",
  "questions": [...],
  "totalQuestions": 50,
  "timeLimit": 90,
  "startTime": "2024-01-01T10:00:00Z"
}
```

### 3. Submit Answer (Practice)

**POST** `/test/practice/answer`

Submits answer for practice test with instant feedback.

**Request:**

```json
{
  "sessionId": "guid",
  "questionId": "guid",
  "selectedAnswer": "C"
}
```

**Response:**

```json
{
  "isCorrect": true,
  "correctAnswer": "C",
  "explanation": "The correct answer is C because...",
  "nextQuestion": {
    "questionId": "guid",
    "questionText": "Next question...",
    "options": [...]
  }
}
```

### 4. Submit Answer (Mock)

**POST** `/test/mock/answer`

Submits answer for mock exam (no immediate feedback).

**Request:**

```json
{
  "sessionId": "guid",
  "questionId": "guid",
  "selectedAnswer": "B"
}
```

### 5. Complete Test

**POST** `/test/complete`

Completes test session and returns results.

**Request:**

```json
{
  "sessionId": "guid"
}
```

**Response:**

```json
{
  "score": 85.5,
  "totalQuestions": 20,
  "correctAnswers": 17,
  "timeTaken": "00:25:30",
  "performance": {
    "Mathematics": 90.0,
    "English": 80.0
  },
  "recommendations": ["Focus on English grammar"],
  "attemptId": "guid"
}
```

### 6. Get Test History

**GET** `/test/history?page=1&pageSize=10`

Returns user's test attempt history.

**Response:**

```json
{
  "items": [
    {
      "attemptId": "guid",
      "testType": "Practice",
      "courseName": "ATS Examination",
      "levelName": "ATS1",
      "score": 85.5,
      "completedAt": "2024-01-01T10:30:00Z",
      "timeTaken": "00:25:30"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10
}
```

### 7. Get Test Analytics

**GET** `/test/analytics`

Returns user's test performance analytics.

**Response:**

```json
{
  "averageScore": 82.3,
  "totalTests": 15,
  "improvementTrend": 5.2,
  "subjectPerformance": {
    "Mathematics": 88.5,
    "English": 76.1
  },
  "weeklyProgress": [
    {"week": "2024-W01", "averageScore": 78.0},
    {"week": "2024-W02", "averageScore": 82.3}
  ]
}
```

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

**Request:**

```json
{
  "name": "Updated Course Name",
  "description": "Updated description"
}
```

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
  "name": "Foundation Level",
  "order": 1
}
```

#### 2. Create Level (Direct)

**POST** `/admin/levels`

Creates a level with explicit course ID.

**Request:**

```json
{
  "courseId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Foundation Level",
  "order": 1
}
```

#### 3. Get Level by ID

**GET** `/admin/levels/{id}`

Returns level details with subjects.

#### 4. Update Level

**PUT** `/admin/levels/{id}`

Updates level information.

**Request:**

```json
{
  "name": "Updated Level Name",
  "order": 2
}
```

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
  "name": "Mathematics"
}
```

#### 2. Create Subject (Direct)

**POST** `/admin/subjects`

Creates a subject with explicit level ID.

**Request:**

```json
{
  "levelId": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Mathematics"
}
```

#### 3. Get Subject by ID

**GET** `/admin/subjects/{id}`

Returns subject details.

#### 4. Update Subject

**PUT** `/admin/subjects/{id}`

Updates subject information.

**Request:**

```json
{
  "name": "Updated Subject Name"
}
```

#### 5. Delete Subject

**DELETE** `/admin/subjects/{id}`

Deletes a subject.

### Question Bank Management

#### 1. Create Question

**POST** `/admin/questions`

Creates a new question in the question bank.

**Request:**

```json
{
  "courseId": "guid",
  "levelId": "guid",
  "subjectId": "guid",
  "questionText": "What is the capital of Nigeria?",
  "options": ["Lagos", "Abuja", "Kano", "Port Harcourt"],
  "correctAnswers": ["B"],
  "explanation": "Abuja is the capital city of Nigeria.",
  "difficultyLevel": "Easy",
  "questionType": "MultipleChoice"
}
```

#### 2. Get Questions

**GET** `/admin/questions?courseId={guid}&levelId={guid}&page=1&pageSize=20`

Returns paginated questions with filters.

#### 3. Update Question

**PUT** `/admin/questions/{id}`

Updates an existing question.

#### 4. Delete Question

**DELETE** `/admin/questions/{id}`

Deletes a question from the bank.

### Analytics Dashboard

#### 1. Dashboard Overview

**GET** `/analytics/dashboard`

Returns comprehensive dashboard metrics.

**Response:**

```json
{
  "totalUsers": 1250,
  "activeSubscriptions": 890,
  "totalRevenue": 4500000,
  "monthlyGrowth": 12.5,
  "recentActivity": [
    {
      "type": "NewUser",
      "count": 25,
      "date": "2024-01-01"
    }
  ]
}
```

#### 2. Revenue Analytics

**GET** `/analytics/revenue?period=monthly&year=2024`

Returns detailed revenue analytics.

**Response:**

```json
{
  "totalRevenue": 4500000,
  "monthlyBreakdown": [
    {"month": "January", "revenue": 450000, "subscriptions": 90},
    {"month": "February", "revenue": 520000, "subscriptions": 104}
  ],
  "topCourses": [
    {"courseName": "ATS Examination", "revenue": 2250000, "percentage": 50.0}
  ]
}
```

#### 3. User Engagement

**GET** `/analytics/engagement`

Returns user engagement metrics.

**Response:**

```json
{
  "dailyActiveUsers": 450,
  "averageSessionDuration": "00:45:30",
  "testCompletionRate": 78.5,
  "courseEngagement": [
    {
      "courseName": "ATS Examination",
      "activeUsers": 320,
      "averageTestsPerUser": 8.5
    }
  ]
}
```

#### 4. Performance Insights

**GET** `/analytics/performance`

Returns platform performance insights.

**Response:**

```json
{
  "averageTestScore": 76.8,
  "subjectPerformance": [
    {"subject": "Mathematics", "averageScore": 82.1},
    {"subject": "English", "averageScore": 71.5}
  ],
  "difficultyAnalysis": {
    "Easy": 89.2,
    "Medium": 76.8,
    "Hard": 58.3
  }
}
```

### Bulk Upload System

#### 1. Upload Questions (Excel)

**POST** `/bulk-upload/questions`

Uploads questions from Excel file.

**Request:** Multipart form with Excel file

**Response:**

```json
{
  "success": true,
  "processedCount": 150,
  "errorCount": 5,
  "errors": [
    {"row": 23, "error": "Invalid course ID"}
  ]
}
```

#### 2. Upload Users (Excel)

**POST** `/bulk-upload/users`

Bulk uploads user accounts from Excel.

#### 3. Upload Study Materials

**POST** `/bulk-upload/materials`

Uploads study materials in bulk.

#### 4. Get Upload History

**GET** `/bulk-upload/history?page=1&pageSize=10`

Returns upload operation history.

### Student Management

#### 1. Get All Students

**GET** `/student-management/students?page=1&pageSize=20&search=john`

Returns paginated list of students with search.

**Response:**

```json
{
  "items": [
    {
      "userId": "guid",
      "fullName": "John Doe",
      "email": "john@example.com",
      "studentId": "STU001",
      "registrationDate": "2024-01-01T00:00:00Z",
      "lastLoginDate": "2024-01-15T10:30:00Z",
      "subscriptionStatus": "Active",
      "totalTestsTaken": 25
    }
  ],
  "totalCount": 1250
}
```

#### 2. Get Student Details

**GET** `/student-management/students/{userId}`

Returns detailed student information.

#### 3. Update Student Subscription

**PUT** `/student-management/students/{userId}/subscription`

Manages student subscription status.

**Request:**

```json
{
  "action": "Extend",
  "courseId": "guid",
  "levelId": "guid",
  "extensionDays": 30,
  "reason": "Customer service extension"
}
```

#### 4. Reset Student Tests

**POST** `/student-management/students/{userId}/reset-tests`

Resets student's test attempts.

#### 5. Send Announcement

**POST** `/student-management/announcements`

Sends announcements to students.

**Request:**

```json
{
  "title": "System Maintenance",
  "message": "Platform will be down for maintenance...",
  "targetAudience": "AllUsers",
  "courseId": null,
  "levelId": null,
  "priority": "High"
}
```

### Admin Alerts

#### 1. Get Active Alerts

**GET** `/admin-alerts/active`

Returns current active system alerts.

**Response:**

```json
[
  {
    "alertId": "guid",
    "type": "PaymentFailure",
    "severity": "High",
    "message": "Multiple payment failures detected",
    "createdAt": "2024-01-01T10:00:00Z",
    "isResolved": false
  }
]
```

#### 2. Resolve Alert

**PUT** `/admin-alerts/{alertId}/resolve`

Marks an alert as resolved.

#### 3. Get Alert History

**GET** `/admin-alerts/history?page=1&pageSize=20`

Returns alert history with pagination.

---

## Payment Endpoints

### 1. Initialize Payment

**POST** `/payment/initialize`

Initializes payment for course subscription.

**Request:**

```json
{
  "courseId": "550e8400-e29b-41d4-a716-446655440000",
  "levelId": "550e8400-e29b-41d4-a716-446655440001",
  "amount": 5000.00,
  "provider": "Paystack"
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

## Error Responses

All endpoints return consistent error responses:

```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error messages"]
}
```

**Common HTTP Status Codes:**
- `200` - Success
- `201` - Created
- `204` - No Content
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error

---

## Data Models

### User Entity

```csharp
{
  "id": "string",
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
  "provider": "string",
  "reference": "string",
  "status": "string",
  "createdAt": "datetime"
}
```

### Question Bank Entity

```csharp
QuestionBank {
  "questionId": "guid",
  "courseId": "guid",
  "levelId": "guid",
  "subjectId": "guid",
  "questionText": "string",
  "options": "string[]",
  "correctAnswers": "string[]",
  "explanation": "string",
  "difficultyLevel": "Easy|Medium|Hard",
  "questionType": "MultipleChoice|TrueFalse|Essay",
  "createdAt": "datetime"
}
```

### Test Session Entity

```csharp
TestSession {
  "sessionId": "guid",
  "userId": "string",
  "courseId": "guid",
  "levelId": "guid",
  "testType": "Practice|Mock",
  "status": "Active|Completed|Abandoned",
  "startTime": "datetime",
  "endTime": "datetime?",
  "timeLimit": "int?",
  "currentQuestionIndex": "int",
  "answers": "object",
  "autoSaveData": "object"
}
```

### User Test Attempt Entity

```csharp
UserTestAttempt {
  "attemptId": "guid",
  "userId": "string",
  "courseId": "guid",
  "levelId": "guid",
  "testType": "Practice|Mock",
  "score": "decimal",
  "totalQuestions": "int",
  "correctAnswers": "int",
  "timeTaken": "timespan",
  "answers": "object",
  "completedAt": "datetime"
}
```

### Admin Alert Entity

```csharp
AdminAlert {
  "alertId": "guid",
  "type": "PaymentFailure|SystemError|UserActivity",
  "severity": "Low|Medium|High|Critical",
  "message": "string",
  "details": "object",
  "isResolved": "boolean",
  "resolvedAt": "datetime?",
  "resolvedBy": "string?",
  "createdAt": "datetime"
}
```

### UserCourse Entity

```csharp
UserCourse {
  "userCourseId": "guid",
  "userId": "string",
  "courseId": "guid",
  "levelId": "guid",
  "subscriptionStartDate": "datetime",
  "subscriptionEndDate": "datetime",
  "status": "string",
  "paymentId": "guid",
  "createdAt": "datetime",
  "updatedAt": "datetime"
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
```--

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
- **Test Configuration**: Question limits, time restrictions
- **Analytics Settings**: Data retention, refresh intervals
- **Upload Limits**: File size restrictions for bulk operations
- **Alert Thresholds**: System monitoring parameters

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

### Test System Testing

1. **Practice Test Flow**:
   - Start practice test session
   - Submit answers with instant feedback
   - Complete test and verify results
   - Check analytics updates

2. **Mock Exam Flow**:
   - Start timed mock exam
   - Submit answers (no immediate feedback)
   - Complete within time limit
   - Review comprehensive results

3. **Question Bank Testing**:
   - Create questions via admin panel
   - Test different difficulty levels
   - Verify question randomization
   - Test bulk upload functionality

### Admin Panel Testing

1. **Analytics Dashboard**:
   - Verify real-time metrics
   - Test date range filters
   - Check export functionality

2. **Student Management**:
   - Search and filter students
   - Test subscription management
   - Verify announcement system

3. **Bulk Upload**:
   - Test Excel file uploads
   - Verify error handling
   - Check processing logs

### Payment Testing

- Use Paystack/Monnify test keys
- Test webhook endpoints with provider test data
- Verify subscription creation and activation
- Test renewal and extension flows

### OAuth Testing

Navigate to `/test-oauth.html` for Google authentication testing.

---

## Recent Updates & Features

### Phase 6 - Practice Tests & Mock Exams
- **Practice Mode**: Self-paced tests with instant feedback
- **Mock Exam Mode**: Timed tests with feedback after completion
- **Question Bank Management**: Comprehensive question storage and retrieval
- **Test Analytics**: Performance tracking and progress insights
- **Auto-save**: Automatic session saving for interrupted tests

### Phase 7 - Admin Panel Advanced Features
- **Analytics Dashboard**: Real-time metrics and insights
- **Bulk Upload System**: Excel-based data import (requires EPPlus package)
- **Student Management**: Comprehensive student administration tools
- **Admin Alert System**: Automated system monitoring and notifications
- **Role-based Access Control**: Granular permission management

### Security Enhancements
- **Audit Trails**: Comprehensive logging of all admin actions
- **Rate Limiting**: Enhanced protection against abuse
- **Input Validation**: Robust validation across all endpoints
- **Error Handling**: Graceful error responses with detailed logging

### Performance Optimizations
- **Database Indexing**: Optimized queries for better performance
- **Caching**: Strategic caching for frequently accessed data
- **Pagination**: Efficient data retrieval for large datasets
- **Background Processing**: Async operations for heavy tasks

---

## Development Notes

### Required NuGet Packages
- **EPPlus**: For Excel file processing in bulk upload features
- **Serilog**: For comprehensive logging
- **AspNetCoreRateLimit**: For rate limiting functionality
- **QuestPDF**: For PDF generation (receipts, reports)

### Database Migrations
Ensure all migrations are applied for new entities:
- QuestionBank
- TestSession
- UserTestAttempt
- AdminAlert
- BulkUploadLog

### Configuration Requirements
- **Test Settings**: Configure question limits and time restrictions
- **Analytics Settings**: Set up data retention policies
- **Alert Thresholds**: Configure system monitoring parameters
- **File Upload Limits**: Set maximum file sizes for bulk uploads
