using System;

namespace PetaframeworkStd.Exceptions
{
    public class NotExistsCommandException : Exception
    {
        public NotExistsCommandException(String command) : base(command)
        {
            this.Message = command;
        }
        private const String _msgPattern = "Command {0} not exists!";
        private String _msg;
        public new String Message
        {
            get { return String.Format(_msgPattern, _msg); }
            private set
            {
                _msg = value;
            }
        }
    }
}
