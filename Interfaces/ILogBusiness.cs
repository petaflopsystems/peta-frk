using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Petaframework.Interfaces
{
    public interface ILogBusiness
    {
        IQueryable<IPtfkLog> ListFromEntity(long entityID, string entityName);
    }
}
