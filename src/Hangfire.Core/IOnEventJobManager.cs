using Hangfire.Common;

namespace Hangfire
{
    public interface IOnEventJobManager
    {
        void AddOrUpdate(string recurringJobId, Job job, ReportOnEventJobOptions options);

        void RemoveIfExists(string recurringJobId);

        void Trigger(string recurringJobId);
    }
}
