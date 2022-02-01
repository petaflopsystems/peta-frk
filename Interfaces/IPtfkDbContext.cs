using Microsoft.EntityFrameworkCore.Metadata;

namespace Petaframework.Interfaces
{
    public interface IPtfkDbContext
    {        
        IModel Model { get; }
    }    
}
