using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework
{
    public class Enums
    {
        public enum LogType
        {
            Delete,
            Update,
            Create
        }

        public enum EnvironmentStatus
        {
            Online,
            MaintenanceMode,
            Offline
        }

        public enum Log
        {
            Info,
            Warning,
            Error,
            Trace,
            Critical
        }
    }
}
