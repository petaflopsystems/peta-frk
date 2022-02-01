using PetaframeworkStd.Interfaces;
using System;
using System.Net;
using System.Net.Http;

namespace PetaframeworkStd.WebApi
{
    public class Response
    {
        public HttpStatusCode StatusCode { get; set; }
        public HttpContent Content { get; set; }
        public ResponseException ResponseException { get; set; }
        public IServiceParameter Parameters { get; set; }
        public IService Service { get; set; }
        public String Message
        {
            get
            {
                if (Content != null)
                {
                    return Content.ReadAsStringAsync().Result;
                }
                else return "";
            }
        }

        public T GetReturnedObject<T>() where T : class
        {
            try
            {
                if (Content != null)
                    return Content.ReadAsAsync<T>().Result;
                else
                    return null;
            }
            catch (Exception ex)
            {
                this.ResponseException = new ResponseException(Content);
                return null;
            }            
        }
    }

    public class ResponseException : Exception
    {
        public ResponseException(HttpContent content) : base(content.ReadAsStringAsync().Result) { }
    }
}
