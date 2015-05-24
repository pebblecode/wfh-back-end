namespace PebbleCode.WTFAIWFT.Api.NotificationHub

open FSharp.Interop.Dynamic
open Microsoft.AspNet.SignalR
open Microsoft.Owin
open global.Owin
open PebbleCode.WTFAIWFT.Domain

type Startup () =
    member this.Configuration (app:IAppBuilder) =
        let rootFolder = @"C:\data"
        GlobalHost.DependencyResolver.Register(typeof<WfhHub>, fun () -> (new WfhHub(rootFolder) :> obj)) |> ignore
        app.MapSignalR() |> ignore

[<assembly: OwinStartup(typeof<Startup>)>]
do()