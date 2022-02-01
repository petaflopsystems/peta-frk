using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.Interfaces
{
    public interface IPtfkWorker
    {
        String Entity { get; set; }
        String Login { get; set; }
        DateTime Date { get; set; }
        DateTime Creation { get; set; }
        String Tid { get; set; }
        String Task { get; set; }
        bool? Event { get; set; }
        bool? End { get; set; }
        string DelegateTo { get; set; }
        string Type { get; set; }
        string Script { get; set; }
        long Id { get; set; }
        string Creator { get; set; }

        string UId { get; set; }
    }
}
