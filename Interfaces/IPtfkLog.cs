using System;

namespace Petaframework.Interfaces
{
    public interface IPtfkLog
    {
        long Id { get; set; }
        string EntityName { get; set; }
        long EntityId { get; set; }
        string JsonOrigin { get; set; }
        string JsonChange { get; set; }
        DateTime Date { get; set; }
        string LoginChange { get; set; }
        string LogType { get; set; }
        long? ProcessTaskId { get; set; }
        bool OnEvent { get; set; }
        bool End { get; set; }
        string DelegateTo { get; set; }
        string Role { get; set; }
        string ReadBy { get; set; }
    }
}
