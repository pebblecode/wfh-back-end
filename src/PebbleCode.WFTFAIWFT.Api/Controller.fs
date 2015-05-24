namespace PebbleCode.WTFAIWFT.Api.Controllers

open System.Net
open System.Net.Http
open System.Web.Http
open System
open PebbleCode.WTFAIWFT.Domain
open System.Threading.Tasks
open System.Web.Http.Results

[<CLIMutable>]
type PingResponse =
    {   Message : string
        CreatedAt : DateTimeOffset }

[<CLIMutable>]
type UpdateWorkerStatus =
    {   Email : string
        StatusDetails : string }

type StatusType =
    | WorkInOffice = 0
    | WorkOutOfOffice = 1
    | Sick = 2
    | Holiday = 3


module Map =
    let workingOrAbsent (statusType:StatusType, statusDetails) =
        match statusType with
        | StatusType.WorkInOffice -> Working WorkingInOffice
        | StatusType.WorkOutOfOffice -> RemoteWorkingLocation statusDetails |> WorkingOutOfOffice |> Working
        | StatusType.Sick -> Absent ReasonForAbsence.Sick
        | StatusType.Holiday -> Absent Holidays
        | _ -> failwith "unknown status type in url"

type PingController () =
    inherit ApiController ()
    member this.Get() =
        let response = {Message = "Ok from ping controller"; CreatedAt = DateTimeOffset.Now }
        this.Request.CreateResponse(HttpStatusCode.OK, response)

type StatusesController (workerStatusAggregateRepository:WorkerStatusAggregateRepository) =
    inherit ApiController()

    member x.Post
        (   [<FromUri>]
            statusType : StatusType,
            [<FromBody>]
            updateWorkerStatus : UpdateWorkerStatus) =

        let id : WorkerId = { Email = EmailAddress updateWorkerStatus.Email}
        let newStatus : WorkerStatus =
            Daily (Map.workingOrAbsent (statusType, updateWorkerStatus.StatusDetails))
        let change : ChangeWorkerStatus =
            {   RequestedAt = DateTimeOffset.Now
                NewStatus = newStatus }
        let workerStatusAggregate = workerStatusAggregateRepository.Get(id)
        async {
            do! workerStatusAggregate.applyChange(change)
            return (OkResult(x.Request) :> IHttpActionResult)
        } |> Async.StartAsTask
