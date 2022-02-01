using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.POCO
{
    public class Department : IDepartment
    {
        public string Name { get; set; }

        public string ID { get; set; }

        public string Boss { get; set; }

        public string BossID { get; set; }

        public IEnumerable<IDepartment> DepartmentalHierarchy { get; set; } = new List<Department>();

        public string BossMail { get; set; }
    }
}
