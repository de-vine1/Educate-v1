using System.Text;
using System.Text.Json;
using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class BulkUploadService : IBulkUploadService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<BulkUploadService> _logger;

    public BulkUploadService(
        AppDbContext context,
        IEmailService emailService,
        ILogger<BulkUploadService> logger
    )
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<object> UploadCoursesAsync(Stream fileStream, string fileName, string adminId)
    {
        var uploadLog = new BulkUploadLog
        {
            AdminId = adminId,
            UploadType = "Courses",
            FileName = fileName,
            TotalRows = 0,
            SuccessfulRows = 0,
            FailedRows = 1,
            ErrorLog = JsonSerializer.Serialize(
                new[] { "Excel processing requires EPPlus NuGet package installation" }
            ),
            Status = "Failed",
            CompletedAt = DateTime.UtcNow,
        };

        _context.BulkUploadLogs.Add(uploadLog);
        await _context.SaveChangesAsync();

        return new
        {
            Success = false,
            UploadId = uploadLog.UploadId,
            TotalRows = 0,
            SuccessfulRows = 0,
            FailedRows = 1,
            Errors = new[] { "Excel processing not implemented - requires EPPlus package" },
        };
    }

    public async Task<object> UploadStudentsAsync(
        Stream fileStream,
        string fileName,
        string adminId
    )
    {
        var uploadLog = new BulkUploadLog
        {
            AdminId = adminId,
            UploadType = "Students",
            FileName = fileName,
            TotalRows = 0,
            SuccessfulRows = 0,
            FailedRows = 1,
            ErrorLog = JsonSerializer.Serialize(
                new[] { "Excel processing requires EPPlus NuGet package installation" }
            ),
            Status = "Failed",
            CompletedAt = DateTime.UtcNow,
        };

        _context.BulkUploadLogs.Add(uploadLog);
        await _context.SaveChangesAsync();

        return new
        {
            Success = false,
            UploadId = uploadLog.UploadId,
            TotalRows = 0,
            SuccessfulRows = 0,
            FailedRows = 1,
            Errors = new[] { "Excel processing not implemented - requires EPPlus package" },
        };
    }

    public async Task<object> UploadQuestionsAsync(
        Stream fileStream,
        string fileName,
        string adminId
    )
    {
        var uploadLog = new BulkUploadLog
        {
            AdminId = adminId,
            UploadType = "Questions",
            FileName = fileName,
            TotalRows = 0,
            SuccessfulRows = 0,
            FailedRows = 1,
            ErrorLog = JsonSerializer.Serialize(
                new[] { "Excel processing requires EPPlus NuGet package installation" }
            ),
            Status = "Failed",
            CompletedAt = DateTime.UtcNow,
        };

        _context.BulkUploadLogs.Add(uploadLog);
        await _context.SaveChangesAsync();

        return new
        {
            Success = false,
            UploadId = uploadLog.UploadId,
            TotalRows = 0,
            SuccessfulRows = 0,
            FailedRows = 1,
            Errors = new[] { "Excel processing not implemented - requires EPPlus package" },
        };
    }

    public async Task<object> GetUploadHistoryAsync()
    {
        return await _context
            .BulkUploadLogs.Include(log => log.Admin)
            .Select(log => new
            {
                log.UploadId,
                log.UploadType,
                log.FileName,
                AdminName = log.Admin.FirstName + " " + log.Admin.LastName,
                log.TotalRows,
                log.SuccessfulRows,
                log.FailedRows,
                log.Status,
                log.CreatedAt,
                log.CompletedAt,
            })
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();
    }
}
