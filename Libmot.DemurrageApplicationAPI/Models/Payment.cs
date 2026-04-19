namespace Libmot.DemurrageApplication.Models;

public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public decimal AmountPaid { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public PaymentGateway Gateway { get; set; } = PaymentGateway.Manual;
    public PaymentStatus Status { get; set; } = PaymentStatus.Confirmed;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string RecordedByUserId { get; set; } = string.Empty;
    public string? GatewayResponse { get; set; }   //  JSON response from Paystack flow
    public string? Notes { get; set; }
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Card,
    Paystack,
    Flutterwave
}

public enum PaymentGateway
{
    Manual,
    Paystack,
    Flutterwave
}

public enum PaymentStatus
{
    Pending,
    Confirmed,
    Failed,
    Reversed
}