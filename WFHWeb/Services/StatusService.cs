namespace WFHWeb.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;

    using WFHWeb.DataModels;
    using WFHWeb.Models;

    public class StatusService
    {
        private static readonly object SyncRoot = new object();
        private static volatile StatusService instance;

        private StatusService()
        {
        }

        internal static StatusService Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new StatusService();
                        }
                    }
                }

                return instance;
            }
        }

        internal void SetStatus(string dataDir, WorkingStatusData workingStatus)
        {
            lock (SyncRoot)
            {
                string dataFile = GetCurrentFile(dataDir);
                var workingStatuses = new List<WorkingStatusData>();
                if (File.Exists(dataFile))
                {
                    workingStatuses = JsonConvert.DeserializeObject<List<WorkingStatusData>>(File.ReadAllText(dataDir));
                }

                var existingStatus = workingStatuses.SingleOrDefault(ws => ws.Email == workingStatus.Email);

                if (existingStatus != null)
                {
                    existingStatus.StatusType = workingStatus.StatusType;
                }
                else
                {
                    workingStatuses.Add(workingStatus);
                }

                File.WriteAllText(dataFile, JsonConvert.SerializeObject(workingStatuses, Formatting.Indented));
            }
        }

        internal IList<WorkingStatusData> GetAllStatuses(string dataDir)
        {
            lock (SyncRoot)
            {
                string dataFile = GetCurrentFile(dataDir);
                if (File.Exists(dataFile))
                {
                    return JsonConvert.DeserializeObject<List<WorkingStatusData>>(File.ReadAllText(dataFile));
                }

                return new List<WorkingStatusData>();
            }
        }

        private static string GetCurrentFile(string dataDir)
        {
            return Path.Combine(dataDir, string.Format("{0}.json", DateTime.UtcNow.ToString("yyyy-MM-dd")));
        }
    }
}