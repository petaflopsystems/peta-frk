using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.POCO
{
    public class Role : IRole
    {
        public string Name { get; set; }

        public string ID { get; set; }

        public IDepartment Department { get; set; }
    }
}
