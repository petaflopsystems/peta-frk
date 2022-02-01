using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace PetaframeworkStd
{
    public class ApiClient
    {
        HttpClient _client;
        public ApiClient(string url)
        {
            _client = new HttpClient();
            _client = new HttpClient();
            _client.BaseAddress = new Uri(url);
            _client.DefaultRequestHeaders.Accept.Clear();
            //_client.DefaultRequestHeaders.Add("auth-token", GetSendToken());
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));
            _client.Timeout = TimeSpan.FromMinutes(60);
        }

        public HttpClient GetClient()
        {
            return _client;
        }

        public async System.Threading.Tasks.Task<HttpResponseMessage> PostBJsonAsync(string path, object content, params KeyValuePair<string, string>[] headerValues)
        {
            HttpClient client = GetClient();

            if (headerValues != null && headerValues.Length > 0)
            {
                foreach (var item in headerValues)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }
            HttpResponseMessage model = new HttpResponseMessage();
            MediaTypeFormatter bsonFormatter = new BsonMediaTypeFormatter();
            var task = await client.PostAsync(path, content, bsonFormatter);

            return task;
        }


        public HttpResponseMessage Put(string path, object content, params KeyValuePair<string, string>[] headerValues)
        {
            HttpClient client = GetClient();

            if (headerValues != null && headerValues.Length > 0)
            {
                foreach (var item in headerValues)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }
            HttpResponseMessage model = new HttpResponseMessage();
            var task = client.PutAsync(path, content != null ? new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content))) : null)
                .ContinueWith((System.Threading.Tasks.Task<HttpResponseMessage> taskwithresponse) =>
                {
                    var response = taskwithresponse.Result;
                    try
                    {
                        var httpResult = response.Content.ReadAsByteArrayAsync();
                        httpResult.Wait();
                        model = response;
                    }
                    catch (Exception ex)
                    {
                        model = response;
                    }
                });
            task.Wait();
            return model;
        }

        public async System.Threading.Tasks.Task<HttpResponseMessage> GetAsync(string path)
        {
            HttpClient client = GetClient();
            var task = await client.GetAsync(path);
            return task;
        }
    }
}
