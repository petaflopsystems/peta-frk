using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;
using PetaframeworkStd.Commons;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using static Petaframework.Enums;
using static Petaframework.Strict.ConfigurationManager;

namespace Petaframework
{
    public abstract class PtfkWorkflow<T> : BaseWorkflow, IPtfkWorkflow<T> where T : IPtfkForm
    {
        public const string END_TASK_NAME = "#";

        protected delegate void PtfkEventHandler(PtfkEventArgs<ProcessTask> Task);
        protected event PtfkEventHandler RoutingEvent;

        protected PtfkFormStruct CurrStruct { get; private set; }
        protected readonly IPtfkSession CurrSession;
        protected readonly T CurrEntity;
        private const string END_TASK_ID = "-1";
        private System.Threading.Tasks.Task<PetaframeworkStd.BPMN.IBPMN> CurrBPMNFile;
        private System.Threading.Tasks.Task<BusinessProcess> CurrBusinessProcess;
        private Permission[] CurrPermissions;
        private IPtfkBusiness<T> BusinessClass;
        //private System.Threading.Tasks.Task _OwnerSummary;

        private bool _ReadOnlyFlag = false;

        private bool _IsNewInstance = false;

        protected virtual void SetBusinessProcess(System.Threading.Tasks.Task<BusinessProcess> businessProcess)
        {
            CurrBusinessProcess = businessProcess;
            CurrPermissions = Strict.ConfigurationManager.GetPermissions(businessProcess.Result.Entity);
            CurrSession.SetOwnerBag();
        }

        public PtfkWorkflow(PtfkFormStruct form, IPtfkSession session, IPtfkForm entity)
        {
            CurrStruct = form;
            CurrEntity = (T)entity;
            CurrSession = session;
            CurrBusinessProcess = GetBusinessProcess();
            CurrPermissions = Strict.ConfigurationManager.GetPermissions(CurrEntity.GetType().Name);
            CurrSession.SetOwnerBag();
            GetLogs(0);
            RoutingEvent += OnTaskRouting;

        }

        public PtfkWorkflow(IPtfkSession session, IPtfkForm entity, IPtfkBusiness<T> businessClass)
        {
            CurrEntity = (T)entity;
            CurrSession = session;
            BusinessClass = businessClass;
            CurrBusinessProcess = GetBusinessProcess();
            CurrPermissions = Strict.ConfigurationManager.GetPermissions(typeof(T).Name);
            CurrSession.SetOwnerBag();
            GetLogs(0);
            RoutingEvent += OnTaskRouting;
        }

        static string _lockerBusinessWorkflow = "";
        /// <summary>
        /// Starts/Set Workflow business class (once time on first call)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static Type GetOrSetWorkflowBusiness(T entity)
        {
            if (entity == null)
                return null;
            var name = nameof(IPtfkWorkflow<T>) + entity.GetType().Name;
            var master = new PtfkCache();
            Type WorkflowBusinessConcreteClass = master.GetCache<Type>(null, name);
            if (WorkflowBusinessConcreteClass == null)
            {
                Type concreteClass = null;
                lock (_lockerBusinessWorkflow)
                {
                    if (WorkflowBusinessConcreteClass != null)
                        return WorkflowBusinessConcreteClass;
                    var type = typeof(IPtfkWorkflow<T>);
                    foreach (var item in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetReferencedAssemblies().Where(y => y.FullName.Equals(typeof(Tools).Assembly.FullName)).Any()))
                    {
                        concreteClass = item.GetExportedTypes().Where(x => x.GetInterfaces().Contains(type)).FirstOrDefault();
                        if (concreteClass != null || WorkflowBusinessConcreteClass != null)
                            break;
                    }
                    if (concreteClass == null)
                    {
                        ErrorTable.Err015();
                    }
                }
                master.SetCache<Type>(null, name, concreteClass);
                return concreteClass;
            }
            return WorkflowBusinessConcreteClass;
        }

        public virtual void OnTaskRouting(PtfkEventArgs<ProcessTask> Task)
        {
            var handler = RoutingEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        private List<IPtfkLog> CurrLogs;
        private List<IPtfkLog> GetLogs(long _id = 0)
        {
            try
            {
                //if (_id == 0 && CurrLogs != null)
                //    return CurrLogs;

                var id = _id == 0 ? (CurrEntity as IPtfkEntity).Id : _id;
                if (id == 0)
                    return new List<IPtfkLog>();

                if (CurrLogs != null && CurrLogs.FirstOrDefault().EntityId == id)
                    return CurrLogs;

                var business = GetBusinessClassLogResult();
                var method = business.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.ListLog)) && x.GetParameters().Length == 1).FirstOrDefault();
                var lst = (from l in method.Invoke(business, new object[] { CurrEntity }) as List<IPtfkLog>
                           where l.EntityName.Equals(CurrBusinessProcess.Result?.Entity) &&
                             l.EntityId == id
                           orderby l.Id
                           select l).ToList();
                if (_id == 0)
                    CurrLogs = lst;
                return lst;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool DropLastStage()
        {
            var id = (CurrEntity as IPtfkEntity).Id;
            var business = GetBusinessClassLogResult();
            var method = business.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.ListLog)) && x.GetParameters().Length == 1).FirstOrDefault();
            var tp = LogType.Create.ToString();
            var lst = (from l in method.Invoke(business, new object[] { CurrEntity }) as List<IPtfkLog>
                       where l.EntityName.Equals(CurrBusinessProcess.Result.Entity) &&
                         l.EntityId == id
                         && l.LogType != tp
                       select l).ToList();

            method = business.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.Delete)) && x.GetParameters().Length == 1).FirstOrDefault();
            try
            {
                var ret = method.Invoke(business, new object[] { lst.LastOrDefault().Id }) as System.Threading.Tasks.Task;
                ret.Wait();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async System.Threading.Tasks.Task<bool> MarkAsRead()
        {
            var id = (CurrEntity as IPtfkEntity).Id;
            var business = GetBusinessClassLogResult();
            var method = business.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.Read)) && x.GetParameters().Length == 1).FirstOrDefault();
            var tp = LogType.Create.ToString();
            IPtfkLog ptfkLog = GetLogs().LastOrDefault<IPtfkLog>();
            var elems = Tools.FromJson<List<ReadByStruct>>(ptfkLog?.ReadBy) ?? new List<ReadByStruct>();
            bool done = true;
            if (ptfkLog != null && elems.Where(x => x.Login.Equals(CurrSession.Login)).Count() == 0)
            {
                elems.Add(new ReadByStruct { Date = DateTime.Now, Login = CurrSession.Login });
                ptfkLog.ReadBy = Tools.ToJson(elems);

                method = business.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.Save)) && x.GetParameters().Length == 1).FirstOrDefault();
                try
                {
                    await (method.Invoke(business, new object[] { ptfkLog }) as System.Threading.Tasks.Task);
                    done = true;
                }
                catch (Exception ex)
                {
                    done = false;
                }
            }

            return done;
        }

        //private class BusinessProcessEngine
        //{
        //    public BusinessProcess BusinessProcess { get; set; }
        //}

        private async System.Threading.Tasks.Task<BusinessProcess> GetBusinessProcess()
        {
            try
            {
                var bAux = BusinessClass;
                try
                {
                    bAux = Activator.CreateInstance(bAux.GetType(), CurrSession) as IPtfkBusiness<T>;
                }
                catch (Exception) { }
                var config = bAux.GetConfig((T)CurrEntity);
                //config.JsonContent
                var bpList = Tools.FromJson<List<BusinessProcessMap>>(config.BusinessProcess);
                var ret = await System.Threading.Tasks.Task.Run(() => BusinessProcessMap(bpList.Where(x => x.BusinessProcess.Entity.Equals(typeof(T).Name)).FirstOrDefault()));
                //_OwnerSummary = System.Threading.Tasks.Task.Factory.StartNew(() => FillOwnerSummary(GetBusinessClassLogResult()));
                return ret;
                //return null;
            }
            catch (Exception ex)
            {
                if (CurrEntity.Logger != null)
                {
                    CurrEntity.Logger.LogDebug(Tools.ToJsonPreserveReferences(new { Type = "Error", Exception = ex }));
                }
            }
            return null;
        }

        private BusinessProcess BusinessProcessMap(BusinessProcessMap businessProcessMap)
        {
            CurrBPMNFile = System.Threading.Tasks.Task.Factory.StartNew(() => businessProcessMap?.GetBPMNFile());
            var bp = businessProcessMap?.BusinessProcess;
            //var perms = GetPermissions(bp.Entity).ToList();
            //foreach (var t in bp.Tasks)
            //{
            //    foreach (var c in perms)
            //    {
            //        foreach (var x in t.Profiles)
            //        {
            //            if ((!String.IsNullOrWhiteSpace(c.ProfileID) && c.ProfileID.Equals(x.ID))
            //                || (!String.IsNullOrWhiteSpace(c.Profile) && c.Profile.Equals(x.Name)))
            //            {
            //                t.NextDelegation = c.NextDelegation?.ToList();
            //                break;

            //            }
            //        }
            //        if (t.NextDelegation != null && t.NextDelegation.Any())
            //            break;
            //    }
            //    // var currPermission = CurrPermissions.Where(c => t.Profiles.Where(x => (!String.IsNullOrWhiteSpace(c.ProfileID) && !String.IsNullOrWhiteSpace(x.ID) && c.ProfileID.Equals(x.ID)) || (!String.IsNullOrWhiteSpace(c.Profile) && !String.IsNullOrWhiteSpace(x.Name) && c.Profile.Equals(x.Name))).Any()).FirstOrDefault();
            //    //t.DelegateTo = currPermission?.DelegateTo?.ToList();

            //}
            return bp;
        }

        public IEnumerable<ProcessTask> GetTraceRoute()
        {
            var lst = GetLogs();
            if (lst != null)
            {
                var first = CurrBusinessProcess.Result.GetFirstTask();
                if (!lst.Where(x => x.ProcessTaskId.HasValue && x.ProcessTaskId.Value.ToString().Equals(first.ID)).Any())
                    yield return first;
                var ids = lst.Where(x => x.ProcessTaskId.HasValue).OrderBy(x => x.Date).Select(x => x.ProcessTaskId.Value);
                foreach (var item in ids)
                    yield return GetTask(item.ToString());
            }
        }

        private bool IsTheEnd = false;
        public ProcessTask GetCurrentTask()
        {
            if (CurrBusinessProcess.Result == null)
                return (ProcessTask)null;
            List<IPtfkLog> logs = CurrLogs ?? GetLogs(0);
            if (logs == null)
            {
                _IsNewInstance = true;
                return CurrBusinessProcess.Result.GetFirstTask();
            }
            IPtfkLog ptfkLog = logs.LastOrDefault<IPtfkLog>();
            if (ptfkLog != null && ptfkLog.End)
            {
                IsTheEnd = true;
                GetEndTask((ProcessTask)null, ptfkLog.Date);
            }
            string str;
            if (ptfkLog == null)
            {
                str = (string)null;
            }
            else
            {
                long? processTaskId = ptfkLog.ProcessTaskId;
                ref long? local = ref processTaskId;
                str = local.HasValue ? local.GetValueOrDefault().ToString() : (string)null;
            }
            string taskID = str;
            if (string.IsNullOrWhiteSpace(taskID))
                return CurrBusinessProcess.Result.Tasks.FirstOrDefault<ProcessTask>();
            ProcessTask task = GetTask(taskID);
            task.SetStartDate(ptfkLog.Date);
            task.SetReadBy(Tools.FromJson<List<ReadByStruct>>(ptfkLog.ReadBy));
            return task;
        }

        public bool Finished(long entityID = 0)
        {
            if (entityID > 0L)
                GetCurrentTask(entityID);
            else
                GetCurrentTask();
            return IsTheEnd;
        }

        public ProcessTask GetBeforeTask()
        {
            if (CurrBusinessProcess.Result == null)
                return null;

            IPtfkLog[] lst = GetLogs().ToArray();

            if (lst == null || lst.Count() <= 1)
            {
                return CurrBusinessProcess.Result.GetFirstTask();
            }
            int idx = lst.Count() - 1;
            var beforeTask = lst[idx];//Get Before
            var currTaskID = beforeTask?.ProcessTaskId?.ToString();
            var currTask = GetCurrentTask();
            while (!String.IsNullOrWhiteSpace(currTaskID) && currTaskID.Equals(currTask.ID))
            {
                try
                {
                    idx--;
                    beforeTask = lst[idx];
                    currTaskID = beforeTask?.ProcessTaskId?.ToString();
                }
                catch (Exception)
                {
                    return CurrBusinessProcess.Result.GetFirstTask();
                }
            }

            if (String.IsNullOrWhiteSpace(currTaskID))
                if (currTask != null && currTask.ID == END_TASK_ID)
                    try
                    {
                        var lID = lst[lst.Count() - 2]?.ProcessTaskId?.ToString();
                        if (!String.IsNullOrWhiteSpace(lID))
                            return GetTask(lID);//On final Task, return before task
                        else
                            return CurrBusinessProcess.Result.GetFirstTask();
                    }
                    catch (Exception ex)
                    {
                        return CurrBusinessProcess.Result.GetFirstTask();
                    }
                else
                    return CurrBusinessProcess.Result.GetFirstTask();
            else
            {
                var task = GetTask(currTaskID);
                return task;
            }
        }

        public bool CheckPermissionOnCurrentTask(long entityID)
        {
            try
            {
                var t = GetCurrentTask(entityID);
                if (t.ID == END_TASK_ID)
                    return false;
                if (entityID > 0 && t.IsFirstTask())//Confirmation that actually is first
                    t = GetCurrentTask(entityID);
                CheckProfile(t);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private ProcessTask CheckProfile(ProcessTask processTask)
        {
            var listPermissions = CurrPermissions.ToList();
            if (processTask == null && listPermissions.Count == 0)
                Petaframework.ErrorTable.Err012(CurrSession.Login, CurrBusinessProcess.Result?.Name);
            else
            if (processTask == null && listPermissions.Any())
                return processTask;

            if (String.IsNullOrWhiteSpace(processTask.ID))
                return processTask;
            var enabledList = new List<string>();

            var isAdmin = listPermissions.Where(x => x.IsAdmin == true && (x.Profile.Equals("*") || x.EnabledTo.Contains(CurrSession.Login))).Any();
            if (isAdmin)
                return processTask;
            if (processTask.IsServiceTask())
                Petaframework.ErrorTable.Err013(CurrSession, String.Concat(processTask.Name, ":", CurrEntity?.Id));
            var lastLog = GetLogs()?.LastOrDefault();
            var departmentID = CurrSession.Department?.ID ?? "";
            var hasDepartment = !String.IsNullOrWhiteSpace(departmentID);
            var creator = CreatorID();
            creator ??= (processTask.IsFirstTask() ? CurrSession.Login : "");
            //creator ??= "";
            //departmentID ??= "";
            foreach (var item in processTask.Profiles)
            {
                foreach (var permission in listPermissions.Where(x => x.Profile.ToLower().Equals(item.Name.ToLower()) || item.ID.Equals(x.ProfileID)))
                {
                    if ((permission != null && permission.IsPrivate.HasValue && permission.IsPrivate.Value && CurrSession.Login.Equals(creator)) ||
                        (lastLog != null && lastLog.DelegateTo != null &&
                        (
                          (
                            lastLog.DelegateTo.Contains(Constants.TOKEN_USER_ID + CurrSession.Login) ||
                            lastLog.DelegateTo.Contains(CurrSession.Login)
                           )
                                 ||
                            (
                                    hasDepartment &&
                                        (
                                        lastLog.DelegateTo.Contains(Constants.TOKEN_DEPARTMENT_ID + departmentID) || lastLog.DelegateTo.Contains(departmentID)
                                        )
                            )
                         )
                         )
                      )
                        enabledList.AddRange(new string[] { CurrSession.Login });
                    else
                        enabledList.AddRange(permission.EnabledTo);
                }
            }
            if (!CurrEntity.GetCacheFlag())
            {
                if (listPermissions.Count == 0 || listPermissions.Where(x => processTask.Profiles.Where(i => x.Profile.Equals(i.Name) || i.ID.Equals(x.ProfileID)).Any()).Count() == 0)//Not found current task profiles
                    Petaframework.ErrorTable.Err011(processTask.Name, CurrEntity.GetType().Name);
                if (!enabledList.Where(x => x.Equals(CurrSession.Login)).Any())//User permission denied
                    Petaframework.ErrorTable.Err013(CurrSession, String.Concat(processTask.Name, ":", CurrEntity?.Id));
            }
            return processTask;
        }

        /// <summary>
        /// Returns the next task. If there are route decisions it will return the possible subsequent tasks
        /// </summary>
        /// <returns>List of next task(s)</returns>
        public List<ProcessTask> GetNextTasks()
        {
            var curr = GetCurrentTask();

            if (curr == null)
                return new List<ProcessTask>();
            if (curr.To.Count == 0)
                return new List<ProcessTask> { GetEndTask(curr, DateTime.Now) };

            return curr.To;
        }

        /// <summary>
        /// Returns the next task if it meets the route parameters.Failing to meet the route returns the current task.
        /// </summary>
        /// <returns>An attested next task</returns>
        public ProcessTask GetNextTask(PtfkFormStruct form)
        {
            try
            {

                var current = GetCurrentTask();
                try
                {
                    _TaskToVerify = current;
                    var next = GetNextTaskState(form);
                }
                catch (PtfkException pex)
                {

                }
                catch (Exception ex)
                {
                    return current;
                }
                if (current == null || (!current.IsFirstTask() && (_CheckedTask == null || _CheckedTask.IsFirstTask())))
                    return current;
                return _CheckedTask;

            }
            catch (Exception ex)
            {
                CurrEntity.Logger.LogDebug(Tools.ToJsonPreserveReferences(new { Type = "Error", Exception = ex }));
                return null;
            }
        }

        private ProcessTask EndTask;
        private ProcessTask GetEndTask(ProcessTask curr, DateTime startDate)
        {
            if (CurrBusinessProcess.Result == null || CurrBusinessProcess.Result.Tasks.Count == 0)
                return null;

            if (curr == null || curr.ID == END_TASK_ID)
                curr = CurrBusinessProcess.Result.Tasks.Where(a => a.ID.Equals(GetLogs().Where(x => x.ProcessTaskId.HasValue && !x.ProcessTaskId.Equals(END_TASK_ID) && !String.IsNullOrWhiteSpace(x.ProcessTaskId.Value.ToString())).LastOrDefault().ProcessTaskId.Value.ToString())).FirstOrDefault();
            if (curr == null)
                return null;
            var t = new ProcessTask { ID = END_TASK_ID, From = new List<ProcessTask> { curr }, Name = END_TASK_NAME, Parent = curr.Parent };
            t.SetStartDate(startDate);
            var allProfiles = new List<Profile>();
            foreach (var item in t.Parent.Tasks)
                allProfiles.AddRange(item.Profiles);
            allProfiles = allProfiles.Distinct().ToList();
            t.Profiles = allProfiles;
            foreach (var item in curr.Fields)
                t.Fields.Add(new KeyValuePair<ProcessField, string>(item.Key, Constants.FieldReadOnlyChar));
            EndTask = t;
            return t;
        }

        public List<string> GetInvisibleFieldsOnCurrentTask()
        {

            return GetCurrentTask().Fields.Where(x => !x.Value.Where(f => f.Equals(Constants.FieldNoShowChar)).Any()).Select(x => x.Key.Name).ToList();

        }

        public PtfkFormStruct GetCurrentTaskState(PtfkFormStruct form)
        {
            CurrStruct = form;

            var currTask = GetCurrentTask();
            if (currTask != null && currTask.ID != END_TASK_ID)
            {
                CheckProfile(currTask);
                //if (_IsNewInstance)
                //    currTask = new ProcessTask();
                var args = new PtfkEventArgs<ProcessTask>(ref currTask);
                //OnTaskRouting(args);
                if (currTask != null)
                    GoTo(currTask);
                else
                    GoTo(new ProcessTask());
            }
            else
                GoToEnd();
            return CurrStruct;//Finalized task or new instance!
        }

        public PtfkFormStruct GetBeforeTaskState(PtfkFormStruct form)
        {
            CurrStruct = form;
            _ReadOnlyFlag = true;

            var currTask = GetBeforeTask();
            if (currTask != null && currTask.ID != END_TASK_ID)
            {
                if (!CurrStruct._SkipProfileCheck)
                    CheckProfile(currTask);
                //if (_IsNewInstance)
                //    currTask = new ProcessTask();
                var args = new PtfkEventArgs<ProcessTask>(ref currTask);
                //OnTaskRouting(args);
                if (currTask != null)
                    GoTo(currTask);
                else
                    GoTo(new ProcessTask());
            }
            else
                GoToEnd();
            return CurrStruct;//Finalized task or new instance!
        }

        private ProcessTask _CheckedTask;
        private ProcessTask _TaskToVerify;
        private bool _SetCheckedTask = false;
        public PtfkFormStruct GetNextTaskState(PtfkFormStruct form)
        {
            CurrStruct = form;

            if (_TaskToVerify != null)
            {
                var args = new PtfkEventArgs<ProcessTask>(ref _TaskToVerify);
                _SetCheckedTask = true;
                OnTaskRouting(args);
                _TaskToVerify = null;

                return CurrStruct;
            }

            var currTask = GetNextTasks();
            if (currTask.Any())
            {
                var t = currTask.FirstOrDefault();
                var args = new PtfkEventArgs<ProcessTask>(ref t);
                _SetCheckedTask = true;
                _CheckedTask = t;
                OnTaskRouting(args);
                CheckProfile(t);
            }
            else
                GoToEnd();//Finalized task!

            return CurrStruct;
        }

        public ProcessTask GetTask(string taskID)
        {
            if (taskID == END_TASK_ID)
                return EndTask;
            var t = CurrBusinessProcess.Result.Tasks.Where(x => x.ID.Equals(taskID)).FirstOrDefault();
            if (t == null)
                throw new PtfkException(PtfkException.ExceptionCode.TaskByIdNotFound, String.Format("Task with Id [{0}] not found on [{1}] workflow!", taskID, CurrEntity));
            t.Parent = CurrBusinessProcess.Result;
            return t;
        }

        public List<ProcessTask> ListAllTasks()
        {
            return CurrBusinessProcess.Result.Tasks;
        }

        public ProcessTask GetFirstTask()
        {
            return CurrBusinessProcess.Result.Tasks.Where(x => x.IsFirstTask()).FirstOrDefault();
        }

        /// <summary>
        /// Returns the current task of a given entity
        /// </summary>
        /// <param name="entityID">The entity id</param>
        /// <returns>Current task</returns>
        public ProcessTask GetCurrentTask(long entityID)
        {
            if (entityID == 0)
                return null;
            //var entity = BusinessClass.Read(entityID);
            var logs = GetLogs(entityID);

            var last = logs?.LastOrDefault();
            if (last != null && last.End)
            {
                IsTheEnd = true;
                return GetEndTask(null, last.Date);
            }

            var currTaskID = last?.ProcessTaskId?.ToString();
            if (String.IsNullOrWhiteSpace(currTaskID))
                return CurrBusinessProcess.Result.Tasks.FirstOrDefault();
            else
            {
                var task = GetTask(currTaskID);
                task.SetStartDate(last.Date);
                task.SetReadBy(Tools.FromJson<List<ReadByStruct>>(last.ReadBy));
                return task;
            }
        }

        /// <summary>
        /// Assemble the form according to the given task id
        /// </summary>
        /// <param name="taskID"></param>
        public void GoTo(string taskID)
        {
            var taskToAssembly = GetTask(taskID);
            if (taskToAssembly == null)
                throw new PetaframeworkStd.Exceptions.NotExistsProcessTaskExpection(taskID);
            if (_SetCheckedTask)
            {
                _CheckedTask = taskToAssembly;
                _SetCheckedTask = false;
            }
            GoTo(taskToAssembly);
        }

        private void GoTo(ProcessTask task)
        {
            if (task != null)
                AssembleTaskView(task);
        }

        //internal static PtfkFormStruct ToCacheState<T>(this PtfkFormStruct str, T form) where T : IPtfkForm, IPtfkEntity
        //{
        //    str.html = new List<HtmlElement> { new HtmlElement { } };
        //    foreach (var item in form?.CurrentWorkflow?.GetCurrentTask()?.Fields)
        //    {
        //        str.html[0].Html.Add()
        //    }
        //}

        private void AssembleTaskView(ProcessTask taskToAssembly, bool readable = false)
        {
            if (readable)
            {
                var removeIdxs = new List<int>();
                var idx = 0;
                foreach (var html in CurrStruct.html[0].Html)
                {
                    switch (html.ReadableType)
                    {
                        case ReadableFieldType.Always:
                            html.Readonly = true;
                            html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                            break;
                        case ReadableFieldType.WhenFilled:
                            if (html.Value != null && !String.IsNullOrWhiteSpace(html.PlainValue.ToString()))
                            {
                                html.Readonly = true;
                                html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                            }
                            else
                                removeIdxs.Add(idx);
                            break;
                        case ReadableFieldType.WhenFinalized:
                            if (taskToAssembly.ID == END_TASK_ID)
                            {
                                html.Readonly = true;
                                html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                            }
                            else
                                removeIdxs.Add(idx);
                            break;
                        case ReadableFieldType.Never:
                        default:
                            removeIdxs.Add(idx);
                            break;
                    }
                    idx++;
                }
                removeIdxs.Reverse();
                foreach (var item in removeIdxs)
                    CurrStruct.html[0].Html.RemoveAt(item);

                var fieldSet = CurrStruct.html.FirstOrDefault();
                var taskText = "[" + END_TASK_NAME + "]";
                if (Petaframework.Tools.IsBase64(fieldSet.Caption))
                    fieldSet.Stage = Petaframework.Tools.EncodeBase64(taskText);
                else
                    fieldSet.Stage = taskText;
            }
            else
            {
                var isAdmin = IsAdmin();
                var readables = CurrStruct.html[0].Html.Where(x => x.Readable).ToArray();
                foreach (var item in taskToAssembly.Fields)
                {
                    var html1 = CurrStruct.GetHtml(item.Key.Name.Replace("_", ""));
                    if (html1 == null)
                        html1 = CurrStruct.GetHtml("id" + item.Key.Name.Replace("_", ""));
                    var html2 = CurrStruct.GetMirroredHtml(item.Key.Name.Replace("_", ""));

                    var lst = new List<HtmlElement> { html1, html2 };
                    foreach (var html in lst.Where(x => x != null))
                    {
                        if (item.Value.Contains(Constants.FieldMandatoryChar))
                        {
                            if (html.Validate == null)
                                html.Validate = new Validate { Required = true };
                            else
                                html.Validate.Required = true;

                            if (html.CurrentFormCaption.Key != null && html.CurrentFormCaption.Key.PropertyType.Equals(typeof(bool)) && html.CurrentFormCaption.Value.MaxLength == 2 && (html.PlainValue == null || html.PlainValue.ToString() == "0"))
                                html.Value = "";
                        }
                        if (item.Value.Contains(Constants.FieldShowEmptyChar))
                        {
                            html.Value = "";
                        }
                        if (_ReadOnlyFlag || (!isAdmin && item.Value.Contains(Constants.FieldReadOnlyChar)))
                        {
                            html.Readonly = true;
                            html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                        }
                        if (item.Value.Contains(Constants.FieldNoShowChar))
                            CurrStruct.html[0].Html.Remove(html);
                        //TODO if Integration Char
                    }
                }

                if (_ReadOnlyFlag)
                {
                    var exc = readables.Where(x => !CurrStruct.html[0].Html.Where(y => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(y.Name) && y.Name.Equals(x.Name)).Any()).ToList();
                    foreach (var html in exc)
                    {
                        switch (html.ReadableType)
                        {
                            case ReadableFieldType.Always:
                                html.Readonly = true;
                                html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                                break;
                            case ReadableFieldType.WhenFilled:
                                if (html.Value != null && !String.IsNullOrWhiteSpace(html.PlainValue.ToString()))
                                {
                                    html.Readonly = true;
                                    html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                                }
                                else
                                    html.Readonly = false;
                                break;
                            case ReadableFieldType.WhenFinalized:
                                if (taskToAssembly.ID == END_TASK_ID)
                                {
                                    html.Readonly = true;
                                    html.Mode = TypeDef.InputType.ReadOnly.ToString().ToLower();
                                }
                                else
                                    html.Readonly = false;
                                break;
                        }
                    }
                    CurrStruct.html[0].Html.AddRange(exc.Where(x => x.Readonly));
                }

                var fieldSet = CurrStruct.html.FirstOrDefault();
                if (_ReadOnlyFlag)
                    fieldSet.Readonly = true;
                var taskText = "[" + taskToAssembly.Name.Replace("[", "").Replace("]", "") + "]";
                if (Petaframework.Tools.IsBase64(fieldSet.Caption))
                    fieldSet.Stage = Petaframework.Tools.EncodeBase64(taskText);
                else
                    if (String.IsNullOrWhiteSpace(fieldSet.Caption))
                    fieldSet.Stage = Petaframework.Tools.EncodeBase64("[" + END_TASK_NAME + "]");
                else
                    fieldSet.Stage = taskText;

                var saveButton = fieldSet.Html.Where(x => x.Type.Equals(ElementType.submit.ToString())).FirstOrDefault();
                if (saveButton != null)
                {
                    if (!String.IsNullOrWhiteSpace(taskToAssembly.UI.SaveButtonText))
                        saveButton.Value = taskToAssembly.UI.SaveButtonText;
                    else
                        saveButton.Value = "Prosseguir";
                }
                if (!taskToAssembly.UI.ShowClearButton)
                {
                    var clearButton = fieldSet.Html.Where(x => x.Type.Equals(ElementType.reset.ToString())).FirstOrDefault();
                    fieldSet.Html.Remove(clearButton);
                }

                if (!String.IsNullOrWhiteSpace(taskToAssembly.UI.SuccessMessage))
                    CurrStruct.message = Tools.EncodeBase64(taskToAssembly.UI.SuccessMessage);
            }
            var culture = Tools.CurrentFormatProvider as System.Globalization.CultureInfo;
            CurrStruct.html[0].Html
                .Where(x => x.Type == ElementType.date.ToString())
                .ToList()
                .ForEach(x => x.Value = GetDate(x, false, culture));
            CurrStruct.html[0].Html
                .Where(x => x.Type == ElementType.datetime_local.ToString().Replace("_", "-"))
                .ToList()
                .ForEach(x => x.Value = GetDate(x, true, culture));

        }

        private object GetDate(HtmlElement x, bool longDatetime, System.Globalization.CultureInfo culture)
        {
            var dt = (x.Value != null && String.IsNullOrWhiteSpace(x.Value.ToString())) ?
                                        DateTime.MinValue : Convert.ToDateTime(x.PlainValue != null ?
                                                x.PlainValue :
                        x.Value);
            if (dt == DateTime.MinValue)
                return "";
            if (longDatetime)
                return dt.ToString(culture).Substring(0, "04/03/2020 00:00".Length);
            return dt.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
        }

        public bool IsAdmin()
        {
            try
            {
                //return false;
                return CurrPermissions.Where(x => x.IsAdmin.HasValue && x.IsAdmin.Value && (x.Profile.Equals("*") || x.EnabledTo.Contains(CurrSession.Login))).Any();
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool IsNotPrivateUser()
        {
            try
            {
                return CurrPermissions.Where(x => (!x.IsPrivate.HasValue || !x.IsPrivate.Value) && x.EnabledTo.Contains(CurrSession.Login)).Any();
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool HasHierarchyFlag(ProcessTask taskToCheck = null)
        {
            try
            {
                var t = taskToCheck == null ? GetCurrentTask() : taskToCheck;
                var currPermission = CurrPermissions.Where(c => t.Profiles.Where(x => c.ProfileID.Equals(x.ID) || c.Profile.Equals(x.Name)).Any()).FirstOrDefault();
                return currPermission.HierarchyFlag;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void GoToEnd()
        {
            var t = GetCurrentTask();
            t = CheckProfile(t);
            _SetCheckedTask = true;
            _CheckedTask = GetEndTask(t, DateTime.Now);
            GoTo(_CheckedTask);
        }

        public PtfkFormStruct GetReadableState(PtfkFormStruct form)
        {
            CurrStruct = form;

            var t = GetCurrentTask();
            //t = CheckProfile(t);
            AssembleTaskView(t, true);
            return CurrStruct;//Finalized task or new instance!
        }

        public PtfkFormStruct GetTaskState(ProcessTask task, PtfkFormStruct form)
        {
            CurrStruct = form;

            var t = task;
            t = CheckProfile(t);
            AssembleTaskView(t, false);
            return CurrStruct;
        }

        public string GetCurrentDiagram(string hexFillColor = "#6495ed", string hexStrokeColor = "#fff", string endedProcessColor = "#23d160")
        {
            var bpmn = CurrBPMNFile.Result;
            var task = GetCurrentTask();

            var lst = new List<ProcessTask>();

            if (IsTheEnd)
            {
                lst = GetTraceRoute().ToList();
                lst.LastOrDefault().MarkAsEnd();
            }
            else
                lst.Add(task);

            return bpmn.FillColor(IsTheEnd ? endedProcessColor : hexFillColor, hexStrokeColor, lst.ToArray());
        }

        public string GetDiagram()
        {
            var bpmn = CurrBPMNFile.Result;
            return bpmn.ToVendorFormat(CurrBusinessProcess.Result);
        }

        public bool HasBusinessProcess()
        {
            return CurrBusinessProcess.Result?.Tasks.Count > 0;
        }

        public string GetWorkFlowTitle()
        {
            var ret = "";
            try
            {
                ret = CurrBusinessProcess.Result.Name;
            }
            catch (Exception ex)
            {
                try
                {
                    ret = CurrEntity.FormLabel;
                }
                catch (Exception)
                {
                    try
                    {
                        ret = CurrBusinessProcess.Result.Entity;
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            if (!String.IsNullOrWhiteSpace(ret))
                return ret;
            else
                return GetType().Name;
        }

        public bool HasTasks()
        {
            return CurrBusinessProcess != null && CurrBusinessProcess.Result != null && CurrBusinessProcess.Result.Tasks.Count > 0;
        }

        public List<IPtfkSession> ListWorkflowUsers()
        {
            return PtfkCache.GetOrSetWorkflowUsers(CurrSession);
        }

        public IPtfkSession GetStartOwner()
        {
            var log = GetLogs().FirstOrDefault();
            var user = PtfkCache.GetOrSetWorkflowUsers(CurrSession).Where(x => x.Login.Equals(log.LoginChange)).FirstOrDefault();
            if (user == null && !String.IsNullOrWhiteSpace(log.LoginChange))
                return new PrivatePtfkSession { Login = log.LoginChange };
            if (user != null)
                return user;
            return null;
        }

        public IPtfkSession GetLastTaskOwner()
        {
            var log = GetLogs().LastOrDefault();
            var user = PtfkCache.GetOrSetWorkflowUsers(CurrSession).Where(x => x.Login.Equals(log.LoginChange)).FirstOrDefault();
            if (user == null && !String.IsNullOrWhiteSpace(log.LoginChange))
                return new PrivatePtfkSession { Login = log.LoginChange };
            if (user != null)
                return user;
            return null;
        }

        public IPtfkSession GetLastOwner()
        {
            var log = GetLogs().Where(x => !x.LoginChange.Equals(CurrSession.Login)).OrderBy(x => x.Date).LastOrDefault();
            if (log == null)
                return CurrSession;
            var user = PtfkCache.GetOrSetWorkflowUsers(CurrSession).Where(x => x.Login.Equals(log.LoginChange)).FirstOrDefault();
            if (user == null && !String.IsNullOrWhiteSpace(log.LoginChange))
                return new PrivatePtfkSession { Login = log.LoginChange };
            if (user != null)
                return user;
            return null;
        }

        public List<string> GetPermissionsOnCurrentTask(ProcessTask taskToGetPermissions = null)
        {
            var all = GetPermissions(CurrBusinessProcess.Result.Entity).ToList();
            var t = taskToGetPermissions == null ? GetCurrentTask() : taskToGetPermissions;

            var perms = t.Profiles?.Where(x => all.Where(p => !String.IsNullOrWhiteSpace(p.ProfileID) && p.ProfileID.Equals(x.ID)).Any()).ToList();

            var lst = (from p in all
                       join t1 in t.Profiles on p.ProfileID equals t1.ID
                       select p.EnabledTo
                    );

            List<string> ret = new List<string>();
            foreach (var item in lst)
            {
                ret.AddRange(item);
            }
            return ret.Distinct().ToList();
        }

        public List<string> GetLastTaskProfileOwner(ProcessTask t)
        {
            List<string> source = new List<string>();
            Profile pid = t.Profiles.FirstOrDefault<Profile>();
            IPtfkLog last = this.GetLogs().Join(this.CurrBusinessProcess.Result.Tasks, (Func<IPtfkLog, string>)(l => l.ProcessTaskId.Value.ToString()), (Func<ProcessTask, string>)(tk => tk.ID), (l, tk) => new
            {
                l,
                tk
            }).Where(_param1 => _param1.tk.Profiles.Where(x => x.ID.Equals(pid.ID)).Any()).OrderBy(_param1 => _param1.l.Date).Select(_param1 => _param1.l).LastOrDefault();
            if (last == null)
                return source;
            IPtfkLog ptfkLog = this.CurrLogs.Where(l => !l.ProcessTaskId.Equals((object)last.ProcessTaskId)).OrderBy(l => l.Id).LastOrDefault();
            if (ptfkLog != null && !string.IsNullOrWhiteSpace(ptfkLog.DelegateTo))
            {
                List<string> stringList = Tools.FromJson<List<string>>(ptfkLog.DelegateTo, false);
                source.AddRange(stringList);
                return source.ToList();
            }
            if (ptfkLog == null || string.IsNullOrWhiteSpace(ptfkLog.LoginChange))
                return source;
            List<string> stringList1 = new List<string>()
      {
        ptfkLog.LoginChange
      };
            source.AddRange(stringList1);
            return source.ToList<string>();
        }

        public DateTime? CreationDate()
        {
            List<IPtfkLog> logs = this.GetLogs();
            if (logs == null)
                return new DateTime?();
            var dt = logs.Where(x => x.LogType == Enums.LogType.Create.ToString()).FirstOrDefault()?.Date;
            if (dt == null)
                return logs.OrderBy(x => x.Date).FirstOrDefault().Date;
            return dt;
        }

        public String CreatorID()
        {
            List<IPtfkLog> logs = this.GetLogs();
            if (logs == null)
                return String.Empty;
            var id = logs.Where(x => x.LogType == Enums.LogType.Create.ToString()).FirstOrDefault()?.LoginChange;
            if (String.IsNullOrWhiteSpace(id))
                return logs.OrderBy(x => x.Date).FirstOrDefault()?.LoginChange;
            return id;
        }

        public List<string> GetDelegates(ProcessTask taskToGetDelegates = null)
        {
            var t = taskToGetDelegates == null ? GetCurrentTask() : taskToGetDelegates;
            var log = this.GetLogs().Where(x => x.ProcessTaskId.ToString().Equals(t.ID)).LastOrDefault();
            try
            {
                var implicitPerms = GetPermissionsOnCurrentTask(t);
                var explicitPerms = !String.IsNullOrWhiteSpace(log?.DelegateTo) ? Tools.FromJson<List<string>>(log?.DelegateTo).Distinct().ToList() : new List<string>();
                return implicitPerms.Union(explicitPerms).Distinct().ToList();
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
        }
    }
}
