using MediatR;

namespace Educate.Application.Account.Commands.UpdateUserSettings;

public record UpdateUserSettingsCommand(
    string UserId,
    bool MarketingEmails,
    bool SystemNotifications,
    bool ExamReminders,
    bool PublicProfile,
    bool ShowExamResults,
    string Language,
    string Timezone
) : IRequest<UpdateUserSettingsResult>;

public record UpdateUserSettingsResult(
    bool Success,
    string Message
);
