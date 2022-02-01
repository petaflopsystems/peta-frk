using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Petaframework.POCO;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Petaframework
{
    internal class PrivatePtfkSession : IPtfkSession
    {
        private IPtfkSession _Current;

        public string Login { get; set; } = "";

        public string ID { get; set; }

        public IPtfkSession Current
        {
            get
            {
                return this._Current == null ? (IPtfkSession)this : this._Current;
            }
            private set
            {
                this._Current = value;
            }
        }

        public string IdToken { get; set; } = "";

        public string AccessToken { get; set; } = "";

        public DictionaryEntry Claims { get; set; }

        public Dictionary<string, object> Bag { get; set; }

        public List<string> AnothersPermissionsLogins { get; set; } = new List<string>();

        public IDepartment Department { get; set; }

        public IEnumerable<IRole> Roles { get; set; }

        public string Name { get; set; }

        public bool IsAdmin { get; set; }

        public string Email { get; set; }

        public void SetCurrentInstance(IPtfkSession owner)
        {
            this.Current = owner;
            this.SetOwnerBag();
        }

        public bool IsAnonimousUser()
        {
            return Login.Equals("anonimous") || ID == null;
        }

        public PrivatePtfkSession()
        {
        }

        public PrivatePtfkSession(Petaframework.POCO.Department department, List<Role> roles)
        {
            this.Department = (IDepartment)department;
            this.Roles = (IEnumerable<IRole>)roles;
        }
    }

    public class ConfigConverter<I, T> : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(I);
        }
        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.Value == null)
                return default(T);
            JObject jobject = JObject.Load(reader);
            T instance = (T)Activator.CreateInstance(typeof(T));
            serializer.Populate(jobject.CreateReader(), (object)instance);
            return instance;
        }
    }
}
