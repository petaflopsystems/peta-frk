using PetaframeworkStd.Interfaces;
using PetaframeworkStd.WebApi;
using System;

namespace PetaframeworkStd.Exceptions
{
    public class ServiceFailedException : Exception
    {
        private const string _msgPattern = "Service {0} response failed!";
        private const string _msgPatternWithResponse = "Service {0} failed with response: {1}";
        private WebApi.Response _response;

        private ServiceFailedException()
        {
        }

        public ServiceFailedException(string serviceName)
          : base(ServiceFailedException.GetMessage(serviceName, ""))
        {
            this.ServiceName = serviceName;
        }

        public ServiceFailedException(
          IService service,
          IServiceParameter parameters,
          PetaframeworkStd.WebApi.Response response,
          BaseGateway gate)
          : base(ServiceFailedException.GetMessage(service.GetType().Name, response))
        {
            this.ServiceName = service.GetType().Name;
            this.Response = ((int)response?.StatusCode).ToString() + " " + response?.Content?.ReadAsStringAsync().Result;
            this.Parameters = parameters;
            if (gate != null && gate.WrongResponses == null)
                gate.WrongResponses = new System.Collections.Generic.List<Response>();
            _response = response;
            gate?.WrongResponses?.Add(response);
        }

        public ServiceFailedException(IService service, string response, IServiceParameter parameters)
          : base(ServiceFailedException.GetMessage(service.GetType().Name, response))
        {
            this.ServiceName = service.GetType().Name;
            this.Response = response;
            _response = new Response { Content = new System.Net.Http.StringContent(response) };
            this.Parameters = parameters;
        }


        public void UpdateGateway(BaseGateway gatewayToBeUpdated)
        {
            if (_response != null)
                gatewayToBeUpdated?.WrongResponses?.Add(_response);
        }

        private static string GetMessage(string serviceName, PetaframeworkStd.WebApi.Response response = null)
        {
            return !string.IsNullOrEmpty(response.Message) ? string.Format("Service {0} failed with response: {1}", serviceName, (((int)response.StatusCode).ToString() + "_" + response.Content.ReadAsStringAsync().Result)) : string.Format("Service {0} response failed!", serviceName);
        }

        private static string GetMessage(string serviceName, string response = "")
        {
            return !string.IsNullOrEmpty(response) ? string.Format("Service {0} failed with response: {1}", serviceName, response) : string.Format("Service {0} response failed!", serviceName);
        }

        public ServiceFailedException SetParameters(IServiceParameter parameters)
        {
            this.Parameters = parameters;
            return this;
        }

        public string Response { get; private set; }

        public string ServiceName { get; set; }

        public IServiceParameter Parameters { get; internal set; }
    }
}
