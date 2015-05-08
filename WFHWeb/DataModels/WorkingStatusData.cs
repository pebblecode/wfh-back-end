namespace WFHWeb.DataModels
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class WorkingStatusData
    {
        public string Email { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public StatusType StatusType { get; set; }

        public string StatusDetails { get; set; }
    }
}
