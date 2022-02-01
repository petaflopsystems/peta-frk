using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework.Interfaces
{
    public interface IUserManager
    {
        IPtfkSession GetUserById(String id);
    }

}
