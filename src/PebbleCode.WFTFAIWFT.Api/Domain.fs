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
with
    member x.Id =
        match x with
        | StatusChanged sc -> sc.WorkerId
and StatusChanged = 
    {   ChangedAt : DateTimeOffset
        WorkerId : WorkerId
        NewStatus : WorkerStatus }

type WorkerStatusAggregate
    (   workerId : WorkerId,
        storeChanged : DomainEvent -> Async<unit>,
        publishUpdate :DomainEvent -> unit) =

    let state = ref Initial

    let apply (eve : DomainEvent) =
        match eve with
        | StatusChanged sc ->
             state := WorkerStatus sc.NewStatus

    member val Id = workerId with get

    member internal x.replay (events : DomainEvent list) =
        List.iter apply events

    member x.applyChange (change : ChangeWorkerStatus) = async {
        let changed =
            StatusChanged
                {   ChangedAt = DateTimeOffset.Now
                    NewStatus = change.NewStatus
                    WorkerId = workerId }
        do! storeChanged changed
        apply changed
        publishUpdate changed }

open System.Reactive.Linq
open System.Collections.Concurrent

type WorkerStatusAggregateRepository
    (   storeEvent : DomainEvent -> Async<unit>,
        loadEvents : WorkerId -> DomainEvent list,
        publish : DomainEvent -> unit) =

    let aggregates = ConcurrentDictionary<WorkerId, WorkerStatusAggregate>()

    let createAggregate workerId =
        let store =
            new MailboxProcessor<AsyncReplyChannel<unit> * DomainEvent> (
                fun inbox ->
                    let rec loop () = async {
                        let! (chn, eve) = inbox.Receive()
                        do! storeEvent eve
                        chn.Reply()
                        return! loop() }
                    loop ()
            )

        let storeFunc eve = store.PostAndAsyncReply(fun rc -> (rc, eve))
        let aggregate = new WorkerStatusAggregate (workerId, storeFunc, publish)
        aggregate.replay (loadEvents workerId)
        store.Start()
        aggregate

    let getOrCreateAggregate workerId =
        aggregates.GetOrAdd(workerId, fun wid -> createAggregate wid)

    member x.Get workerId = getOrCreateAggregate workerId

[<AutoOpen>]
module Async =
    open System.Threading.Tasks

    let inline awaitPlainTask (task: Task) = 
        // rethrow exception from preceding task if it fauled
        let continuation (t : Task) : unit =
            match t.IsFaulted with
            | true -> raise t.Exception
            | arg -> ()
        task.ContinueWith continuation |> Async.AwaitTask

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WorkerStatusAggregateRepository =
    open System.IO
    open System.Text
    open System.Reactive.Disposables
    open Newtonsoft.Json

    let encoding = Encoding.UTF8

    let serializer = JsonSerializer()

    let getPath rootPath (EmailAddress email) =
        Path.Combine(rootPath, email)

    let createFilesystemRepository (rootFolderPath, publish) =
        if not (Directory.Exists(rootFolderPath)) then
            Directory.CreateDirectory(rootFolderPath) |> ignore

        let storeEvent (domainEvent : DomainEvent) = async {
            let path = getPath rootFolderPath domainEvent.Id.Email
            use f = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None)
            use wr = new StreamWriter(f, encoding)
            serializer.Serialize(wr, domainEvent)
            wr.WriteLine() }

        let loadEvents (workerId : WorkerId) =
            let path = getPath rootFolderPath workerId.Email
            use f = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None)
            if f.Length > 0L then
                use r = new StreamReader(f, encoding)
                [while not r.EndOfStream do
                    let l = r.ReadLine()
                    use sr = new StringReader(l)
                    use jr = new JsonTextReader(sr)
                    yield serializer.Deserialize<DomainEvent>(jr)]
            else List.empty

        WorkerStatusAggregateRepository(storeEvent, loadEvents, publish)


module InitializeHub =
    open System.IO
    open System.Text
    open Newtonsoft.Json

    let json = new JsonSerializer()

    let publishHistory rootFolderPath publish =
        let loadHistory (rootFolderPath) =
            let (|IsStatusChange|_|) (x:DomainEvent) =
                match x with
                | StatusChanged sc -> Some sc

            let isDefault (change : StatusChanged) =
                match change.NewStatus with
                | Default _ -> true
                | _ -> false

            let latestOrNone (changes : StatusChanged seq) =
                let latest (changes : StatusChanged seq) = changes |> Seq.maxBy (fun e -> e.ChangedAt)
                match Seq.isEmpty changes with
                | true -> None
                | false -> Some (latest changes)

            Directory.GetFiles(rootFolderPath)
            |> Seq.map (
                fun workerFilePath ->
                    use str = File.Open(workerFilePath, FileMode.Open, FileAccess.Read, FileShare.None)
                    use r = new StreamReader(str, Encoding.UTF8)
                    let events =
                        [ while not r.EndOfStream do
                                let l = r.ReadLine()
                                yield JsonConvert.DeserializeObject<DomainEvent>(l) ]
                        |> List.choose (function IsStatusChange x -> Some x | _ -> None)
                    let workerId = (events |> Seq.head).WorkerId
                    let latestDefault =
                        events
                        |> Seq.filter isDefault
                        |> latestOrNone
                    let latestDaily =
                        events
                        |> Seq.filter (not << isDefault)
                        |> latestOrNone
                    workerId, (latestDefault, latestDaily)
                )
        let chooseDefaultOrDaily (workerId,(def, daily)) =
            let currentDate = DateTimeOffset.UtcNow
            let appliesToday (date:DateTimeOffset) =
                date.DateTime = currentDate.Date
                || (date.Year = currentDate.Year
                    && date.Month = currentDate.Month 
                    && date.Day = currentDate.Day + 1 
                    && currentDate.Hour < 3)
            match (def, daily) with
            | Some d, None -> d
            | None, Some d -> d
            | Some a, Some b ->
                if appliesToday b.ChangedAt then b else a
            | None, None -> 
                {   ChangedAt = DateTimeOffset.UtcNow
                    WorkerId = workerId
                    NewStatus = Default WorkingInOffice }

        let hist = loadHistory rootFolderPath |> List.ofSeq
        hist
        |> Seq.iter (chooseDefaultOrDaily >> publish)

type NotifyWorkFromHome =
    interface
        abstract member Update : StatusChanged -> unit
        abstract member Initialize : unit -> unit
    end

open Microsoft.AspNet.SignalR.Hubs
open Microsoft.AspNet.SignalR

type WfhHub (dataPath) =
    inherit Hub<NotifyWorkFromHome> ()
    member this.Send (message) =
        this.Clients.All.Update(message)
    member this.Initialize () =
        InitializeHub.publishHistory dataPath this.Clients.All.Update
