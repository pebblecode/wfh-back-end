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
    let configureContainerBuilder (httpConfiguration, hubConfiguration) =
        let builder = ContainerBuilder()
        builder.Register(fun context ->
            WfhConfig.readFromConfigurationManager ()).AsSelf().SingleInstance() |> ignore;
        let createRepo rootFolder =
            let hub = GlobalHost.ConnectionManager.GetHubContext<WfhHub, NotifyWorkFromHome>()
            let publish eve = 
                match eve with
                | StatusChanged sc -> hub.Clients.All.Update sc
            WorkerStatusAggregateRepository.createFilesystemRepository (rootFolder, publish)
        builder.Register(fun ctx ->
            let config = ctx.Resolve<WfhConfig>()
            createRepo config.RootFolder).AsSelf() |> ignore
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
            appBuilder.RunSignalR())
            ) |> ignore
        app.UseWebApi(httpConfiguration) |> ignore

[<assembly: OwinStartup(typeof<Startup>)>]
do()