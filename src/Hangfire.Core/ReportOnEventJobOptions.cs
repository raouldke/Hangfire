using System;
using System.Linq.Expressions;

namespace Hangfire
{
    using Hangfire.Annotations;
    using Hangfire.States;
    public class ReportOnEventJobOptions
    {
        private string _queueName;

        public string ReportName { get; }

        public ReportOnEventJobOptions(String signal, String reportName, Expression<Func<Double, bool>> predicate)
        {
            QueueName = EnqueuedState.DefaultQueue;

            if (String.IsNullOrWhiteSpace(signal)) throw new ArgumentException("signal");

            Signal = signal;

            ReportName = reportName;

            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public String Signal { get; }


        [NotNull]
        public string QueueName
        {
            get => _queueName;
            set
            {
                // just to inforce the validation
                new EnqueuedState(value);
               
                _queueName = value;
            }
        }
        public Expression<Func<Double, Boolean>> Predicate { get; }
    }
}
