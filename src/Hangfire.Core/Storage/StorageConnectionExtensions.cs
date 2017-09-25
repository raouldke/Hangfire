// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Common;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace Hangfire.Storage
{
    public static class StorageConnectionExtensions
    {
        public static IDisposable AcquireDistributedJobLock(
            [NotNull] this IStorageConnection connection, 
            [NotNull] string jobId, 
            TimeSpan timeout)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));

            return connection.AcquireDistributedLock(
                $"job:{jobId}:state-lock",
                timeout);
        }

        public static long GetOnEventJobCount([NotNull] this JobStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            return connection.GetSetCount("on-event-jobs");
        }

        public static List<OnEventJobDto> GetOnEventJobs(
            [NotNull] this JobStorageConnection connection,
            int startingFrom,
            int endingAt)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetRangeFromSet("on-event-jobs", startingFrom, endingAt);
            return GetOnEventJobDtos(connection, ids);
        }

        public static List<OnEventJobDto> GetOnEventJobs([NotNull] this IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetAllItemsFromSet("on-event-jobs");
            return GetOnEventJobDtos(connection, ids);
        }

        public static long GetRecurringJobCount([NotNull] this JobStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            return connection.GetSetCount("recurring-jobs");
        }

        public static List<RecurringJobDto> GetRecurringJobs(
            [NotNull] this JobStorageConnection connection,
            int startingFrom,
            int endingAt)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetRangeFromSet("recurring-jobs", startingFrom, endingAt);
            return GetRecurringJobDtos(connection, ids);
        }

        public static List<RecurringJobDto> GetRecurringJobs([NotNull] this IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ids = connection.GetAllItemsFromSet("recurring-jobs");
            return GetRecurringJobDtos(connection, ids);
        }

        private static List<RecurringJobDto> GetRecurringJobDtos(IStorageConnection connection, IEnumerable<string> ids)
        {
            var result = new List<RecurringJobDto>();
            foreach (var id in ids)
            {
                var hash = connection.GetAllEntriesFromHash($"recurring-job:{id}");

                if (hash == null)
                {
                    result.Add(new RecurringJobDto { Id = id, Removed = true });
                    continue;
                }

                var dto = new RecurringJobDto
                {
                    Id = id,
                    Cron = hash["Cron"],
                    Name = hash.ContainsKey("Name") ? hash["Name"] : String.Empty
                };

                try
                {
                    var invocationData = JobHelper.FromJson<InvocationData>(hash["Job"]);
                    dto.Job = invocationData.Deserialize();
                }
                catch (JobLoadException ex)
                {
                    dto.LoadException = ex;
                }

                if (hash.ContainsKey("NextExecution"))
                {
                    dto.NextExecution = JobHelper.DeserializeDateTime(hash["NextExecution"]);
                }

                if (hash.ContainsKey("LastJobId") && !string.IsNullOrWhiteSpace(hash["LastJobId"]))
                {
                    dto.LastJobId = hash["LastJobId"];

                    var stateData = connection.GetStateData(dto.LastJobId);
                    if (stateData != null)
                    {
                        dto.LastJobState = stateData.Name;
                    }
                }
                
                if (hash.ContainsKey("Queue"))
                {
                    dto.Queue = hash["Queue"];
                }

                if (hash.ContainsKey("LastExecution"))
                {
                    dto.LastExecution = JobHelper.DeserializeDateTime(hash["LastExecution"]);
                }

                if (hash.ContainsKey("TimeZoneId"))
                {
                    dto.TimeZoneId = hash["TimeZoneId"];
                }

                if (hash.ContainsKey("CreatedAt"))
                {
                    dto.CreatedAt = JobHelper.DeserializeDateTime(hash["CreatedAt"]);
                }

                result.Add(dto);
            }

            return result;
        }

        private static List<OnEventJobDto> GetOnEventJobDtos(IStorageConnection connection, IEnumerable<string> ids)
        {
            var result = new List<OnEventJobDto>();
            foreach (var id in ids)
            {
                var hash = connection.GetAllEntriesFromHash($"on-event-job:{id}");

                if (hash == null)
                {
                    result.Add(new OnEventJobDto { Id = id, Removed = true });
                    continue;
                }

                var dto = new OnEventJobDto
                {
                    Id = id,
           
                    TriggerSignal = hash["SignalId"],

                    Name = hash.ContainsKey("Name") ? hash["Name"] : String.Empty
                };

                try
                {
                    Expression<Func<Double, Boolean>> expr = JsonConvert.DeserializeObject<Expression<Func<Double, Boolean>>>(hash["TriggerExpr"], OnEventJobManager.jsonSettings.Value);

                    dto.TriggerExpression = expr.ToString();
                }
                catch (Exception)
                {
                   
                }

                try
                {
                    var invocationData = JobHelper.FromJson<InvocationData>(hash["Job"]);
                    dto.Job = invocationData.Deserialize();
                }
                catch (JobLoadException ex)
                {
                    dto.LoadException = ex;
                }

                if (hash.ContainsKey("LastJobId") && !string.IsNullOrWhiteSpace(hash["LastJobId"]))
                {
                    dto.LastJobId = hash["LastJobId"];

                    var stateData = connection.GetStateData(dto.LastJobId);
                    if (stateData != null)
                    {
                        dto.LastJobState = stateData.Name;
                    }
                }

                if (hash.ContainsKey("IsActive") 
                    && !String.IsNullOrEmpty(hash["IsActive"]))
                {
                    Boolean.TryParse(hash["IsActive"], out bool r);
                    dto.IsActive = r;
                }

                if (hash.ContainsKey("Queue"))
                {
                    dto.Queue = hash["Queue"];
                }

                if (hash.ContainsKey("LastExecution"))
                {
                    dto.LastExecution = JobHelper.DeserializeDateTime(hash["LastExecution"]);
                }

                if (hash.ContainsKey("TimeZoneId"))
                {
                    dto.TimeZoneId = hash["TimeZoneId"];
                }

                if (hash.ContainsKey("CreatedAt"))
                {
                    dto.CreatedAt = JobHelper.DeserializeDateTime(hash["CreatedAt"]);
                }

                result.Add(dto);
            }

            return result;
        }
    }
}
