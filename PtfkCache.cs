using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Petaframework
{
    public class PtfkCache : IPtfkCache
    {
        private static Timer _timer;
        private static Timer _timerGlobal;
        private static void Run(object state)
        {
            Initialized = true;

            _Running = true;

            //var lstToRemove = new List<String>();
            ////lock (UserCache)
            //{
            //    foreach (var item in UserCache)
            //    {
            //        if (UserCache[item.Key].ContainsKey(_LAST_TIME_ATTRNAME) && ((DateTime)UserCache[item.Key][_LAST_TIME_ATTRNAME]) < DateTime.Now.AddMinutes(-_CacheLifetimeMinutes))
            //            lstToRemove.Add(item.Key);
            //        else
            //            MakeUserCache(item.Key);
            //    }
            //    foreach (var item in lstToRemove)
            //    {
            //        UserCache.Remove(item);
            //    }
            //}
            if (UserCache != null && UserCache.Any())
                lock (UserCache)
                    Strict.ConfigurationManager.ReadOrWriteCollection<Dictionary<String, Dictionary<String, object>>>("User", CopyUser(), true);
            _Running = false;
        }

        private static void RunGlobal(object state)
        {
            InitializedGlobal = true;

            _RunningGlobal = true;

            if (GlobalCache != null && GlobalCache.Any())
                lock (GlobalCache)
                    Strict.ConfigurationManager.ReadOrWriteCollection<Dictionary<String, object>>("Global", CopyGlobal(), true);

            _RunningGlobal = false;
        }

        private static Dictionary<string, object> CopyGlobal()
        {
            Dictionary<string, object> c = new Dictionary<string, object>();
            foreach (var item in GlobalCache)
            {
                c[item.Key] = item.Value;
            }
            return c;
        }

        private static Dictionary<String, Dictionary<String, object>> CopyUser()
        {
            var c = new Dictionary<String, Dictionary<String, object>>();
            foreach (var item in UserCache)
            {
                c[item.Key] = item.Value;
            }
            return c;
        }

        public static bool Initialized { get; private set; }
        public static bool InitializedGlobal { get; private set; }
        static bool _Running { get; set; }
        static bool _RunningGlobal { get; set; }
        static Dictionary<String, Dictionary<String, object>> UserCache;
        static Dictionary<String, object> GlobalCache;
        static IPtfkConfig _Config;
        static int _CacheLifetimeMinutes { get; set; } = 20;

        internal static List<KeyValuePair<String, KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>>> WorkflowEntitiesLists;

        static bool _DisabledAll;
        static bool _DisabledList;

        const string _LAST_TIME_ATTRNAME = "_LAST_TIME_ATTRNAME";

        private static void Initialize(IPtfkSession owner, IPtfkConfig configClass, int cacheLifetimeMinutes)
        {
            InitializeParams();

            if (GlobalCache == null || UserCache == null)
            {
                if (GlobalCache == null)
                    GlobalCache = new Dictionary<string, object>();
                lock (GlobalCache)
                    GlobalCache = Strict.ConfigurationManager.ReadOrWriteCollection<Dictionary<String, object>>("Global", GlobalCache);
                if (GlobalCache == null)
                    GlobalCache = new Dictionary<string, object>();
                _CacheLifetimeMinutes = cacheLifetimeMinutes;
                _timerGlobal = new Timer(RunGlobal, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(_CacheLifetimeMinutes));
                //}
                //if (UserCache == null)
                //{     
                if (UserCache == null)
                    UserCache = new Dictionary<string, Dictionary<string, object>>();
                lock (UserCache)
                {
                    UserCache = Strict.ConfigurationManager.ReadOrWriteCollection<Dictionary<String, Dictionary<String, object>>>("User", UserCache);
                }
                if (UserCache == null)
                    UserCache = new Dictionary<string, Dictionary<string, object>>();
                _CacheLifetimeMinutes = cacheLifetimeMinutes;
                if (owner != null && !UserCache.ContainsKey(owner.Login))
                {
                    UserCache[owner.Login] = new Dictionary<string, object>();
                    UserCache[owner.Login][_LAST_TIME_ATTRNAME] = DateTime.Now;
                }
                _timer = new Timer(Run, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(_CacheLifetimeMinutes));

            }
            //_Config = configClass;
            if (_Config == null)
                SetConfig();
            //System.Threading.Tasks.Task.Factory.StartNew(() => MakeUserCache(owner.Login), System.Threading.Tasks.TaskCreationOptions.LongRunning);
        }

        private static void InitializeParams()
        {
            var en = GetAppConfig("Cache.Disabled");
            if (en == null)
                _DisabledAll = false;
            else
                _DisabledAll = (en != null && Tools.GetBoolValue(en));

            en = GetAppConfig("Cache.OnLists");
            if (en == null)
                _DisabledList = true;
            else
                _DisabledList = (en != null && !Tools.GetBoolValue(en));

            if (WorkflowEntitiesLists == null)
            {
                WorkflowEntitiesLists = new List<KeyValuePair<string, KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>>>();
            }
        }

        private static object GetAppConfig(string key)
        {
            try
            {
                return Petaframework.Strict.ConfigurationManager.CurrentConfiguration["AppSettings:" + key];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SetConfig()
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_Config != null)
                    break;
                foreach (var tp in ass.GetTypes())
                {
                    if (tp.GetInterfaces().Contains(typeof(IPtfkConfig)))
                    {
                        _Config = Activator.CreateInstance(tp) as IPtfkConfig;
                        break;
                    }
                }
            }
        }

        public T GetCache<T>(IPtfkSession owner, string cacheID, bool isTransient = false) where T : class
        {
            if (_DisabledAll)
                return null;
            if (GlobalCache == null)
                //GlobalCache = new Dictionary<string, object>();
                Initialize(owner, null, 2);
            if (GlobalCache.ContainsKey(cacheID))
                try
                {
                    return (T)GlobalCache[cacheID];
                }
                catch (Exception ex)
                {
                    try
                    {
                        var fromJson = Tools.FromJson<T>(GlobalCache[cacheID].ToString());
                        var c = isTransient && fromJson == null ? null : fromJson;
                        if (c != null)
                            GlobalCache[cacheID] = c;
                        return c;
                    }
                    catch (Exception ex2)
                    {
                        return null;
                    }
                }

            if (UserCache == null)
                UserCache = new Dictionary<string, Dictionary<string, object>>();
            if (owner != null)
            {
                if (!UserCache.ContainsKey(owner.Login))
                    UserCache[owner.Login] = new Dictionary<string, object>();
                if (UserCache.ContainsKey(owner.Login) && UserCache[owner.Login].ContainsKey(cacheID))
                    try
                    {
                        return (T)UserCache[owner.Login][cacheID];
                    }
                    catch (Exception)
                    {
                        try
                        {
                            var c = isTransient ? null : Tools.FromJson<T>(UserCache[owner.Login][cacheID].ToString());
                            if (c != null)
                                UserCache[owner.Login][cacheID] = c;
                            return c;
                        }
                        catch (Exception ex2)
                        {
                            return null;
                        }
                    }
            }
            return null;
        }

        public void SetCache<T>(IPtfkSession owner, string cacheID, T value) where T : class
        {
            if (owner == null)//GlobalCache
            {
                if (GlobalCache == null)
                {
                    Initialize(owner, null, 2);
                }
                GlobalCache[cacheID] = Tools.ToJson(value ?? new object(), true);
            }
            else
            {
                if (UserCache == null)
                    UserCache = new Dictionary<string, Dictionary<string, object>>();
                if (!UserCache.ContainsKey(owner.Login))
                    UserCache[owner.Login] = new Dictionary<string, object>();
                UserCache[owner.Login][cacheID] = Tools.ToJson(value, true);
            }
        }

        internal static PtfkFormStruct GetOrSetWorkflowEntity<T>(T entity, bool setter = false, PtfkFormStruct form = null) where T : IPtfkForm, IPtfkEntity
        {

            if (entity == null || entity.CurrentWorkflow == null || !entity.CurrentWorkflow.HasBusinessProcess() || _DisabledAll)
            {
                return null;
            }
            var master = new PtfkCache();
            PtfkWorkflow<T>.GetOrSetWorkflowBusiness(entity);//Starts/Set Workflow business class
            var name = String.Concat(entity?.ClassName, "-ALL");
            var e = master.GetCache<PtfkFormStruct>(null, name);
            if (form == null)
                return e != null ? e : RunWorkflowEntity(entity, name, master);
            else
                if (setter && form.html[0].Html.Count() > 1)
                master.SetCache<PtfkFormStruct>(null, name, form);
            return master.GetCache<PtfkFormStruct>(null, name);
        }

        private static PtfkFormStruct RunWorkflowEntity<T>(T entity, string name, PtfkCache master) where T : IPtfkForm, IPtfkEntity
        {
            try
            {
                var f = new PtfkFormStruct { ID = entity.Id, url = "http://localhost", action = TypeDef.Action.READ.ToString() };

                var config = new Petaframework.PageConfig(entity.ClassName, f, entity.Owner);
                var gen = new PtfkGen(new PageConfig(entity.ClassName, f, entity.Owner));
                gen.CurrentPageConfig.SkipCache = true;
                entity.GetType().GetProperty(nameof(PtfkForm<T>.CurrentGenerator)).SetValue(entity, gen);

                PtfkFormStruct e = gen.GetFormObject(entity);
                gen.CurrentPageConfig.SkipCache = false;
                e.userRoles = null;
                e.ID = 0;
                master.SetCache<PtfkFormStruct>(null, name, e);
                return e;
            }
            catch (PtfkException pex)
            {
                return null;
            }
            catch (Exception ex)
            {
                entity.Logger?.Log(Microsoft.Extensions.Logging.LogLevel.Debug, new Microsoft.Extensions.Logging.EventId(0, nameof(RunWorkflowStage)), null, new { Exp = ex, ExpMessage = String.Format("Entity Type: {0} > ID: {1}", entity.ClassName, entity.Id) });
                return null;
            }
        }

        internal static PtfkFormStruct GetOrSetWorkflowStage<T>(T entity, bool setter = false, PtfkFormStruct form = null) where T : IPtfkForm, IPtfkEntity
        {
            if (entity == null || entity.CurrentWorkflow == null || !entity.CurrentWorkflow.HasBusinessProcess() || _DisabledAll)
            {
                return null;
            }
            var master = new PtfkCache();

            PtfkWorkflow<T>.GetOrSetWorkflowBusiness(entity);//Starts/Set Workflow business class
            var name = String.Concat(entity?.ClassName, "-SID-", entity?.CurrentWorkflow?.GetCurrentTask().ID);
            var e = master.GetCache<PtfkFormStruct>(null, name);
            if (setter && (e == null || e.html[0].Html.Count() <= 1))
            {
                if (form == null || form?.html?[0].Html.Count <= 1)
                    master.SetCache<PtfkFormStruct>(null, name, master.RunWorkflowStage(entity));

                else
                {
                    form.userRoles = null;
                    master.SetCache<PtfkFormStruct>(null, name, form);
                }

                e = master.GetCache<PtfkFormStruct>(null, name);
            }
            return e;
        }

        private PtfkFormStruct RunWorkflowStage<T>(T entity) where T : IPtfkForm, IPtfkEntity
        {
            try
            {
                entity.IsCache = true;
                var f = new PtfkFormStruct { ID = entity.Id, url = "http://localhost", action = TypeDef.Action.READ.ToString() };

                var e = Tools.FromJson<PtfkFormStruct>(entity.Run(f));
                e.userRoles = null;
                e.ID = 0;

                return e;
            }
            catch (PtfkException pex)
            {
                return null;
            }
            catch (Exception ex)
            {
                entity.Logger?.Log(Microsoft.Extensions.Logging.LogLevel.Debug, new Microsoft.Extensions.Logging.EventId(0, nameof(RunWorkflowStage)), null, new { Exp = ex, ExpMessage = String.Format("Entity Type: {0} > ID: {1}", entity.ClassName, entity.Id) });
                return null;
            }
        }

        internal static List<ListItem> GetOrSetSelectOptions(IPtfkSession owner, String className, List<ListItem> value = null)
        {
            InitializeParams();

            if (_DisabledAll && value != null)
                return value;
            var master = new PtfkCache();
            if (_DisabledList && value != null)
            {
                master.SetCache<List<ListItem>>(owner, nameof(ListItem) + className, value);
                return value;
            }
            var c = master.GetCache<List<ListItem>>(owner, nameof(ListItem) + className);
            if (c != null)
                return c;
            if (value != null)
            {
                master.SetCache<List<ListItem>>(owner, nameof(ListItem) + className, value);
                return value;
            }
            return null;
        }

        internal static List<IPtfkSession> GetOrSetWorkflowUsers(IPtfkSession owner)
        {
            var master = new PtfkCache();

            //TODO colocar outros tipo de bases de dados de usuário aqui se necessário

            var c = master.GetCache<List<IPtfkSession>>(null, nameof(IPtfkSession) + "_WorkflowUsers");
            if (c != null && c.Where(x => x.Login.Equals(owner.Login)).Any())
                return c;
            else
            {
                if (c == null)
                {
                    c = Strict.ConfigurationManager.ReadOrWriteUsers(owner);
                    c.Add(owner);
                    master.SetCache<List<IPtfkSession>>(null, nameof(IPtfkSession) + "_WorkflowUsers", c);
                }
            }
            return c;
        }

        private class InternalKeyValue<TKey, TValue>
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public InternalKeyValue(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
            }
        }

        public static R TransientValue<T, R>(string ID, T valueToCheck, Func<R> valueToSet)
        {
            var master = new PtfkCache();
            var val = master.GetCache<InternalKeyValue<T, R>>(null, "_TRANSIENT_" + ID, true);
            if (val == null)
                val = new InternalKeyValue<T, R>(default(T), default(R));

            if (!valueToCheck.Equals(val.Key))
            {
                var newValue = new InternalKeyValue<T, R>(valueToCheck, valueToSet.Invoke());
                master.SetCache<InternalKeyValue<T, R>>(null, "_TRANSIENT_" + ID, newValue);
                return Tools.FromJson<KeyValuePair<T, R>>(master.GetCache<String>(null, "_TRANSIENT_" + ID, true)).Value;
            }

            return val.Value;
        }

        internal static KeyValuePair<String, KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>> GetOrSetWorkflowEntitiesList(String ID, List<HtmlElement> list = null, List<IPtfkEntity> lstEntities = null)
        {
            lock (WorkflowEntitiesLists)
            {
                if (WorkflowEntitiesLists == null)
                {
                    WorkflowEntitiesLists = new List<KeyValuePair<String, KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>>>();
                    WorkflowEntitiesLists.Add(new KeyValuePair<string, KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>>(ID, new KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>(list, lstEntities)));
                    return WorkflowEntitiesLists.FirstOrDefault();
                }
                var e = WorkflowEntitiesLists.Where(x => x.Key.Equals(ID)).FirstOrDefault();
                if (list != null)
                {

                    if (!String.IsNullOrWhiteSpace(e.Key))
                    {
                        WorkflowEntitiesLists.Remove(e);
                    }
                    var elem = new KeyValuePair<string, KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>>(ID, new KeyValuePair<List<HtmlElement>, List<IPtfkEntity>>(list, lstEntities));
                    WorkflowEntitiesLists.Add(elem);
                    return elem;

                }
                return e;
            }
        }

        internal static T GetOrSet<T>(string keyName, Func<T> actionToUpdate, int minutesToRefresh = 5)
        {
            var master = new PtfkCache();
            var elem = master.GetCache<InternalKeyValue<DateTime, T>>(null, keyName);
            var dt = DateTime.Now;
            if (elem == null || ((dt - elem.Key).Minutes > minutesToRefresh))
            {
                elem = new InternalKeyValue<DateTime, T>(dt, actionToUpdate.Invoke());
                master.SetCache<InternalKeyValue<DateTime, T>>(null, keyName, elem);
            }
            return elem.Value;
        }
    }
}
