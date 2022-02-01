using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.POCO
{
    public class SessionEntity : IPtfkSession
    {
        private IPtfkSession _Current;

        public IPtfkSession Current
        {
            get
            {
                return this._Current == null ? (IPtfkSession)this : this._Current;
            }
            private set
            {
                this._Current = value;
            }
        }

        public string ID { get; set; }

        public string Login { get; set; } = "anonimous";

        public string IdToken { get; set; }

        public string AccessToken { get; set; }

        public bool IsAdmin { get; set; }

        public List<string> AnothersPermissionsLogins { get; set; } = new List<string>();

        public Dictionary<string, object> Bag { get; set; }

        public IDepartment Department { get; set; }

        public IEnumerable<IRole> Roles { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public bool IsAnonimousUser()
        {
            return Login.Equals("anonimous");
        }

        public void SetCurrentInstance(IPtfkSession _owner)
        {
            this.Current = _owner;
            this.Current.SetOwnerBag();
        }
    }
}
