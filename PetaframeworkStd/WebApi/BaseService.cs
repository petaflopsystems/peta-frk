using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace PetaframeworkStd.WebApi
{
    public abstract class BaseService : IBaseService
    {
        public static Type ResponseType;
        private static List<string> _ServicesWithURLVariables;
        private StringBuilder _StackTracer = new StringBuilder();

        public string Path { get; internal set; }

        public string Name { get; internal set; }

        internal BaseGateway Gateway { get; set; }

        protected BaseGateway CurrentGateway
        {
            get
            {
                return Gateway;
            }
        }

        public string StackTrace
        {

            get
            {
                try
                {
                    var txt = _StackTracer.ToString();
                    return txt;

                }
                catch (Exception ex)
                {
                    _StackTracer = new StringBuilder();
                    return "";
                }
            }
        }

        public BaseService()
        {

        }
        internal BaseService(string name, string path)
        {
            Path = path;
            Name = name;
        }

        private bool IsSimpleType(Type type)
        {
            return
                type.IsPrimitive ||
                new Type[] {
            typeof(Enum),
            typeof(String),
            typeof(Decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
                }.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }

        /// <summary>
        /// Http Get. If ServiceParameter contains value, send As QueryString.
        /// Request URL Example: http://yourserver.com/ping?parameter1=hello&parameter2=world
        /// </summary>
        /// <param name="serviceParameters">Parameter class that extends Service Parameter</param>
        /// <param name="type">Get Type</param>
        /// <returns>Petaframework Response</returns>
        public Response Get(IServiceParameter serviceParameters, BaseService.ParameterType type = BaseService.ParameterType.Path)
        {
            _StackTracer = new StringBuilder();
            if (BaseService._ServicesWithURLVariables == null)
                BaseService._ServicesWithURLVariables = new List<string>();
            HttpClient httpClient = Gateway.GetHttpClient();
            _StackTracer.AppendLine(String.Format("Method:::{0}:::{1}:::{2}", nameof(Get), Petaframework.Tools.ToJson(serviceParameters, true), type.ToString()));
            if (serviceParameters.Authorization != null)
                httpClient.DefaultRequestHeaders.Authorization = serviceParameters.Authorization;
            else if (Gateway.CurrentAuthorization != null)
                httpClient.DefaultRequestHeaders.Authorization = Gateway.CurrentAuthorization;
            StringBuilder stringBuilder1 = new StringBuilder();
            Response response1 = new Response()
            {
                StatusCode = HttpStatusCode.NotFound
            };
            if (httpClient.BaseAddress.ToString().EndsWith("/") && Path.StartsWith("/"))
                Path = Path.Substring(1);
            _StackTracer.AppendLine(String.Format("Path:::{0}", Path));
            switch (type)
            {
                case BaseService.ParameterType.Header:
                case BaseService.ParameterType.RequestBoby:
                    if (response1.StatusCode != HttpStatusCode.OK)
                    {
                        StringBuilder stringBuilder2 = new StringBuilder("?");
                        foreach (KeyValuePair<string, object> toSendParameters in serviceParameters.ToSendParametersList)
                            stringBuilder2.Append(toSendParameters.Key + "=" + toSendParameters.Value.ToString() + "&");
                        string str1 = Path + "/" + stringBuilder2.ToString();
                        _StackTracer.AppendLine(String.Format("PathChanged:::{0}", str1));
                        Response response2 = GetResponse(httpClient.GetAsync(str1.Replace("//", "/")).Result, serviceParameters);
                        if (response2.StatusCode == HttpStatusCode.OK)
                        {
                            List<string> withUrlVariables1 = BaseService._ServicesWithURLVariables;
                            Uri baseAddress1 = httpClient.BaseAddress;
                            string str2 = ((object)baseAddress1 != null ? baseAddress1.ToString() : (string)null) + Path;
                            if (!withUrlVariables1.Contains(str2))
                            {
                                List<string> withUrlVariables2 = BaseService._ServicesWithURLVariables;
                                Uri baseAddress2 = httpClient.BaseAddress;
                                string str3 = ((object)baseAddress2 != null ? baseAddress2.ToString() : (string)null) + Path;
                                withUrlVariables2.Add(str3);
                            }
                            return response2;
                        }
                    }
                    return response1;
                case BaseService.ParameterType.QueryString:
                    StringBuilder stringBuilder3 = new StringBuilder("?");
                    foreach (KeyValuePair<string, object> toSendParameters in serviceParameters.ToSendParametersList)
                        if (toSendParameters.Value != null)
                            stringBuilder3.Append(toSendParameters.Key + "=" + (toSendParameters.Value.GetType().Equals(typeof(DateTime)) ? ((DateTime)toSendParameters.Value).ToString("yyyy-MM-dd HH:mm:ss") : toSendParameters.Value.ToString()) + "&");
                    string str4 = Path + "/" + stringBuilder3.ToString();
                    _StackTracer.AppendLine(String.Format("PathChanged:::{0}", str4));
                    return GetResponse(httpClient.GetAsync(str4.Replace("//", "/")).Result, serviceParameters);
                default:
                    if (serviceParameters.ToSendParametersList != null && serviceParameters.ToSendParametersList.Count<KeyValuePair<string, object>>() > 0)
                    {
                        if (serviceParameters.ToSendParametersList.Count<KeyValuePair<string, object>>() > 1)
                        {
                            foreach (KeyValuePair<string, object> toSendParameters in serviceParameters.ToSendParametersList)
                                stringBuilder1.Append(toSendParameters.Key + "=" + (toSendParameters.Value.GetType().Equals(typeof(DateTime)) ? ((DateTime)toSendParameters.Value).ToString("yyyy-MM-dd HH:mm:ss") : toSendParameters.Value.ToString()) + "&");
                        }
                        else
                            stringBuilder1.Append(serviceParameters.ToSendParametersList.FirstOrDefault<KeyValuePair<string, object>>().Value.ToString());
                    }
                    string str5 = Path + "/" + stringBuilder1.ToString();
                    _StackTracer.AppendLine(String.Format("PathChanged:::{0}", str5));
                    return GetResponse(httpClient.GetAsync(str5.Replace("//", "/")).Result, serviceParameters);
            }
        }

        /// <summary>
        /// Http Get. If ServiceParameter contains value search a similar parameter on Service Path to change and send request.
        /// Request URL Example: http://yourserver.com/ping/parameter1value/pong/parameter2value
        /// </summary>
        /// <param name="serviceParameters">Parameter class that extends Service Parameter</param>
        /// <returns>Petaframework Response</returns>
        public Response GetAPI(IServiceParameter serviceParameters)
        {
            _StackTracer = new StringBuilder();
            var client = Gateway.GetHttpClient();
            if (serviceParameters.Authorization != null)
                client.DefaultRequestHeaders.Authorization = serviceParameters.Authorization;
            else
            if (Gateway.CurrentAuthorization != null)
                client.DefaultRequestHeaders.Authorization = Gateway.CurrentAuthorization;

            StringBuilder queryString = new StringBuilder();
            var path = Path;// + "/" + queryString.ToString();
            if (serviceParameters.ToSendParametersList.Any())
                foreach (var item in serviceParameters.ToSendParametersList)
                    path = path.Replace("{" + item.Key.ToLower() + "}", item.Value.ToString());

            HttpResponseMessage httpResponse = client.GetAsync(client.BaseAddress.AbsoluteUri + path).Result;
            return GetResponse(httpResponse, serviceParameters);
        }

        /// <summary>
        /// Post with Parameters As FormData
        /// </summary>
        /// <param name="serviceParameters">Parameter class that extends Service Parameter</param>
        /// <returns>Petaframework Response</returns>
        public Response PostFormData(IServiceParameter serviceParameters)
        {
            _StackTracer = new StringBuilder();
            serviceParameters.SetDefaultValues(Gateway.CurrentSession, null);
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (var param in serviceParameters.ToSendParametersList)
            {
                if (param.Value == null)
                {
                    parameters.Add(param.Key, "");
                }
                else
                {
                    if (IsSimpleType(param.Value.GetType()))
                        parameters.Add(param.Key, param.Value.ToString());
                    else
                        if (param.Value.GetType().IsArray || (param.Value.GetType().IsGenericType && param.Value.GetType().GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        Array arr;
                        if (param.Value.GetType().IsGenericType && param.Value.GetType().GetGenericTypeDefinition() == typeof(List<>))
                            arr = param.Value.GetType().GetMethod(nameof(List<object>.ToArray)).Invoke(param.Value, null) as Array;
                        else
                            arr = param.Value as Array;
                        foreach (var item in arr)
                        {
                            parameters.Add(param.Key, item.ToString());
                        }
                    }
                    else
                        parameters.Add(param.Key, Newtonsoft.Json.JsonConvert.SerializeObject(param.Value));
                }
            }

            var client = Gateway.GetHttpClient();
            if (serviceParameters.Authorization != null)
                client.DefaultRequestHeaders.Authorization = serviceParameters.Authorization;
            else
                if (Gateway.CurrentAuthorization != null)
                client.DefaultRequestHeaders.Authorization = Gateway.CurrentAuthorization;

            MultipartFormDataContent form = new MultipartFormDataContent();
            HttpContent content = new StringContent("file");
 
            foreach (var item in parameters)
            {
                form.Add(new StringContent(item.Value), item.Key);
            }

            HttpResponseMessage httpResponse = null;
            if (serviceParameters.ToSendFile != null && new FileInfo(serviceParameters.ToSendFile.FullName).Exists)
            {
                using (var stream = File.OpenRead(serviceParameters.ToSendFile.FullName))
                {
                    content = new StreamContent(stream);
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "file",
                        FileName = serviceParameters.ToSendFile.Name
                    };
                    form.Add(content);

                    try
                    {
                        httpResponse = (client.PostAsync(Path, form)).Result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else
                httpResponse = (client.PostAsync(Path, form)).Result;
            return GetResponse(httpResponse, serviceParameters);
        }

        private Response GetResponse(
      HttpResponseMessage httpResponse,
      IServiceParameter parameters)
        {
            Response response;
            if (httpResponse.IsSuccessStatusCode && httpResponse.StatusCode == HttpStatusCode.OK)
                response = new Response()
                {
                    Content = httpResponse.Content,
                    StatusCode = httpResponse.StatusCode
                };
            else
                response = new Response()
                {
                    Content = httpResponse.Content,
                    StatusCode = httpResponse.StatusCode,
                    ResponseException = new ResponseException(httpResponse.Content)
                };
            response.Parameters = parameters;
            response.Service = this as IService;
            _StackTracer.AppendLine(String.Format("Response:::{0}", Petaframework.Tools.ToJson(response, true)));
            return response;
        }

        /// <summary>
        /// Post with Parameters As Boby
        /// </summary>
        /// <param name="serviceParameters">Parameter class that extends Service Parameter</param>
        /// <returns>Petaframework Response</returns>
        public Response PostBody(IServiceParameter serviceParameters)
        {
            _StackTracer = new StringBuilder();
            serviceParameters.SetDefaultValues(Gateway.CurrentSession, null);
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            var client = Gateway.GetHttpClient();
            if (serviceParameters.Authorization != null)
                client.DefaultRequestHeaders.Authorization = serviceParameters.Authorization;
            else
                if (Gateway.CurrentAuthorization != null)
                client.DefaultRequestHeaders.Authorization = Gateway.CurrentAuthorization;

            MultipartFormDataContent form = new MultipartFormDataContent();
            HttpContent content = new StringContent(ToJson(serviceParameters), Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponse = null;
            if (serviceParameters.ToSendFile != null && new FileInfo(serviceParameters.ToSendFile.FullName).Exists)
            {
                using (var stream = File.OpenRead(serviceParameters.ToSendFile.FullName))
                {
                    content = new StreamContent(stream);
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "file",
                        FileName = serviceParameters.ToSendFile.Name
                    };
                    form.Add(content);

                    try
                    {
                        httpResponse = (client.PostAsync(Path, form)).Result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                httpResponse = (client.PostAsync(Path, content)).Result;
            }
            return GetResponse(httpResponse, serviceParameters);
        }

        private String ToJson(Object text)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(text);
        }

        public enum ParameterType
        {
            Path,
            Header,
            QueryString,
            RequestBoby,
        }
    }
}
