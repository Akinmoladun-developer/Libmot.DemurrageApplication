using Libmot.DemurrageApplicationAPI.Models;

namespace Libmot.DemurrageApplicationAPI
{
    public class InvoiceLineItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
    }
}
