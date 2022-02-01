using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetaframeworkStd.Interfaces
{
    public interface IEntity
    {
        long Id { get; set; }

        string ClassName { get; }

        [NotMapped]
        [JsonIgnore]
        ILogger Logger { get; }
    }
}
