using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Petaframework.Interfaces;
using PetaframeworkStd;
using PetaframeworkStd.Commons;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using static Petaframework.Enums;
using static Petaframework.Settings;
using static Petaframework.TypeDef;

namespace Petaframework
{
    [NotMapped]
    public abstract class PtfkForm<T> : IPtfkForm where T : IPtfkForm
    {
        [NotMapped]
        [JsonIgnore]
        public ILogger Logger { set; get; }
        [NotMapped]
        [JsonIgnore]
        public IPtfkSession Owner { get; set; } = new PrivatePtfkSession();

        public IPtfkSession GetOwner()
        {
            return Owner;
        }

        [NotMapped]
        [JsonIgnore]
        public string ClassName => GetType().Name;

        protected delegate void PtfkEventHandler(PtfkEntityEventArgs<T> Entity);
        protected delegate void PtfkFileInfoEventHandler(PtfkEventArgs<PtfkFileInfo> File);

        protected event PtfkEventHandler SaveEntityBeforeEvent;
        protected event PtfkEventHandler SaveEntityAfterEvent;
        protected event PtfkEventHandler ReadEntityEvent;
        protected event PtfkEventHandler DeleteEntityBeforeEvent;
        protected event PtfkEventHandler DeleteEntityAfterEvent;
        protected event PtfkEventHandler ListEntityEvent;
        protected event PtfkFileInfoEventHandler FileUploadedEvent;

        protected event PtfkEventHandler IntegrationsBeforeSaveEntityEvent;
        protected event PtfkEventHandler IntegrationsAfterSaveEntityEvent;

        protected event PtfkEventHandler ServiceTaskEvent;

        public object Clone()
        {
            return MemberwiseClone();
        }

        private IPtfkBusiness<T> _BusinessClass;
        internal IPtfkBusiness<T> BusinessClass
        {
            get
            {
                if (_BusinessClass == null)
                {
                    try
                    {
                        GetType().GetMethod(nameof(IPtfkEntity.SetBusiness)).Invoke(this, null);
                        if (_BusinessClass == null)
                            ErrorTable.Err001();
                        else return _BusinessClass;
                    }
                    catch (Exception ex)
                    {
                        ErrorTable.Err001();
                    }

                    return null;
                }
                else return _BusinessClass;
            }
            set
            {
                _BusinessClass = value;
            }
        }

        public bool HasBusinessRestrictionsBySession()
        {
            if (Owner.IsAdmin && !IsBearerRequest())
                return false;
            return BusinessClass.GetBusinessRestrictionsBySession();
        }

        private bool IsBearerRequest()
        {
            try
            {
                return Owner.Bag != null && Owner.Bag.ContainsKey(Constants.BearerRequestFlag) && Tools.GetBoolValue(Owner.Bag[Constants.BearerRequestFlag]);
            }
            catch
            {
                return false;
            }            
        }

        public IPtfkBusiness<T> GetBusinessClass()
        {
            return BusinessClass;
        }

        public bool IsAutoSave()
        {
            try
            {
                return this.CurrentGenerator != null && this.CurrentGenerator.CurrentPageConfig != null && this.CurrentGenerator.CurrentPageConfig.CurrDForm != null && this.CurrentGenerator.CurrentPageConfig.CurrDForm.AutoSave;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        [JsonIgnore]
        public PtfkGen CurrentGenerator { get; protected set; }

        [NotMapped]
        public String FormLabel { get; protected internal set; }

        [NotMapped]
        [JsonIgnore]
        public ProcessTask CurrentProcessTask { get; internal set; } = new ProcessTask();

        [NotMapped]
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public String CurrentProcessTaskID { get { return CurrentProcessTask.ID; } }

        public string GetSelectedRole()
        {
            return this.CurrentGenerator?.GetSelectedUserRole()?.guid;
        }

        /// <summary>
        /// Indicates the Storage strategy to preserve files for this entity
        /// </summary>
        [NotMapped]
        [Newtonsoft.Json.JsonIgnore]
        public StorageMode EntityStorageMode { get; protected set; } = PtfkFileInfo.StorageMode;

        public PtfkForm()
        {
            ReadEntityEvent += OnReadEntity;
            SaveEntityBeforeEvent += OnBeforeSaveEntity;
            SaveEntityAfterEvent += OnAfterSaveEntity;
            DeleteEntityBeforeEvent += OnBeforeDeleteEntity;
            DeleteEntityAfterEvent += OnAfterDeleteEntity;
            ListEntityEvent += OnListEntity;
            FileUploadedEvent += OnFileUploaded;
            IntegrationsBeforeSaveEntityEvent += IntegrationsBeforeSaveEntityAsync;
            IntegrationsAfterSaveEntityEvent += IntegrationsAfterSaveEntityAsync;

            ServiceTaskEvent += OnServiceTaskRunning;
        }

        protected virtual void OnReadEntity(PtfkEntityEventArgs<T> e)
        {
            var handler = ReadEntityEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        protected virtual void OnBeforeSaveEntity(PtfkEntityEventArgs<T> e)
        {
            var handler = SaveEntityBeforeEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }


        protected virtual void OnBeforeDeleteEntity(PtfkEntityEventArgs<T> e)
        {
            var handler = DeleteEntityBeforeEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        protected virtual void IntegrationsBeforeSaveEntityAsync(PtfkEntityEventArgs<T> e)
        {
            var handler = IntegrationsBeforeSaveEntityEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }
        protected virtual void IntegrationsAfterSaveEntityAsync(PtfkEntityEventArgs<T> e)
        {
            var handler = IntegrationsAfterSaveEntityEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        protected virtual void OnAfterSaveEntity(PtfkEntityEventArgs<T> e)
        {
            var handler = SaveEntityAfterEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        protected virtual void OnAfterDeleteEntity(PtfkEntityEventArgs<T> e)
        {
            var handler = DeleteEntityAfterEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        protected virtual void OnServiceTaskRunning(PtfkEntityEventArgs<T> e)
        {
            var handler = ServiceTaskEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        private IEnumerable<MethodInfo> _RuntimeMethods;
        private IEnumerable<MethodInfo> RuntimeMethods
        {
            get
            {
                if (_RuntimeMethods == null)
                    _RuntimeMethods = GetType().GetRuntimeMethods();
                return _RuntimeMethods;
            }
        }

        private bool IsOverridenMethod(String methodName)
        {
            try
            {
                if (CurrentGenerator != null && CurrentGenerator.CurrentPageConfig != null && CurrentGenerator.CurrentPageConfig.CurrDForm != null && CurrentGenerator.CurrentPageConfig.CurrDForm.AutoSave)
                    return false;
            }
            catch (Exception ex) { }

            return RuntimeMethods.Where(x => x.Name.Equals(methodName)).FirstOrDefault().DeclaringType == GetType();
        }
        internal bool IsServiceExecution()
        {
            return this.Owner.Login.Equals(Constants.ServiceWorkerLogin) || _RunAsService;
        }
        private bool _RunAsService = false;
        internal void RunAsService(T entity, PageConfig config)
        {
            _RunAsService = true;
            var runtimeMethods = GetType().GetRuntimeMethods();
            if (IsOverridenMethod(nameof(OnServiceTaskRunning)))
            {
                BusinessClass.Session = config.Owner;
                PtfkFileInfo.InitializeBusinessClass(entity, config.Owner);
                CurrentGenerator = new PtfkGen(config);
                (entity as PtfkForm<T>).CurrentGenerator = CurrentGenerator;
                System.Threading.Tasks.Task<IPtfkWorkflow<T>> i = Tools.GetIWorkflow<T>(config.Owner, entity, BusinessClass);
                PtfkEntityEventArgs<T> e = new PtfkEntityEventArgs<T>(entity, i.Result.GetCurrentTask(), i.Result);
                var before = Tools.ToJson(entity);
                OnServiceTaskRunning(e);
                var after = Tools.ToJson(e.Entity);
                var c = i.Result?.GetCurrentTask();
                var t = i.Result?.GetNextTask(CurrentGenerator.GetFormObject(e.Entity));
                if (t != null)
                    (entity as PtfkForm<T>).CurrentProcessTask = t;
                if (!c.ID.Equals(t.ID))
                {
                    c = i.Result?.GetCurrentTask();
                    t = i.Result?.GetNextTask(CurrentGenerator.GetFormObject(e.Entity));
                    if (t != null)
                        (entity as PtfkForm<T>).CurrentProcessTask = t;
                }
                var hasDiff = !before.Equals(after);

                if (hasDiff || !c.ID.Equals(t.ID))
                    SaveEntity(e.Entity, config);
            }
        }

        protected virtual void OnListEntity(PtfkEntityEventArgs<T> e)
        {
            var handler = ListEntityEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        public virtual void OnFileUploaded(PtfkEventArgs<PtfkFileInfo> e)
        {
            var handler = FileUploadedEvent;
            if (handler != null)
            {
                //handler(e);
            }
        }

        /// <summary>
        /// A Key é o nome do atributo que necessitará de validações
        /// </summary>
        internal List<KeyValuePair<String, Validate>> Validates { get; set; }

        List<KeyValuePair<PropertyInfo, FormCaptionAttribute>> _captions;
        internal List<KeyValuePair<PropertyInfo, FormCaptionAttribute>> Captions
        {
            get
            {
                if (CustomCaptions != null && CustomCaptions.Any())
                {
                    if (_captions != null)
                        foreach (var item in CustomCaptions)
                        {
                            var index = _captions.FindIndex(x => x.Key.Equals(item.Key));
                            if (index >= 0)
                                _captions[index] = item;
                        }
                    else
                    {
                        _captions = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();
                        _captions.AddRange(CustomCaptions);
                    }

                }
                return _captions;

            }
            set { _captions = value; }
        }

        internal List<KeyValuePair<PropertyInfo, FormCaptionAttribute>> CustomCaptions { get; set; }

        [NotMapped]
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int ParentID
        {
            get; set;
        }

        public void Log(Petaframework.Enums.Log logType, object objToLog)
        {
            Log(logType, Tools.ToJson(objToLog, true));
        }

        public void Log(Petaframework.Enums.Log logType, string msg, Exception ex = null, params object[] args)
        {
            var originalMsg = msg;
            try
            {
                if (Logger == null)
                    return;

                msg = "<" + ClassName + ">:" + (new System.Diagnostics.StackFrame(1, true)).GetFileLineNumber() + " " + msg;

                PrintLog(logType, msg, ex, args);
            }
            catch (Exception exlog)
            {
                try
                {
                    msg = "<" + ClassName + ">:" + (new System.Diagnostics.StackFrame(1, true)).GetFileLineNumber() + " " + Petaframework.Tools.EncodeBase64(originalMsg);
                    PrintLog(logType, msg, ex, args);

                }
                catch (Exception ex2)
                {
                    ErrorTable.Err005(ex2);
                }
            }
        }

        private void PrintLog(Petaframework.Enums.Log logType, string msg, Exception ex, object[] args)
        {
            switch (logType)
            {
                case Petaframework.Enums.Log.Info:
                    if (ex != null)
                        Logger.LogInformation(msg, ex, args);
                    else
                        Logger.LogInformation(msg, args);
                    break;
                case Petaframework.Enums.Log.Warning:
                    if (ex != null)
                        Logger.LogWarning(msg, ex, args);
                    else
                        Logger.LogWarning(msg, args);
                    break;
                case Petaframework.Enums.Log.Error:
                    if (ex != null)
                        Logger.LogError(msg, ex, args);
                    else
                        Logger.LogError(msg, args);
                    break;
                case Petaframework.Enums.Log.Trace:
                    if (ex != null)
                        Logger.LogTrace(msg, ex, args);
                    else
                        Logger.LogTrace(msg, args);
                    break;
                case Petaframework.Enums.Log.Critical:
                    if (ex != null)
                        Logger.LogCritical(msg, ex, args);
                    else
                        Logger.LogCritical(msg, args);
                    break;
                default:
                    if (ex != null)
                        Logger.LogInformation(msg, ex, args);
                    else
                        Logger.LogInformation(msg, args);
                    break;
            }
        }

        private void PrintLogEventID(Petaframework.Enums.Log logType, string msg, EventId eventID, Exception ex, object[] args)
        {
            switch (logType)
            {
                case Petaframework.Enums.Log.Info:
                    if (ex != null)
                        Logger.LogInformation(eventID, ex, msg, args);
                    else
                        Logger.LogInformation(eventID, msg, args);
                    break;
                case Petaframework.Enums.Log.Warning:
                    if (ex != null)
                        Logger.LogWarning(eventID, ex, msg, args);
                    else
                        Logger.LogWarning(eventID, msg, args);
                    break;
                case Petaframework.Enums.Log.Error:
                    if (ex != null)
                        Logger.LogError(eventID, ex, msg, args);
                    else
                        Logger.LogError(eventID, msg, args);
                    break;
                case Petaframework.Enums.Log.Trace:
                    if (ex != null)
                        Logger.LogTrace(eventID, ex, msg, args);
                    else
                        Logger.LogTrace(eventID, msg, args);
                    break;
                case Petaframework.Enums.Log.Critical:
                    if (ex != null)
                        Logger.LogCritical(eventID, ex, msg, args);
                    else
                        Logger.LogCritical(eventID, msg, args);
                    break;
                default:
                    if (ex != null)
                        Logger.LogInformation(eventID, ex, msg, args);
                    else
                        Logger.LogInformation(eventID, msg, args);
                    break;
            }
        }

        public void AddCustomMode(String propertyName, InputType mode)
        {
            var prop = GetType().GetProperties().Where(x => x.Name.Equals(propertyName)).FirstOrDefault();
            if (prop != null)
            {
                FormCaptionAttribute attri = null;

                var attrs = prop.GetCustomAttributes(typeof(FormCaptionAttribute), true);

                if (attrs.Length == 0)
                {
                    attrs = prop.DeclaringType.GetCustomAttributes(true);
                    foreach (object attr in attrs)
                    {
                        var model = attr as Microsoft.AspNetCore.Mvc.ModelMetadataTypeAttribute;

                        if (model != null)
                            foreach (var metadata in model.MetadataType.GetProperties().Where(x => x.Name.Equals(propertyName)))
                            {
                                foreach (var metaAttr in metadata.GetCustomAttributes(true))
                                {
                                    FormCaptionAttribute authAttr = metaAttr as FormCaptionAttribute;
                                    if (authAttr != null)
                                    {
                                        attri = authAttr;
                                        break;
                                    }
                                }
                            }
                    }
                }
                else
                    attri = ((FormCaptionAttribute)attrs[0]);

                if (attri != null)
                {
                    attri.InputType = mode;
                    AddCustomMode(prop, attri);
                }
                else
                    ErrorTable.Err002(propertyName);
            }

        }

        private void AddCustomMode(PropertyInfo key, FormCaptionAttribute caption)
        {
            if (CustomCaptions == null)
                CustomCaptions = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();

            CustomCaptions.Add(new KeyValuePair<PropertyInfo, FormCaptionAttribute>(key, caption));
        }

        protected void AddValidate(String attribute, Validate validate)
        {
            if (Validates == null)
            {
                Validates = new List<KeyValuePair<string, Validate>>();
            }
            Validates.Add(new KeyValuePair<string, Validate>(attribute, validate));
        }

        protected void AddValidate(String attribute, Boolean required, String requiredMessage, Boolean IsIntegerField = false, Boolean IsRealField = false, Boolean IsEmail = false)
        {
            if (Validates == null)
            {
                Validates = new List<KeyValuePair<string, Validate>>();
            }
            AddValidate(attribute, new Validate
            {
                Required = required ? true : new bool?(),
                Number = IsRealField ? true : new bool?(),
                Digits = IsIntegerField ? true : new bool?(),
                Email = IsEmail ? true : new bool?(),
                Messages =
                new Messages
                {
                    Required = requiredMessage
                }
            });
        }

        protected void AddCaption(PropertyInfo key, FormCaptionAttribute caption)
        {
            if (Captions == null)
                Captions = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();

            if (!Captions.Any(x => x.Key.Equals(key)))
            {
                if (caption.Required || Tools.IsNumberType(key.PropertyType))
                    AddValidate(key.Name, caption.Required, caption.RequiredMessage, Tools.IsIntegerType(key.PropertyType), Tools.IsRealType(key.PropertyType));
                Captions.Add(new KeyValuePair<PropertyInfo, FormCaptionAttribute>(key, caption));
            }
        }

        public KeyCaptionRequired<String, string, bool> SetCaption(String key, String Caption, Boolean required)
        {
            return new KeyCaptionRequired<String, string, bool>(key, Caption, required);
        }

        public String GetLabel(String attributeName)
        {
            if (!String.IsNullOrWhiteSpace(FormCaptionAttribute.ResourceFile))
            {
                try
                {
                    var rm = new ResourceManager(FormCaptionAttribute.ResourceFile, Assembly.GetExecutingAssembly());
                    var valor = rm.GetString(GetAttributeInfo(attributeName).LabelText);
                    if (!String.IsNullOrWhiteSpace(valor))
                        return valor;
                    else
                        return GetAttributeInfo(attributeName).LabelText;
                }
                catch (Exception ex)
                {
                    return GetAttributeInfo(attributeName).LabelText;
                }
            }
            else
                return GetAttributeInfo(attributeName).LabelText;
        }

        public String GetTooltip(String attributeName)
        {
            if (!String.IsNullOrWhiteSpace(FormCaptionAttribute.ResourceFile))
            {
                try
                {
                    var rm = new ResourceManager(FormCaptionAttribute.ResourceFile, Assembly.GetExecutingAssembly());
                    var valor = rm.GetString(GetAttributeInfo(attributeName).Tooltip);
                    if (!String.IsNullOrWhiteSpace(valor))
                        return valor;
                    else
                        return GetAttributeInfo(attributeName).Tooltip;
                }
                catch (Exception ex)
                {
                    return GetAttributeInfo(attributeName).Tooltip;
                }
            }
            else
                return GetAttributeInfo(attributeName).Tooltip;
        }

        public String GetMask(String attributeName)
        {
            return GetAttributeInfo(attributeName).GetMask();
        }

        public int? GetMaxLength(String attributeName)
        {
            var val = GetAttributeInfo(attributeName).MaxLength;
            if (val == 0)
                return null;
            return val;
        }

        public string GetMirroredOf(String attributeName)
        {
            var val = GetAttributeInfo(attributeName).MirroredOf;
            return val;
        }

        public IEnumerable<KeyValuePair<String, Object>> GetReadables(ReadableFieldType readableFieldType)
        {
            var temp = GetAttributeByNameValue("", null);//Executa essa linha para certificar de que todos os campos foram incluidos
            var list = Captions.Where(x => x.Value.Equals(readableFieldType)).Select(x => new KeyValuePair<String, Object>(
                x.Key.Name,
                 GetType().GetProperty(x.Key.Name).GetValue(this, null)
             ));
            return list;
        }

        public Validate GetValidate(String attributeName)
        {
            var item = Validates.Where(x => x.Key.Equals(attributeName)).FirstOrDefault();
            if (!String.IsNullOrWhiteSpace(item.Key))
                return item.Value;
            else
                return null;
        }

        public List<ListItem> GetSelectOptions(String attributeName)
        {
            var att = GetAttributeInfo(attributeName);
            if (att.OptionsType != RequestMode.ServerSide)
            {
                return new List<ListItem>();
            }
            if (att != null && !String.IsNullOrWhiteSpace(att.OptionsContext))
            {
                try
                {
                    if (PtfkCache.GetOrSetSelectOptions(Owner, att.OptionsContext) != null)
                        return PtfkCache.GetOrSetSelectOptions(Owner, att.OptionsContext);

                    var hasMethod = GetType().GetMethods().Where(x => x.Name.Equals(att.OptionsContext)).Any();
                    object obj = null;
                    if (!hasMethod)
                    {
                        Assembly assembly = GetType().Assembly;
                        Type t = assembly.GetType(assembly.GetName().Name + "." + att.OptionsContext);
                        if (t == null)
                        {
                            assembly = GetType().BaseType.Assembly;
                            t = assembly.GetType(assembly.GetName().Name + "." + att.OptionsContext);
                        }
                        if (t != null)
                        {
                            var frm = (IPtfkEntity)Activator.CreateInstance(t);
                            frm.Owner = this.Owner;
                            frm.SetBusiness();
                            obj = frm.ItemsList();
                        }
                    }
                    else
                        obj = GetType().GetMethod(att.OptionsContext).Invoke(this, null);

                    try
                    {
                        if (obj != null)
                        {
                            var item = obj as List<ListItem>;
                            PtfkCache.GetOrSetSelectOptions(Owner, att.OptionsContext, item.Where(x => !String.IsNullOrWhiteSpace(x.Text)).ToList());
                            return item;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorTable.Err003();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("LINQ"))
                        ErrorTable.Err004(ClassName + " [" + attributeName + " (" + att.OptionsContext + ")]", ex.InnerException.Message);
                    else
                        if (ex.InnerException != null && ex.InnerException.Message.ToLower().Contains("err"))
                        throw ex.InnerException;
                    throw ex;
                }

                return null;
            }
            else return null;
        }

        public List<ListItem> GetSelectOptions()
        {
            var n = nameof(IPtfkEntity.ItemsList);
            var hasMethod = GetType().GetMethods().Where(x => x.Name.Equals(n)).Any();
            object obj = null;
            if (hasMethod)
                try
                {
                    obj = GetType().GetMethod(n).Invoke(this, null);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ErrorTable.Err004(ClassName + " [" + n + "]", ex.InnerException.Message);
                    else
                        ErrorTable.Err004(ClassName + " [" + n + "]", ex.Message);
                }
            try
            {
                if (obj != null)
                {
                    var item = obj as List<ListItem>;
                    return item;
                }
            }
            catch (Exception ex)
            {
                ErrorTable.Err003();
            }
            return null;
        }

        public IQueryable<IPtfkForm> GetDataItems()
        {
            return GetDataItems(null);
        }

        public IQueryable<IPtfkForm> GetDataItems(IPtfkForm entity = null)
        {
            var n = nameof(PtfkForm<T>.DataItems);
            var hasMethod = GetType().GetMethods().Where(x => x.Name.Equals(n)).Any();
            object obj = null;
            if (hasMethod)
                obj = GetType().GetMethod(n).Invoke(this, new object[] { entity });
            try
            {
                if (obj != null)
                {
                    var item = obj as IQueryable<IPtfkForm>;
                    return item;
                }
            }
            catch (Exception ex)
            {
                ErrorTable.Err003();
            }
            return null;
        }

        [NotMapped]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public PtfkFilter CurrentFilter { get; internal set; }
        public PtfkFilter FilterDataItems(PtfkFilter filterParam, IPtfkForm entity = null)
        {
            try
            {
                filterParam.FilteredProperties = (filterParam.FilteredProperties == null || filterParam.FilteredProperties.Length == 0 ?
                            GetListProperties() : filterParam.FilteredProperties);
                if (this.IsPtfkClass())
                {
                    (entity as PtfkForm<T>).CurrentFilter = filterParam;
                    var i = GetDataItems(entity);
                    filterParam.SetResult(i, i.Count());
                    return filterParam;
                }
                var obj = BusinessClass.FilterEntities(filterParam);

                return obj;
            }
            catch (Exception ex)
            {
                PtfkConsole.WriteLine(nameof(FilterDataItems) + ":{0}" + filterParam.SqlGenerated, ex);
                Logger.LogError(ex, nameof(FilterDataItems) + ":{0}", filterParam.SqlGenerated);
                ErrorTable.Err003();
            }
            return null;
        }

        private static bool IsNullExpression(Expression exp)
        {
            if (exp is UnaryExpression uExp) exp = uExp.Operand;

            if (exp is MemberExpression mExp && mExp.Expression is ConstantExpression cExp)
            {
                object value = mExp.Member is PropertyInfo pInfo ? pInfo.GetValue(cExp.Value) :
                    mExp.Member is FieldInfo fInfo ? fInfo.GetValue(cExp.Value) :
                    throw new NotSupportedException();

                return value == null;
            }

            if (exp is ConstantExpression constantExpression)
                return constantExpression.Value == null;

            return false;
        }

        public List<Dictionary<String, object>> GetFilteredDataByFields(IEnumerable<string> fields, IEnumerable<long> onlyThisIds = null)
        {
            try
            {
                var item = GetList(this, !Owner.IsAdmin);
                var filtered = item.Select(CreateNewStatement(fields));
                if (onlyThisIds != null)
                    filtered = filtered.Where(x => onlyThisIds.Any(e => e.Equals(x.Id)));
                List<Dictionary<String, object>> lst = new List<Dictionary<string, object>>();
                List<System.Threading.Tasks.Task> tasks = new();
                foreach (var elem in filtered)
                {
                    var dic = new Dictionary<String, object>();
                    foreach (var label in fields)
                    {
                        dic.Add(label, elem.GetType().GetProperty(label).GetValue(elem));
                    }
                    if (onlyThisIds != null)
                        tasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() =>
                        {
                            var bAux = BusinessClass;
                            try
                            {
                                bAux = Activator.CreateInstance(bAux.GetType(), Owner) as IPtfkBusiness<T>;
                            }
                            catch (Exception) { }
                            var workflow = Tools.GetIWorkflow<T>(Owner, bAux.Read(elem.Id).Result, bAux).Result;
                            var permission = false;
                            try { permission = workflow != null && workflow.CheckPermissionOnCurrentTask(elem.Id); }
                            catch (Exception) { }

                            var completed = false;
                            try { completed = workflow.Finished(); }
                            catch (Exception) { }

                            var initial = false;
                            try { initial = workflow.GetCurrentTask().IsFirstTask(); }
                            catch (Exception) { }

                            dic.Add(Constants.OutboundData.TaskPermissionLabel, permission);
                            dic.Add(Constants.OutboundData.TaskInitialLabel, initial);
                            dic.Add(Constants.OutboundData.TaskCompletedLabel, completed);
                        }
                            ));
                    lst.Add(dic);
                }
                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
                return lst;
            }
            catch (Exception ex)
            {
                ErrorTable.Err003();
            }
            return null;
        }

        private Func<T, T> CreateNewStatement(IEnumerable<string> fields)
        {
            // input parameter "o"
            var xParameter = Expression.Parameter(typeof(T), "o");

            // new statement "new Data()"
            var xNew = Expression.New(typeof(T));

            // create initializers
            var bindings = fields.Select(o => o.Trim())
                .Select(o =>
                {

                    // property "Field1"
                    var mi = typeof(T).GetProperty(o);

                    // original value "o.Field1"
                    var xOriginal = Expression.Property(xParameter, mi);

                    // set value "Field1 = o.Field1"
                    return Expression.Bind(mi, xOriginal);
                }
            );

            // initialization "new Data { Field1 = o.Field1, Field2 = o.Field2 }"
            var xInit = Expression.MemberInit(xNew, bindings);

            // expression "o => new Data { Field1 = o.Field1, Field2 = o.Field2 }"
            var lambda = Expression.Lambda<Func<T, T>>(xInit, xParameter);

            // compile to Func<Data, Data>
            return lambda.Compile();
        }

        private IQueryable<PtfkForm<T>> _CurrentDataItems;
        public IQueryable<IPtfkForm> ApplyFilter(PtfkFilter filter, IQueryable<IPtfkForm> source)
        {
            var param = Expression.Parameter(typeof(T), "p");
            var isPtfkWorker = typeof(T).GetInterfaces().Contains(typeof(IPtfkWorker));
            var temp = (isPtfkWorker ? source : (source as IQueryable<PtfkForm<T>>)).Cast<T>();
            temp = temp.OrderBy(filter.FilteredProperties[filter.OrderByColumnIndex > 0 ? filter.OrderByColumnIndex - 1 : 0], filter.OrderByAscending);
            if (string.IsNullOrWhiteSpace(filter.FilteredValue))
            {
                filter.Applied = true;

                if (isPtfkWorker && filter.PreFilterDatetime != DateTime.MinValue)
                    return temp.Cast<IPtfkWorker>().Where(x => x.Date > filter.PreFilterDatetime).Skip(filter.PageIndex * filter.PageSize).Take(filter.PageSize).ToList().Cast<IPtfkForm>().AsQueryable();
                return temp.Skip(filter.PageIndex * filter.PageSize).Take(filter.PageSize).ToList().Cast<IPtfkForm>().AsQueryable();
            }
            var result = new List<long>();

            if (isPtfkWorker && filter.PreFilterDatetime != DateTime.MinValue)
            {
                var date = nameof(IPtfkWorker.Date);
                DateTime dt = filter.PreFilterDatetime;
                DateTime.TryParse(filter.FilteredValue, out dt);

                var condition =
Expression.Lambda<Func<T, bool>>(

Expression.GreaterThan(
Expression.Property(param, date),
Expression.Constant(dt, typeof(DateTime))
),

param
);
                result.AddRange(temp.Where(condition).Select(x => x.Id));
                if (!result.Any())
                {
                    filter.Applied = true;
                    return (new List<IPtfkForm>()).AsQueryable();
                }
            }


            foreach (String prop in filter.FilteredProperties)
            {
                if (GetType().GetProperty(prop).PropertyType == typeof(string))
                {
                    //contains
                    String Ids = "";
                    var method = Ids.GetType().GetMethod(nameof(Ids.Contains), new Type[] { typeof(String) });
                    var call = Expression.Call(Expression.Property(param, prop), method, Expression.Constant(filter.FilteredValue));
                    var nullCheck = Expression.NotEqual(Expression.Property(param, prop), Expression.Constant(null));
                    var condition = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(nullCheck, call), param);
                    try
                    {
                        result.AddRange(temp.Where(condition).Select(x => x.Id));
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                    if (GetType().GetProperty(prop).PropertyType == typeof(DateTime) || GetType().GetProperty(prop).PropertyType == typeof(DateTime?))
                {
                    //convert 
                    DateTime dt = DateTime.MinValue;
                    DateTime.TryParse(filter.FilteredValue, out dt);
                    if (dt != DateTime.MinValue)
                    {
                        var dtI = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
                        var dtF = new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);
                        var condition =
Expression.Lambda<Func<T, bool>>(
    Expression.And(

    Expression.GreaterThanOrEqual(
        Expression.Property(param, prop),
        Expression.Constant(dtI, typeof(DateTime))
    ),
    Expression.LessThanOrEqual(
        Expression.Property(param, prop),
        Expression.Constant(dtF, typeof(DateTime))
    )
    ),
    param
); // for LINQ to SQl/Entities skip Compile() call
                        result.AddRange(temp.Where(condition).Select(x => x.Id));

                    }
                }
                else
                if (GetType().GetProperty(prop).PropertyType == typeof(bool) || GetType().GetProperty(prop).PropertyType == typeof(bool?))
                {
                    if (Tools.GetBoolValue(filter.FilteredValue) ||
                        filter.FilteredValue.ToLower().Equals("sim") ||
                        filter.FilteredValue.ToLower().Equals("yes") ||
                        filter.FilteredValue.ToLower().Equals("não") ||
                        filter.FilteredValue.ToLower().Equals("no") ||
                        filter.FilteredValue.ToLower().Equals("nao")
                        )
                    {

                        //equals bool
                        var condition =
                        Expression.Lambda<Func<T, bool>>(
                            Expression.Equal(
                                Expression.Convert(
                                    Expression.Property(param, prop), typeof(bool)),
                                Expression.Constant(Tools.GetBoolValue(filter.FilteredValue), typeof(bool))
                            ),
                            param
                        );
                        try
                        {
                            result.AddRange(temp.Where(condition).Select(x => x.Id));
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                else
                {
                    try
                    {
                        //TODO verificar outros tipos menos comuns
                        //equals
                        var condition =
                         Expression.Lambda<Func<T, bool>>(
                             Expression.Equal(
                                 Expression.Convert(Expression.Property(param, prop), typeof(string)),
                             Expression.Constant(filter.FilteredValue)
                             ),
                             param
                         ); // for LINQ to SQl/Entities skip Compile() call

                        result.AddRange(temp.Where(condition).Select(x => x.Id));
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            if (result.Any())
            {
                filter.Applied = true;
                return source.Cast<IPtfkForm>().Where(x => result.Distinct().Contains(x.Id));
            }
            return source.Cast<IPtfkForm>();
        }

        private string[] GetListProperties()
        {
            var validInputs = GetInputs(InputType.WithListing, InputType.OnlyForListing).ToList();
            var hiddenInputs = GetInputs(InputType.Hidden).ToList();
            hiddenInputs.ForEach(x => x.Value.IsImplicit = true);
            try
            {
                var values = InputType.GetValues(typeof(InputType)).OfType<InputType>().ToList();
                validInputs.AddRange(GetInputs(values.ToArray()).Where(x => x.Value.IsImplicit));
            }
            catch (Exception ex)
            {
            }
            return validInputs.Select(x => x.Key.Name).ToArray();
        }

        public String GetClientSideContext(String attributeName)
        {
            var att = GetAttributeInfo(attributeName);
            if (att.OptionsType == RequestMode.ClientSide)
            {
                return att.OptionsContext;
            }
            return String.Empty;
        }

        public String GetContext(String attributeName)
        {
            var att = GetAttributeInfo(attributeName);
            return att.OptionsContext;
        }

        public string GetSubformEntityName(String attributeName)
        {
            var val = GetAttributeInfo(attributeName).SubformEntityName;
            return val;
        }

        internal FormCaptionAttribute GetAttributeInfo(String propertyName)
        {
            if (Captions != null)
            {
                try
                {
                    return Captions.Where(x => x.Key.Name.Equals(propertyName)).FirstOrDefault().Value;
                }
                catch (Exception ex)
                {
                }
            }
            else
                GetAttributeByNameValue("", "");//Executa essa linha para certificar de que todos os campos foram incluidos
            if (Captions != null)
            {
                try
                {
                    return Captions.Where(x => x.Key.Name.Equals(propertyName)).FirstOrDefault().Value;
                }
                catch (Exception ex)
                {
                }
            }
            return null;
        }

        protected IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> GetAttributeByNameValue(String attributePropertyName, object value)
        {
            var name = attributePropertyName;

            if (Captions != null)
            {

                if (String.IsNullOrWhiteSpace(attributePropertyName))
                    return Captions;

                var retorno = Captions.Where(x => Convert.ChangeType(x.Value.GetType().GetProperty(attributePropertyName).GetValue(x.Value, null), value.GetType()).Equals(value));
                var list = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();
                foreach (var item in Captions)
                {
                    try
                    {
                        var captionAttValue = item.Value.GetType().GetProperty(attributePropertyName).GetValue(item.Value, null);
                        if (Convert.ChangeType(captionAttValue, value.GetType()).Equals(value))
                        {
                            list.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                if (list.Any())
                    return list.ToList();
            }

            PropertyInfo[] props = GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                object[] attrs = prop.GetCustomAttributes(true);
                if (attrs.Length == 0)
                    attrs = prop.DeclaringType.GetCustomAttributes(true);
                foreach (object attr in attrs)
                {
                    var model = attr as Microsoft.AspNetCore.Mvc.ModelMetadataTypeAttribute;
                    if (model != null && model.MetadataType != null)
                        foreach (var metadata in model.MetadataType.GetProperties())
                        {
                            foreach (var metaAttr in metadata.GetCustomAttributes(true))
                            {
                                FormCaptionAttribute authAttr = metaAttr as FormCaptionAttribute;
                                if (authAttr != null)
                                {
                                    AddCaption(metadata, authAttr);
                                }
                            }
                        }
                    else
                    {
                        var modelAttrs = attr as FormCaptionAttribute;
                        if (modelAttrs != null)
                        {
                            AddCaption(prop, modelAttrs);
                        }
                    }
                }
            }

            if (Captions != null)
            {

                if (String.IsNullOrWhiteSpace(attributePropertyName))
                    return Captions;

                var retorno = Captions.Where(x => Convert.ChangeType(x.Value.GetType().GetProperty(attributePropertyName).GetValue(x.Value, null), value.GetType()).Equals(value));
                var list = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();
                foreach (var item in Captions)
                {
                    try
                    {
                        var captionAttValue = item.Value.GetType().GetProperty(attributePropertyName).GetValue(item.Value, null);
                        if (Convert.ChangeType(captionAttValue, value.GetType()).Equals(value))
                        {
                            list.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                if (list.Any())
                    return list.ToList();
            }
            return new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();
        }

        public string GetIdAttributeName()
        {
            var name = nameof(FormCaptionAttribute.PrimaryKey);
            var list = GetAttributeByNameValue(name, true).Select(x => x.Key.Name);
            if (list.Any())
                return list.FirstOrDefault();

            return "";
        }

        public List<string> GetPasswordMaskAttributeNames()
        {
            var name = nameof(FormCaptionAttribute.HasPasswordMask);
            var list = GetAttributeByNameValue(name, true).Select(x => x.Key.Name);
            if (list.Any())
                return list.ToList();

            return new List<string>();
        }

        internal IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> GetAllCaptions()
        {
            var temp = GetAttributeByNameValue("", null);//Executa essa linha para certificar de que todos os campos foram incluidos
            return Captions;
        }

        public IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> GetInputs(params InputType[] returnWithListing)
        {
            var restrictionsFromWorkflow = new List<string>();
            if (IsCache && CurrentGenerator?.CurrentPageConfig?.SkipCache == false)
            {
                restrictionsFromWorkflow = CurrentWorkflow.GetInvisibleFieldsOnCurrentTask();
            }

            var temp = GetAllCaptions();
            if (returnWithListing != null && returnWithListing.Length > 0)
            {
                var list = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();
                foreach (var cap in Captions.Where(x => restrictionsFromWorkflow.Count == 0 || restrictionsFromWorkflow.Contains(x.Key.Name)))
                {
                    foreach (InputType inputType in cap.Value.InputType.GetUniqueFlags())
                    {
                        if (returnWithListing.Contains(inputType))
                        {
                            list.Add(cap);
                            break;
                        }
                    }
                }
                return list.OrderBy(o => o.Value.ShowOrder).Distinct();
            }
            return Captions.Where(x => !x.Value.InputType.GetUniqueFlags().Contains(InputType.None)).OrderBy(o => o.Value.ShowOrder);
        }

        public IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> GetSubforms(params InputType[] returnWithListing)
        {
            var temp = GetAttributeByNameValue("", null);//Executa essa linha para certificar de que todos os campos foram incluidos
            if (returnWithListing != null && returnWithListing.Length > 0)
            {
                var list = new List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>();
                foreach (var cap in Captions.Where(x => x.Value.IsSubform))
                {
                    foreach (InputType inputType in cap.Value.InputType.GetUniqueFlags())
                    {
                        if (returnWithListing.Contains(inputType))
                        {
                            list.Add(cap);
                            break;
                        }
                    }
                }
                return list.OrderBy(o => o.Value.ShowOrder).Distinct();
            }
            return Captions.Where(x => !x.Value.IsSubform).OrderBy(o => o.Value.ShowOrder);
        }

        public IEnumerable<KeyValuePair<String, Object>> GetImplicits()
        {
            var temp = GetAttributeByNameValue("", null);//Executa essa linha para certificar de que todos os campos foram incluidos
            var implicitList = Captions.Where(x => x.Value.IsImplicit).Select(x => new KeyValuePair<String, Object>(
                x.Key.Name,
                 GetType().GetProperty(x.Key.Name).GetValue(this, null)
             ));
            return implicitList.Union(_ImplicitsDev);
        }
        private List<KeyValuePair<String, Object>> _ImplicitsDev = new List<KeyValuePair<string, object>>();
        public void AddImplicitValue(String Key, Object Value)
        {
            _ImplicitsDev.Add(new KeyValuePair<string, object>(Key, Value));
        }

        private string GetTextAttributeNameFromList()
        {
            var name = nameof(FormCaptionAttribute.TextLabelOnList);
            var list = GetAttributeByNameValue(name, true).Select(x => x.Key.Name);
            if (list.Any())
                return list.FirstOrDefault();

            return GetIdAttributeName();
        }

        public void SetBusinessClass<T1>(IPtfkBusiness<T1> business)
        {
            _BusinessClass = (IPtfkBusiness<T>)business;
            if (!BaseWorkflow.HasResult)
                BaseWorkflow.BusinessClassLog = System.Threading.Tasks.Task.Factory.StartNew(() => Tools.GetIBusinessLogClass(business.GetType().Assembly)).ContinueWith(antecedent => BaseWorkflow.GetBusinessClassLogResult());
        }

        public void SetOutputMessage(String message)
        {
            if (this.OutputMessage == null)
                this.OutputMessage = new List<string>();
            this.OutputMessage.Add(message);
        }

        /// <summary>
        /// Overridable method responsible for returning a ListItem list that fill selection controls.
        /// </summary>
        /// <returns></returns>
        public virtual List<ListItem> ItemsList()
        {
            var IDattrValue = GetIdAttributeName();
            var textAttrValue = GetTextAttributeNameFromList();
            var retorno = new List<ListItem>();
            var all = HasBusinessRestrictionsBySession() ? BusinessClass.List(Owner) : BusinessClass.List();
            foreach (var item in all)
            {
                try
                {
                    retorno.Add(Tools.NewListItem(item.GetType().GetProperty(textAttrValue).GetValue(item)?.ToString(), item.GetType().GetProperty(IDattrValue).GetValue(item)?.ToString(), false, ""));
                }
                catch (Exception)
                {

                }
            }
            return retorno;
        }

        private IQueryable<T> GetList(IPtfkForm entity, bool onlyByOwner = false)
        {
            if (GetType().GetInterfaces().Contains(typeof(IPtfkLog)))
            {
                var o = (entity != null ? entity : this) as IPtfkLog;
                var b = BusinessClass as ILogBusiness;
                if (!string.IsNullOrWhiteSpace(o.EntityName))
                    return b.ListFromEntity(o.EntityId, o.EntityName) as IQueryable<T>;
            }
            if (onlyByOwner)
                return BusinessClass.List(Owner);
            return HasBusinessRestrictionsBySession() ? BusinessClass.List(Owner) : BusinessClass.List();
        }

        public virtual IQueryable<IPtfkForm> DataItems(IPtfkForm entity)
        {
            var lst = new List<PtfkForm<T>>();
            if (String.IsNullOrWhiteSpace(Owner.Login))
                return new List<IPtfkForm>().AsQueryable();
            if (GetType().GetInterfaces().Contains(typeof(IPtfkWorker)))
            {
                //Adicionar os que o usuário deve tomar ação
                var all = (BusinessClass.ListAll() as IQueryable<IPtfkWorker>).AsEnumerable();
                var process = PtfkCache.GetOrSet<List<String>>("ProcessList", () => all.Select(x => x.Entity).Distinct().ToList(), 30);
                var items = new List<IPtfkWorker>();
                var departmentID = Owner.Department?.ID;
                foreach (var item in process)
                {
                    var perms = Strict.ConfigurationManager.GetPermissions(item).Where(x => x.EnabledTo != null &&
                                                                                            (x.EnabledTo.Contains(Owner.Login) ||
                                                                                             x.EnabledTo.Contains(Constants.TOKEN_USER_ID + Owner.Login) ||
                                                                                             x.EnabledTo.Contains(Constants.TOKEN_DEPARTMENT_ID + departmentID) ||
                                                                                             x.HierarchyFlag)).ToList();
                    if (perms.Any())
                    {
                        var config = BusinessClass.GetConfig(item);
                        var bp = PtfkCache.GetOrSet(nameof(BusinessProcessMap) + "_" + item, () => Tools.FromJson<List<BusinessProcessMap>>(config.BusinessProcess)?.Where(x => x.BusinessProcess.Entity.Equals(item)).FirstOrDefault(), 30);
                        var st = nameof(ServiceTask);
                        var toAdd = from a in all
                                    join b in bp.BusinessProcess.Tasks on a.Tid equals b.ID
                                    where
                                    a.Creator.Equals(Owner.Login) || (
                                    !b.Type.Equals(st) && (perms.Where(x => b.Profiles.Where(r => r.ID.Equals(x.ProfileID) || r.Name.Equals(x.Profile)).Any() && (
                                                                                           (x.EnabledTo.Contains(Owner.Login) ||
                                                                                            x.EnabledTo.Contains(Constants.TOKEN_USER_ID + Owner.Login) ||
                                                                                            x.EnabledTo.Contains(Constants.TOKEN_DEPARTMENT_ID + departmentID))

                                                                                            )

                                                                                           || ((x.HierarchyFlag && a.DelegateTo != null &&
                                                                                               (a.DelegateTo.Contains(Constants.TOKEN_USER_ID + Owner.Login) ||
                                                                                                a.DelegateTo.Contains(Constants.TOKEN_DEPARTMENT_ID + departmentID))
                                                                                               ))

                                                                                             ).Any()
                                            || perms.Where(p => p.IsAdmin.Value).Any()))
                                    select a;
                        items.AddRange(toAdd);
                    }
                    else
                        items.AddRange(all.Where(a => a.Creator.Equals(Owner.Login)));
                }

                foreach (IPtfkWorker item in items.GroupBy(x => x.Id).Select(x => x.FirstOrDefault()).Where(item => item.End.HasValue && item.End.Value))
                {
                    item.Task = PtfkWorkflow<T>.END_TASK_NAME;
                }
                _CurrentDataItems = items.Distinct().Cast<PtfkForm<T>>().AsQueryable();
                return _CurrentDataItems;
            }
            var list = GetList(entity);
            foreach (var item in list)
            {
                lst.Add(item as PtfkForm<T>);
            }
            _CurrentDataItems = lst.AsQueryable();
            return _CurrentDataItems;
        }

        internal virtual String Run(PtfkFormStruct formJson, String formType, IPtfkSession owner)
        {
            Owner = owner;
            return Make(new Petaframework.PageConfig(formType, formJson, Owner));
        }

        public virtual String Run(PtfkFormStruct formJson)
        {
            return Make(new Petaframework.PageConfig(ClassName, formJson, Owner));
        }

        public bool HasPermitionOnCurrentTask(long entityID)
        {
            if (_CurrentWorkflow == null)
                SetCurrentWorkflow();
            return _CurrentWorkflow != null && _CurrentWorkflow.CheckPermissionOnCurrentTask(entityID);
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IPtfkWorkflow<IPtfkForm> CurrentWorkflow
        {
            get
            {
                if (_CurrentWorkflow == null)
                    SetCurrentWorkflow();
                return _CurrentWorkflow as IPtfkWorkflow<IPtfkForm>;
            }
        }

        public bool IsCache { set; private get; }
        public virtual long Id { get => throw new NotImplementedException(String.Format("Property {0} not found in entity {1}", nameof(Id), ClassName)); set => throw new NotImplementedException(String.Format("Property {0} not found in entity {1}", nameof(Id), ClassName)); }

        [NotMapped]
        public List<string> OutputMessage { private set; get; }

        public bool GetCacheFlag() { return IsCache; }

        private IPtfkWorkflow<T> _CurrentWorkflow;
        private void SetCurrentWorkflow()
        {
            try
            {
                _CurrentWorkflow = Tools.GetIWorkflow<T>(Owner, BusinessClass.Read((this as IPtfkEntity).Id).Result, BusinessClass).Result;
            }
            catch (Exception ex)
            {
                _CurrentWorkflow = null;
            }

        }

        public virtual string Make(PageConfig config)
        {
            PtfkException.SetLastOccurrence(Owner, null);
            var containsLog = GetType().GetInterfaces().Contains(typeof(IPtfkLog));
            var containsWorker = GetType().GetInterfaces().Contains(typeof(IPtfkWorker));
            if (!config.CurrDForm.readable)
                Tools.CheckPermission(this, Owner);
            CurrentGenerator = new PtfkGen(config);

            var isListPage = new System.Uri(config.CurrDForm.url).LocalPath.ToLower().EndsWith("/" + ClassName.ToLower() + "/list");

            if (!isListPage && ((config.CurrDForm.method != null && config.CurrDForm.method.ToLower() == Constants.FormMethod.Form) ||
                (!containsLog &&
                !containsWorker &&
                (config.CurrDForm.method == null || config.CurrDForm.method.ToLower() == Constants.FormMethod.Get))))
                CurrentGenerator.LockDatatableView = true;

            T obj = BusinessClass.Read((Convert.ToInt64(config.CurrDForm.ID))).Result;
            if (obj != null)
                (obj as IPtfkForm).Logger = Logger;

            PtfkEntityEventArgs<T> args;
            if (config.CurrDForm.implicitCodes != null)
            {
                foreach (var item in config.CurrDForm.implicitCodes)
                {
                    var p = GetType().GetProperty(item.Key);
                    if (p != null)
                        p.SetValue(this, Convert.ChangeType(item.Value, p.PropertyType));
                }
            }

            if (containsLog)
            {
                var o = this as IPtfkLog;
                var b = BusinessClass as ILogBusiness;
                if (CurrentGenerator.CurrentPageConfig.CurrDForm.readable && !string.IsNullOrWhiteSpace(o.EntityName) && o.EntityId > 0)
                {
                    //Read another Entity
                    config.CurrDForm.ID = o.EntityId;

                    return Tools.GetIPtfkEntityByClassName(GetType().Assembly, o.EntityName, Owner).Run(config.CurrDForm);
                }
            }
            System.Threading.Tasks.Task<IPtfkWorkflow<T>> i = Tools.GetIWorkflow<T>(Owner, obj, BusinessClass);

            switch (TypeDef.GetAction(config.CurrDForm.action))
            {
                case TypeDef.Action.READ:
                    args = new PtfkEntityEventArgs<T>(ref obj);
                    OnReadEntity(args);
                    var element = obj as PtfkForm<T>;
                    element.Owner = Owner;
                    element.IsCache = IsCache;
                    element.FormLabel = FormLabel;

                    if (i.Result != null)
                    {
                        if (i.Result.HasBusinessProcess() && !IsServiceExecution())
                            i.Result.MarkAsRead();

                        if (i.Result.HasBusinessProcess() && !isListPage)
                            CurrentGenerator.LockDatatableView = true;
                        else
                            if (config.CurrDForm?.method?.ToLower() == Constants.FormMethod.Form)
                            CurrentGenerator.LockDatatableView = false;
                        var ret = CurrentGenerator.CurrentPageConfig.CurrDForm.readable ?
                            i.Result.GetReadableState(CurrentGenerator.GetFormObject(element, CurrentGenerator.CurrentPageConfig.CurrDForm.readable)) :
                            i.Result.GetCurrentTaskState(CurrentGenerator.GetFormObject(element, CurrentGenerator.CurrentPageConfig.CurrDForm.readable));

                        return CurrentGenerator.GetJsonForm(ret);
                    }
                    if (config.CurrDForm?.method == null || config.CurrDForm?.method?.ToLower() == Constants.FormMethod.Get)
                        CurrentGenerator.LockDatatableView = false;
                    return CurrentGenerator.GetJsonForm(element);
                case TypeDef.Action.DELETE:
                    obj = BusinessClass.Read((Convert.ToInt64(config.CurrDForm.ID))).Result;
                    (obj as IPtfkForm).Logger = Logger;
                    args = new PtfkEntityEventArgs<T>(ref obj);
                    OnBeforeDeleteEntity(args);
                    if (config.CurrDForm.ID > 0)
                        BusinessClass.Delete(Convert.ToInt64(config.CurrDForm.ID));
                    try
                    {
                        OnAfterDeleteEntity(args);
                    }
                    catch (Exception ex)
                    {
                        Log(Petaframework.Enums.Log.Error, Tools.ToJson(new { ex.Message, ex.StackTrace, ex.InnerException, Owner = Owner.Current.Login }, true, false), ex);
                    }

                    return CurrentGenerator.GetJsonForm(this);
                case TypeDef.Action.LIST:
                    return CurrentGenerator.GetJsonControl(this, config.CurrDForm);
                default://Save Or Update
                    if (containsLog || containsWorker)
                        return null;
                    if (i.Result != null)
                        CurrentGenerator.LockDatatableView = i.Result.HasBusinessProcess();
                    var fStruct = new PtfkFormStruct();
                    var toSave = (T)config.CurrDForm.GetType().GetMethod(nameof(fStruct.GenerateBase)).MakeGenericMethod(typeof(T)).Invoke(config.CurrDForm, new object[] { Owner, i, CurrentGenerator });
                    (toSave as IPtfkForm).Logger = Logger;
                    i = Tools.GetIWorkflow<T>(Owner, toSave, BusinessClass);
                    ProcessTask taskToGetPermissions = i.Result?.GetNextTask(config.CurrDForm);
                    var currentTask = i.Result?.GetCurrentTask();
                    if (taskToGetPermissions != null)
                        (toSave as PtfkForm<T>).CurrentProcessTask = CurrentGenerator.CurrentPageConfig.CurrDForm.AutoSave ? currentTask : taskToGetPermissions;
                    if (CurrentGenerator.CurrentPageConfig.CurrDForm.AutoSave)
                        taskToGetPermissions = currentTask;
                    (toSave as PtfkForm<T>).CurrentGenerator = CurrentGenerator;

                    bool autosave = false;
                    try
                    {
                        autosave = Convert.ToBoolean((toSave as PtfkForm<T>)?.CurrentGenerator?.CurrentPageConfig?.CurrDForm?.AutoSave);
                    }
                    catch (Exception ex) { }

                    if (taskToGetPermissions != null && i.Result != null && !autosave)
                    {
                        if (currentTask != null && i.Result.HasHierarchyFlag(currentTask))
                        {
                            if (Owner?.Department?.DepartmentalHierarchy == null)
                            {
                                ErrorTable.Err016(Owner);
                            }
                            foreach (var item in Owner?.Department?.DepartmentalHierarchy)
                            {
                                if (!Owner.Login.Equals(Owner.Department.BossID))
                                {
                                    taskToGetPermissions.DelegateTo.Add(Constants.TOKEN_USER_ID + Owner.Department.BossID);
                                    break;
                                }
                                else
                                if (!item.BossID.Equals(Owner.Login))
                                {
                                    taskToGetPermissions.DelegateTo.Add(Constants.TOKEN_USER_ID + item.BossID);
                                    break;
                                }
                            }
                        }
                        List<string> permissionsOnCurrentTask = i.Result.GetPermissionsOnCurrentTask(taskToGetPermissions);
                        if (config?.CurrDForm?.ID > 0 && !i.Result.HasHierarchyFlag(currentTask) && taskToGetPermissions?.DelegateTo?.Count<string>() == 0 && (permissionsOnCurrentTask == null || permissionsOnCurrentTask.Count<string>() == 0))
                        {
                            List<string> taskProfileOwner = i.Result.GetLastTaskProfileOwner(currentTask);
                            permissionsOnCurrentTask.AddRange((IEnumerable<string>)taskProfileOwner);
                        }
                        taskToGetPermissions.DelegateTo.AddRange((IEnumerable<string>)permissionsOnCurrentTask);
                    }

                    args = new PtfkEntityEventArgs<T>(toSave, currentTask, i.Result);
                    if (args.Entity?.Logger == null)
                        args.Entity.Logger = Logger;
                    if (IsOverridenMethod(nameof(IntegrationsBeforeSaveEntityAsync)))
                    {
                        var beforeIntegrations = System.Threading.Tasks.Task.Factory.StartNew(() => BackgroundWorker(false, args.Copy(), i));
                    }

                    if (IsOverridenMethod(nameof(OnBeforeSaveEntity)))
                        OnBeforeSaveEntity(args);
                    var hasAfterSaveAsyncEvent = IsOverridenMethod(nameof(IntegrationsAfterSaveEntityAsync));
                    var hasAfterSaveEvent = IsOverridenMethod(nameof(OnAfterSaveEntity));
                    SaveEntity(toSave, config, hasAfterSaveAsyncEvent ? false : (hasAfterSaveEvent ? false : true));

                    args = new PtfkEntityEventArgs<T>(toSave, currentTask, i.Result);


                    if (hasAfterSaveAsyncEvent)
                    {
                        var afterIntegrations = System.Threading.Tasks.Task.Factory.StartNew(() => BackgroundWorker(true, args, i));
                    }
                    if (hasAfterSaveEvent)
                    {
                        var a = new PtfkEntityEventArgs<T>(toSave, currentTask, i.Result);
                        if (a.Entity?.Logger == null)
                            a.Entity.Logger = Logger;
                        var before = Tools.ToJson(a.Entity);
                        OnAfterSaveEntity(a);
                        var after = Tools.ToJson(a.Entity);
                        if (before != after)
                            SaveEntity(a.Entity, config, hasAfterSaveAsyncEvent ? false : true);
                    }
                    if ((toSave as PtfkForm<T>)?.CurrentGenerator?.CurrentPageConfig?.CurrDForm?.AutoSave != true)
                        config?.CurrDForm?.MediaFiles?.Clear();
                    if (i.Result != null && i.Result.HasBusinessProcess())
                    {
                        var str = CurrentGenerator.GetFormObject(toSave);
                        str._SkipProfileCheck = true;
                        return CurrentGenerator.GetJsonForm(i.Result.GetBeforeTaskState(str));
                    }
                    return CurrentGenerator.GetJsonForm(this);
            }
        }

        protected void SaveEntity(T toSave, PageConfig config, bool lastEvent = false)
        {
            foreach (var item in config.CurrDForm.MediaFiles.Select(x => x.EntityProperty).Distinct())
            {
                try
                {

                    var json = Tools.ToJson(config.CurrDForm.MediaFiles.Where(x => x.EntityProperty.EndsWith(item)).Select(x => x.Hash).Distinct());
                    var prop = toSave.GetType().GetProperty(item);
                    if (prop.SetMethod == (MethodInfo)null)
                        prop = toSave.GetType().GetProperty(item.Replace(Constants.EntityPtfkFileInfoPrefix, ""));
                    prop.SetValue(toSave, json);

                }
                catch (Exception ex)
                {
                    ErrorTable.Err018(GetAllCaptions().Where(x => x.Key.Name.Equals(item)).FirstOrDefault().Value.LabelText, ex);
                }
            }
            var task = BusinessClass.Save(toSave);
            task.Wait();
            var saveMedias = true;
            var entityID = (toSave as IPtfkEntity).Id;

            if (config.CurrDForm.MediaFiles.Any())
            {
                var savedMedias = new List<long>();
                foreach (var item in config.CurrDForm.MediaFiles)
                {
                    item.EntityId = entityID;
                    var mediaID = PtfkFileInfo.SaveMedia(task.Result, item, Owner);
                    saveMedias = saveMedias && (mediaID > 0);
                    savedMedias.Add(mediaID);
                }

                foreach (var item in PtfkFileInfo.GetFiles(Owner).Where(x => config.CurrDForm.MediaFiles.Where(y => y.Hash.Equals(x.Hash) &&
                                                                                                               y.EntityName.Equals(x.EntityName)).Any()))
                {
                    item.ParentID = entityID;
                    var newFI = new System.IO.FileInfo(System.IO.Path.Combine(item.FileInfo.Directory.FullName, Guid.NewGuid().ToString() + item.FileInfo.Extension));

                    if (new System.IO.FileInfo(item.FileInfo.FullName).Exists)
                    {
                        item.FileInfo.MoveWithReplace(newFI, Owner);
                        item.FileInfo = newFI;
                    }
                }

                if (!this.IsAutoSave() && lastEvent)
                    config.CurrDForm.MediaFiles.Clear();
                if (!this.IsAutoSave())
                    PtfkFileInfo.InactivateMedias(Owner, entityID, ClassName, savedMedias.Distinct().ToArray());
            }
            if (!saveMedias)
                ErrorTable.Err010();
            task.Wait();
            var iEntity = (toSave as IPtfkEntity);
            if (task.Exception != null)
                throw task.Exception;
            if (iEntity.Id <= 0)
                throw new UnsavedEntityException();
            toSave = task.Result;
            if (iEntity.CurrentWorkflow != null && iEntity.CurrentWorkflow.HasTasks())
                return;
            PtfkCache.GetOrSetSelectOptions(Owner, iEntity.ClassName, iEntity.ItemsList().Where(x => !String.IsNullOrWhiteSpace(x.Text)).ToList());
        }

        private void BackgroundWorker(bool isEnd, PtfkEntityEventArgs<T> args, System.Threading.Tasks.Task<IPtfkWorkflow<T>> i)
        {
            if (isEnd)
            {
                try
                {
                    IntegrationsAfterSaveEntityAsync(args);
                    var config = new PageConfig(ClassName, CurrentGenerator.GetFormObject(args.Entity), Owner);
                    config.CurrDForm.IsIntegrationRun = true;
                    SaveEntity(args.Entity, config, isEnd);
                }
                catch (Exception ex)
                {
                    Log(Petaframework.Enums.Log.Error, "Error on " + nameof(IntegrationsAfterSaveEntityAsync), ex);
                }
            }
            else
            {
                try
                {
                    IntegrationsBeforeSaveEntityAsync(args);
                    var config = new PageConfig(ClassName, CurrentGenerator.GetFormObject(args.Entity), Owner);
                    config.CurrDForm.IsIntegrationRun = true;
                    SaveEntity(args.Entity, config);
                }
                catch (Exception ex)
                {
                    Log(Petaframework.Enums.Log.Error, "Error on " + nameof(IntegrationsBeforeSaveEntityAsync), ex);
                }
            }
        }

        internal List<IPtfkEntityJoin> _JoinedEntities;
        public List<IPtfkEntityJoin> GetJoinedEntities(string propertyName, long entityToId)
        {
            return GetJoinedEntities(propertyName).Where(x => x.EntityToId.Equals(entityToId)).ToList();
        }
        public List<IPtfkEntityJoin> GetJoinedEntities(string propertyName)
        {
            if (Captions == null)
                GetAttributeByNameValue("", null);
            var caption = Captions.Where(x => x.Key.Name.Equals(propertyName) || x.Value.MirroredOf.Equals(propertyName)).FirstOrDefault();
            if (_JoinedEntities == null || _JoinedEntities.Where(x => x.PropertyFrom.Equals(caption.Value.MirroredOf)).Count() == 0)
                ReadJoinedEntities(propertyName);
            var id = (this as PetaframeworkStd.Interfaces.IEntity).Id;
            var className = ClassName;
            return _JoinedEntities.Where(x => x.EntityFrom.Equals(className) && x.EntityFromId.Equals(id) && x.PropertyFrom.Equals(caption.Value.MirroredOf)).ToList();
        }
        public List<IPtfkEntityJoin> ListJoinedEntities() { return _JoinedEntities; }
        public void ClearJoinedEntities(List<IPtfkEntityJoin> startList)
        {
            _JoinedEntities = startList;
        }
        string _locker = "";
        private void ReadJoinedEntities(string propertyName)
        {
            var id = (this as PetaframeworkStd.Interfaces.IEntity).Id;
            lock (_locker)
                if (id > 0)
                {
                    PtfkEntityJoined.BusinessClassMedia = Tools.GetIBusinessEntityJoinClass(BusinessClass.GetType().Assembly);
                    var instance = Activator.CreateInstance(PtfkEntityJoined.BusinessClassMedia.GetType(), new object[] { Owner });

                    var method = PtfkEntityJoined.BusinessClassMedia.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.List)) && x.GetParameters().Length == 0).FirstOrDefault();
                    var list = method.Invoke(PtfkEntityJoined.BusinessClassMedia, null) as IQueryable<IPtfkEntityJoin>;

                    var caption = Captions.Where(x => x.Key.Name.Equals(propertyName)).FirstOrDefault();
                    var className = ClassName;

                    if (CurrentGenerator?.CurrentPageConfig?.CurrDForm?.AutoSave == true)
                    {
                        var fromDB = BusinessClass.Read(id).Result;
                        if (!String.IsNullOrWhiteSpace(caption.Value.MirroredOf))
                        {
                            var fromDbObj = fromDB.GetType().GetProperty(caption.Value.MirroredOf).GetValue(fromDB);
                            if (fromDbObj != null)
                            {
                                var fromEntityValue = Tools.FromJson<Dictionary<String, List<String>>>(fromDbObj.ToString());
                                if (fromEntityValue != null)
                                {
                                    if (_JoinedEntities == null)
                                        _JoinedEntities = new List<IPtfkEntityJoin>();

                                    foreach (var item in fromEntityValue.FirstOrDefault().Value)
                                    {
                                        _JoinedEntities.Add(
                                       new PtfkEntityJoined
                                       {
                                           EntityFrom = ClassName,
                                           EntityFromId = id,
                                           EntityTo = caption.Value.OptionsContext,
                                           EntityToId = Convert.ToInt64(item),
                                           PropertyFrom = caption.Value.MirroredOf
                                       }
                                       );
                                    }
                                }
                            }
                        }
                    }

                    var entities = (from m in list
                                    where m.EntityFrom.Equals(className) && m.EntityFromId.Equals(id) &&
                                    m.PropertyFrom.Equals(caption.Value.MirroredOf) &&
                                    string.IsNullOrWhiteSpace(m.IsDeath)
                                    select m);
                    if (_JoinedEntities == null)
                        _JoinedEntities = new List<IPtfkEntityJoin>();
                    lock (_JoinedEntities)
                        foreach (IPtfkEntityJoin item in entities)
                        {
                            _JoinedEntities.Add(item);
                        }
                }
                else
                    if (_JoinedEntities == null)
                    _JoinedEntities = new List<IPtfkEntityJoin>();
        }

        public void SetJoinedEntities(List<String> toEntityIds, string propertyName)
        {
            if (Captions == null)
                GetAttributeByNameValue("", null);
            var cap = Captions.Where(x => x.Key.Name.Equals(propertyName)).FirstOrDefault();
            if (_JoinedEntities == null)
                ReadJoinedEntities(cap.Value.MirroredOf);
            var curr = GetJoinedEntities(cap.Value.MirroredOf);

            var prop = GetType().GetProperty(cap.Value.MirroredOf);
            var vObj = prop.GetValue(this);
            var val = String.Empty;
            if (vObj != null)
                val = vObj.ToString();

            if (String.IsNullOrWhiteSpace(val))
            {
                var e = new Dictionary<String, List<String>>();
                e[cap.Value.OptionsContext] = toEntityIds;
                prop.SetValue(this, Tools.ToJson(e));
            }
            else
            {
                var arr = Tools.FromJson<Dictionary<String, List<String>>>(val);
                arr[cap.Value.OptionsContext].AddRange(toEntityIds);
                arr[cap.Value.OptionsContext] = arr[cap.Value.OptionsContext].Distinct().ToList();
            }
            foreach (var item in toEntityIds.Select(x => Convert.ToInt64(x)))
            {
                var lst = curr.Where(x => x.EntityToId.Equals(item));
                if (!lst.Any())
                {
                    var ent = new PtfkEntityJoined
                    {
                        EntityFrom = ClassName,
                        EntityFromId = (this as PetaframeworkStd.Interfaces.IEntity).Id,
                        EntityTo = cap.Value.OptionsContext,
                        EntityToId = item,
                        IsDeath = "",
                        PropertyFrom = cap.Value.MirroredOf
                    };
                    _JoinedEntities.Add(ent);
                }
                else
                    lst.First().IsDeath = "";
            }
        }
    }

    [Serializable]
    public class KeyCaptionRequired<K, C, R>
    {
        public K Key
        { get; set; }

        public C Caption
        { get; set; }

        public R Required
        { get; set; }

        public KeyCaptionRequired(K _Key, C _Caption, R _Required)
        {
            Key = _Key;
            Caption = _Caption;
            Required = _Required;
        }
        public KeyCaptionRequired()
        { }
    }


    public class Validate
    {
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Email { get; set; }
        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Required { get; set; }
        [JsonProperty("minlength", NullValueHandling = NullValueHandling.Ignore)]
        public int? Minlength { get; set; }
        [JsonProperty("messages", NullValueHandling = NullValueHandling.Ignore)]
        public Messages Messages { get; set; }
        [JsonProperty("equalTo", NullValueHandling = NullValueHandling.Ignore)]
        public string EqualTo { get; set; }
        [JsonProperty("number", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Number { get; set; }
        [JsonProperty("digits", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Digits { get; set; }
    }

    public class Messages
    {
        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        public string Required { get; set; }
        [JsonProperty("minlength", NullValueHandling = NullValueHandling.Ignore)]
        public string Minlength { get; set; }
        [JsonProperty("equalTo", NullValueHandling = NullValueHandling.Ignore)]
        public string EqualTo { get; set; }
        [JsonProperty("number", NullValueHandling = NullValueHandling.Ignore)]
        public string Number { get; set; }
        [JsonProperty("digits", NullValueHandling = NullValueHandling.Ignore)]
        public string Digits { get; set; }
    }

    public class Errors
    {
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { get; set; }
        [JsonProperty("checked", NullValueHandling = NullValueHandling.Ignore)]
        public string Checked { get; set; }
    }

    public class Option
    {
        [JsonProperty("selected", NullValueHandling = NullValueHandling.Ignore)]
        public string Selected { get; set; }
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }
        [JsonProperty("html", NullValueHandling = NullValueHandling.Ignore)]
        public string Html { get; set; }

        [JsonProperty("autoOpen", NullValueHandling = NullValueHandling.Ignore)]
        public bool AutoOpen { get; set; }
        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int Height { get; set; }
        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public int Width { get; set; }
        [JsonProperty("modal", NullValueHandling = NullValueHandling.Ignore)]
        public bool Modal { get; set; }
        [JsonProperty("buttons", NullValueHandling = NullValueHandling.Ignore)]
        public string Buttons { get; set; }
        [JsonProperty("close", NullValueHandling = NullValueHandling.Ignore)]
        public string Close { get; set; }
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
    }


    public class HtmlElement
    {
        [JsonIgnore]
        internal KeyValuePair<PropertyInfo, FormCaptionAttribute> CurrentFormCaption { get; set; }

        [JsonIgnore]
        public string MirroredOf { get; set; }

        [JsonIgnore]
        public bool Readable { get; set; }
        [JsonIgnore]
        public ReadableFieldType ReadableType { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { get; set; }
        [JsonProperty("stage", NullValueHandling = NullValueHandling.Ignore)]
        public string Stage { get; set; }
        [JsonProperty("tooltip", NullValueHandling = NullValueHandling.Ignore)]
        public string Tooltip { get; set; }
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; protected set; }
        [JsonProperty("placeholder", NullValueHandling = NullValueHandling.Ignore)]
        public string Placeholder { get; set; }
        [JsonProperty("validate", NullValueHandling = NullValueHandling.Ignore)]
        public Validate Validate { get; set; }
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        [JsonProperty("class", NullValueHandling = NullValueHandling.Ignore)]
        public string Class { get; set; }
        [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        public List<Option> Options { get; set; }

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int? Size { get; set; }
        [JsonProperty("html", NullValueHandling = NullValueHandling.Ignore)]
        public List<HtmlElement> Html { get; set; }

        private object _value;
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public object Value
        {
            get { return _value; }
            set
            {
                if (value is String)
                {
                    _value = value.ToString().Replace('"', '\'').Replace(System.Environment.NewLine, String.Empty).Replace(@"\", @"/");
                }
                else
                    _value = value;
            }
        }

        public void SetValue(object value, params MaskTypeEnum[] mask)
        {
            Value = value;
            PlainValue = value;
            if (mask != null && mask.Length > 0)
                Mask = mask[0].ToString();
        }

        [JsonIgnore]
        internal object PlainValue { get; set; }

        [JsonProperty("readonly", NullValueHandling = NullValueHandling.Ignore)]
        public bool Readonly { get; set; }

        [JsonProperty("maxLength", NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxLength { get; set; }

        [JsonProperty("mask", NullValueHandling = NullValueHandling.Ignore)]
        public string Mask { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ElementType TypeSetter
        {
            set
            {
                Type = value.ToString().Replace("_", "-").ToLower();
            }
        }

        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public string Context { get; set; }

        [JsonProperty("entity", NullValueHandling = NullValueHandling.Ignore)]
        public string Entity { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
        [JsonProperty("mode", NullValueHandling = NullValueHandling.Ignore)]
        public string Mode { get; set; }
        [JsonProperty("default", NullValueHandling = NullValueHandling.Ignore)]
        public string Default { get; set; }

        [JsonProperty("showOrder", NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore]
        public int ShowOrder { get; set; }

        [JsonProperty("grpId", NullValueHandling = NullValueHandling.Ignore)]
        public int GroupId { get; set; }

        [JsonProperty("grpList", NullValueHandling = NullValueHandling.Ignore)]
        public List<KeyValuePair<int, string>> GroupList { get; set; }
    }
}