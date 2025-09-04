using Microsoft.Extensions.Configuration;

namespace Educate.Tests.Helpers;

public static class TestConfigurationHelper
{
    private static readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    public static IConfiguration GetConfiguration() => _configuration;

    public static class PaymentSecrets
    {
        public static string PaystackSecretKey => _configuration["Paystack:SecretKey"]!;
        public static string PaystackPublicKey => _configuration["Paystack:PublicKey"]!;
        public static string MonnifyApiKey => _configuration["Monnify:ApiKey"]!;
        public static string MonnifySecretKey => _configuration["Monnify:SecretKey"]!;
    }

    public static class EmailSecrets
    {
        public static string SendGridApiKey => _configuration["SendGrid:ApiKey"]!;
        public static string SmtpPassword => _configuration["Smtp:Password"]!;
    }

    public static string GetConnectionString() =>
        _configuration["ConnectionStrings:DefaultConnection"]!;

    public static class DatabaseSecrets
    {
        public static string ConnectionString =>
            _configuration["ConnectionStrings:DefaultConnection"]!;
    }
}
