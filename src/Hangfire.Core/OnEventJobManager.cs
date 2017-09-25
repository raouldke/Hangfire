using System;
using System.Collections.Generic;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire;
using System.Linq.Expressions;
using Aq.ExpressionJsonSerializer;
using Hangfire.Annotations;
using Newtonsoft.Json;

namespace Hangfire
{
    public class OnEventJobManager : IOnEventJobManager
    {
        private readonly JobStorage _storage;
        private readonly IBackgroundJobFactory _factory;

        public static readonly Lazy<JsonSerializerSettings> jsonSettings
            = new Lazy<JsonSerializerSettings>(() =>
            {
                var a = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
                a.Converters.Add(new ExpressionJsonConverter(System.Reflection.Assembly.GetAssembly(typeof(OnEventJobManager))));
                return a;
            });
        
        public OnEventJobManager()
            : this(JobStorage.Current)
        {
            
        }

        public OnEventJobManager([NotNull] JobStorage storage)
            : this(storage, new BackgroundJobFactory())
        {
        }

        public OnEventJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void AddOrUpdate(string recurringJobId, Job job, ReportOnEventJobOptions options)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (job == null) throw new ArgumentNullException(nameof(job));
           
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            using (var connection = _storage.GetConnection())
            {
                var recurringJob = new Dictionary<string, string>();
                var invocationData = InvocationData.Serialize(job);

                recurringJob["Job"] = JobHelper.ToJson(invocationData);

                recurringJob["SignalId"] = options.Signal;
                recurringJob["TriggerExpr"] = JsonConvert.SerializeObject(options.Predicate, jsonSettings.Value);
                recurringJob["Queue"] = options.QueueName;
                recurringJob["LastValue"] = null;
                recurringJob["Name"] = options.ReportName;

                var existingJob = connection.GetAllEntriesFromHash($"on-event-job:{recurringJobId}");
                if (existingJob == null)
                {
                    recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(DateTime.UtcNow);
                }

                using (var transaction = connection.CreateWriteTransaction())
                {
                    transaction.SetRangeInHash(
                        $"on-event-job:{recurringJobId}",
                        recurringJob);

                    transaction.AddToSet("on-event-jobs", recurringJobId);
                    transaction.Commit();
                }
            }
        }

        public void Trigger(string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            {
                var hash = connection.GetAllEntriesFromHash($"on-event-job:{recurringJobId}");
                if (hash == null)
                {
                    return;
                }

                var job = JobHelper.FromJson<InvocationData>(hash["Job"]).Deserialize();
                var state = new EnqueuedState { Reason = "Triggered using on-event job manager" };

                if (hash.ContainsKey("Queue"))
                {
                    state.Queue = hash["Queue"];
                }

                var context = new CreateContext(_storage, connection, job, state);
                context.Parameters["OnEventJobId"] = recurringJobId;
                _factory.Create(context);
            }
        }

        public void RemoveIfExists(string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveHash($"on-event-job:{recurringJobId}");
                transaction.RemoveFromSet("on-event-jobs", recurringJobId);
                transaction.Commit();
            }
        }
    }
}
