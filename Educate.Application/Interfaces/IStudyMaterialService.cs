namespace Educate.Application.Interfaces;

public interface IStudyMaterialService
{
    Task<IEnumerable<object>> GetMaterialsBySubjectAsync(Guid subjectId, string userId);
    Task<object?> GetMaterialAsync(Guid materialId, string userId);
    Task<bool> CanAccessMaterialAsync(Guid materialId, string userId);
}
