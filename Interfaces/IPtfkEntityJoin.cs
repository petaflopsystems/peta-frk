using PetaframeworkStd.Interfaces;
using System;

namespace Petaframework.Interfaces
{
    public interface IPtfkEntityJoin : IEntity, IPtfkMortal, ICloneable
    {
        String EntityFrom { get; set; }
        String PropertyFrom { get; set; }
        long EntityFromId { get; set; }
        String EntityTo { get; set; }
        long EntityToId { get; set; }
    }
}
