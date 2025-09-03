namespace Educate.Application.Models.DTOs;

public class PaystackWebhookDto
{
    public string Event { get; set; } = string.Empty;
    public PaystackWebhookData Data { get; set; } = new();
}

public class PaystackWebhookData
{
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Gateway_response { get; set; } = string.Empty;
}

public class MonnifyWebhookDto
{
    public string EventType { get; set; } = string.Empty;
    public MonnifyWebhookData EventData { get; set; } = new();
}

public class MonnifyWebhookData
{
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
}

public class PaystackVerificationResponse
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaystackVerificationData Data { get; set; } = new();
}

public class PaystackVerificationData
{
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Gateway_response { get; set; } = string.Empty;
}

public class MonnifyVerificationResponse
{
    public bool RequestSuccessful { get; set; }
    public string ResponseMessage { get; set; } = string.Empty;
    public MonnifyVerificationData ResponseBody { get; set; } = new();
}

public class MonnifyVerificationData
{
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
}
