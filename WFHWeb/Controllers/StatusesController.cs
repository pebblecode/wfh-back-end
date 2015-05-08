using System.Web.Http;

namespace WFHWeb.Controllers
{
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
            return this.Ok(StatusService.Instance.GetAllStatuses(this.dataDir).Select(ToUserStatusInfo).ToList());
        }

        public static UserStatusInfo ToUserStatusInfo(WorkingStatusData workingStatusData)
        {
            return new UserStatusInfo
            {
                Email = workingStatusData.Email,
                Status = new StatusInfo
                {
                    StatusType = workingStatusData.StatusType,
                    StatusDetails = workingStatusData.StatusDetails,
                    InOffice = workingStatusData.StatusType == StatusType.WorkInOffice
                }
            };
        }


        [HttpPost]
        [Route("Slack")]
        public IHttpActionResult SetStatusFromSlack([FromBody] string slackData)
        {
            var foo = this.Request.Content.ReadAsStringAsync().Result;
            //Get Slack info
            //Get User Id
            //Get Status
            //Post Status
            //Return 
            return Ok(foo);
        }
    }
}
