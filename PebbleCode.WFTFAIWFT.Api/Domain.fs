namespace PebbleCode.WTFAIWFT.Domain

open System
open System.Threading

type EmailAddress = EmailAddress of string

type WorkerId =
    {   Email : EmailAddress }

type SlackUserId = SlackUserId of string

type ChangeWorkerStatus =
    {   RequestedAt : DateTimeOffset
        NewStatus : WorkerStatus }
and StatusUpdateSource =
    | Slack of SlackUserId
    | Tribe of EmailAddress
    | Email of EmailAddress
and WorkerStatus =
    | Default of WorkingLocation
    | Daily of WorkingOrAbsent
and WorkingOrAbsent =
    | Working of WorkingLocation
    | Absent of ReasonForAbsence
and WorkingLocation =
    | WorkingInOffice
    | WorkingOutOfOffice of RemoteWorkingLocation
and RemoteWorkingLocation = RemoteWorkingLocation of string
and ReasonForAbsence =
    | Holidays
    | Sick

type WorkerStatusState =
    | Initial
    | WorkerStatus of WorkerStatus

type DomainEvent =
    | StatusChanged of StatusChanged
and StatusChanged =
    {   ChangedAt : DateTimeOffset
        WorkerId : WorkerId
        NewStatus : WorkerStatusState }

type WorkerStatusAggregate
    (   workerId : WorkerId,
        storeChanged : DomainEvent -> Async<unit>,
        publishUpdate :DomainEvent -> unit) =

    let state = ref Initial

    let apply (eve : DomainEvent) =
        let (StatusChanged changed) = eve
        state := changed.NewStatus

    member val Id = workerId with get

    member internal x.replay (events : DomainEvent list) =
        List.iter apply events

    member x.applyChange (change : ChangeWorkerStatus) = async {
        let changed =
            StatusChanged
                {   ChangedAt = DateTimeOffset.Now
                    NewStatus = WorkerStatus change.NewStatus
                    WorkerId = workerId }
        do! storeChanged changed
        apply changed
        publishUpdate changed }

open System.Reactive.Linq
open System.Collections.Concurrent

type WorkerStatusAggregateRepository
    (   storeEvent : DomainEvent -> Async<unit>,
        loadEvents : WorkerId -> DomainEvent list,
        publish : DomainEvent -> unit,
        cancellationToken : CancellationToken) =

    let aggregates = ConcurrentDictionary<WorkerId, WorkerStatusAggregate>()

    let createAggregate workerId =
        let store =
            new MailboxProcessor<AsyncReplyChannel<unit> * DomainEvent> (
                (   fun inbox ->
                        let rec loop () = async {
                            let! (chn, eve) = inbox.Receive()
                            do! storeEvent eve
                            chn.Reply() }
                        loop ()
                ), cancellationToken
            )

        let storeFunc eve = store.PostAndAsyncReply(fun rc -> (rc, eve))
        let aggregate = new WorkerStatusAggregate (workerId, storeFunc, publish)
        aggregate.replay (loadEvents workerId)
        store.Start()
        aggregate

    let getOrCreateAggregate workerId =
        aggregates.GetOrAdd(workerId, fun wid -> createAggregate wid)

    member x.Get workerId = getOrCreateAggregate workerId 
