using System.Collections.Generic;
using WFHWeb.DataModels;

namespace WFHWeb.Services
{
    public interface IStatusService
    {
        void SetStatus(string dataDir, WorkingStatusData workingStatus);
        IList<WorkingStatusInfo> GetAllStatuses(string dataDir);
        void SetDefault(string dataDir, WorkingStatusData workingStatus);
    }

    public class WorkingStatusInfo
    {
        public readonly WorkingStatusData WorkingStatusData;
        public readonly bool IsDefault;

        public WorkingStatusInfo(WorkingStatusData workingStatusData, bool isDefault)
        {
            WorkingStatusData = workingStatusData;
            IsDefault = isDefault;
        }
    }
}
