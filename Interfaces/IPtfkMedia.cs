using PetaframeworkStd.Interfaces;
using System;

namespace Petaframework.Interfaces
{
    public interface IPtfkMedia : IEntity, IPtfkMortal
    {
        string Name { get; set; }
        string Extension { get; set; }
        long Size { get; set; }
        string Hash { get; set; }
        byte[] Bytes { get; set; }
        string Path { get; set; }        
        string EntityName { get; set; }
        long EntityId { get; set; }
        string EntityProperty { get; set; }
        string ExternalInfo { get; set; }
    }
}
