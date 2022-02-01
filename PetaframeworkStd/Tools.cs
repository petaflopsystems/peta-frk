using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace PetaframeworkStd
{
    public static class ToolsOLD
    {
        //public static String ToJson(Object text)
        //{
        //    return Newtonsoft.Json.JsonConvert.SerializeObject(text);
        //}
        //public static String ToJson(Object text, Boolean ignoreLoopHandling)
        //{
        //    if (ignoreLoopHandling)
        //        return Newtonsoft.Json.JsonConvert.SerializeObject(text, new JsonSerializerSettings
        //        {
        //            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        //        });
        //    else
        //        return ToJson(text);
        //}

        //public static IEnumerable<KeyValuePair<string, object>> GetToSendParameters(Interfaces.IServiceParameter objectToListProperties)
        //{
        //    foreach (var item in objectToListProperties.GetType().GetProperties())
        //    {
        //        if (!item.Name.Equals(nameof(Interfaces.IServiceParameter.Authorization)) &&
        //            !item.Name.Equals(nameof(Interfaces.IServiceParameter.ToSendFile)) &&
        //            !item.Name.Equals(nameof(Interfaces.IServiceParameter.ToSendParametersList)))
        //            yield return new KeyValuePair<string, object>(item.Name, item.GetValue(objectToListProperties));
        //    }
        //}

        //public static T FromJson<T>(string json)
        //{
        //    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        //}

        //public static String DecodeBase64(string str)
        //{
        //    var base64EncodedBytes = System.Convert.FromBase64String(str);
        //    return Encoding.GetEncoding("iso-8859-1").GetString(base64EncodedBytes);
        //}

        //public static String EncodeBase64(string str)
        //{
        //    if (String.IsNullOrWhiteSpace(str))
        //        return String.Empty;
        //    var plainTextBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(str);
        //    return System.Convert.ToBase64String(plainTextBytes);
        //}

        //public static bool IsBase64(string base64String)
        //{
        //    if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0
        //       || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
        //        return false;

        //    try
        //    {
        //        Convert.FromBase64String(base64String);
        //        return true;
        //    }
        //    catch (Exception exception)
        //    {
        //    }
        //    return false;
        //}

        //public static string GetMacAddress()
        //{
        //    var macAddr = (from nic in NetworkInterface.GetAllNetworkInterfaces()
        //                   where nic.OperationalStatus == OperationalStatus.Up
        //                   select nic.GetPhysicalAddress().ToString()).FirstOrDefault();
        //    return macAddr;
        //}

        //public static string GetMacAddresses()
        //{
        //    var macAddr = (from nic in NetworkInterface.GetAllNetworkInterfaces()
        //                   where nic.OperationalStatus == OperationalStatus.Up
        //                   select nic.GetPhysicalAddress().ToString());
        //    return string.Join(",", macAddr);
        //}

        //public static bool ContainsMacAddress(string mac)
        //{
        //    var macAddrs = (from nic in NetworkInterface.GetAllNetworkInterfaces()
        //                    where nic.OperationalStatus == OperationalStatus.Up
        //                    select nic.GetPhysicalAddress().ToString()).ToList();
        //    return macAddrs.Contains(mac);
        //}

        //public static IPAddress GetCurrentIP()
        //{
        //    IPAddress IP = IPAddress.Parse("127.0.0.1");
        //    var host = Dns.GetHostEntry(Dns.GetHostName());
        //    foreach (var ip in host.AddressList)
        //    {
        //        if (ip.AddressFamily == AddressFamily.InterNetwork)
        //        {
        //            IP = ip;
        //            break;
        //        }
        //    }
        //    return IP;
        //}

        //public static IPAddress GetHostIP(String hostnameOrAddress)
        //{
        //    IPAddress IP = IPAddress.Parse("127.0.0.1");
        //    var host = Dns.GetHostEntry(hostnameOrAddress);
        //    foreach (var ip in host.AddressList)
        //    {
        //        if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.Equals(IP))
        //        {
        //            if (ip.ToString().Equals(IP.ToString()))
        //                IP = GetCurrentIP();
        //            else
        //                IP = ip;
        //            break;
        //        }
        //    }
        //    if (IP.ToString().Equals("127.0.0.1"))
        //    {
        //        return GetCurrentIP(); ;
        //    }
        //    return IP;
        //}

        //public class FileInfoContractResolver : DefaultContractResolver
        //{
        //    protected override JsonContract CreateContract(Type objectType)
        //    {
        //        return objectType == typeof(FileInfo) ? (JsonContract)this.CreateISerializableContract(objectType) : base.CreateContract(objectType);
        //    }
        //}

        //public enum LogType
        //{
        //    Info,
        //    Warning,
        //    Error,
        //    Trace,
        //    Critical
        //}
    }


}
