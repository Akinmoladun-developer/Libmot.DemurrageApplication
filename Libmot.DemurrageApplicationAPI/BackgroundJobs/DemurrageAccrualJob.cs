using Libmot.DemurrageApplicationAPI.Services;

namespace Libmot.DemurrageApplicationAPI.BackgroundJobs
{
    //public class DemurrageAccrualJob
    //{
    //    private readonly IDemurrageService _demurrageService;
    //    private readonly ILogger<DemurrageAccrualJob> _logger;

    //    public DemurrageAccrualJob(
    //        IDemurrageService demurrageService,
    //        ILogger<DemurrageAccrualJob> logger)
    //    {
    //        _demurrageService = demurrageService;
    //        _logger = logger;
    //    }

    //    /// <summary>
    //    /// Entry point called by Hangfire at 23:00 UTC (= midnight WAT).
    //    /// </summary>
    //    public async Task ExecuteAsync()
    //    {
    //        _logger.LogInformation(
    //            "DemurrageAccrualJob triggered at {UtcNow}", DateTime.UtcNow);

    //        var result = await _demurrageService.RunDailyAccrualJobAsync();

    //        _logger.LogInformation(
    //            "DemurrageAccrualJob finished — " +
    //            "Shipments: {S}, Accruals: {A}, Invoices: {I}, Amount: ₦{M:N2}, Errors: {E}",
    //            result.ShipmentsProcessed,
    //            result.NewAccrualsCreated,
    //            result.InvoicesGenerated,
    //            result.TotalAmountAccrued,
    //            result.Errors.Count);
    //    }
    //}

    public class DemurrageAccrualJob
    {
        private readonly IDemurrageService _demurrageService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<DemurrageAccrualJob> _logger;

        public DemurrageAccrualJob(
            IDemurrageService demurrageService,
            IInvoiceService invoiceService,
            ILogger<DemurrageAccrualJob> logger)
        {
            _demurrageService = demurrageService;
            _invoiceService = invoiceService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation(
                "DemurrageAccrualJob triggered at {UtcNow}", DateTime.UtcNow);

            // Step 1: Post daily accruals and auto-generate Day-8 invoices
            var result = await _demurrageService.RunDailyAccrualJobAsync();

            // Step 2: Mark any past-due invoices as Overdue
            await _invoiceService.MarkOverdueAsync();

            _logger.LogInformation(
                "DemurrageAccrualJob finished — " +
                "Shipments: {S}, Accruals: {A}, Invoices: {I}, Amount: ₦{M:N2}, Errors: {E}",
                result.ShipmentsProcessed,
                result.NewAccrualsCreated,
                result.InvoicesGenerated,
                result.TotalAmountAccrued,
                result.Errors.Count);
        }

    }
}
