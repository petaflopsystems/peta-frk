using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Petaframework.Interfaces;
using Petaframework.POCO;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.POCO.OutboundData;

namespace Petaframework.Json
{
    public class PtfkDataviewConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<DataView>);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            List<DataView> lst = new List<DataView>();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.None:
                        break;
                    case JsonToken.StartObject:
                        var instance = new JsonSerializer().Deserialize<POCO.OutboundData.DataView>(reader);
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

        public static Dictionary<string, string> GetJsonPropertyNames<T>()
        {
            Dictionary<string, string> _dict = new Dictionary<string, string>();

            PropertyInfo[] props = typeof(T).GetProperties();
            foreach (PropertyInfo prop in props)
            {
                object[] attrs = prop.GetCustomAttributes(true);
                foreach (object attr in attrs)
                {
                    JsonPropertyAttribute authAttr = attr as JsonPropertyAttribute;
                    if (authAttr != null)
                    {
                        string propName = prop.Name;
                        string auth = authAttr.PropertyName;

                        _dict.Add(propName, auth);
                    }
                }
            }

            return _dict;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            var obj = value as List<POCO.OutboundData.DataView>;

            var props = GetJsonPropertyNames<POCO.OutboundData.DataView>();
            var labels = props[nameof(POCO.OutboundData.DataView.Labels)];
            var view = props[nameof(POCO.OutboundData.DataView.View)];
            var otm = props[nameof(POCO.OutboundData.DataView.IsOptimized)];

            var json = Tools.ToJson(value, true);
            Dictionary<String, KeyValuePair<String, object>> allLabels = new();
            var jArr = JArray.Parse(json);

            int idx = 1;
            int idxList = 0;
            Dictionary<string, object> dictionary = new();
            Dictionary<String, Dictionary<String, object>> tempLabels = new();
            foreach (JObject result in jArr)
            {
                if (!obj[idxList++].IsOptimized)
                {
                    idx = 1;
                    foreach (var item in result[labels])
                    {
                        //object aux = item;
                        var lang = ((JProperty)item);
                        foreach (JProperty jo in lang.Value)
                        {
                            dictionary.Add(jo.Name.StartsWith(Constants.OutboundData.TaskPrefixLabel) || jo.Name.ToLower().Equals("id") ? jo.Name : (idx++).ToString(), jo.Value);
                            allLabels.Add(jo.Name, dictionary.Last());
                        }
                        tempLabels.Add(lang.Name, dictionary);
                        result[labels][lang.Name] = JToken.FromObject(dictionary);
                        dictionary = new Dictionary<string, object>();
                    }
                    List<Dictionary<string, object>> lst = new();
                    foreach (var item in result[view])
                    {
                        dictionary = new();
                        foreach (JProperty jo in item)
                        {
                            if (jo.Name.StartsWith(Constants.OutboundData.TaskPermissionLabel))
                                dictionary.Add(jo.Name, jo.Value.ToString().ToLower());
                            else
                                dictionary.Add(allLabels.Where(x => x.Key.Equals(jo.Name)).FirstOrDefault().Value.Key, jo.Value);
                        }
                        lst.Add(dictionary);
                    }
                    result[view] = JToken.FromObject(lst);
                    result[otm] = JToken.FromObject(true);                    
                }
            }

            JToken t = JToken.FromObject(jArr);

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
