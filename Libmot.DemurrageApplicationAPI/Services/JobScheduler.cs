using Hangfire;
using Libmot.DemurrageApplicationAPI.BackgroundJobs;

namespace Libmot.DemurrageApplicationAPI.Services
{
    
    public interface IJobSchedulerService
    {
        void ScheduleRecurringJobs();
    }

    public class JobSchedulerService : IJobSchedulerService
    {
        private readonly IRecurringJobManager _recurringJobManager;

        public JobSchedulerService(IRecurringJobManager recurringJobManager)
        {
            _recurringJobManager = recurringJobManager;
        }

        public void ScheduleRecurringJobs()
        {
            _recurringJobManager.AddOrUpdate<DemurrageAccrualJob>(
                "daily-demurrage-accrual",
                job => job.ExecuteAsync(),
                "0 23 * * *", // 23:00 UTC = 00:00 WAT
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
        }
    }
}
