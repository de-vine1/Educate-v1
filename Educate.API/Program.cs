using AspNetCoreRateLimit;
using Educate.API.Extensions;
using Educate.API.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Services.AddSerilogLogging(builder.Configuration);
builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddGoogleAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddSecurityServices();
builder.Services.AddEmailServices(builder.Configuration);

var app = builder.Build();

// Seed roles
await app.Services.SeedRolesAsync();

// Configure pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestValidationMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseIpRateLimiting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Educate API v1");
    c.RoutePrefix = "swagger";
});

app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
