#r @"C:\dev\hack\wfh-back-end\packages\Rx-Core.2.2.5\lib\net45\System.Reactive.Core.dll"
#r @"C:\dev\hack\wfh-back-end\packages\Rx-Linq.2.2.5\lib\net45\System.Reactive.Linq.dll"
#r @"C:\dev\hack\wfh-back-end\packages\Rx-Interfaces.2.2.5\lib\net45\System.Reactive.Interfaces.dll"
#r @"C:\dev\hack\wfh-back-end\packages\FsPickler.1.2.2\lib\net45\FsPickler.dll"
#r @"C:\dev\hack\wfh-back-end\packages\FsPickler.Json.1.2.2\lib\net45\FsPickler.Json.dll"
#r @"C:\dev\hack\wfh-back-end\packages\Microsoft.AspNet.SignalR.Core.2.2.0\lib\net45\Microsoft.AspNet.SignalR.Core.dll"
#r @"C:\dev\hack\wfh-back-end\packages\Newtonsoft.Json.6.0.5\lib\net45\Newtonsoft.Json.dll"
#load "Domain.fs"

open System
open PebbleCode.WTFAIWFT.Domain

let folderPath = @"C:\data"

let publish e = printfn "%A" e

let repo = WorkerStatusAggregateRepository.createFilesystemRepository(folderPath, publish)

let me = { Email = EmailAddress "martin@pebblecode.com"}

let agg = repo.Get(me)

let chng = { RequestedAt = DateTimeOffset.Now; NewStatus = Default WorkingInOffice}
let chng2 = { RequestedAt = DateTimeOffset.Now; NewStatus = Default (WorkingOutOfOffice(RemoteWorkingLocation "home"))}

agg.applyChange (chng) |> Async.RunSynchronously;;
agg.applyChange (chng2) |> Async.RunSynchronously;;
