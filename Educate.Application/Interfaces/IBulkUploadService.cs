namespace Educate.Application.Interfaces;

public interface IBulkUploadService
{
    Task<object> UploadCoursesAsync(Stream fileStream, string fileName, string adminId);
    Task<object> UploadStudentsAsync(Stream fileStream, string fileName, string adminId);
    Task<object> UploadQuestionsAsync(Stream fileStream, string fileName, string adminId);
    Task<object> GetUploadHistoryAsync();
}
