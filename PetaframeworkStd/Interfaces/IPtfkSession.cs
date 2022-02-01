using System.Collections.Generic;

namespace PetaframeworkStd.Interfaces
{
    public interface IPtfkSession
    {
        string Login { get; set; }

        string Name { get; set; }

        string Email { get; set; }

        bool IsAdmin { get; set; }

        string ID { get; set; }

        IPtfkSession Current { get; }

        void SetCurrentInstance(IPtfkSession owner);

        string IdToken { get; }

        string AccessToken { get; }

        Dictionary<string, object> Bag { get; set; }

        List<string> AnothersPermissionsLogins { get; }

        IDepartment Department { get; set; }

        IEnumerable<IRole> Roles { get; set; }

        bool IsAnonimousUser();
    }

    public interface IDepartment
    {
        string Name { get; }
        string ID { get; }
        string Boss { get; }
        string BossMail { get; }
        string BossID { get; }
        IEnumerable<IDepartment> DepartmentalHierarchy { get; set; }
    }

    public interface IRole
    {
        string Name { get; }
        string ID { get; }
        IDepartment Department { get; }
    }
}
