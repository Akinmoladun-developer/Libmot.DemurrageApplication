namespace Libmot.DemurrageApplicationAPI.DTOs.Demurrage
{
    public class DemurrageJobResultDto
    {
        public DateTime JobRunAt { get; set; }
        public int ShipmentsProcessed { get; set; }
        public int NewAccrualsCreated { get; set; }
        public int InvoicesGenerated { get; set; }
        public int NotificationsSent { get; set; }
        public decimal TotalAmountAccrued { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
