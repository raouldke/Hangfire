
using System;
using Hangfire.Common;

namespace Hangfire.Storage
{
    public class OnEventJobDto
    {
            public string Id { get; set; }
            public string TriggerExpression { get; set; }
            public string TriggerSignal { get; set; }
            public string Name { get; set; }
            public Boolean IsActive { get; set; }
            public string Queue { get; set; }
            public Job Job { get; set; }
            public JobLoadException LoadException { get; set; }
            public string LastJobId { get; set; }
            public string LastJobState { get; set; }
            public DateTime? LastExecution { get; set; }
            public DateTime? CreatedAt { get; set; }
            public bool Removed { get; set; }
            public string TimeZoneId { get; set; }
    }
}
