using MediatR;

namespace Educate.Application.Account.Queries.GetUserSettings;

public record GetUserSettingsQuery(string UserId) : IRequest<UserSettingsDto>;

public record UserSettingsDto(
    bool MarketingEmails,
    bool SystemNotifications,
    bool ExamReminders,
    bool PublicProfile,
    bool ShowExamResults,
    string Language,
    string Timezone
);
