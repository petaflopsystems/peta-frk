using System.Net.Http;

namespace PetaframeworkStd.Interfaces
{
    public interface IBaseGateway
    {
        string Name { get;  }

        HttpClient GetHttpClient();

    }
}