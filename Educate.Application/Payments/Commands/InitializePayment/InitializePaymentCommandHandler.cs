using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Application.Subscriptions.Queries.CheckSubscriptionEligibility;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Educate.Application.Payments.Commands.InitializePayment;

public class InitializePaymentCommandHandler : IRequestHandler<InitializePaymentCommand, PaymentInitializationResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly UserManager<User> _userManager;
    private readonly IAppDbContext _context;
    private readonly IMediator _mediator;

    public InitializePaymentCommandHandler(
        IPaymentService paymentService, 
        UserManager<User> userManager,
        IAppDbContext context,
        IMediator mediator)
    {
        _paymentService = paymentService;
        _userManager = userManager;
        _context = context;
        _mediator = mediator;
    }

    public async Task<PaymentInitializationResponse> Handle(InitializePaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Check subscription eligibility FIRST
        var eligibility = await _mediator.Send(
            new CheckSubscriptionEligibilityQuery(request.UserId, request.LevelId), 
            cancellationToken
        );

        if (!eligibility.CanSubscribe)
        {
            return new PaymentInitializationResponse(false, eligibility.Reason, "", "", "");
        }

        // 2. Get User to retrieve Email
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new PaymentInitializationResponse(false, "User not found", "", "", "");
        }

        // 3. Get Level to retrieve Price
        var level = await _context.Levels.FindAsync(new object[] { request.LevelId }, cancellationToken);
        if (level == null)
        {
             return new PaymentInitializationResponse(false, "Level not found", "", "", "");
        }

        // 4. Call Payment Service
        return await _paymentService.InitializePaymentAsync(request.UserId, request.LevelId, user.Email!, level.Price);
    }
}
