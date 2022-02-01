using Microsoft.Extensions.Logging;
using Petaframework;
using PetaframeworkStd.Exceptions;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PetaframeworkStd.WebApi
{
    public abstract class BaseGateway : IServicesGateway
    {
        private readonly HttpClient _client;
        private readonly IGatewayConfig _config;
        private List<KeyValueObject<IService, IServiceParameter, Type>> _services = new List<KeyValueObject<IService, IServiceParameter, Type>>();

        protected IPtfkSession Session;
        public IPtfkSession CurrentSession { get { return Session; } }

        protected IEntity Entity;

        protected internal Uri BaseAddressInternal;

        private EventId _EventId = new EventId(new Random().Next(), "Service Gateway Logs");

        public IEntity CurrentEntity { get { return Entity; } }

        public IServiceParameter CurrentParameter { get; set; }

        public HttpClient GetHttpClient()
        {
            HttpClient httpClient = this.HttpMessageHandler != null ? new HttpClient(this.HttpMessageHandler, true) : new HttpClient();
            this.SetConfiguration(this._config, httpClient);
            if (this.BaseAddressInternal != (Uri)null)
                httpClient.BaseAddress = this.BaseAddressInternal;
            return httpClient;
        }

        public string Name { get; private set; }
        public string ConnectedServiceURL { get; private set; }

        public List<IService> Services => _services.Select(x => x.Key as IService).ToList();

        protected System.Net.Http.HttpMessageHandler HttpMessageHandler { get; set; }

        protected AuthenticationHeaderValue Authorization { set; get; }

        internal AuthenticationHeaderValue CurrentAuthorization
        {
            get { return Authorization; }
        }

        public List<Response> WrongResponses { get; internal protected set; }

        public void AddWrongResponses(IEnumerable<Response> list)
        {
            if (this.WrongResponses == null)
                this.WrongResponses = new List<Response>();
            this.WrongResponses.AddRange(list);
        }

        public Response LastHttpResponse { get; protected set; }

        public void SetHttpResponse(Response response, Func<Response, Exception> callback = null)
        {
            if (WrongResponses == null)
                WrongResponses = new List<Response>();
            if (response.StatusCode != System.Net.HttpStatusCode.OK && !WrongResponses.Contains(response))
            {
                IEntity currentEntity = CurrentEntity;
                if (currentEntity != null)
                {
                    ILogger logger = currentEntity.Logger;
                    if (logger != null)
                        LoggerExtensions.LogInformation(logger, _EventId, "Service Wrong Response: {1} Owner:{0}",
              CurrentSession?.Login,
              Tools.ToJson(response, true)
                    );
                }
                WrongResponses.Add(response);
            }
            LastHttpResponse = response;
            if (callback != null)
            {
                var e = callback.Invoke(response);
                if (e != null)
                {
                    var c = new StringContent(e.Message);
                    WrongResponses.Add(new Response { ResponseException = new ResponseException(c), StatusCode = response.StatusCode, Content = c, Parameters = response.Parameters, Service = response.Service });
                }
            }
        }

        public void RemoveWrongResponse<IServiceClass>() where IServiceClass : class, IService
        {
            var response = WrongResponses.Where<Response>((Func<Response, bool>)(x => x.Service.GetType().Equals(typeof(IServiceClass))));
            var toDel = new List<Response>();
            foreach (var item in response)
            {
                if (response != null)
                    toDel.Add(item);
            }
            foreach (var item in toDel.ToList())
                WrongResponses.Remove(item);
        }

        public BaseGateway(string name, IPtfkSession session, IEntity entity)
        {
            Session = session;
            Entity = entity;
            Name = name;
            _client = new HttpClient();
            _config = GetConfigFromFile(name);
            SetConfiguration(_config);
        }

        public BaseGateway(string name, IPtfkSession session, IEntity entity, bool hasConnectedService)
        {
            Session = session;
            Entity = entity;
            Name = name;
            _client = new HttpClient();
            _config = GetConfigFromFile(name);
            SetConfiguration(_config);
            if (hasConnectedService)
                ConnectedServiceURL = _client.BaseAddress.ToString();
        }

        internal IService GetServiceByName(string name)
        {
            return Services.Where(x => x.Name.Equals(name)).FirstOrDefault();
        }

        internal IService GetServiceByPath(string path)
        {
            return Services.Where(x => x.Path.Equals(path)).FirstOrDefault();
        }

        public String GetServiceAsString<ServiceClass>(IServiceParameter parameter) where ServiceClass : BaseService, IService, new()
        {
            CurrentParameter = parameter;
            var t = typeof(ServiceClass);
            var elem = _services.Where(x => x.Key.GetType().Equals(t)).FirstOrDefault();
            try
            {
                var consumable = elem.Key as IConsumable<String>;
                return consumable.Consume(parameter);
            }
            catch (Exception ex)
            {
                LogError<ServiceClass>(ex, nameof(GetServiceAsString));
                var consumable = elem.Key;
                var obj = elem.Key.GetType().GetMethod(nameof(IConsumable<String>.Consume)).Invoke(elem.Key, new object[] { parameter });

                return obj.ToString();
            }
        }

        private void LogError<ServiceClass>(Exception ex, string serviceMethod)
        {
            try
            {
                IServiceParameter serviceParameter = CurrentParameter;
                if (ex.GetType() == typeof(ServiceFailedException))
                {
                    ServiceFailedException serviceFailedException = (ServiceFailedException)ex;
                    if (serviceFailedException.Parameters != null)
                        serviceParameter = serviceFailedException.Parameters;
                }
                IEntity currentEntity = CurrentEntity;
                if (currentEntity == null)
                    return;
                ILogger logger = currentEntity.Logger;
                if (logger == null)
                    return;
                LoggerExtensions.LogError(logger, _EventId, ex, string.Format("Error on Service {0}.{2}: {1} Params: {3} Owner:{4}", (object)typeof(ServiceClass).Name, ex.InnerException != null ? (object)Tools.ToJson((object)ex.InnerException, true) : (object)Tools.ToJson((object)ex, true), (object)serviceMethod, (object)Tools.ToJson((object)serviceParameter.ToSendParametersList, true), (object)CurrentSession.Login), Array.Empty<object>());
            }
            catch (Exception ex1)
            {
                IEntity currentEntity = CurrentEntity;
                if (currentEntity == null)
                    return;
                ILogger logger = currentEntity.Logger;
                if (logger == null)
                    return;
                LoggerExtensions.LogError(logger, _EventId, ex, string.Format("Error on Service {0}.{2}: {1} Params: {3} Owner:{4}", (object)typeof(ServiceClass).Name, ex.InnerException != null ? (object)Tools.ToJson((object)ex.InnerException, true) : (object)Tools.ToJson((object)ex, true), (object)serviceMethod, (object)("Error on serialize Parameters:" + Tools.ToJson((object)ex1, true)), (object)CurrentSession.Login), Array.Empty<object>());
            }
        }

        public Boolean GetServiceAsBoolean<ServiceClass>(IServiceParameter parameter) where ServiceClass : BaseService, IService, new()
        {
            CurrentParameter = parameter;
            var t = typeof(ServiceClass);
            var elem = _services.Where(x => x.Key.GetType().Equals(t)).FirstOrDefault();
            try
            {
                var consumable = elem.Key as IConsumable<Boolean>;
                return consumable.Consume(parameter);
            }
            catch (Exception ex)
            {
                LogError<ServiceClass>(ex, nameof(GetServiceAsBoolean));
                var consumable = elem.Key;
                var obj = elem.Key.GetType().GetMethod(nameof(IConsumable<String>.Consume)).Invoke(elem.Key, new object[] { parameter });

                return Convert.ToBoolean(obj);
            }
        }

        public DateTime GetServiceAsDateTime<ServiceClass>(IServiceParameter parameter) where ServiceClass : BaseService, IService, new()
        {
            CurrentParameter = parameter;
            Type t = typeof(ServiceClass);
            KeyValueObject<IService, IServiceParameter, Type> keyValueObject = _services.Where<KeyValueObject<IService, IServiceParameter, Type>>((Func<KeyValueObject<IService, IServiceParameter, Type>, bool>)(x => x.Key.GetType().Equals(t))).FirstOrDefault<KeyValueObject<IService, IServiceParameter, Type>>();
            try
            {
                return (keyValueObject.Key as IConsumable<DateTime>).Consume(parameter);
            }
            catch (Exception ex)
            {
                LogError<ServiceClass>(ex, nameof(GetServiceAsDateTime));
                IService consumable = keyValueObject.Key;
                return Convert.ToDateTime(keyValueObject.Key.GetType().GetMethod(nameof(IConsumable<String>.Consume)).Invoke((object)keyValueObject.Key, new object[] { parameter }));
            }
        }

        public Double GetServiceAsDouble<ServiceClass>(IServiceParameter parameter) where ServiceClass : BaseService, IService, new()
        {
            CurrentParameter = parameter;
            var t = typeof(ServiceClass);
            var elem = _services.Where(x => x.Key.GetType().Equals(t)).FirstOrDefault();
            try
            {
                var consumable = elem.Key as IConsumable<Double>;
                return consumable.Consume(parameter);
            }
            catch (Exception ex)
            {
                LogError<ServiceClass>(ex, nameof(GetServiceAsDouble));
                var consumable = elem.Key;
                var obj = elem.Key.GetType().GetMethod(nameof(IConsumable<Double>.Consume)).Invoke(elem.Key, new object[] { parameter });

                return Convert.ToDouble(obj);
            }
        }

        public Object GetService<ServiceClass>(IServiceParameter parameter) where ServiceClass : BaseService, IService, new()
        {
            CurrentParameter = parameter;
            var t = typeof(ServiceClass);
            var elem = _services.Where(x => x.Key.GetType().Equals(t)).FirstOrDefault();
            var consumable = elem.Key as IConsumable<object>;
            return consumable.Consume(parameter);
        }

        public ResponseType GetServiceAs<ServiceClass, ResponseType>(IServiceParameter parameter)
            where ServiceClass : BaseService, IService, new()
            where ResponseType : class
        {
            try
            {
                CurrentParameter = parameter;
                var t = typeof(ServiceClass);
                var elem = _services.Where(x => x.Key.GetType().Equals(t)).FirstOrDefault();
                var consumable = elem.Key as IConsumable<ResponseType>;
                if (consumable == null)
                {
                    throw new Exceptions.ServiceNotFoundException(elem.Key, typeof(ResponseType).FullName);
                }
                return consumable.Consume(parameter);
            }
            catch (Exception ex)
            {
                LogError<ServiceClass>(ex, nameof(GetServiceAs));
                throw ex;
            }
        }

        /// <summary>
        /// Add Service to Gateway thar return Generic Types.
        /// </summary>
        /// <typeparam name="ServiceType">Service Type that implements IService and IConsumable</typeparam>
        /// <typeparam name="ParameterClassType">Service Parameter Type that implements IServiceParameter</typeparam>
        /// <typeparam name="ResponseType">Response Type of Service</typeparam>
        /// <param name="path">The relative path of Service</param>
        /// <param name="name">Description of Service</param>
        /// <returns></returns>
        protected virtual IService AddService<ServiceType, ParameterClassType, ResponseType>(String path, String name)
            where ServiceType : BaseService, IService, IConsumable<ResponseType>, new()
            where ParameterClassType : IServiceParameter, new()
            where ResponseType : class
        {
            var elem = new ServiceType
            {
                Path = path,
                Name = name,
                Gateway = this
            };
            _services.Add(new KeyValueObject<IService, IServiceParameter, Type>(elem, new ParameterClassType(), typeof(ResponseType)));
            return elem;
        }

        /// <summary>
        /// Add Service to Gateway that return a Boolean Value.
        /// </summary>
        /// <typeparam name="ServiceType">Service Type that implements IService and IConsumable</typeparam>
        /// <typeparam name="ParameterClassType">Service Parameter Type that implements IServiceParameter</typeparam>
        /// <param name="path">The relative path of Service</param>
        /// <param name="name">Description of Service</param>
        /// <returns></returns>
        protected virtual IService AddServiceBoolean<ServiceType, ParameterClassType>(String path, String name)
    where ServiceType : BaseService, IService, IConsumable<bool>, new()
    where ParameterClassType : IServiceParameter, new()

        {
            var elem = new ServiceType
            {
                Path = path,
                Name = name,
                Gateway = this
            };
            _services.Add(new KeyValueObject<IService, IServiceParameter, Type>(elem, new ParameterClassType(), typeof(bool)));
            return elem;
        }

        /// <summary>
        /// Add Service to Gateway that return a Double Value.
        /// </summary>
        /// <typeparam name="ServiceType">Service Type that implements IService and IConsumable</typeparam>
        /// <typeparam name="ParameterClassType">Service Parameter Type that implements IServiceParameter</typeparam>
        /// <param name="path">The relative path of Service</param>
        /// <param name="name">Description of Service</param>
        /// <returns></returns>
        protected virtual IService AddServiceDouble<ServiceType, ParameterClassType>(String path, String name)
    where ServiceType : BaseService, IService, IConsumable<double>, new()
    where ParameterClassType : IServiceParameter, new()

        {
            var elem = new ServiceType
            {
                Path = path,
                Name = name,
                Gateway = this
            };
            _services.Add(new KeyValueObject<IService, IServiceParameter, Type>(elem, new ParameterClassType(), typeof(double)));
            return elem;
        }

        protected virtual IService AddServiceDateTime<ServiceType, ParameterClassType>(
  string path,
  string name)
  where ServiceType : BaseService, IService, IConsumable<DateTime>, new()
  where ParameterClassType : IServiceParameter, new()
        {
            ServiceType serviceType1 = new ServiceType();
            serviceType1.Path = path;
            serviceType1.Name = name;
            serviceType1.Gateway = this;
            ServiceType serviceType2 = serviceType1;
            this._services.Add(new KeyValueObject<IService, IServiceParameter, Type>((IService)serviceType2, (IServiceParameter)new ParameterClassType(), typeof(DateTime)));
            return (IService)serviceType2;
        }

        private IGatewayConfig GetConfigFromFile(String name)
        {
            var config = new ConfigFile(name);
            return config;
        }

        private void SetConfiguration(IGatewayConfig config, HttpClient httpClient = null)
        {
            var client = httpClient != null ? httpClient : _client;
            if (config == null)
                throw new EntryPointNotFoundException();
            client.BaseAddress = new System.Uri(config.Url);
            PtfkConsole.WriteConfig("Service " + config.Name + " path" , config.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            if (config.HeaderTokens?.ToSend != null)
                foreach (var toSend in config?.HeaderTokens?.ToSend)
                {
                    client.DefaultRequestHeaders.Add(toSend.Key, toSend.Value);
                }
            if (config.HeaderMediaTypes != null)
                foreach (var mediaTypes in config?.HeaderMediaTypes)
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaTypes));
                }
            if (config.TimeoutMinutes <= 0)
                client.Timeout = TimeSpan.FromSeconds(30);
            else
                client.Timeout = TimeSpan.FromMinutes(config.TimeoutMinutes);
        }

        public IGatewayConfig GetConfig()
        {
            return _config;
        }

        public List<Exception> ServiceExceptionsThrown()
        {
            if (this._services != null)
            {
                foreach (IService service in this._services.Select(x => x.Key))
                {
                    if (this.WrongResponses != null)
                        return this.WrongResponses.Select(w => new Exception(w.Service.Name + ": " + ((int)w.StatusCode).ToString() + "_" + w.Message, w.ResponseException)).ToList();
                }
            }
            return null;
        }

        public Exception ServiceExceptionThrown()
        {
            List<Exception> source = this.ServiceExceptionsThrown();
            return source != null && source.Count<Exception>() > 0 ? new Exception("<br />" + string.Join("<br /> * ", source.Select<Exception, string>((Func<Exception, string>)(e => e.Message)).Distinct<string>())) : (Exception)null;
        }
    }
}
