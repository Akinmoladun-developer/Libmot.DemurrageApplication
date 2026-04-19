namespace Libmot.DemurrageApplicationAPI.DTOs.Invoice
{
    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int ShipmentId { get; set; }
        public string WaybillNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string OriginState { get; set; } = string.Empty;
        public string DestinationState { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public DateTime ArrivalDate { get; set; }
        public DateTime FreeDaysExpiry { get; set; }
        public int DemurrageDays { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public bool IsSystemGenerated { get; set; }
        public string? Notes { get; set; }
        public string? PdfPath { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = new();
        public List<InvoicePaymentDto> Payments { get; set; } = new();
    }

    public class InvoiceLineItemDto
    {
        public string TierName { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public int Days { get; set; }
        public decimal DailyRate { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class InvoicePaymentDto
    {
        public int Id { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class InvoiceListDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string WaybillNumber { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal BalanceRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }

    }
}
