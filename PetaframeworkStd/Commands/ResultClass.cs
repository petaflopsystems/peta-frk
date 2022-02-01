using Newtonsoft.Json;
using System;

namespace PetaframeworkStd.Commands
{
    [Serializable]
    public class ResultClass
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Duration { get; set; }
        public string Command { get; set; }
        public bool IsLocalhost { get; set; } = false;
        public string OSPlatform
        {
            get
            {
                return OS.GetCurrent();
            }
        }
        public String GenericArgs { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public String CallerUser { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime EndDate { get; set; }

        public string ID { get; set; }
    }
}
