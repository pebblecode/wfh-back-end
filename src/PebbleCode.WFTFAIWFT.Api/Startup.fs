namespace PebbleCode.WTFAIWFT.Api

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
open Autofac.Integration.SignalR
open System.Reflection
open Microsoft.Owin.Host
open System.Configuration
open System.Reactive.Linq

type HttpRoute =
    {   controller : string
        statusType : RouteParameter }

type WfhConfig =
    {   RootFolder : string } 
with
    static member readFromConfigurationManager () =
        let rootFolder = ConfigurationManager.AppSettings.["datafolder"]
        {   RootFolder = rootFolder}

module Config =
    open Microsoft.AspNet.SignalR.Infrastructure

    let configureContainerBuilder (httpConfiguration, hubConfiguration:HubConfiguration) =
        let builder = ContainerBuilder()
        builder.Register(fun context ->
            WfhConfig.readFromConfigurationManager ()).AsSelf().SingleInstance() |> ignore;
        let createRepo rootFolder (connectionManager:IConnectionManager) =
            let hub = connectionManager.GetHubContext<WfhHub, NotifyWorkFromHome>()
            EventBus.hot.Subscribe(fun x -> hub.Clients.All.Update x) |> ignore
            let publish eve = EventBus.liveSubject.OnNext(eve)
            WorkerStatusAggregateRepository.createFilesystemRepository (rootFolder, publish)

        builder.Register(fun ctx ->
            let config = ctx.Resolve<WfhConfig>()
            let connectionManager = hubConfiguration.Resolver.Resolve<IConnectionManager>()
            createRepo config.RootFolder connectionManager).AsSelf().SingleInstance() |> ignore
        builder.RegisterApiControllers(Assembly.GetExecutingAssembly()) |> ignore
        builder.RegisterWebApiFilterProvider(httpConfiguration)
        builder.RegisterHubs(Assembly.GetExecutingAssembly()) |> ignore
        builder


type Startup () =

    static member RegisterWebApi(httpConfiguration: HttpConfiguration) =
        // Configure routing
        httpConfiguration.MapHttpAttributeRoutes()
        httpConfiguration.Routes.MapHttpRoute(
            "DefaultApi", // Route name
            "api/{controller}/{statusType}", // URL with parameters
            { controller = "{controller}"; statusType = RouteParameter.Optional } // Parameter defaults
        ) |> ignore

        // Configure serialization
        httpConfiguration.Formatters.XmlFormatter.UseXmlSerializer <- false
        httpConfiguration.Formatters.JsonFormatter.SerializerSettings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        httpConfiguration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(StringEnumConverter())

    member this.Configuration (app:IAppBuilder) =

        let httpConfiguration = new HttpConfiguration()
        let hubConfiguration = new HubConfiguration()
        let builder = Config.configureContainerBuilder (httpConfiguration, hubConfiguration)
        let container = builder.Build()
        httpConfiguration.DependencyResolver <-  new AutofacWebApiDependencyResolver(container)
        Startup.RegisterWebApi(httpConfiguration)
        hubConfiguration.Resolver <- new AutofacDependencyResolver(container)

        app.UseAutofacMiddleware(container) |> ignore
        app.Map(
            "/signalr",
            (fun appBuilder ->
            appBuilder.UseCors(CorsOptions.AllowAll) |> ignore
            appBuilder.RunSignalR(hubConfiguration))
            ) |> ignore
        app.UseWebApi(httpConfiguration) |> ignore

[<assembly: OwinStartup(typeof<Startup>)>]
do()