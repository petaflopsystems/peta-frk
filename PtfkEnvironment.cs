using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Petaframework;
using Petaframework.Interfaces;
using PetaframeworkStd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.Enums;

namespace Petaframework
{
    internal class PtfkEnvironment
    {
        public static PtfkEnvironment CurrentEnvironment;

        public IWebHostEnvironment WebHostEnvironment { get; private set; }
        public IConfiguration Configuration { get; private set; }

        private IPtfkDbContext _PtfkDbContext = null;
        public IPtfkDbContext PtfkDbContext
        {
            get
            {
                if (_PtfkDbContext == null)
                    throw new PtfkException(PtfkException.ExceptionCode.PtfkDbContextNotFound, "IPtfkDbContext instance not found!");
                return _PtfkDbContext;
            }
            private set { _PtfkDbContext = value; }
        }
        public IPtfkLog LogClass { get; private set; }
        public IPtfkWorker WorkerClass { get; private set; }
        public IPtfkMedia MediaClass { get; private set; }
        public IPtfkEntityJoin EntityJoinClass { get; private set; }

        internal IPtfkEntity ConsumerClass { get; set; }

        internal Logger Log { get; set; }
        internal ILogger Logger { get; set; }
        public EnvironmentStatus Status
        {
            get
            {
                try
                {
                    var s = Strict.ConfigurationManager.Builder().GetValue<bool>(Constants.AppSettings.MaintenanceMode, false);
                    if (s)
                        return EnvironmentStatus.MaintenanceMode;
                    else
                        return EnvironmentStatus.Online;
                }
                catch
                {
                    return EnvironmentStatus.Offline;
                }
            }
        }

        internal void AddDbContext(IPtfkDbContext context)
        {
            this.PtfkDbContext = context;
        }
        internal void AddLogClass(IPtfkLog logClass)
        {
            this.LogClass = logClass;
        }
        internal void AddWorkerClass(IPtfkWorker workerClass)
        {
            this.WorkerClass = workerClass;
        }
        internal void AddMediaClass(IPtfkMedia mediaClass)
        {
            this.MediaClass = mediaClass;
        }
        internal void AddEntityJoinClass(IPtfkEntityJoin entityJoinClass)
        {
            this.EntityJoinClass = entityJoinClass;
        }

        public PtfkEnvironment(IConfiguration configuration, IWebHostEnvironment env = null, ILogger logger = null)
        {
            WebHostEnvironment = env;
            Configuration = configuration;
            Log = new Logger(logger);
            Logger = logger;
        }

        internal bool SetMaintenanceMode(bool newValue)
        {
            var ok = Strict.ConfigurationManager.AddOrUpdateAppSetting(Constants.AppSettings.MaintenanceMode, newValue);

            return Status == EnvironmentStatus.MaintenanceMode;
        }

        internal bool HasPtfkDbContext()
        {
            try
            {
                return _PtfkDbContext != null;

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    public class Logger
    {

        private ILogger _Logger { get; set; }
        public Logger(ILogger logger) { _Logger = logger; }
        public void Information(string message, params string[] args)
        {
            try
            {
                this._Logger.LogInformation(message, args);
                PtfkConsole.WriteLine(message, args);
            }
            catch (Exception e)
            {
                PtfkConsole.WriteError(e.Message);
            }
        }

        public void Warning(string message, params string[] args)
        {
            this._Logger.LogWarning(message, args);
            PtfkConsole.WriteLine(message, args);
        }

        public void Debug(string message, params string[] args)
        {
            this._Logger.LogDebug(message, args);
            PtfkConsole.WriteLine(message, args);
        }

        public void Critical(string message, params string[] args)
        {
            this._Logger.LogCritical(message, args);
            PtfkConsole.WriteLine(message, args);
        }

        public void Error(string message, params string[] args)
        {
            this._Logger.LogError(message, args);
            PtfkConsole.WriteLine(message, args);
        }
    }
}
