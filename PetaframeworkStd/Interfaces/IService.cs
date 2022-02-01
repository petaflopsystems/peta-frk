using PetaframeworkStd.WebApi;
using System;

namespace PetaframeworkStd.Interfaces
{
    public interface IService : IBaseService
    {
        Response PostFormData(IServiceParameter serviceParameters);        
    }
}