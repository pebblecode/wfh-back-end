using System.Collections.Generic;

namespace WFHWeb.App_Start
{
    using System.Web.Http;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    public class GlobalConfig
    {
        public static void ConfigJson(HttpConfiguration configuration)
        {
            configuration.Formatters.Remove(configuration.Formatters.XmlFormatter);

            var json = configuration.Formatters.JsonFormatter;
            json.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }
    }

}