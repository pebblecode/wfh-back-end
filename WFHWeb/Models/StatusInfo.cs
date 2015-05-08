namespace WFHWeb.Models
{
    using WFHWeb.DataModels;

    public class StatusInfo
    {
        public StatusType StatusType { get; set; }

        public string StatusDetails { get; set; }

        public bool InOffice { get; set; }

        public bool Default { get; set; }
    }
}