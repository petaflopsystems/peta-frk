using System;
using System.Collections.Generic;
using System.Text;

namespace PetaframeworkStd.Exceptions
{
    public class NotExistsProcessTaskExpection : Exception
    {
        public NotExistsProcessTaskExpection(String taskID) : base(taskID)
        {
            this.Message = taskID;
        }
        private const String _msgPattern = "Task with id {0} not exists!";
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
