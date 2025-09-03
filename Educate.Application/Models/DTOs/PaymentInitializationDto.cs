namespace Educate.Application.Models.DTOs;

public class PaymentInitializationRequest
{
    public Guid CourseId { get; set; }
    public Guid LevelId { get; set; }
    public string PaymentProvider { get; set; } = string.Empty; // "Paystack" or "Monnify"
    public string CallbackUrl { get; set; } = string.Empty;
}

public class PaymentInitializationResponse
{
    public bool Success { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class PaystackInitializeRequest
{
    public int Amount { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Callback_url { get; set; } = string.Empty;
}

public class PaystackInitializeResponse
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaystackData Data { get; set; } = new();
}

public class PaystackData
{
    public string Authorization_url { get; set; } = string.Empty;
    public string Access_code { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public class MonnifyInitializeRequest
{
    public decimal Amount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentDescription { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "NGN";
    public string ContractCode { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
}

public class MonnifyInitializeResponse
{
    public bool RequestSuccessful { get; set; }
    public string ResponseMessage { get; set; } = string.Empty;
    public MonnifyResponseBody ResponseBody { get; set; } = new();
}

public class MonnifyResponseBody
{
    public string TransactionReference { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}
