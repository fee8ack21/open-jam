namespace PaymentService.Data.Entities;

public enum TransactionType
{
    Created = 0,
    Webhook = 1,
    Success = 2,
    Fail = 3,
    Cancel = 4,
}
