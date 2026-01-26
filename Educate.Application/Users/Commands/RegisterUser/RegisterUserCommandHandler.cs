using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;


namespace Educate.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterResponse>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var existingUserByUsername = await _userManager.FindByNameAsync(request.Username);
        if (existingUserByUsername != null)
            return new RegisterResponse(false, "Username already exists");

        // Check if email already exists
        var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail != null)
            return new RegisterResponse(false, "Email already exists");

        // Create new user using Factory
        var user = User.Create(request.FirstName, request.LastName, request.Email, request.Username);

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new RegisterResponse(false, $"Registration failed: {errors}");
        }

        // Generate email confirmation token
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Send confirmation email (Synchronous to ensure delivery before response)
        try
        {
            await _emailService.SendEmailConfirmationAsync(user.Email!, user.FirstName, confirmationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
        }

        return new RegisterResponse(
            true,
            "Registration successful. Your email is not yet verified. Some features may be restricted until verification is complete.",
            user.Id,
            true
        );
    }
}
