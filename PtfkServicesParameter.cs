using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework
{
    public class PtfkServicesParameter
    {
        public IConfiguration Configuration { internal get; set; }
        public IWebHostEnvironment WebHostEnvironment { internal get; set; } = null;
        public ILogger Logger { internal get; set; } = null;

        internal IPtfkEntity ConsumerClass { get; private set; }
        public void Consumer<T>() where T : IPtfkEntity, new()
        {
            this.ConsumerClass = new T();
        }
    }
}
