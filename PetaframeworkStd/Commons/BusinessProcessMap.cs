using Newtonsoft.Json;
using PetaframeworkStd.BPMN;
using System;

namespace PetaframeworkStd.Commons
{
    public class BusinessProcessMap
    {
        public BusinessProcess BusinessProcess { get; set; }
        public String FileContent { get; set; }

        [JsonIgnore]
        public IBPMN Vendor { get; set; }

        private Type _vendor = typeof(BPMN_IO);
        [JsonIgnore]
        public Type VendorType
        {
            get
            {
                if (Vendor != null)
                {
                    _vendor = Vendor.GetType();
                }
                return _vendor;
            }
            set { _vendor = value; }
        }

        private string _vendorName;
        public string VendorName { get { return VendorType.Name; } set { _vendorName = VendorType.Name; } }

        public IBPMN GetBPMNFile()
        {
            try
            {
                var obj = Activator.CreateInstance(_vendor) as IBPMN;
                obj.FromVendorFormat(this.FileContent);
                return obj;
            }
            catch (Exception ex)
            {

                return null;
            }
        }
    }
}
