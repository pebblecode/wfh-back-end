namespace PebbleCode.WFTFAIWFT.Api

open System
open System.Net
open System.Net.Http
open System.Web
open System.Web.Http
open System.Web.Routing
open PebbleCode.WTFAIWFT.Domain
open Microsoft.AspNet.SignalR
open Autofac
open Autofac.Integration.WebApi
open System.Reflection
open Newtonsoft.Json.Converters

type HttpRoute = {
    controller : string
    statusType : RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

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

    member x.Application_Start() =
        let createRepo _ =
            let hub = GlobalHost.ConnectionManager.GetHubContext<WfhHub, NotifyWorkFromHome>()
            let publish eve = 
                match eve with
                | StatusChanged sc -> hub.Clients.All.Update sc
            WorkerStatusAggregateRepository.createFilesystemRepository (@"C:\data", publish)
        let builder = new ContainerBuilder()
        let config = GlobalConfiguration.Configuration;
        builder.RegisterApiControllers(Assembly.GetExecutingAssembly()) |> ignore
        builder.RegisterWebApiFilterProvider(config);
        builder.Register(createRepo).AsSelf() |> ignore
        let container = builder.Build()
        config.DependencyResolver <-  new AutofacWebApiDependencyResolver(container)
        Global.RegisterWebApi(config)
        config.EnsureInitialized()
