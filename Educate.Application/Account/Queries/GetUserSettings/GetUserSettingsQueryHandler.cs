using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Account.Queries.GetUserSettings;

public class GetUserSettingsQueryHandler : IRequestHandler<GetUserSettingsQuery, UserSettingsDto>
{
    private readonly IAppDbContext _context;

    public GetUserSettingsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSettingsDto> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (settings == null)
        {
            // If settings don't exist, return defaults but also ensure they exist for future
            // Note: Normally we'd create them during registration, but for existing users we create here
            settings = UserSettings.CreateDefault(request.UserId);
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new UserSettingsDto(
            settings.MarketingEmails,
            settings.SystemNotifications,
            settings.ExamReminders,
            settings.PublicProfile,
            settings.ShowExamResults,
            settings.Language,
            settings.Timezone
        );
    }
}
