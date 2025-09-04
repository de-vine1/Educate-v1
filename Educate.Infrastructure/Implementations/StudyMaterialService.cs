using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class StudyMaterialService : IStudyMaterialService
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<StudyMaterialService> _logger;

    public StudyMaterialService(
        AppDbContext context,
        ISubscriptionService subscriptionService,
        ILogger<StudyMaterialService> logger
    )
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetMaterialsBySubjectAsync(Guid subjectId, string userId)
    {
        var subject = await _context
            .Subjects.Include(s => s.Level)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

        if (subject == null)
            return Enumerable.Empty<object>();

        var hasAccess = await _subscriptionService.HasActiveSubscriptionAsync(
            userId,
            subject.Level.CourseId,
            subject.LevelId
        );

        var materials = await _context
            .StudyMaterials.Where(sm => sm.SubjectId == subjectId && sm.IsActive)
            .Select(sm => new
            {
                sm.MaterialId,
                sm.Title,
                sm.Description,
                sm.MaterialType,
                sm.FileSize,
                sm.IsDownloadable,
                sm.Version,
                sm.UpdatedAt,
                HasAccess = hasAccess,
                IsLocked = !hasAccess,
            })
            .OrderBy(sm => sm.Title)
            .ToListAsync();

        return materials;
    }

    public async Task<object?> GetMaterialAsync(Guid materialId, string userId)
    {
        var material = await _context
            .StudyMaterials.Include(sm => sm.Subject)
            .ThenInclude(s => s.Level)
            .FirstOrDefaultAsync(sm => sm.MaterialId == materialId && sm.IsActive);

        if (material == null)
            return null;

        var hasAccess = await CanAccessMaterialAsync(materialId, userId);
        if (!hasAccess)
            return null;

        return new
        {
            material.MaterialId,
            material.Title,
            material.Description,
            material.MaterialType,
            material.FilePath,
            material.FileSize,
            material.IsDownloadable,
            material.Version,
            material.UpdatedAt,
        };
    }

    public async Task<bool> CanAccessMaterialAsync(Guid materialId, string userId)
    {
        var material = await _context
            .StudyMaterials.Include(sm => sm.Subject)
            .ThenInclude(s => s.Level)
            .FirstOrDefaultAsync(sm => sm.MaterialId == materialId);

        if (material == null)
            return false;

        return await _subscriptionService.HasActiveSubscriptionAsync(
            userId,
            material.Subject.Level.CourseId,
            material.Subject.LevelId
        );
    }
}
