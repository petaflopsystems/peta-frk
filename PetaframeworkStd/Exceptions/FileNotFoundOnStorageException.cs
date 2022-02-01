using System;
using System.Collections.Generic;
using System.Text;

namespace PetaframeworkStd.Exceptions
{
    public class FileNotFoundOnStorageException : Exception
    {
        public FileNotFoundOnStorageException(string message) : base(message)
        {
        }
    }
}


