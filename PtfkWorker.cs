using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;
using PetaframeworkStd.Commons;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Petaframework
{
    public static class PtfkWorker
    {
        public static bool IsRunning { private set; get; } = false;
        public static object BusinessClassWorker { get; internal set; }
        /// <summary>
        /// Number of tasks on each Processor. Default Value: 2
        /// </summary>
        public static int ConcurrentProcessPerProcessor { get; set; } = 2;
        public static int IntervalToCheckInMinutes { get; set; } = 20;

        private static IPtfkSession _owner = new PrivatePtfkSession
        {
            Login = Constants.ServiceWorkerLogin
        };

        private static ILogger _logger;
        private static Timer _timer;
        private static IPtfkWorker _workerClass;

        private static void Start()
        {
            Type type = null;
            _logger.LogInformation("PtfkWorker started...");
            try
            {
                Tools.GetIBusinessWorkerClass(_workerClass.GetType().Assembly, _owner);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Not found {0} on assembly {1}.", nameof(IPtfkWorker), _workerClass.GetType().Assembly.FullName);
            }
            if (BusinessClassWorker == null)
            {
                List<Assembly> allAssemblies = new List<Assembly>();
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                foreach (string dll in Directory.GetFiles(path, "*.dll"))
                    try
                    {
                        Tools.GetIBusinessWorkerClass(Assembly.LoadFile(dll), _owner);
                        if (BusinessClassWorker != null)
                            break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Assembly [{0}] not loaded.", nameof(IPtfkWorker), dll);
                    }
            }

            _timer = new Timer(Run, null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(PtfkWorker.IntervalToCheckInMinutes));
        }

        internal static IQueryable<IPtfkWorker> ListAll(IPtfkWorker workerClass, IPtfkSession owner)
        {
            Tools.GetIBusinessWorkerClass(workerClass.GetType().Assembly, owner);
            return ListAll();
        }
        private static IQueryable<IPtfkWorker> ListAll()
        {
            var n = nameof(ServiceTask);
            var method = BusinessClassWorker.GetType().GetMethod(nameof(IPtfkBusiness<object>.ListAll));
            var lst = (from l in method.Invoke(BusinessClassWorker, null) as IQueryable<IPtfkWorker>
                       where l.Type.Equals(n)
                             && (!l.End.HasValue || !l.End.Value)
                       select l);
            return lst;

        }

        private static void Run(object state)
        {
            if (!IsRunning)
            {
                _logger.LogInformation("Running...");
                IsRunning = true;
                var concurrentProcess = Environment.ProcessorCount * ConcurrentProcessPerProcessor;
                try
                {
                    //var n = nameof(ServiceTask);
                    //var method = BusinessClassWorker.GetType().GetMethod(nameof(IPtfkBusiness<object>.ListAll));
                    var lst = ListAll().ToList();
                    var statics = GetStaticIds();
                    if (statics != null && statics.Any())
                        lst = lst.Where(x => statics.Contains(x.Id)).ToList();

                    _logger.LogInformation("Total count... " + lst.Count());
                    StringBuilder str = new StringBuilder();
                    lst.ToList().ForEach(x => str.Append(String.Concat(x.Id, "-", x.Tid, "|")));

                    _logger.LogInformation("Records to load: " + str.ToString());

                    PtfkForm<IPtfkForm> basicForm;
                    IPtfkBusiness<IPtfkForm> basicBusiness;
                    System.Threading.Tasks.Task<IPtfkForm> basicTask;
                    List<System.Threading.Tasks.Task> allTasks = new List<System.Threading.Tasks.Task>();
                    foreach (var item in lst)
                    {
                        var timeDiff = DateTime.Now - item.Date;
                        var scp = String.IsNullOrWhiteSpace(item.Script) || item.Script.ToLower().Equals("null") ? new PtfkTaskScript() : Tools.FromJson<PtfkTaskScript>(item.Script);
                        if (scp != null)
                        {
                            if (timeDiff >= scp.WaitFor)
                            {
                                _logger.LogInformation("Loading record " + String.Concat(item.Id, "-", item.Tid));

                                var iPeta = Tools.GetIPtfkEntityByClassName(_workerClass.GetType().Assembly, item.Entity, _owner, _logger);
                                var business = iPeta.GetType().GetProperty(nameof(basicForm.BusinessClass), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(iPeta);
                                var task = (System.Threading.Tasks.Task)business.GetType().GetMethod(nameof(basicBusiness.Read)).Invoke(business, new object[] { item.Id });
                                var obj = task.GetType().GetProperty(nameof(basicTask.Result))?.GetValue(task) as IPtfkForm;
                                obj.GetType().GetProperty(nameof(basicForm.Owner)).SetValue(obj, _owner);

                                allTasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() =>
                                    InvokeMethod(obj, item, _owner, iPeta)
                                ));

                                if (allTasks.Count() >= concurrentProcess)
                                {
                                    System.Threading.Tasks.Task.WaitAny(allTasks.ToArray());
                                    allTasks = allTasks.Where(x => !x.IsCompleted).ToList();
                                }

                            }
                        }
                    }
                    System.Threading.Tasks.Task.WaitAll(allTasks.ToArray());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on processing Service Task!");
                }

                IsRunning = false;
                _logger.LogInformation("bye!");
            }
        }

        private static void InvokeMethod(IPtfkForm obj, IPtfkWorker item, IPtfkSession owner, IPtfkEntity iPeta)
        {
            try
            {
                iPeta.GetType().GetMethod(nameof(PtfkForm<IPtfkForm>.RunAsService), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .Invoke(iPeta, new object[] { obj, new PageConfig(item.Entity, new PtfkGen(TypeDef.Action.UPDATE, _owner).GetFormObject(obj), _owner) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on processing Service Task [" + obj.ClassName + ":" + obj.Id + "]!");
            }
        }

        [Obsolete]
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UsePetaframeWorker(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, IPtfkWorker workerClass, ILogger logger, int ConcurrentProcessPerProcessor = 2, int intervalToCheck = 20)
        {
            CheckPtfkStructure();
            _workerClass = workerClass;
            _logger = logger;
            PtfkWorker.ConcurrentProcessPerProcessor = ConcurrentProcessPerProcessor;
            PtfkWorker.IntervalToCheckInMinutes = intervalToCheck;
            PtfkWorker.Start();

            return hostBuilder;
        }

        public static Microsoft.Extensions.Hosting.IHostBuilder UsePetaframeWorker(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder, IPtfkWorker workerClass, ILogger logger, int ConcurrentProcessPerProcessor = 2, int intervalToCheck = 20)
        {
            CheckPtfkStructure();
            _workerClass = workerClass;
            _logger = logger;
            PtfkWorker.ConcurrentProcessPerProcessor = ConcurrentProcessPerProcessor;
            PtfkWorker.IntervalToCheckInMinutes = intervalToCheck;
            PtfkWorker.Start();

            return hostBuilder;
        }


        private static long[] GetStaticIds()
        {
            try
            {
                var str = Petaframework.Strict.ConfigurationManager.CurrentConfiguration["AppConfiguration:Worker.StaticList.Ids"];
                var lst = str.Split(',', StringSplitOptions.RemoveEmptyEntries);
                return lst.Select(x => Convert.ToInt64(x)).ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }


        private static void CheckPtfkStructure()
        {
            var assemblyPath = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var fi = new FileInfo(Path.Combine(assemblyPath.Directory.FullName, "_Converters", "HtmlToPdfConverter.dll"));
            if (!fi.Exists)
                ErrorTable.Err017();
        }
    }
}
