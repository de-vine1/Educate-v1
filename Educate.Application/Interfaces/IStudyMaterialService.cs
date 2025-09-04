namespace Educate.Application.Interfaces;

public interface IStudyMaterialService
{
    Task<IEnumerable<object>> GetMaterialsBySubjectAsync(int subjectId, string userId);
    Task<object?> GetMaterialAsync(int materialId, string userId);
    Task<bool> CanAccessMaterialAsync(int materialId, string userId);
}
