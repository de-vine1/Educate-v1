using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class UserSettings
{
    [Key, ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;
    
    // Email Preferences
    public bool MarketingEmails { get; set; } = true;
    public bool SystemNotifications { get; set; } = true;
    public bool ExamReminders { get; set; } = true;

    // Privacy Settings
    public bool PublicProfile { get; set; } = false;
    public bool ShowExamResults { get; set; } = false;

    // Localization
    public string Language { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";

    public User User { get; set; } = null!;

    public static UserSettings CreateDefault(string userId)
    {
        return new UserSettings
        {
            UserId = userId,
            MarketingEmails = true,
            SystemNotifications = true,
            ExamReminders = true,
            PublicProfile = false,
            ShowExamResults = false,
            Language = "en",
            Timezone = "UTC"
        };
    }

    public void Update(
        bool marketingEmails, 
        bool systemNotifications, 
        bool examReminders, 
        bool publicProfile, 
        bool showExamResults, 
        string language, 
        string timezone)
    {
        MarketingEmails = marketingEmails;
        SystemNotifications = systemNotifications;
        ExamReminders = examReminders;
        PublicProfile = publicProfile;
        ShowExamResults = showExamResults;
        Language = language;
        Timezone = timezone;
    }
}
