using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;

namespace PetaframeworkStd.Interfaces
{
    public interface IServiceParameter
    {
        [JsonIgnore]
        List<KeyValuePair<string, object>> ToSendParametersList { get; }

        [JsonIgnore]
        FileInfo ToSendFile { get; }

        [JsonIgnore]
        AuthenticationHeaderValue Authorization { get; }

        void SetDefaultValues(IPtfkSession session, IEntity entity);

        IDictionary GetHeaderParameters();
    }
}