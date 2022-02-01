using Newtonsoft.Json;
using Petaframework.Interfaces;
using Petaframework.Json;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.Enums;
using static Petaframework.PtfkEnvironment;

namespace Petaframework.POCO
{
    public class OutboundData
    {
        public class Check
        {
            [JsonProperty("app")]
            public string AppVersion { get; set; }
            [JsonProperty("frk")]
            public string FrameworkVersion { get; set; }
            [JsonProperty("stt")]
            public EnvironmentStatus EnvironmentState { get; set; }
        }

        public class UserSearch
        {
            [JsonProperty("usr")]
            [JsonConverter(typeof(PtfkSessionConverter))]
            public IPtfkSession UserSession { get; set; }
        }

        public class Maintenance
        {
            [JsonProperty("maintenanceMode")]
            public bool IsInMaintenanceMode { get; set; }
        }


        public class Filter
        {
            [JsonProperty("dtv")]
            [JsonConverter(typeof(Json.PtfkDataviewConverter))]
            public List<DataView> Dataview { get; set; }

            [JsonProperty("wfs")]
            [JsonConverter(typeof(SimpleWorkerConverter))]
            public List<IPtfkWorker> Workflows { get; set; }

        }

        public class DataView
        {
            [JsonProperty("entity")]
            public string EntityName { get; set; }
            [JsonProperty("labels")]
            public Dictionary<String, Dictionary<string, string>> Labels { get; set; }
            [JsonProperty("view")]
            public List<Dictionary<String, object>> View { get; set; }
            [JsonProperty("otm")]
            public bool IsOptimized { get; set; }

            [JsonProperty("lac")]
            public DateTime LastActivity { get; set; }

        }

        public class SimpleWorker : IPtfkWorker
        {
            [JsonProperty("entity")]
            public string Entity { get; set; }
            [JsonProperty("login")]
            public string Login { get; set; }
            [JsonProperty("date")]
            public DateTime Date { get; set; }
            [JsonProperty("creation")]
            public DateTime Creation { get; set; }
            [JsonProperty("tId")]
            public string Tid { get; set; }
            [JsonProperty("task", NullValueHandling = NullValueHandling.Ignore)]
            public string Task { get; set; }
            [JsonIgnore]
            public bool? Event { get; set; }
            [JsonProperty("end")]
            public bool? End { get; set; }
            [JsonProperty("delegateTo")]
            public string DelegateTo { get; set; }
            [JsonIgnore]
            public string Type { get; set; }
            [JsonIgnore]
            public string Script { get; set; }
            [JsonProperty("id")]
            public long Id { get; set; }
            [JsonProperty("creator")]
            public string Creator { get; set; }
            [JsonProperty("uid")]
            public string UId { get; set; }
        }

        public class List
        {
            [JsonProperty("xml")]
            public String DiagramXML { get; set; }

            [JsonProperty("wfs")]
            [JsonConverter(typeof(SimpleWorkerConverter))]
            public List<IPtfkWorker> Workflows { get; set; }
        }

        public class DetailsItem
        {
            [JsonProperty("ett")]
            public string EntityName { get; set; }

            [JsonProperty("inf")]
            public ItemInfo Informations { get; set; }
            [JsonProperty("xml")]
            public String DiagramXML { get; set; }

            public class ItemInfo
            {
                [JsonProperty("msg")]
                public string MessagePattern { get; set; }

                [JsonProperty("task")]
                public string CurrentTask { get; set; }
                [JsonProperty("startDate")]
                public DateTime StartDate { get; set; }

                [JsonProperty("limitDate")]
                public DateTime LimitDate { get; set; }

                [JsonProperty("taskAvg")]
                public string TaskAverage { get; set; }

                [JsonProperty("processStart")]
                public DateTime? ProcessStart { get; set; }

                [JsonProperty("processDuration")]
                public string ProcessDuration { get; set; }

                [JsonProperty("end")]
                public bool End { get; set; }

                [JsonProperty("id")]
                public long Id { get; set; }

                [JsonProperty("creator")]
                public string Creator { get; set; }

                [JsonProperty("delegates")]
                public string Delegates { get; set; }

                [JsonProperty("lastOwner")]
                [JsonConverter(typeof(PtfkSessionConverter))]
                public IPtfkSession LastOwner { get; set; }

                [JsonProperty("creatorID")]
                public string CreatorID { get; set; }

                [JsonProperty("lastOwnerID")]
                public string LastOwnerID { get; set; }

                [JsonProperty("delegatesIDs")]
                public string DelegatesIDs { get; set; }
            }
        }

        public class Archive
        {
            [JsonProperty("success")]
            public bool Success { get; set; } = false;
        }

        public class UploaderFile
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "name")]
            public String Name { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "uuid")]
            public String Uuid { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "size")]
            public long Size { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "thumbnailUrl")]
            public String ThumbnailUrl { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "s3key")]
            public String S3Key { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "path")]
            public String Path { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "uid")]
            public String UID { get; set; }
        }

        public class Summary
        {
            [JsonProperty("ent")]
            public System.Collections.Generic.List<Petaframework.POCO.EntitySummary> EntitiesList { get; set; }
            [JsonProperty("ext")]
            public UserExtract UserExtract { get; set; }

        }
        public class UserExtract
        {
            [JsonProperty("drafts")]
            public int Drafts { get; set; }
            [JsonProperty("pending")]
            public int Pending { get; set; }
            [JsonProperty("running")]
            public int Running { get; set; }
            [JsonProperty("completed")]
            public int Completed { get; set; }
            [JsonProperty("lac")]
            public DateTime LastActivity { get; set; }
        }
    }
}
