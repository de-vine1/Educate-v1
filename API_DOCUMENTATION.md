# Educate Authentication API

## Overview
Complete authentication system with registration, login, OAuth, password reset, and enterprise security features for the Educate platform.

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

### 5. Forgot Password
**POST** `/api/auth/forgot-password`

Initiates password reset process. Sends reset email if account exists.

**Request:**
```json
{
  "email": "john.doe@example.com"
}
```

**Response:**
```json
{
  "success": true,
  "message": "If this email exists, you will receive a password reset link."
}
```

### 6. Reset Password
**POST** `/api/auth/reset-password`

Resets user password using JWT token from email. Invalidates all active sessions.

**Request:**
```json
{
  "token": "jwt-reset-token-from-email",
  "newPassword": "NewSecurePass123",
  "confirmPassword": "NewSecurePass123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password has been reset successfully. All active sessions have been invalidated for security."
}
```

### 7. Set Password (OAuth Users)
**POST** `/api/auth/set-password`

Allows OAuth users to set a password for standard login. Requires JWT authentication.

**Headers:**
```
Authorization: Bearer {jwt-token}
```

**Request:**
```json
{
  "newPassword": "SecurePass123",
  "confirmPassword": "SecurePass123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password set successfully. You can now login with email and password."
}
```

### 8. Google OAuth Login
**GET** `/api/auth/google-login`

Initiates Google OAuth flow. Redirects to Google for authentication.

**Usage:** Navigate to this URL in browser to start OAuth flow.

**OAuth Registration Flow:**
1. User authenticates with Google
2. System creates account with OAuth data (no password)
3. User receives JWT token with password setup prompt
4. User sets password via `/api/auth/set-password`
5. System sends confirmation email with login link

### 9. Google OAuth Callback
**GET** `/api/auth/google-callback`

Handles Google OAuth callback. Creates account if new user, logs in existing user. Prompts password setup for OAuth-only accounts.

**Response (New OAuth User):**
```json
{
  "success": true,
  "message": "Account created and logged in successfully. Please set a password to enable standard login.",
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresAt": "2024-01-02T12:00:00Z"
}
```

**Response (Existing User):**
```json
{
  "success": true,
  "message": "Login successful",
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
- **Password Reset**: JWT-based tokens, 30-minute expiration
- **Rate Limiting**: 3 password reset requests per 15 minutes per IP
- **Session Security**: All tokens invalidated on password reset


## Error Responses

### Account Locked
```json
{
  "success": false,
  "message": "Account is locked due to multiple failed login attempts"
}
```

### Rate Limited
```json
{
  "success": false,
  "message": "Too many password reset requests. Please try again later."
}
```

### Invalid Credentials
```json
{
  "success": false,
  "message": "Invalid credentials"
}
```

### Invalid Reset Token
```json
{
  "success": false,
  "message": "Invalid or expired reset token"
}
```

### Validation Errors
```json
{
  "success": false,
  "message": "Username already exists"
}
```

## Email Notifications

The system sends automated emails for:
- **Registration**: Email confirmation with verification link
- **Welcome**: Account activation confirmation
- **Login**: Security notification with IP and device details
- **Password Reset**: Secure reset link with 30-minute expiration
- **Reset Confirmation**: Password change notification with security details
- **OAuth Password Setup**: Account completion confirmation with login link

## Testing

Use the provided HTTP file (`Educate.API.http`) to test all endpoints. For OAuth testing, use a browser to navigate to the google-login endpoint. You can also test using Swagger or Postman.

### Complete Authentication Flow Testing:
1. **Standard Registration**: Register new user → Email confirmation → Login
2. **Password Reset**: Test password reset → Reset with new password → Login
3. **Google OAuth Registration**: OAuth login → Account creation → Set password → Email confirmation
4. **Google OAuth Login**: Existing OAuth user → Standard login
5. **Rate Limiting**: Multiple reset requests → Rate limit response
6. **Account Lockout**: Multiple failed logins → Account locked response
7. **OAuth Password Setup**: Use `/set-password.html` page or API endpoint