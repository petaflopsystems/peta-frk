using System;
using System.Collections.Generic;
using System.Text;

namespace PetaframeworkStd.Interfaces
{
    public interface IConsumable<out ReturnedType>
    {
        ReturnedType Consume(IServiceParameter parameter);
    }
}
