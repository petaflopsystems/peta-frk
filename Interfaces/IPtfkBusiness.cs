using PetaframeworkStd.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Petaframework.Interfaces
{
    public interface IPtfkBusiness<T>
    {
        Task<T> Save(T entity);

        Task<T> Read(long id);

        Task<T> Update(T entity);

        Task Delete(long id);

        IQueryable<T> List();

        IQueryable<T> List(IPtfkSession session);

        IQueryable<T> List(T entity);
        
        IPtfkConfig GetConfig(T entity);

        IPtfkConfig GetConfig(string entityName);

        IPtfkSession Session { get; set; }

        IQueryable<T> ListAll();

        PtfkFilter FilterEntities(PtfkFilter filterParam);

        System.Collections.Generic.List<IPtfkLog> ListLog(IPtfkForm entity);

        /// <summary>
        /// Flag to limit the data return only for current session. This flag does not apply to administrator access.
        /// </summary>
        bool BusinessRestrictionsBySession { set; }
        bool GetBusinessRestrictionsBySession();
    }
}
