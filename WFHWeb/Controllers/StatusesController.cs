using System.Threading.Tasks;
using System.Web.Http;

namespace WFHWeb.Controllers
{
    using System;
    using System.Linq;
    using System.Net.Http;
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
        public async Task<IHttpActionResult> SetStatusFromSlack()
        {
            // this is simon's token :)
            var token = "xoxp-2165237499-2607133545-4790338792-f5ac10";
            var foo = await this.Request.Content.ReadAsStringAsync();
            var slackData = HttpUtility.ParseQueryString(foo);
            var userid = slackData["user_id"];

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://slack.com/api/");
            var response = await httpClient.GetAsync(string.Format("users.info?token={0}&&user={1}", token, userid));

            return Ok(response);
        }
    }
}
