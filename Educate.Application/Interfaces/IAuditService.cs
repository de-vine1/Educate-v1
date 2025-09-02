namespace Educate.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string details, string ipAddress, string userAgent);
}
