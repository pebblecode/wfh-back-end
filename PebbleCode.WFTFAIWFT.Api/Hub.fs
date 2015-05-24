namespace PebbleCode.WTFAIWFT.Api.NotificationHub

open FSharp.Interop.Dynamic
open Microsoft.AspNet.SignalR
open Microsoft.Owin
open global.Owin

type HollerMessage =
    interface
        abstract member SomethingHappened : string -> unit
    end

type ChatHub () =
    inherit Hub<HollerMessage> ()
    member this.Send (name, message) =
        this.Clients.All.SomethingHappened(message)

type Startup () =
    member this.Configuration (app:IAppBuilder) =
        app.MapSignalR() |> ignore

[<assembly: OwinStartup(typeof<Startup>)>]
do()