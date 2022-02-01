using System;
using System.Collections.Generic;

namespace PetaframeworkStd.Interfaces
{
    public interface IGatewayConfig
    {
        String Name { get; }
        List<string> EnabledHosts { get;  }
        string Url { get; }
        IHeaderToken HeaderTokens { get;  }
        string GetConfigToReceiveHeaderValue(String headerKey);
        string GetConfigToSendHeaderValue(String headerKey);
        List<string> HeaderMediaTypes { get; }
        int TimeoutMinutes { get; set; }
    }

    public interface IHeaderToken
    {
        Dictionary<string, string> ToSend { get;  }
        Dictionary<string, string> ToReceive { get; }
    }
}