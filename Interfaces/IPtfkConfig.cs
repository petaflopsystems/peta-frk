using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.Interfaces
{
    public interface IPtfkConfig
    {
        long Id { get; set; }
        string JsonContent { get; set; }
        string ProcessedDate { get; set; }
        string ProcessedTables { get; set; }
        string UserLog { get; set; }
        string BusinessProcess { get; set; }
    }
}
