using System.Threading.Tasks;

namespace Petaframework
{
    public abstract class BaseWorkflow
    {
        internal static bool HasResult { get; private set; } = false;
        internal static Task<object> BusinessClassLog { private get; set; }

        internal static Interfaces.IPtfkLog ClassLog { get; set; }

        internal static object GetBusinessClassLogResult()
        {            
            var r = BusinessClassLog?.Result;
            HasResult = BusinessClassLog!=null && BusinessClassLog.IsCompleted;
            return r;
        }
    }
}