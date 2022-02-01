using Newtonsoft.Json;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PetaframeworkStd.WebApi
{
    internal class ConfigFile : IGatewayConfig
    {
        const string BaseDirectoryName = "GatewaySettings";
        private static DirectoryInfo CONFIG_PATH
        {
            get
            {
                var d = OS.IsGnu() ? new DirectoryInfo(BaseDirectoryName) : new DirectoryInfo(OS.GetAssemblyPath(BaseDirectoryName));
                return d;
            }
        }
        public ConfigFile() { }
        public ConfigFile(String name)
        {
            var config = ListConfigs().Where(x => x.Name.ToLower().Equals(name.ToLower())).FirstOrDefault();            
            if (config == null)
                throw new Exception("Config File Not find!");
            this.EnabledHosts = config.EnabledHosts;
            this.HeaderTokens = config.HeaderTokens;
            this.Url = config.Url.Trim();
            this.Name = name;
            this.HeaderMediaTypes = config.HeaderMediaTypes;
            this.TimeoutMinutes = config.TimeoutMinutes;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> EnabledHosts { get; set; }
        public string Url { get; set; }
        public HeaderTokens HeaderTokens { get; set; }
        public String Name { get; set; }

        IHeaderToken IGatewayConfig.HeaderTokens => HeaderTokens;

        public List<string> HeaderMediaTypes { get; set; }

        public int TimeoutMinutes { get; set; }

        public string GetConfigToReceiveHeaderValue(String headerKey)
        {
            try
            {
                return this.HeaderTokens.ToReceive.Where(x => x.Key.Equals(headerKey)).FirstOrDefault().Value;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public string GetConfigToSendHeaderValue(String headerKey)
        {
            try
            {
                return this.HeaderTokens.ToSend.Where(x => x.Key.Equals(headerKey)).FirstOrDefault().Value;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public List<ConfigFile> ListConfigs()
        {
            try
            {
                var lst = new List<ConfigFile>();
                var configFiles = CONFIG_PATH.GetFiles("*.json");
                foreach (var item in configFiles)
                {
                    try
                    {
                        var c = Petaframework.Tools.FromJson<ConfigFile>(File.ReadAllText(item.FullName));
                        c.Name = item.Name.Substring(0, item.Name.LastIndexOf('.'));
                        lst.Add(c);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                return lst;
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException(CONFIG_PATH.Parent.Name + "/" + CONFIG_PATH.Name + " not found!");
            }
        }
    }
    public class HeaderTokens : IHeaderToken
    {
        public Dictionary<string, string> ToSend { get; set; }
        public Dictionary<string, string> ToReceive { get; set; }
    }


}
