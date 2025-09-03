# AI Coding Instructions for Educate Platform

## Architecture Overview

This is a **Clean Architecture** .NET 9 educational platform with four layers:
- **Educate.API**: Web API controllers, middleware, extensions, configuration
- **Educate.Application**: Application services, DTOs, interfaces (business logic contracts)
- **Educate.Domain**: Domain entities, value objects, domain exceptions
- **Educate.Infrastructure**: Data access, external services, implementations

**Key Data Flow**: Controllers → Application Interfaces → Infrastructure Implementations → Database (PostgreSQL)

## Core Domain Model

The platform centers around a **Course → Level → Subject → Test** hierarchy:
- **User** (extends IdentityUser): Student accounts with subscription management
- **Course**: Top-level educational programs (e.g., "ATS Examination", "ICAN Examination")
- **Level**: Course subdivisions (e.g., ATS1, ATS2, Foundation, Skills)  
- **Subject**: Specific topics within levels
- **Subscription**: User access management with expiration tracking
- **Payment**: Paystack/Monnify integration for subscription billing

## Your Established Coding Patterns

### Controller Standards
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")] // When role-based
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    // Always validate ModelState first
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Use strongly-typed DTOs for responses
    return Ok(new LoginResponseDto 
    { 
        Success = true, 
        Message = "Login successful",
        Token = token 
    });
}
```

### Your Error Handling Pattern
```csharp
// Consistent error response structure
return BadRequest(new RegisterResponseDto 
{ 
    Success = false, 
    Message = "Username already exists" 
});

// Always use null-coalescing for metadata
var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
```

### Your Service Implementation Pattern
```csharp
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;

    // Constructor injection with all dependencies
    public PaymentService(AppDbContext context, IEncryptionService encryptionService, /*...*/)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    // Switch expressions for provider selection
    return request.PaymentProvider.ToLower() switch
    {
        "paystack" => await InitializePaystackPayment(/*...*/),
        "monnify" => await InitializeMonnifyPayment(/*...*/),
        _ => new PaymentInitializationResponse { Success = false, Message = "Invalid provider" }
    };
}
```

### Your EF Core Query Patterns
```csharp
// Always use Include for navigation properties
var course = await _context.Courses
    .Include(c => c.Levels)
    .ThenInclude(l => l.Subjects)
    .FirstOrDefaultAsync(c => c.CourseId == id);

// Anonymous projections for API responses
var courses = await _context.Courses
    .Select(c => new
    {
        c.CourseId,
        c.Name,
        c.Description,
        LevelCount = c.Levels.Count()
    })
    .ToListAsync();
```

### Your Audit Logging Standard
```csharp
// Manual audit logging in controllers
var userId = User.FindFirst("sub")?.Value ?? "Unknown";
var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
await _auditService.LogAsync(userId, "CREATE_COURSE", $"Created course: {course.Name}", ipAddress, userAgent);
```

### Your Validation Patterns
```csharp
// Database uniqueness checks before operations
if (await _context.Courses.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower()))
    return BadRequest("A course with this name already exists.");

// Identity operations with error aggregation
if (!result.Succeeded)
{
    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
    return BadRequest(new RegisterResponseDto 
    { 
        Success = false, 
        Message = $"Registration failed: {errors}" 
    });
}
```

## Project-Specific Patterns

### Extension-Based Service Registration
Services are registered via extension methods in `Educate.API/Extensions/`:
```csharp
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
```
Each extension encapsulates complex service setup (e.g., `DatabaseExtensions.cs` configures EF Core with PostgreSQL).

### Middleware Pipeline Order (Critical)
```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestValidationMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseIpRateLimiting();
// ... followed by standard ASP.NET middleware
```

### Service Layer Pattern
Controllers inject application interfaces, implementations live in Infrastructure:
- Interface: `Educate.Application.Interfaces.ISubscriptionService`
- Implementation: `Educate.Infrastructure.Implementations.SubscriptionService`

### Background Services
`SubscriptionBackgroundService` runs scheduled tasks for subscription expiry checks and notifications.

## Authentication & Security

- **Identity Framework**: Custom `User` entity extending `IdentityUser` 
- **JWT + Refresh Tokens**: Dual-token authentication system
- **OAuth**: Google authentication integration via `GoogleAuthExtensions`
- **Rate Limiting**: AspNetCoreRateLimit with IP-based throttling
- **Data Protection**: Personal data encryption using ASP.NET Core Data Protection
- **Audit Logging**: All requests logged via `AuditMiddleware`

## Configuration Management

Critical settings in `appsettings.json`:
- **ConnectionStrings**: PostgreSQL database (port 5435)
- **SendGrid**: Email service configuration
- **Paystack/Monnify**: Payment provider settings
- **JWT**: Token signing and validation
- **Security**: Encryption keys and audit retention

User secrets for sensitive data (use `dotnet user-secrets` commands).

## Development Workflows

### Build & Run
```bash
dotnet build                    # Build entire solution
dotnet run --project Educate.API   # Start API server
```

### Database Operations
```bash
dotnet ef migrations add <name> --project Educate.Infrastructure --startup-project Educate.API
dotnet ef database update --project Educate.Infrastructure --startup-project Educate.API
```

### Testing API Endpoints
Use `Educate.API.http` file with REST Client extension, or Swagger UI at `/swagger`.

## Integration Points

- **Email**: SendGrid via `IEmailService` (registration, password reset, notifications)
- **Payments**: Dual provider support (Paystack + Monnify) via `IPaymentService`
- **Logging**: Serilog with file and console sinks to `logs/` directory
- **Background Processing**: Subscription expiry notifications and status updates

## Key Conventions

- **GUID Primary Keys**: All entities use `Guid` IDs (e.g., `CourseId`, `UserId`)
- **UTC Timestamps**: All DateTime fields stored/calculated in UTC
- **Async/Await**: All database operations are asynchronous
- **Include Navigation Properties**: EF queries explicitly include related data via `.Include()`
- **Structured Logging**: Use template parameters: `_logger.LogInformation("User {UserId} subscribed to {CourseId}", userId, courseId)`

## Common Anti-Patterns to Avoid

- Don't inject `AppDbContext` directly in controllers - use application services
- Don't hardcode email templates - use the template system in `EmailService`
- Don't bypass the middleware pipeline order
- Don't forget to configure CORS for new endpoints requiring frontend access

## Essential Class Reference

### Core Entities (Educate.Domain.Entities)
```csharp
// User extends IdentityUser
public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? StudentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }
    public string? OAuthProvider { get; set; }
    // Navigation: Subscriptions, Payments, TestResults
}

// Course hierarchy: Course → Level → Subject
public class Course
{
    public Guid CourseId { get; set; }  // Primary Key
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public virtual ICollection<Level> Levels { get; set; }
}

public class Level 
{
    public Guid LevelId { get; set; }   // Primary Key
    public Guid CourseId { get; set; }  // Foreign Key
    public string Name { get; set; }
    public int Order { get; set; }      // Sequence within course
    public virtual Course Course { get; set; }
    public virtual ICollection<Subject> Subjects { get; set; }
}

public class Subject
{
    public Guid SubjectId { get; set; } // Primary Key
    public Guid LevelId { get; set; }   // Foreign Key
    public string Name { get; set; }
    public virtual Level Level { get; set; }
}
```

### Key DTOs (Educate.Application.Models.DTOs)
```csharp
// Authentication DTOs
public class RegisterDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}

public class LoginDto
{
    public string EmailOrUsername { get; set; }  // Accepts both
    public string Password { get; set; }
}

public class LoginResponseDto 
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// Course Management DTOs
public class CreateCourseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class CreateLevelDto
{
    public Guid CourseId { get; set; }
    public string Name { get; set; }
    public int Order { get; set; }
}

public class CreateSubjectDto
{
    public Guid LevelId { get; set; }
    public string Name { get; set; }
}

// Pagination
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; }
    public bool HasNextPage { get; }
    public bool HasPreviousPage { get; }
}
```

### Core Interfaces (Educate.Application.Interfaces)
```csharp
public interface IJwtService
{
    Task<string> GenerateTokenAsync(User user);
    Task<string> GenerateRefreshTokenAsync(User user);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    string GeneratePasswordResetToken(User user);
    bool ValidatePasswordResetToken(string token, out string userId);
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
    Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken);
    Task SendLoginNotificationAsync(string toEmail, string userName, string ipAddress, string userAgent);
}

public interface IAuditService
{
    Task LogAsync(string userId, string action, string details, string ipAddress, string userAgent);
}

public interface IPaymentService
{
    Task<PaymentInitializationResponse> InitializePaymentAsync(string userId, PaymentInitializationRequest request);
    Task<bool> ProcessPaystackWebhookAsync(string signature, string payload);
    Task<bool> ProcessMonnifyWebhookAsync(string signature, string payload);
}

public interface ISubscriptionService
{
    Task CheckExpiredSubscriptionsAsync();
    Task NotifyExpiringSubscriptionsAsync();
}
```

### Controller Route Patterns
```csharp
// Authentication routes
[HttpPost("register")]              // POST /api/auth/register
[HttpPost("login")]                 // POST /api/auth/login
[HttpGet("confirm-email")]          // GET /api/auth/confirm-email?token=
[HttpPost("refresh-token")]         // POST /api/auth/refresh-token
[HttpPost("forgot-password")]       // POST /api/auth/forgot-password
[HttpPost("reset-password")]        // POST /api/auth/reset-password

// Admin routes (require AdminOnly policy)
[HttpPost("courses")]               // POST /api/admin/courses
[HttpGet("courses/{id}")]           // GET /api/admin/courses/{guid}
[HttpPut("courses/{id}")]           // PUT /api/admin/courses/{guid}
[HttpDelete("courses/{id}")]        // DELETE /api/admin/courses/{guid}

// Nested resource routes
[HttpPost("courses/{courseId}/levels")]     // POST /api/admin/courses/{guid}/levels
[HttpPost("levels/{levelId}/subjects")]     // POST /api/admin/levels/{guid}/subjects

// Analytics routes
[HttpGet("analytics/subscriptions")]        // GET /api/admin/analytics/subscriptions
[HttpGet("analytics/engagement")]           // GET /api/admin/analytics/engagement
```

### Database Context Usage
```csharp
// Always inject AppDbContext in services, not controllers
private readonly AppDbContext _context;

// Standard query patterns with Include
var course = await _context.Courses
    .Include(c => c.Levels)
    .ThenInclude(l => l.Subjects)
    .FirstOrDefaultAsync(c => c.CourseId == id);

// Case-insensitive uniqueness checks
if (await _context.Courses.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower()))
    return BadRequest("A course with this name already exists.");

// Pagination with Skip/Take
var courses = await _context.Courses
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```
