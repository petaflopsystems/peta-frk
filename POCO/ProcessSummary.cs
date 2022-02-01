using Newtonsoft.Json;
using System.Linq;

namespace Petaframework.POCO
{
    public class EntitySummary
    {
        [JsonProperty("n")]
        public string Name { get; set; }
        [JsonProperty("h")]
        public bool HasWorkflow { get; set; }
        [JsonProperty("v")]
        public long Version { get; set; }

        [JsonProperty("e")]
        public string EntityName { get; set; }

        [JsonProperty("t",NullValueHandling = NullValueHandling.Ignore)]
        public TaskSummary[] Tasks { get; set; }

        [JsonProperty("u")]
        public UserSummary[] AvailableFor { get; set; }

        [JsonIgnore]
        internal System.Threading.Tasks.Task<int> OwnerClosedTasks { get; set; }


        public bool HasPrivateProfile(long taskId) {
            var t = Tasks.Where(x => x.TaskId.Equals(taskId)).FirstOrDefault();
            foreach (var item in AvailableFor)
            {
                if (item.IsPrivate && t.ProfileId.Contains(item.ProfileId))
                    return true;
            }
            return false;
        }
    }

    public class TaskSummary
    {
        [JsonProperty("n")]
        public string Name { get; set; }
        [JsonProperty("i")]
        public long TaskId { get; set; }
        [JsonProperty("p")]
        public string[] ProfileId { get; set; }
        [JsonIgnore]
        internal System.Threading.Tasks.Task<int> OwnerCount { get; set; }

    }

    public class UserSummary
    {
        [JsonProperty("l")]
        public string Login { get; set; }
        [JsonProperty("a")]
        public bool IsAdmin { get; set; }
        [JsonProperty("p")]
        public bool IsPrivate { get; set; }
        [JsonProperty("i")]
        public string ProfileId { get; set; }
    }
}