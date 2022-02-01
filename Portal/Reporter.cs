using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Petaframework.Interfaces;
using Petaframework.POCO;
using PetaframeworkStd;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.Enums;

namespace Petaframework.Portal
{
    public class Reporter : PortalBase, IPortalBase
    {
        public Reporter(string reportType, IPtfkEntity entity, PtfkFormStruct form, Func<String, IPtfkSession> onSearchingSession)
        {
            Entity = entity;
            base.OnSearchingSession = onSearchingSession;
            ReportType = reportType ?? "";
            FormStruct = form;
        }

        internal readonly PtfkFormStruct FormStruct;
        internal readonly String ReportType;

        PtfkReportEvents _Events;
        public PtfkReportEvents Events
        {
            get
            {
                return _Events;
            }
            set
            {
                value.Parent = this;
                _Events = value;
            }
        }

        public ActionResult ToActionResult()
        {
            try
            {
                /*Type (t) options:
                 *    -> [default]              needs { q.type, q.json.__id }       returns { true|false } // checks whether the owner is allowed to act on the current task
                 *  d -> [detailsItem]          needs { q.type, q.json.__id }       returns { inf, xml }
                 *  l -> [list]                 needs { q.type }                    returns { [wfs], xml }
                 *  s -> [summary]              needs { }                           returns { [ ent ], ext }
                 *  c -> [check]                needs { }                           returns { ver, stt }   
                 *  u -> [user]                 needs { q.json.simulated }          returns { usr }
                 *  m -> [maintenanceMode]      needs { q.json.maintenanceMode }    returns { maintenanceMode }
                 *  f -> [filter]               needs { q.type, q.json }            returns { [ wfs ], [ dtv ] }
                 */
                object ret;
                if (this.Entity == null)
                    return null;
                switch (Constants.ReporterOptions.GetEnum(ReportType))
                {
                    case Constants.ReporterOptions.Enum.Summary://summary - Ptfk Entities on Current Environment
                        var scontext = new SummaryResponseContext();
                        var workerEtt = PtfkEnvironment.CurrentEnvironment.WorkerClass as IPtfkEntity;
                        int drafts = 0,
                            pending = 0,
                            running = 0,
                            completed = 0;
                        //var business = workerEtt.GetType().GetMethod(nameof(PtfkForm<IPtfkForm>.GetBusinessClass)).Invoke(workerEtt, new object[] { });

                        InvokeAction(() => this.Events.OnSummary.Invoke(scontext), scontext);
                        //var filter = workerEtt.GetPtfkFilter(this.FormStruct.FilterObject);
                        scontext.FillOwnerSummary();

                        foreach (var ett in scontext.SummaryList)
                        {
                            var entity = Tools.GetIPtfkEntityByClassName(PtfkEnvironment.CurrentEnvironment.LogClass.GetType().Assembly, ett.EntityName, GetOwner());
                            if (entity.CurrentWorkflow != null && entity.CurrentWorkflow.HasBusinessProcess())
                                foreach (var summTask in ett.Tasks)
                                {
                                    var t = entity.CurrentWorkflow.GetTask(summTask.TaskId.ToString());
                                    if (t.IsFirstTask())
                                        drafts += summTask.OwnerCount.Result;
                                    else
                                        if (t.IsEndTask())
                                        completed += summTask.OwnerCount.Result;
                                    else
                                    {
                                        var perms = entity.CurrentWorkflow.GetDelegates(t);
                                        if (perms.Contains(GetOwner().Login) || perms.Contains(Constants.TOKEN_USER_ID + GetOwner().Login) ||
                                            (!String.IsNullOrWhiteSpace(GetOwner().Department?.ID)
                                                                && (perms.Contains(GetOwner().Department.ID) || perms.Contains(Constants.TOKEN_DEPARTMENT_ID + GetOwner().Department.ID))
                                            ))
                                            pending += summTask.OwnerCount.Result;
                                        else
                                            running += summTask.OwnerCount.Result;
                                    }
                                }
                        }
                        var dt = scontext.UserExtract.LastActivity;
                        scontext.UserExtract = new OutboundData.UserExtract
                        {
                            Drafts = drafts,
                            Pending = pending,
                            Running = running,
                            Completed = completed,
                            LastActivity = dt
                        };


                        //SetContextParent(scontext);
                        //this.Events.OnSummary.Invoke(scontext).Wait();
                        //GetContextParent(scontext);
                        ret = scontext.ResponseObject();
                        break;
                    case Constants.ReporterOptions.Enum.DetailsItem:
                        var dcontext = new DetailsResponseContext();
                        InvokeAction(() => this.Events.OnDetails.Invoke(dcontext), dcontext);
                        //SetContextParent(dcontext);
                        //this.Events.OnDetails.Invoke(dcontext).Wait();
                        //GetContextParent(dcontext);
                        ret = dcontext.ResponseObject();
                        break;
                    case Constants.ReporterOptions.Enum.List://list - Active requests to and from the owner (IPtfkWorker)
                        var lcontext = new ListResponseContext();
                        InvokeAction(() => this.Events.OnList.Invoke(lcontext), lcontext);
                        //SetContextParent(lcontext);
                        //this.Events.OnList.Invoke(lcontext).Wait();
                        //GetContextParent(lcontext);
                        ret = lcontext.ResponseObject();
                        break;
                    case Constants.ReporterOptions.Enum.Check://check - to check environment status 
                        var c = new ReportContextBase();
                        SetContextParent(c);
                        c.ExportAsBase64 = false;
                        ret = Tools.ToJson(new POCO.OutboundData.Check
                        {
                            AppVersion = Tools.GetAppVersion(),
                            FrameworkVersion = Tools.GetPetaFrkVersion(),
                            EnvironmentState = PtfkEnvironment.CurrentEnvironment.Status
                        });
                        GetContextParent(c);
                        break;
                    case Constants.ReporterOptions.Enum.UserSearch://user - to search App user
                        var u = new ReportContextBase();
                        SetContextParent(u);
                        u.ExportAsBase64 = true;
                        if (FormStruct == null)
                            PtfkConsole.WriteLine(string.Format("Invalid {0} on class {1}", nameof(FormStruct), nameof(Reporter)));
                        ret = new POCO.OutboundData.UserSearch { UserSession = Entity.Owner.IsAdmin ? OnSearchingSession.Invoke(FormStruct?.simulated) : null };
                        GetContextParent(u);
                        break;
                    case Constants.ReporterOptions.Enum.MaintenanceMode://maintenanceMode - to get and set(only for owner admin) the maintenance mode
                        var m = new ReportContextBase();
                        SetContextParent(m);
                        ///invoke external action here
                        GetContextParent(m);
                        m.ExportAsBase64 = true;
                        if (FormStruct == null)
                            PtfkConsole.WriteLine(string.Format("Invalid {0} on class {1}", nameof(FormStruct), nameof(Reporter)));
                        ret = new POCO.OutboundData.Maintenance { IsInMaintenanceMode = Entity.Owner.IsAdmin && FormStruct.maintenanceMode.HasValue ? PtfkEnvironment.CurrentEnvironment.SetMaintenanceMode(FormStruct.maintenanceMode.Value) : PtfkEnvironment.CurrentEnvironment.Status == EnvironmentStatus.MaintenanceMode };

                        break;
                    case Constants.ReporterOptions.Enum.Filter://filter - to search and response by filter                           
                        var generator = new PtfkGen(new Petaframework.PageConfig(this.Entity.ClassName, this.FormStruct, this.Entity.Owner));
                        if (FormStruct == null || FormStruct.FilterObject == null)
                            PtfkConsole.WriteLine(string.Format("Invalid {0} Filter on class {1}", nameof(FormStruct), nameof(Reporter)));
                        var lctx = new ListResponseContext();
                        this.FormStruct.method = Constants.FormMethod.Options;
                        this.FormStruct.action = Constants.FormAction.List;
                        this.FormStruct.FilterObject.SearchValue = this.FormStruct.FilterObject.SearchValue ?? "";

                        this.FormStruct.FilterObject.StopAfterFirstHtml = true;

                        IPtfkEntity workerEntity = Activator.CreateInstance(PtfkEnvironment.CurrentEnvironment.WorkerClass.GetType()) as IPtfkEntity;
                        workerEntity.Owner = GetOwner();

                        var filter = workerEntity.GetPtfkFilter(this.FormStruct.FilterObject);
                        filter.PreFilterDatetime = this.FormStruct.ActivitiesAfterThisDate;
                        filter.OrderByColumnIndex = Array.IndexOf(filter.FilteredProperties, nameof(IPtfkWorker.Date));

                        var task = Task.Factory.StartNew(() => generator.GetJsonControl(this.Entity, this.FormStruct));
                        InvokeAction(() => this.Events.OnList.Invoke(lctx), lctx);
                        var fcontext = new FilterResponseContext
                        {
                            //DataItems = filter.Result.Items.Cast<IPtfkWorker>().ToList() // SLOW
                            DataItems = workerEntity.ApplyFilter(filter, filter.Result.Items).Cast<IPtfkWorker>().ToList()//FAST
                        };
                        var lastWorker = Task.Factory.StartNew(() =>
                        {
                            if (filter.Result.Items.Any())
                                return filter.Result.Items.Cast<IPtfkWorker>().Select(x => x.Date).Max();
                            return DateTime.Now;
                        });
                        var iDs = fcontext.DataItems.Select(x => x.Id);
                        task.Wait();
                        Dictionary<String, String> labels = new();
                        this.FormStruct.OutputHtmlElementFilter.FirstOrDefault().Html?.ForEach(x => labels.Add(x.Name, Tools.DecodeBase64(x.Caption)));
                        var view = this.Entity.GetFilteredDataByFields(labels.Select(x => x.Key), iDs).ToList();
                        labels.Add(this.Entity.ClassName, this.Entity.FormLabel);

                        var lblTasks = fcontext.DataItems.Select(x => new KeyValuePair<string, string>(Constants.OutboundData.TaskPrefixLabel + x.Tid, x.Task)).Distinct().ToList();
                        fcontext.DataItems.ForEach(x => x.Task = "");

                        lblTasks.ToList().ForEach(x => labels.Add(x.Key, x.Value));

                        var langLabels = new Dictionary<String, Dictionary<string, string>>
                        {
                            { (Tools.CurrentFormatProvider as System.Globalization.CultureInfo).Name.ToLower(), labels }
                        };

                        fcontext.DataView = view.Any() ? new List<POCO.OutboundData.DataView> { new POCO.OutboundData.DataView { Labels = langLabels, View = view, EntityName = this.Entity.ClassName, LastActivity = lastWorker.Result } } : new List<POCO.OutboundData.DataView>();
                        fcontext.ExportAsBase64 = true;


                        ret = fcontext.ResponseObject();
                        break;
                    default:
                        var d = new ReportContextBase();
                        SetContextParent(d);
                        ret = this.Events.OnDefault.Invoke(d);
                        GetContextParent(d);
                        break;
                }
                if (ExportAsBase64)
                    return new OkObjectResult(Tools.EncodeBase64(Tools.ToJson(ret, null)));
                return new OkObjectResult(ret);
            }
            catch (Exception ex)
            {
                PtfkConsole.WriteLine(ex);
                throw new PtfkException("Error on generating report.", ex);
            }
        }

        private void InvokeAction(Func<Task> task, ReportContextBase context)
        {
            SetContextParent(context);
            task.Invoke().Wait();
            GetContextParent(context);
        }

        private void SetContextParent(ContextBase context)
        {
            context.Parent = this;
        }

    }

    public class PtfkReportEvents : ReportContextBase
    {
        public Func<DetailsResponseContext, Task> OnDetails { get; set; }
        public Func<ListResponseContext, Task> OnList { get; set; }

        public Func<SummaryResponseContext, Task> OnSummary { get; set; }

        public Func<ReportContextBase, Object> OnDefault { get; set; }

    }

    public class DetailsResponseContext : ReportContextBase, IReportContext
    {
        private DateTime? _workflowStartedAt;
        public TimeSpan? CurrentWorkflowDuration()
        {
            if (_workflowStartedAt == null)
                _workflowStartedAt = Parent.Entity.CurrentWorkflow.CreationDate();
            return (DateTime.Now - Parent.Entity.CurrentWorkflow.CreationDate());
        }

        public String MessagePattern { internal get; set; }
        public DateTime LimitDate { internal get; set; }
        public int? AvgMinutes { internal get; set; }

        public object ResponseObject()
        {
            var delegates = new System.Collections.Generic.List<string>();
            var lstDelegates = Parent.Entity.CurrentWorkflow.GetDelegates();
            var delegatesIDs = String.Join(',', lstDelegates.Select(x => x));
            var objCreator = Parent.OnSearchingSession(Parent.Entity.CurrentWorkflow.CreatorID());
            var lastOwner = Parent.OnSearchingSession(Parent.Entity.CurrentWorkflow.GetLastOwner().Login);
            var creatorID = objCreator?.Login;
            var creator = objCreator?.Name.ToUpper();
            var lastOwnerID = lastOwner?.Login;
            var duration = CurrentWorkflowDuration();
            if (!Parent.Entity.Owner.IsAdmin)
                creatorID = lastOwnerID = delegatesIDs = "";
            if (lstDelegates == null || lstDelegates.Count == 0)
                delegates = new System.Collections.Generic.List<string> { creator };
            else
                foreach (var item in lstDelegates)
                    delegates.Add(Parent.OnSearchingSession(item)?.Name.ToUpper());

            if (CurrentTask().Type.Equals(nameof(PetaframeworkStd.Commons.ServiceTask)))
                MessagePattern = "";

            return new POCO.OutboundData.DetailsItem
            {
                Informations = new POCO.OutboundData.DetailsItem.ItemInfo
                {
                    MessagePattern = MessagePattern,
                    CurrentTask = Parent.Entity.CurrentWorkflow.GetCurrentTask().Name,
                    StartDate = Parent.Entity.CurrentWorkflow.GetCurrentTask().StartDate,
                    LimitDate = LimitDate,
                    TaskAverage = Tools.ConvertMinutesToDuration(AvgMinutes),
                    ProcessStart = _workflowStartedAt,
                    ProcessDuration = Tools.GetDuration(duration),
                    End = Parent.Entity.CurrentWorkflow.Finished(),
                    Id = Parent.Entity.Id,
                    Creator = creator,
                    Delegates = string.Join("; ", delegates.Where(x => !String.IsNullOrWhiteSpace(x)).ToArray()),
                    LastOwner = lastOwner,
                    CreatorID = creatorID,
                    LastOwnerID = lastOwnerID,
                    DelegatesIDs = delegatesIDs
                },
                DiagramXML = Parent.Entity.CurrentWorkflow.GetCurrentDiagram(),
                EntityName = Parent.Entity.ClassName
            };
        }
    }


    public class ListResponseContext : ReportContextBase, IReportContext
    {
        //public IPtfkSession GetOwner() { return Parent.Entity.Owner; }
        //public PetaframeworkStd.Commons.ProcessTask CurrentTask() { return Parent?.Entity?.CurrentWorkflow?.GetCurrentTask(); }
        //public bool HasFinishedTask() { return Parent?.Entity?.CurrentWorkflow?.Finished() ?? false; }
        public List<IPtfkWorker> DataItems { internal get; set; }

        public object ResponseObject()
        {
            var d = Parent.Entity.CurrentWorkflow.GetDiagram();

            return new POCO.OutboundData.List
            {
                Workflows = DataItems,
                DiagramXML = d
            };
        }
    }


    public class FilterResponseContext : ReportContextBase, IReportContext
    {
        //public IPtfkSession GetOwner() { return Parent.Entity.Owner; }
        //public PetaframeworkStd.Commons.ProcessTask CurrentTask() { return Parent?.Entity?.CurrentWorkflow?.GetCurrentTask(); }
        //public bool HasFinishedTask() { return Parent?.Entity?.CurrentWorkflow?.Finished() ?? false; }
        public List<IPtfkWorker> DataItems { internal get; set; }

        public List<POCO.OutboundData.DataView> DataView { get; set; }

        public object ResponseObject()
        {
            //var d = ParentReporter.Entity.CurrentWorkflow.GetDiagram();

            return new POCO.OutboundData.Filter
            {
                Workflows = DataItems,
                Dataview = DataView
            };
        }
    }

    public class SummaryResponseContext : ReportContextBase, IReportContext
    {
        public System.Collections.Generic.List<Petaframework.POCO.EntitySummary> SummaryList { get; set; }

        [JsonProperty("ext")]
        internal POCO.OutboundData.UserExtract UserExtract { get; set; }

        internal void FillOwnerSummary()
        {
            var obj = PtfkEnvironment.CurrentEnvironment.WorkerClass as IPtfkForm;
            var workerClass = Tools.GetIPtfkEntityByClassName(PtfkEnvironment.CurrentEnvironment.WorkerClass.GetType().Assembly, obj.ClassName, GetOwner());
            var dtItems = workerClass.GetDataItems(PtfkEnvironment.CurrentEnvironment.WorkerClass as IPtfkForm).Cast<IPtfkWorker>();
            var tDate = Task.Factory.StartNew(() => { return dtItems.OrderByDescending(x => x.Date).FirstOrDefault().Date; });
            var creator = GetOwner().Login;
            foreach (var ett in SummaryList)
            {
                if (ett.Tasks != null && ett.Tasks.Any())
                    foreach (var task in ett.Tasks)
                    {
                        if (ett.HasPrivateProfile(task.TaskId))
                            task.OwnerCount = System.Threading.Tasks.Task.Factory.StartNew(() => (from e in dtItems
                                                                                                  where e.Entity.Equals(ett.EntityName) && e.Tid.Equals(task.TaskId.ToString()) &&
                                                                                                        e.Creator.Equals(creator)
                                                                                                  select e.Id).Count());
                        else
                            task.OwnerCount = System.Threading.Tasks.Task.Factory.StartNew(() => (from e in dtItems
                                                                                                  where e.Entity.Equals(ett.EntityName) && e.Tid.Equals(task.TaskId.ToString())
                                                                                                  select e.Id).Count());
                    }
                ett.OwnerClosedTasks = System.Threading.Tasks.Task.Factory.StartNew(() => (from e in dtItems
                                                                                           where e.Entity.Equals(ett.EntityName) && e.End.Value
                                                                                           select e.Id).Count());
            }
            if (UserExtract == null)
                UserExtract = new();
            UserExtract.LastActivity = tDate.Result;
        }

        public object ResponseObject()
        {
            return new
            {
                ent = SummaryList,
                ext = UserExtract
            };
        }

    }
}

