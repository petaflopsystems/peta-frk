using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;

namespace Petaframework
{
    public class PtfkException : Exception
    {
        public PtfkException() : base() { }
        public PtfkException(string msg) : base(msg) { }
        public PtfkException(string msg, Exception innerException) : base(msg, innerException) { }
        public PtfkException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public PtfkException(ExceptionCode code, string msg) : base(msg)
        {
            this.Code = ((int)code).ToString();
        }

        public override string Message
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(this.Code))
                    return String.Concat("[", this.Code, "] ", base.Message);
                return base.Message;
            }
        }


        private static Dictionary<String, Exception> _LastOccurrence;
        public static Exception GetLastOccurrence(IPtfkSession session)
        {
            if (_LastOccurrence == null)
                _LastOccurrence = new Dictionary<string, Exception>();
            if (!_LastOccurrence.ContainsKey(session.Login))
                _LastOccurrence.Add(session.Login, null);
            return _LastOccurrence[session.Login];
        }
        internal static void SetLastOccurrence(IPtfkSession session, Exception e)
        {
            if (session == null || String.IsNullOrWhiteSpace(session.Login))
                return;
            if (_LastOccurrence == null)
                _LastOccurrence = new Dictionary<string, Exception>();
            if (!_LastOccurrence.ContainsKey(session.Login))
                _LastOccurrence.Add(session.Login, e);
            else
                _LastOccurrence[session.Login] = e;
        }

        public string Code { get; internal set; }

        public enum ExceptionCode
        {            
            NotAuthorized = 401,
            /// <summary>
            /// 700 - 899 -> Petaframework general internal exceptions 
            /// </summary>
            PtfkDbContextNotFound = 701,
            PtfkSystemOffline = 702,
            PtfkSystemUnderMaintenance = 703,
            PtfkUniqueValueInfringed = 704,
            /// <summary>
            /// 900 - 999 -> Workflow exceptions 
            /// </summary>
            TaskByIdNotFound = 901,
        }
    }
}
