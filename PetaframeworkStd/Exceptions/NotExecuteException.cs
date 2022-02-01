using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace PetaframeworkStd.Exceptions
{
    public class NotExecuteException : DbException
    {
        public NotExecuteException(string message) : base(message)
        {
        }
    }
}
