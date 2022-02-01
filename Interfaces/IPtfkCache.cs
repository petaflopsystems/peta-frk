using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.Interfaces
{
    public interface IPtfkCache
    {
        T GetCache<T>(IPtfkSession owner, String cacheID, bool isTransient = false) where T : class;
        void SetCache<T>(IPtfkSession owner, String cacheID, T value) where T : class;
    }

}
