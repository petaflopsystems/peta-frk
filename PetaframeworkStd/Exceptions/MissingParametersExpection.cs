using System;

namespace PetaframeworkStd.Exceptions
{
    public class MissingParametersExpection : Exception
    {
        public MissingParametersExpection(string message) : base(message)
        {
        }
    }
}
