using System.Web.Http;

namespace WFHWeb.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using WFHWeb.DataModels;
    using WFHWeb.Models;
    using WFHWeb.Services;

    [RoutePrefix("api/statuses")]
    public class StatusesController : ApiController
    {
        private readonly string dataDir;

        public StatusesController()
        {
            this.dataDir = HttpContext.Current.Server.MapPath("~/App_Data");
        }

        [HttpPost]
        [Route("{statusType}")]
        public IHttpActionResult SetStatus([FromUri]StatusType statusType, [FromBody]UserStatus userStatus)
        {
            var workingStatusData = new WorkingStatusData
            {
                StatusType = statusType, 
                Email = userStatus.Email,
                StatusDetails = userStatus.StatusDetails
            };
            StatusService.Instance.SetStatus(this.dataDir, workingStatusData);

            return this.Ok();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetStatuses()
        {
            IList<WorkingStatusData> currentWorkingStatuses = StatusService.Instance.GetAllStatuses(this.dataDir);
            IList<WorkingStatusData> defaultWorkingStatuses = StatusService.Instance.GetAllStatuses(this.dataDir, true);

            return this.Ok(ToUserStatusInfo(currentWorkingStatuses, defaultWorkingStatuses));
        }

        private static List<UserStatusInfo> ToUserStatusInfo(IEnumerable<WorkingStatusData> currentWorkingStatuses, IEnumerable<WorkingStatusData> defaultWorkingStatuses)
        {
            return currentWorkingStatuses.Select(ws => ToUserStatusInfo(ws, defaultWorkingStatuses)).ToList();
        }

        private static UserStatusInfo ToUserStatusInfo(WorkingStatusData workingStatusData, IEnumerable<WorkingStatusData> defaultWorkingStatuses)
        {
            var defaultWorkingStatus = defaultWorkingStatuses.SingleOrDefault(ws => ws.Email == workingStatusData.Email);
            bool isDefault = defaultWorkingStatus != null && defaultWorkingStatus.StatusType == workingStatusData.StatusType;
            return new UserStatusInfo
            {
                Email = workingStatusData.Email,
                Status = new StatusInfo
                {
                    StatusType = workingStatusData.StatusType,
                    StatusDetails = workingStatusData.StatusDetails,
                    InOffice = workingStatusData.StatusType == StatusType.WorkInOffice,
                    Default = isDefault
                }
            };
        }

        [HttpPost]
        [Route("Slack")]
        public IHttpActionResult SetStatusFromSlack([FromBody] string slackData)
        {
            //Get Slack info
            //Get User Id
            //Get Status
            //Post Status
            //Return 
            return Ok(slackData);
        }
    }
}
