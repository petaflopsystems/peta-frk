using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;

namespace Petaframework
{
    internal class PtfkEntityJoined : IPtfkEntityJoin
    {
        public string EntityFrom { get; set; }
        public long EntityFromId { get; set; }
        public string EntityTo { get; set; }
        public long EntityToId { get; set; }
        public long Id { get; set; }

        public string ClassName => this.GetType().Name;

        public string IsDeath { get; set; }

        internal static object BusinessClassMedia { get; set; }
        public string PropertyFrom { get; set; }

        public ILogger Logger => null;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
