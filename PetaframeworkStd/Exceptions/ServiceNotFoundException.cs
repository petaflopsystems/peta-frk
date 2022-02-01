using PetaframeworkStd.Interfaces;
using System;

namespace PetaframeworkStd.Exceptions
{
    public class ServiceNotFoundException : Exception
    {
        private ServiceNotFoundException() { }

        public ServiceNotFoundException(String serviceName) : base(GetMessage(serviceName))
        {
            this.ServiceName = serviceName;
        }

        public ServiceNotFoundException(IService service, String returnType) : base(GetMessage(service.GetType().Name, returnType))
        {
            this.ServiceName = service.GetType().Name;
            this.ReturnedType = returnType;
        }

        private static string GetMessage(string serviceName, string returnType = "")
        {
            if (!string.IsNullOrEmpty(returnType))
                return String.Format(_msgPatternWithResponse, serviceName, returnType);
            return String.Format(_msgPattern, serviceName);
        }

        private const String _msgPattern = "Service {0} not found!";
        private const String _msgPatternWithResponse = "Service {0} with Returned Type {1} not found!";

        public string ReturnedType { get; private set; }
        public string ServiceName { get; set; }
    }
}
