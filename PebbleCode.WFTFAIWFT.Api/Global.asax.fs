namespace PebbleCode.WFTFAIWFT.Api

open System
open System.Net
open System.Net.Http
open System.Web
open System.Web.Http
open System.Web.Routing

type HomeController () =
    inherit ApiController ()
    member this.Get() =
        this.Request.CreateResponse(HttpStatusCode.OK, "Ok from web api")

type HttpRoute = {
    controller : string
    id : RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

    static member RegisterWebApi(config: HttpConfiguration) =
        // Configure routing
        config.MapHttpAttributeRoutes()
        config.Routes.MapHttpRoute(
            "DefaultApi", // Route name
            "api/{controller}/{id}", // URL with parameters
            { controller = "{controller}"; id = RouteParameter.Optional } // Parameter defaults
        ) |> ignore

        // Configure serialization
        config.Formatters.XmlFormatter.UseXmlSerializer <- true
        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

        // Additional Web API settings

    member x.Application_Start() =
        GlobalConfiguration.Configure(Action<_> Global.RegisterWebApi)