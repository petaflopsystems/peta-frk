using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PetaframeworkStd.Commons
{
    [Serializable]
    public class BusinessProcess
    {
        [JsonProperty("id")]
        public String ID { get; set; } = "0";

        [JsonProperty("name")]
        public String Name { get; set; } = "procName";

        [JsonProperty("tasks")]
        public List<ProcessTask> Tasks { get; set; } = new List<ProcessTask>();

        [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
        public List<ProcessField> Fields { get; set; } = new List<ProcessField>();

        [JsonProperty("routes", NullValueHandling = NullValueHandling.Ignore)]
        public List<Route> Routes { get; set; } = new List<Route>();

        [JsonProperty("entity")]
        public String Entity { get; set; } = "";

        public ProcessTask GetFirstTask()
        {
            var t = Tasks.Where(x => x.From.Count() == 0).FirstOrDefault();
            if (t != null)
                t.Parent = this;
            return t;
        }
    }

    [Serializable]
    public class Route
    {
        public ProcessTask From { get; set; }
        public ProcessTask To { get; set; }
        public string Formula { get; set; }
    }

    [Serializable]
    public class TaskUIElements
    {
        [JsonProperty("successMessage")]
        public String SuccessMessage { get; set; }

        [JsonProperty("saveButtonText")]
        public String SaveButtonText { get; set; }

        [JsonProperty("showClearButton")]
        public bool ShowClearButton { get; set; }

    }

    [Serializable]
    public class ProcessTask
    {
        [JsonProperty("name")]
        public String Name { get; set; }

        [JsonProperty("id")]
        public String ID { get; set; }

        [JsonProperty("to")]
        public List<ProcessTask> To { get; set; } = new List<ProcessTask>();

        [JsonProperty("from")]
        public List<ProcessTask> From { get; set; } = new List<ProcessTask>();

        [JsonProperty("profiles")]
        public List<Profile> Profiles { get; set; } = new List<Profile>();

        [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
        public List<KeyValuePair<ProcessField, String>> Fields { get; set; } = new List<KeyValuePair<ProcessField, String>>();

        [JsonProperty("type")]
        public String Type { get; set; }

        [JsonProperty("ui")]
        [JsonIgnore]
        public TaskUIElements UI { get; set; } = new TaskUIElements { SaveButtonText = "Prosseguir", ShowClearButton = false };

        [JsonProperty("parent")]
        [JsonIgnore]
        public BusinessProcess Parent { get; set; } = new BusinessProcess();
        [JsonIgnore]
        public string FillColor { get; private set; }
        [JsonIgnore]
        public string StrokeColor { get; private set; }
        [JsonIgnore]
        internal bool EndedFlag { get; private set; }

        [JsonIgnore]
        public DateTime StartDate { get; private set; } = DateTime.Now;

        [JsonProperty("script")]
        public PtfkTaskScript Script { get; set; }


        [JsonProperty("delegateTo")]
        [JsonIgnore]
        public List<string> DelegateTo { get; set; } = new List<string>();

        [JsonProperty("readBy", NullValueHandling = NullValueHandling.Ignore)]
        public List<ReadByStruct> ReadBy { get; private set; }

        //[JsonProperty("nextDelegation")]
        //[JsonIgnore]
        //public List<string> NextDelegation{ get; set; } = new List<string>();

        public bool IsFirstTask()
        {
            if (From != null && From.Count() == 0 && To.Any())
                return true;
            return false;
        }

        public bool IsEndTask()
        {
            if (From != null && From.Count > 0 && To.Count == 0)
                return true;
            return false;
        }

        public bool IsLastTask()
        {
            if (To != null && To.Count() == 0 && From.Any())
                return true;
            return false;
        }

        public bool IsServiceTask()
        {
            var v = this.Type?.Equals(nameof(ServiceTask));
            return v.HasValue && v.Value;
        }

        internal void SetColors(string hexFillColor, string hexStrokeColor)
        {
            this.FillColor = hexFillColor;
            this.StrokeColor = hexStrokeColor;
        }

        public void MarkAsEnd()
        {
            this.EndedFlag = true;
        }

        public void SetStartDate(DateTime date)
        {
            this.StartDate = date;
        }

        public void SetReadBy(List<ReadByStruct> lst)
        {
            this.ReadBy = lst;
        }
    }

    [Serializable]
    public struct ReadByStruct
    {
        public DateTime Date { get; set; }
        public string Login { get; set; }
    }

    [Serializable]
    public class ProcessField
    {
        [JsonProperty("name")]
        public String Name { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FieldType Type { get; set; }

        [JsonProperty("behaviors", NullValueHandling = NullValueHandling.Ignore)]
        public String Behaviors { get; set; }
    }

    [Serializable]
    public enum FieldType
    {
        real,
        integer,
        text,
        date,
        datetime
    }

    [Serializable]
    public class Profile
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public String Name { get; set; }
    }
}
