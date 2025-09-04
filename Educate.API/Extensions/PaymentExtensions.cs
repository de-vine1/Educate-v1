using Educate.Application.Interfaces;
using Educate.Infrastructure.Configurations;
using Educate.Infrastructure.Implementations;

namespace Educate.API.Extensions;

public static class PaymentExtensions
{
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure Paystack
        services.Configure<PaystackConfig>(options =>
        {
            options.SecretKey = configuration["Paystack:SecretKey"] ?? string.Empty;
            options.PublicKey = configuration["Paystack:PublicKey"] ?? string.Empty;
            options.BaseUrl = configuration["Paystack:BaseUrl"] ?? "https://api.paystack.co";
            options.CallbackUrl = configuration["Paystack:CallbackUrl"] ?? string.Empty;
        });

        // Configure Monnify
        services.Configure<MonnifyConfig>(options =>
        {
            options.ApiKey = configuration["Monnify:ApiKey"] ?? string.Empty;
            options.SecretKey = configuration["Monnify:SecretKey"] ?? string.Empty;
            options.BaseUrl = configuration["Monnify:BaseUrl"] ?? "https://sandbox.monnify.com";
            options.ContractCode = configuration["Monnify:ContractCode"] ?? string.Empty;
            options.CallbackUrl = configuration["Monnify:CallbackUrl"] ?? string.Empty;
        });

        // Register HttpClient and PaymentService
        services.AddHttpClient<IPaymentService, PaymentService>();

        return services;
    }
}
