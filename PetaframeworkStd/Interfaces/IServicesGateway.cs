using System;
using System.Collections.Generic;

namespace PetaframeworkStd.Interfaces
{
    public interface IServicesGateway : IBaseGateway
    {
        List<IService> Services { get; }
        //IService GetServiceByName(string name);
        //IService GetServiceByPath(string path);
        Exception ServiceExceptionThrown();
    }
}
