using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetaframeworkStd.Commons
{
    public class PtfkTaskScript
    {
        [JsonIgnore]
        public ProcessTask Task { get; set; }

        [JsonProperty("waitFor")]
        public TimeSpan WaitFor { get; set; }

        [JsonProperty("thenEnd")]
        public bool ThenEnd { get; set; } = false;

        [JsonProperty("sendTo")]
        public List<Associates> SendTo { get; set; }

        [JsonProperty("externalMailSenders")]
        public List<String> ExternalMailSenders { get; set; }
    }

    public enum Associates
    {
        Owner,//Who created
        NextParent,//Who approves on the first level
        AllParents,//All approvers
        Actors//Everyone who participated in the process
    }

}
