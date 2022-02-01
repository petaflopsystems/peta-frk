using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Petaframework.Interfaces;
using Petaframework.POCO;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.POCO.OutboundData;

namespace Petaframework.Json
{
    public class SimpleWorkerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPtfkWorker) || objectType == typeof(List<IPtfkWorker>);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            List<IPtfkWorker> lst = new List<IPtfkWorker>();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.None:
                        break;
                    case JsonToken.StartObject:
                        var instance = new JsonSerializer().Deserialize<SimpleWorker>(reader);
                        lst.Add(instance);
                        break;
                    case JsonToken.StartArray:

                        break;
                    case JsonToken.StartConstructor:
                        break;
                    case JsonToken.PropertyName:
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.Raw:
                        break;
                    case JsonToken.Integer:
                        break;
                    case JsonToken.Float:
                        break;
                    case JsonToken.String:
                        break;
                    case JsonToken.Boolean:
                        break;
                    case JsonToken.Null:
                        break;
                    case JsonToken.Undefined:
                        break;
                    case JsonToken.EndObject:
                        break;
                    case JsonToken.EndArray:
                        return lst;
                        break;
                    case JsonToken.EndConstructor:
                        break;
                    case JsonToken.Date:
                        break;
                    case JsonToken.Bytes:
                        break;
                    default:
                        break;
                }
            }
            throw new Exception("invalid input!");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = value.GetType().GetProperty(nameof(IList<object>.Count)) != null ? JToken.FromObject(Tools.FromJson<List<SimpleWorker>>(Tools.ToJson(value, true))) : JToken.FromObject(Tools.FromJson<SimpleWorker>(Tools.ToJson(value, true)));

            if (t.Type != JTokenType.Object)
            {
                t.WriteTo(writer);
            }
            else
            {
                JObject o = (JObject)t;
                o.WriteTo(writer);
            }
        }
    }
}
