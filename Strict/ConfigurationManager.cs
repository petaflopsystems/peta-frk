using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using PetaframeworkStd;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework.Strict
{
    public class ConfigurationManager
    {
        private static IConfiguration _CurrentConfiguration;
        internal static IConfiguration CurrentConfiguration
        {
            get
            {
                if (_CurrentConfiguration == null)
                {
                    var msg = String.Concat(nameof(Petaframework), ".", nameof(Petaframework.Strict), ".", nameof(ConfigurationManager), " not contains ", nameof(IConfiguration), " instance!");
                    if (PtfkEnvironment.CurrentEnvironment != null)
                        PtfkConsole.WriteLine(msg, true);
                    throw new Exception(msg);
                }
                return _CurrentConfiguration;
            }
        }
        public static void Set(IConfiguration configuration)
        {
            _CurrentConfiguration = configuration;
        }
        private static String ConfigPath = "";
        private static String SetConfigPath()
        {
            String p = PetaframeworkStd.OS.GetAssemblyPath();
            PtfkConsole.WriteConfig("App Root path", p);
            ConfigPath = p;
            return ConfigPath;
        }
        internal static String CONFIG_PATH
        {
            get
            {
                if (String.IsNullOrWhiteSpace(ConfigPath))
                    return ConfigPath;

                return SetConfigPath();
            }
        }


        [Serializable]
        public class LogLevel
        {
            public string Default { get; set; }
            public string System { get; set; }
            public string Microsoft { get; set; }
        }

        static Dictionary<string, string> langMessages;
        public static string GetLanguageValue(string key, HttpContext httpContext)
        {
            var l = GetLangFile(httpContext);
            if (l.ContainsKey(key))
                return l[key];
            return string.Empty;
        }

        private static Dictionary<string, string> GetLangFile(HttpContext httpContext)
        {
            if (langMessages != null)
                return langMessages;
            var requestHeaders = Microsoft.AspNetCore.Http.HeaderDictionaryTypeExtensions.GetTypedHeaders(httpContext.Request);
            var defLang = requestHeaders.AcceptLanguage.FirstOrDefault().ToString();
            var t = Server.GetFileOnServer("~/peta-frk/lang/messages." + defLang.ToLower() + ".json");
            if (!t.Exists)
                t = Server.GetFileOnServer("~/peta-frk/lang/messages.json");

            var l = Tools.FromJson<Dictionary<string, string>>(File.ReadAllText(t.FullName));
            langMessages = l;
            return langMessages;
        }

        [Serializable]
        public class Logging
        {
            public bool IncludeScopes { get; set; }
            public LogLevel LogLevel { get; set; }
        }

        [Serializable]
        public class Configuration
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Logging Logging { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<String, String> AppConfiguration;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<String, String> ConnectionStrings;
            [JsonIgnore]
            internal String ConfigPath { get; set; }

            public void Save()
            {
                File.WriteAllText(ConfigPath, Tools.ToJson(this, true, true));
            }
        }

        [Serializable]
        public class Permission
        {
            [JsonProperty("pid", NullValueHandling = NullValueHandling.Ignore)]
            public string ProfileID { get; set; }

            [JsonProperty("profile")]
            public string Profile { get; set; }

            [JsonProperty("enabledTo", NullValueHandling = NullValueHandling.Ignore)]
            public String[] EnabledTo { get; set; }

            [JsonProperty("isPrivate", NullValueHandling = NullValueHandling.Ignore)]
            public bool? IsPrivate { get; set; }

            [JsonProperty("isAdmin", NullValueHandling = NullValueHandling.Ignore)]
            public bool? IsAdmin { get; set; } = false;

            [JsonProperty("hierarchyFlag", NullValueHandling = NullValueHandling.Ignore)]
            public bool HierarchyFlag { get; set; }

        }

        public static Configuration ReadConfiguration(FileInfo configFile)
        {
            try
            {
                return CheckIfExists(configFile, false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Dictionary<String, Permission[]> GetCurrentPermission
        {
            get
            {
                return ReadPermissions(new FileInfo(Path.Combine(CONFIG_PATH, Constants.PermissionJsonFileName)));
            }
        }

        public static Task<Dictionary<String, Permission[]>> GetCurrentPermissionsAsync
        {
            get
            {
                return Task.Factory.StartNew<Dictionary<String, Permission[]>>(() => ReadPermissions(new FileInfo(Path.Combine(CONFIG_PATH, Constants.PermissionJsonFileName))));
            }
        }

        internal static DateTime LastWriteUsersUpdate()
        {
            return new FileInfo(Path.Combine(ConfigurationManager.CONFIG_PATH, "Users.json")).LastWriteTime;
        }

        internal static List<IPtfkSession> ReadOrWriteUsers(params IPtfkSession[] usersToAdd)
        {
            FileInfo fi = new FileInfo(Path.Combine(ConfigurationManager.CONFIG_PATH, Constants.UsersJsonFileName));
            lock (Constants.UsersJsonFileName)
            {
                List<IPtfkSession> ptfkSessionList = ConfigurationManager.ReadUsers(fi);
                if (ptfkSessionList != null)
                {
                    IPtfkSession[] ptfkSessionArray = usersToAdd;
                    if ((ptfkSessionArray != null ? ((uint)ptfkSessionArray.Length > 0U ? 1 : 0) : 0) != 0)
                    {
                        ptfkSessionList.RemoveAll(x => ((IEnumerable<IPtfkSession>)usersToAdd).Where(u => u.Login.Equals(x.Login)).Any());
                        ptfkSessionList.AddRange((IEnumerable<IPtfkSession>)usersToAdd);
                        try
                        {
                            JsonSerializer jsonSerializer = new JsonSerializer();
                            jsonSerializer.Converters.Add((JsonConverter)new JavaScriptDateTimeConverter());
                            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                            jsonSerializer.TypeNameHandling = TypeNameHandling.Auto;
                            jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                            using (StreamWriter streamWriter = new StreamWriter(fi.FullName))
                            {
                                using (JsonWriter jsonWriter = (JsonWriter)new JsonTextWriter((TextWriter)streamWriter))
                                    jsonSerializer.Serialize(jsonWriter, (object)ptfkSessionList, typeof(List<PrivatePtfkSession>));
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                if (ptfkSessionList == null)
                    ptfkSessionList = new List<IPtfkSession>();
                return ptfkSessionList;
            }
        }

        private static List<IPtfkSession> ReadUsers(FileInfo fi)
        {
            if (fi.Exists)
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<IPtfkSession>>(File.ReadAllText(fi.FullName), new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore
                    });
                }
                catch (Exception ex)
                {
                    return new List<IPtfkSession>();
                }
            }
            else
            {
                fi.Create();
                return new List<IPtfkSession>();
            }
        }

        public static Permission[] GetPermissions(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return new Permission[] { };

            var AllPermissions = GetCurrentPermissionsAsync;
            AllPermissions.Wait();
            if (AllPermissions.Result.Count == 0)
                return new Permission[] { };
            var p = new List<Permission>();
            if (AllPermissions.Result.ContainsKey(entityId))
                p.AddRange(AllPermissions.Result[entityId]);
            if (AllPermissions.Result.ContainsKey("*"))
                p.AddRange(AllPermissions.Result["*"]);
            return p.ToArray();
        }

        public static Dictionary<String, Permission[]> ReadPermissions(FileInfo permissionsFile)
        {
            try
            {
                return CheckIfExistsPermissions(permissionsFile, true);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static Dictionary<String, Permission[]> _OnMemoryPermissions;
        private static System.Threading.Timer _timer;
        private static Dictionary<String, Permission[]> CheckIfExistsPermissions(FileInfo fileInfo, bool createIfNotExists)
        {
            if (_timer == null)
                _timer = new System.Threading.Timer(ReadFile, new CallbackParametes { CreateIfNotExists = createIfNotExists, FileInfo = fileInfo }, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            if (_OnMemoryPermissions != null)
                return _OnMemoryPermissions;

            ReadFile(new CallbackParametes { CreateIfNotExists = createIfNotExists, FileInfo = fileInfo });
            return _OnMemoryPermissions;
        }

        private struct CallbackParametes
        {
            public bool CreateIfNotExists { get; set; }
            public FileInfo FileInfo { get; set; }


        }

        private static void ReadFile(object state)
        {
            var p = (CallbackParametes)state;
            var parentFile = new FileInfo(Path.Combine(p.FileInfo.Directory.Parent.FullName, Constants.PermissionJsonFileName));
            if (!p.FileInfo.Exists && !parentFile.Exists)
            {
                if (!p.CreateIfNotExists)
                    _OnMemoryPermissions = null;
                var obj = new Dictionary<String, Permission[]>();
                obj.Add("*", new Permission[] { new Permission { Profile = "*", IsAdmin = true } });
                var json = Tools.ToJson(obj, true, true);
                if (p.FileInfo.Directory.Name.Equals(Constants.PublishProductionPathName) && !parentFile.Exists)
                    File.WriteAllText(parentFile.FullName, json);
                else
                    File.WriteAllText(p.FileInfo.FullName, json);
                _OnMemoryPermissions = obj;
            }
            else
            {
                if (!p.FileInfo.Exists)
                    p.FileInfo = parentFile;

                var jo = JObject.Parse(File.ReadAllText(p.FileInfo.FullName, Encoding.UTF8));
                //var id = jo["report"]["Id"].ToString();
                var permissions = new Dictionary<String, Permission[]>();
                foreach (JProperty item in jo.Children())
                {
                    permissions.Add(item.Path, Tools.FromJson<Permission[]>(item.Value.ToString()));
                }
                _OnMemoryPermissions = permissions;
            }
        }

        internal static T ReadOrWriteCollection<T>(string key, T cache, bool write = false) where T : System.Collections.ICollection, new()
        {
            try
            {
                var fi = new FileInfo(Path.Combine(CONFIG_PATH, nameof(PtfkCache), key + ".cache"));
                PtfkConsole.WriteConfig("PtfkCacke Path", fi.FullName + " Exists? " + fi.Exists);
                lock (CONFIG_PATH)
                {
                    if (cache != null && cache.Count > 0 && write)
                    {
                        lock (cache)
                        {
                            if (!fi.Directory.Exists)
                                fi.Directory.Create();
                            File.WriteAllText(fi.FullName, Tools.ToJson(cache, true));
                        }
                    }
                    else
                         if (cache != null && cache.Count == 0)
                        lock (cache)
                            return Read<T>(fi);

                    return cache;
                }
            }
            catch (Exception ex)
            {
                PtfkConsole.WriteLine("Error on save cache", ex, true);
                return cache;
            }
        }

        private static T Read<T>(FileInfo fi) where T : ICollection
        {
            if (fi.Exists)
            {
                try
                {
                    return Tools.FromJson<T>(File.ReadAllText(fi.FullName));
                }
                catch (Exception ex)
                {
                    return default(T);
                }
            }
            else
                fi.Create();
            return default(T);
        }

        private static Configuration CheckIfExists(FileInfo fileInfo, bool createIfNotExists = true)
        {
            if (!fileInfo.Exists)
            {
                PtfkConsole.WriteLine("Not exists config file: " + fileInfo.FullName, true);
                if (!createIfNotExists)
                    return null;

                //fileInfo.Create();
                var nv = new Dictionary<String, String>();
                nv.Add("DebugMode", "true");
                var conf = new Configuration
                {
                    Logging = new Logging
                    {
                        LogLevel = new LogLevel
                        {
                            Default = "Debug",
                            Microsoft = "Information",
                            System = "Information"
                        }
                    },
                    AppConfiguration = nv
                };

                File.WriteAllText(fileInfo.FullName, JsonConvert.SerializeObject(conf));
                conf.ConfigPath = fileInfo.FullName;
                return conf;
            }
            else
            {
                var conf = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(fileInfo.FullName));
                conf.ConfigPath = fileInfo.FullName;
                return conf;
            }
        }

        public static string GetConnectionString(string key)
        {
            try
            {
                var mongoConnection = CurrentConfiguration.GetSection("ConnectionStrings:" + key)?.Get<MongoDBConnection>();
                if (!String.IsNullOrWhiteSpace(mongoConnection?.ConnectionString))
                    return Tools.ToJson(mongoConnection);
            }
            catch (Exception e)
            {
                PtfkConsole.WriteLine("It is not MongoDB connection!");
            }
            return CurrentConfiguration.GetValue<string>("ConnectionStrings:" + key);
        }

        public static T GetAppConfiguration<T>(string key)
        {
            try
            {
                if (CurrentConfiguration == null)
                    return default;
                return CurrentConfiguration.GetValue<T>("AppConfiguration:" + key);
            }
            catch (Exception)
            {
                return default;
            }

        }

        public static bool ContainsAppConfiguration(string key)
        {
            try
            {
                CurrentConfiguration?.GetValue<string>("AppConfiguration:" + key);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool AddOrUpdateAppSetting<T>(string key, T value)
        {
            try
            {

                var filePath = Path.Combine(OS.GetAssemblyPath(), "appsettings.json");
                string json = File.ReadAllText(filePath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                var sectionPath = key.Split(":")[0];
                if (!string.IsNullOrEmpty(sectionPath))
                {
                    var keyPath = key.Split(":")[1];
                    jsonObj[sectionPath][keyPath] = value;
                }
                else
                {
                    jsonObj[sectionPath] = value;
                }
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, output);
                return true;
            }
            catch (Exception ex)
            {
                PtfkConsole.WriteLine(String.Format("Write app setting {0} fail. Error: {1}", key, Petaframework.Tools.ToJson(ex, true)));
                return false;
            }
        }

        internal static IConfiguration Builder(String relativePath = "")
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(OS.GetAssemblyPath())
            .AddJsonFile(String.IsNullOrWhiteSpace(relativePath) ? "appsettings.json" : relativePath)
            .Build();

            return configuration;
        }
    }
}
