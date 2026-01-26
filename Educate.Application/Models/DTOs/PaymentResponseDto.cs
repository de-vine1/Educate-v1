namespace Educate.Application.Models.DTOs;

public record PaymentVerificationResponse(
    bool Status, 
    string Message, 
    string GatewayResponse, 
    string Reference
);

public record PaymentInitializationResponse(
    bool Status, 
    string Message, 
    string AuthorizationUrl, 
    string AccessCode, 
    string Reference
);


