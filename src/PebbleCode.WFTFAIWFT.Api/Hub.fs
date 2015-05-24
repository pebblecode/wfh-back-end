namespace PebbleCode.WTFAIWFT.Api.NotificationHub

open FSharp.Interop.Dynamic
open Microsoft.AspNet.SignalR
open Microsoft.Owin
open global.Owin
open PebbleCode.WTFAIWFT.Domain
open Microsoft.Owin.Cors

type Startup () =
    member this.Configuration (app:IAppBuilder) =
        let rootFolder = @"C:\data"
//        GlobalHost.DependencyResolver.Register(typeof<WfhHub>, fun () -> (new WfhHub() :> obj)) |> ignore
        app.Map(
            "/signalr",
            (fun appBuilder ->
            appBuilder.UseCors(CorsOptions.AllowAll) |> ignore
            appBuilder.RunSignalR())
            ) |> ignore


[<assembly: OwinStartup(typeof<Startup>)>]
do()