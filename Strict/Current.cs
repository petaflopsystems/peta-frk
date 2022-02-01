using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.Strict
{
    internal class Current
    {
        internal static Dictionary<string, Dictionary<string, object>> Session { get; set; }
    }
}
