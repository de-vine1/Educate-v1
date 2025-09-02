# Educate Authentication API

## Overview
Complete authentication system with registration, login, OAuth, and security features for the Educate platform.

## Authentication Endpoints

### 1. User Registration
**POST** `/api/auth/register`

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
**GET** `/api/auth/confirm-email?token={confirmationToken}`

Confirms user email address using token from confirmation email.

**Response:**
```json
{
  "message": "Email confirmed successfully"
}
```

### 3. User Login
**POST** `/api/auth/login`

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
**POST** `/api/auth/refresh-token`

Refreshes expired JWT token using refresh token.

**Request:**
```json
{
  "token": "expired-jwt-token",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "token": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-01-02T12:00:00Z"
}
```

### 5. Google OAuth Login
**GET** `/api/auth/google-login`

Initiates Google OAuth flow. Redirects to Google for authentication.

**Usage:** Navigate to this URL in browser to start OAuth flow.

### 6. Google OAuth Callback
**GET** `/api/auth/google-callback`

Handles Google OAuth callback. Creates account if new user, logs in existing user.

**Response:**
```json
{
  "success": true,
  "message": "Account created and logged in successfully",
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresAt": "2024-01-02T12:00:00Z"
}
```

## Security Features

### Password Requirements
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter  
- At least one digit

### Account Security
- **Password Hashing**: PBKDF2 with salt via ASP.NET Core Identity
- **Account Lockout**: 5 failed attempts = 15-minute lockout
- **JWT Tokens**: 24-hour expiration with secure signing
- **Refresh Tokens**: 7-day expiration, one-time use
- **Email Confirmation**: Required for full account access

```

## Error Responses

### Account Locked
```json
{
  "success": false,
  "message": "Account is locked due to multiple failed login attempts"
}
```

### Invalid Credentials
```json
{
  "success": false,
  "message": "Invalid credentials"
}
```

### Validation Errors
```json
{
  "success": false,
  "message": "Username already exists"
}
```

## Testing

Use the provided HTTP file (`Educate.API.http`) to test all endpoints. For OAuth testing, use a browser to navigate to the google-login endpoint. You can also test using Swagger or Postman