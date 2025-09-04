namespace Educate.Infrastructure.Configurations;

public class MonnifyConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox.monnify.com";
    public string ContractCode { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}