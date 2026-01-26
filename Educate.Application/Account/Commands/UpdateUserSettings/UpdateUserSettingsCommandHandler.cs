using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Account.Commands.UpdateUserSettings;

public class UpdateUserSettingsCommandHandler : IRequestHandler<UpdateUserSettingsCommand, UpdateUserSettingsResult>
{
    private readonly IAppDbContext _context;

    public UpdateUserSettingsCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateUserSettingsResult> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (settings == null)
        {
            settings = UserSettings.CreateDefault(request.UserId);
            _context.UserSettings.Add(settings);
        }

        settings.Update(
            request.MarketingEmails,
            request.SystemNotifications,
            request.ExamReminders,
            request.PublicProfile,
            request.ShowExamResults,
            request.Language,
            request.Timezone
        );

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateUserSettingsResult(true, "User settings updated successfully");
    }
}
