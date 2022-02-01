using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework
{
    public class NotAccessibleEntityException : Exception
    {

    }

    public class ViolatedEntityException : Exception
    {

    }

    public class UnsavedEntityException : Exception
    {

    }


    public class ConvertionException : Exception
    {
        public ConvertionException(String ElementToTryConvertion) : base(ElementToTryConvertion)
        {

        }
    }

}
