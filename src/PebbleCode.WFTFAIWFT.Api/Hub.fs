namespace PebbleCode.WTFAIWFT.Api.NotificationHub

open FSharp.Interop.Dynamic
open Microsoft.AspNet.SignalR
open Microsoft.Owin
open global.Owin
open PebbleCode.WTFAIWFT.Domain
open Microsoft.Owin.Cors
open System.Web.Http
open Newtonsoft.Json.Converters
open Autofac
open Autofac.Integration.WebApi
open System.Reflection
open Microsoft.Owin.Host

type HttpRoute = {
    controller : string
    statusType : RouteParameter }

type Startup () =

    static member RegisterWebApi(config: HttpConfiguration) =
        // Configure routing
        config.MapHttpAttributeRoutes()
        config.Routes.MapHttpRoute(
            "DefaultApi", // Route name
            "api/{controller}/{statusType}", // URL with parameters
            { controller = "{controller}"; statusType = RouteParameter.Optional } // Parameter defaults
        ) |> ignore

        // Configure serialization
        config.Formatters.XmlFormatter.UseXmlSerializer <- false
        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(StringEnumConverter())

    member this.Configuration (app:IAppBuilder) =
        let rootFolder = @"C:\data"
        GlobalHost.DependencyResolver.Register(typeof<WfhHub>, fun () -> (new WfhHub(rootFolder) :> obj)) |> ignore
        app.Map(
            "/signalr",
            (fun appBuilder ->
            appBuilder.UseCors(CorsOptions.AllowAll) |> ignore
            appBuilder.RunSignalR())
            ) |> ignore

        let createRepo _ =
            let hub = GlobalHost.ConnectionManager.GetHubContext<WfhHub, NotifyWorkFromHome>()
            let publish eve = 
                match eve with
                | StatusChanged sc -> hub.Clients.All.Update sc
            WorkerStatusAggregateRepository.createFilesystemRepository (@"C:\data", publish)
        let builder = new ContainerBuilder()
        let config = new HttpConfiguration()
        builder.RegisterApiControllers(Assembly.GetExecutingAssembly()) |> ignore
        builder.RegisterWebApiFilterProvider(config);
        builder.Register(createRepo).AsSelf() |> ignore
        let container = builder.Build()
        config.DependencyResolver <-  new AutofacWebApiDependencyResolver(container)
        Startup.RegisterWebApi(config)
        app.UseWebApi(config) |> ignore


[<assembly: OwinStartup(typeof<Startup>)>]
do()