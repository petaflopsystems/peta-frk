using Newtonsoft.Json.Linq;
using Petaframework.Interfaces;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using static Petaframework.TypeDef;

namespace Petaframework
{
    [NotMapped]
    public class PtfkGen
    {
        private String _ownerID;
        private TypeDef.Action _ACTION;
        private PageConfig _config;
        private List<KeyValuePair<String, List<ListItem>>> _ListItems;
        private List<IPtfkEntity> _ListDatatableItems;

        private List<ListItem> ManagerListItems(String name, IPtfkForm obj)
        {
            if (_ListItems == null)
                _ListItems = new List<KeyValuePair<string, List<ListItem>>>();
            var item = _ListItems.Where(x => x.Key.Equals(name)).FirstOrDefault();
            if (!String.IsNullOrWhiteSpace(item.Key))
                return item.Value;
            else
            {
                var list = obj.GetSelectOptions(name);
                _ListItems.Add(new KeyValuePair<string, List<ListItem>>(name, list));
                return list;
            }
        }
        public bool LockDatatableView { get; set; } = false;

        public PtfkGen(PageConfig config)
        {
            _ACTION = TypeDef.GetAction(config.Action);
            if (_ACTION == TypeDef.Action._NONE && String.IsNullOrWhiteSpace(config.Action))
                _ACTION = TypeDef.Action.CREATE;
            _ownerID = config.Owner.Login;
            _config = config;
            _config.CurrDForm.Session = _config.Owner;
            if (_ACTION == TypeDef.Action.READ)
                PtfkFileInfo.ClearFiles(_config.PageType, _config.Owner, 0, _config.CurrDForm.ID);
        }

        public PageConfig CurrentPageConfig
        {
            get { return _config; }
        }

        public PtfkFormStruct.UserRole GetSelectedUserRole()
        {
            return CurrentPageConfig?.CurrDForm?.userRoles?.Where(x => x.flag).FirstOrDefault();
        }

        private bool _forceRead = false;
        /// <summary>
        /// Forces to generate Form in Read Action (with DataItems)
        /// </summary>     
        public void SetToReadAction(ref IPtfkEntity iPetadata)
        {
            _forceRead = true;
            iPetadata.Id = 0;
            _ACTION = TypeDef.Action.READ;
            PtfkFileInfo.ClearFiles(_config.PageType, _config.Owner, 0, iPetadata.Id);
        }

        public PtfkGen(TypeDef.Action action, IPtfkSession session)
        {
            _ACTION = action;
            _config = new PageConfig(session);
        }

        public TypeDef.Action GetAction()
        {
            return _ACTION;
        }

        public PtfkFormStruct ChangeFormObject(PtfkFormStruct currStruct, IPtfkForm obj)
        {
            var t = obj.GetType();
            foreach (var item in currStruct.html[0].Html.Where(x => !String.IsNullOrWhiteSpace(x.Name)))
            {
                var p = t.GetProperty(item.Name);
                if (p != null)
                    item.PlainValue = p.GetValue(obj);
                if (item.PlainValue is IList && item.PlainValue.GetType().IsGenericType)
                {
                    var str = new List<String>();
                    var l = (item.PlainValue as IList<String>);
                    if (l != null)
                    {
                        var c = 1;
                        foreach (var v in l)
                        {
                            var i = item.Options.Where(x => x.Value.Equals(v)).FirstOrDefault();
                            str.Add("[" + (c++) + "] " + i.Html);
                        }

                        item.PlainValue = String.Join("</br>", str.ToArray());
                    }
                }
            }

            return currStruct;
        }


        private int _DuplicityCount = 3;
        public PtfkFormStruct GetFormObject(IPtfkForm obj, bool readable = false)
        {
            var iEntity = obj as IPtfkEntity;

            var fromCache = CurrentPageConfig?.SkipCache == true ? null : (readable ?
                PtfkCache.GetOrSetWorkflowEntity<IPtfkEntity>(iEntity) :
                PtfkCache.GetOrSetWorkflowStage<IPtfkEntity>(iEntity, false, null));

            var _allTasks = new List<System.Threading.Tasks.Task>();
            PtfkFormStruct form = new PtfkFormStruct();
            form.readable = readable;
            form.action = "";
            form.method = LockDatatableView ? Constants.FormMethod.Form : Constants.FormMethod.Get;
            if (obj.OutputMessage != null && obj.OutputMessage.Count() > 0)
                form.message = string.Join("</br>", obj.OutputMessage.ToArray());
            form.Session = CurrentPageConfig.Owner;
            form.ownerID = _ownerID;
            form.html = new List<HtmlElement>();
            HtmlElement html = new HtmlElement(), htmlFieldSet = new HtmlElement();
            htmlFieldSet.Html = new List<HtmlElement>();

            var idAttributeName = obj.GetIdAttributeName();
            if (String.IsNullOrWhiteSpace(idAttributeName))
                ErrorTable.Err008();
            form.ID = Convert.ToInt64(obj.GetType().GetProperty(idAttributeName).GetValue(obj, null));
            if (form.ID <= 0)
                form.GUID = Guid.NewGuid().ToString();
            if (!_forceRead && form.ID > 0 && (_ACTION == TypeDef.Action.CREATE || (_ACTION == TypeDef.Action.READ && obj.ParentID <= 0)))
                _ACTION = TypeDef.Action.UPDATE;

            System.Threading.Tasks.Task taskDataItems = System.Threading.Tasks.Task.CompletedTask;
            if (_ACTION != TypeDef.Action.UPDATE && !LockDatatableView)
                taskDataItems = System.Threading.Tasks.Task.Factory.StartNew(() => form.dataItems = GetHtmlItems(_config.ServerSideSearchMode ? GetEmptyDataItem(obj) : obj.GetDataItems(null)));

            form.implicitCodes = obj.GetImplicits().ToList();

            htmlFieldSet.Html.Add(new HtmlElement
            {
                TypeSetter = ElementType.text,
                Caption = Tools.EncodeBase64(obj.GetLabel(idAttributeName)),
                Name = idAttributeName,
                Readonly = true,
                Id = Constants.EntityIdentityPrefix + "lblID",
                Readable = true,
                ShowOrder = -1,
                ReadableType = ReadableFieldType.Always,
                Tooltip = Tools.EncodeBase64(obj.GetTooltip(idAttributeName))
            });

            PtfkFormStruct cloneForm = null;
            if (!CurrentPageConfig?.SkipCache == true)
            {
                if (!obj.GetCacheFlag() && fromCache != null && fromCache.html?[0].Html.Count > 1)
                {

                    cloneForm = fromCache.GetType() == typeof(PtfkFormStruct) ? Tools.FromJson<PtfkFormStruct>(Tools.ToJson(fromCache, true)) : Tools.FromJson<PtfkFormStruct>(fromCache.ToString());
                    var tp = obj.GetType();
                    var lst = (List<KeyValuePair<PropertyInfo, FormCaptionAttribute>>)obj.GetType().GetMethod(nameof(PtfkForm<IPtfkEntity>.GetAllCaptions), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(obj, null);
                    foreach (var item in cloneForm?.html?[0].Html)
                    {
                        var h = new HtmlElement();
                        var cap = lst.Where(x => x.Key.Name.Equals(item.Name)).FirstOrDefault();
                        SetHtmlValue(ref h, cap, obj);
                        item.Value = h.Value;
                        item.PlainValue = h.PlainValue;
                        item.ReadableType = h.ReadableType;
                        item.Readable = h.Readable;
                        item.MirroredOf = h.MirroredOf;
                        item.CurrentFormCaption = cap;

                        if (item.Options != null && item.Options.Any())
                        {
                            var insert = false;
                            if (item.Options.FirstOrDefault().Html.StartsWith("--"))
                                insert = true;
                            var optCache = PtfkCache.GetOrSetSelectOptions(form.Session, item.Name);
                            var t = (optCache != null && optCache.Any() ? optCache : obj.GetSelectOptions(item.Name));
                            if (t != null)
                            {
                                item.Options = t.Select(x => new Option { Description = x.Description, Html = x.Text, Value = x.Value }).ToList();
                                if (insert)
                                    item.Options.Insert(0, new Option { Value = "", Html = Constants.DefaultListSelectOption });
                            }
                        }
                    }
                    cloneForm.url = CurrentPageConfig.CurrDForm.url;
                    cloneForm.userRoles = CurrentPageConfig.Owner?.Roles?.Select(x => new PtfkFormStruct.UserRole { guid = x.ID, name = x.Name + " - " + (CurrentPageConfig.Owner?.Department?.DepartmentalHierarchy?.LastOrDefault() != null ? (CurrentPageConfig.Owner?.Department?.DepartmentalHierarchy?.LastOrDefault().Name + " - ") : "") + x.Department?.Name }).ToList();
                    cloneForm.ID = iEntity.Id;
                }
            }

            if (cloneForm != null)
            {
                htmlFieldSet.Html = cloneForm.html[0].Html;
                htmlFieldSet.GroupList = cloneForm.html[0].GroupList;
            }
            else
            {
                var currInputTypes = GetCurrentInputTypes();

                _allTasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() => PopulateHtml(obj, ref html, currInputTypes, htmlFieldSet)));
                _allTasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() => PopulateSubforms(obj, ref html, currInputTypes, htmlFieldSet)));

                System.Threading.Tasks.Task.WaitAll(_allTasks.ToArray());
            }

            if (!form.readable)
                htmlFieldSet.Html = htmlFieldSet.Html.OrderBy(x => x.ShowOrder).ToList();
            else
                htmlFieldSet.Html = htmlFieldSet.Html.Where(x => x.Readable).OrderBy(x => x.ShowOrder).ToList();

            htmlFieldSet.Caption = Tools.EncodeBase64(obj.FormLabel);
            htmlFieldSet.TypeSetter = ElementType.fieldset;
            if (cloneForm == null || CurrentPageConfig?.SkipCache == true)
            {
                if (!form.readable)
                {
                    if (htmlFieldSet.Html.Count > 1)
                    {
                        htmlFieldSet.Html.Add(new HtmlElement { TypeSetter = ElementType.submit, Value = "Salvar", Id = Constants.EntityIdentityPrefix + "btnSave_" + _config?.PageType });
                        htmlFieldSet.Html.Add(new HtmlElement { TypeSetter = ElementType.reset, Value = "Limpar" });
                    }
                    else
                    {
                        htmlFieldSet.Html.Clear();
                        htmlFieldSet.Mode = InputType.SelectOne.ToString().ToLower();
                    }
                }

                htmlFieldSet.Html.Add(new HtmlElement { TypeSetter = ElementType.container, Value = "Limpar", Id = Constants.EntityDatatablePrefix + _config?.PageType, Name = Constants.EntityDatatablePrefix + _config?.PageType });
            }
            form.html.Add(htmlFieldSet);
            if (CurrentPageConfig?.Owner?.Roles?.Count() > 0)
            {
                form.html[0].Stage = iEntity?.CurrentWorkflow?.GetCurrentTask()?.Name;
                form.userRoles = CurrentPageConfig.Owner.Roles.Select(x => new PtfkFormStruct.UserRole { guid = x.ID, name = x.Name + " - " + (CurrentPageConfig.Owner?.Department?.DepartmentalHierarchy?.LastOrDefault() != null ? (CurrentPageConfig.Owner?.Department?.DepartmentalHierarchy?.LastOrDefault().Name + " - ") : "") + x.Department?.Name }).ToList();
            }
            taskDataItems.Wait();

            var checkDuplicates = form.html[0].Html.GroupBy(x => x.Name).Where(g => g.Count() > 1 && !String.IsNullOrWhiteSpace(g.Key)).Select(y => new { Element = y.Key, Counter = y.Count() }).ToList();
            if (_DuplicityCount > 0 && checkDuplicates.Any())
            {
                _DuplicityCount--;
                return GetFormObject(obj, readable);
            }
            if (iEntity.CurrentWorkflow != null && form.html?[0].Html.Count > 1 && !(CurrentPageConfig?.SkipCache == true))
            {
                if (readable)
                    PtfkCache.GetOrSetWorkflowEntity<IPtfkEntity>(iEntity, true, form);
                else
                    PtfkCache.GetOrSetWorkflowStage<IPtfkEntity>(iEntity, true, form);

            }
            return form;
        }

        private void PopulateSubforms(IPtfkForm obj, ref HtmlElement html, InputType[] currInputTypes, HtmlElement htmlFieldSet)
        {
            foreach (var item in obj.GetSubforms(currInputTypes))
            {
                html = new HtmlElement();
                html.Caption = Tools.EncodeBase64(obj.GetLabel(item.Key.Name));
                html.TypeSetter = ElementType.subform;
                html.Name = item.Key.Name;
                html.Tooltip = Tools.EncodeBase64(obj.GetTooltip(item.Key.Name));
                html.Context = obj.GetContext(item.Key.Name);
                html.Readable = IsReadable(item.Value.ReadableType, html);
                if (item.Value.InputType.HasFlag(InputType.SelectOne))
                {
                    try
                    {
                        try
                        {
                            html.Value = item.Key.GetValue(obj, null);
                        }
                        catch (System.Reflection.TargetException)
                        {
                            html.Value = obj.GetType().GetProperty(item.Key.Name).GetValue(obj, null);
                        }
                        html.PlainValue = html.Value;

                        var typeDef = item.Key.PropertyType.GetGenericArguments()[0]; ;
                        var iform = Activator.CreateInstance(typeDef) as IPtfkForm;

                        if (iform != null)
                        {
                            try
                            {
                                html.Default = iform.GetInputs(InputType.SelectOneText).FirstOrDefault().Key.Name;
                            }
                            catch (Exception ex) { }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                html.Entity = obj.GetSubformEntityName(item.Key.Name);
                html.Validate = obj.GetValidate(item.Key.Name);
                if (String.IsNullOrWhiteSpace(html.Mask))
                    html.MaxLength = obj.GetMaxLength(item.Key.Name);
                html.Mode = item.Value.InputType.ToString().ToLower();
                html.ShowOrder = item.Value.ShowOrder;

                htmlFieldSet.Html.Add(html);
            }
        }

        private void PopulateHtml(IPtfkForm obj, ref HtmlElement html, InputType[] currInputTypes, HtmlElement htmlFieldSet)
        {
            var l = obj.GetInputs(currInputTypes).ToList();

            int i = 1;
            var grp = l.Where(x => !String.IsNullOrWhiteSpace(x.Value.GroupBy)).Select(x => x.Value.GroupBy).Distinct().Select(x => new KeyValuePair<int, string>(i++, x)).ToList();

            foreach (var item in l)
            {
                html = new HtmlElement();
                html.CurrentFormCaption = item;
                html.GroupId = grp.Where(x => x.Value.Equals(item.Value.GroupBy)).FirstOrDefault().Key;
                if (ChechkValidFormType(item.Key.PropertyType))
                {
                    if (obj.GetPasswordMaskAttributeNames().Any(x => x.Equals(item.Key.Name)))
                    {
                        html.TypeSetter = ElementType.password;
                    }
                    else
                    if (item.Key.PropertyType.Equals(typeof(DateTime)))
                    {
                        if (item.Value.MaskType == MaskTypeEnum.date_time)
                            html.TypeSetter = ElementType.datetime_local;
                        else
                            html.TypeSetter = ElementType.date;
                    }
                    else
                    if (item.Key.PropertyType.Equals(typeof(byte[])) ||
                        item.Key.PropertyType.Equals(typeof(PtfkFileInfo)) ||
                        item.Key.PropertyType.Equals(typeof(List<PtfkFileInfo>)))
                    {
                        html.TypeSetter = ElementType.uploader;
                        if (!String.IsNullOrWhiteSpace(item.Value.MirroredOf) && _config?.CurrDForm != null && _ACTION == TypeDef.Action.UPDATE && _config?.CurrDForm.ID > 0)
                        {
                            //Copiar o arquivo do banco de dados para a pasta temporaria
                            Tools.FromDbToTemp(obj, item, _config);
                        }
                    }
                    else
                        if (item.Key.PropertyType.Equals(typeof(Boolean)))
                    {
                        if (item.Value.MaxLength == 2)
                        {
                            html.Options = new List<Option>();
                            foreach (var opt in new List<ListItem> {
                                new ListItem("Sim","1"),
                                new ListItem("Não","0")
                            })
                            {

                                var curOption = new Option { Value = opt.Value, Html = opt.Text, Description = "" };

                                html.TypeSetter = ElementType.radiobuttons;

                                if (opt.Selected)
                                    curOption.Selected = "selected";
                                html.Options.Add(curOption);
                            }
                        }
                        else
                            html.TypeSetter = ElementType.checkbox;
                    }
                    else
                        if (!String.IsNullOrEmpty(item.Value.OptionsContext) || item.Value.OptionsType == RequestMode.ClientSide)
                    {
                        html.TypeSetter = ElementType.select;
                    }
                    else
                        if (item.Value.MaxLength > 255)
                        html.TypeSetter = ElementType.textarea;
                    else
                        html.TypeSetter = ElementType.text;


                    html.Caption = Tools.EncodeBase64(obj.GetLabel(item.Key.Name));
                    html.Tooltip = Tools.EncodeBase64(obj.GetTooltip(item.Key.Name));
                    html.Name = item.Key.Name;
                    SetHtmlValue(ref html, item, obj);
                    if (html.Type.Equals(ElementType.select.ToString()))
                    {
                        html.Options = new List<Option>();
                        bool hasDescription = false;
                        foreach (var opt in obj.GetSelectOptions(item.Key.Name).OrderBy(x => x.Text).ToList())//ManagerListItems(item.Key.Name, obj))
                        {
                            var desc = opt.Description;
                            var curOption = new Option { Value = opt.Value, Html = opt.Text };
                            if (!String.IsNullOrWhiteSpace(desc))
                            {
                                hasDescription = true;
                                curOption.Description = desc;
                                html.TypeSetter = ElementType.radiobuttons;
                            }
                            if (opt.Selected)
                                curOption.Selected = "selected";
                            html.Options.Add(curOption);
                        }
                        if (!hasDescription && item.Value.OptionsType == RequestMode.ServerSide)
                        {
                            if (!item.Value.InputType.HasFlag(InputType.SelectMultiple))
                                html.Options.Insert(0, new Option { Value = "", Html = Constants.DefaultListSelectOption });
                            else
                                html.TypeSetter = ElementType.selectmultiple;
                        }
                        else
                            if (item.Value.OptionsType == RequestMode.ClientSide)
                        {
                            html.Class = "ptfk-clientside";
                            html.Context = obj.GetClientSideContext(item.Key.Name);
                        }
                    }
                    html.Mode = item.Value.InputType.ToString().ToLower();
                    html.Entity = item.Value.EntityName;
                    html.Mask = obj.GetMask(item.Key.Name);
                    html.Validate = obj.GetValidate(item.Key.Name);
                    if (String.IsNullOrWhiteSpace(html.Mask))
                        html.MaxLength = obj.GetMaxLength(item.Key.Name);
                    html.ShowOrder = item.Value.ShowOrder;
                    if (htmlFieldSet.GroupList == null)
                        htmlFieldSet.GroupList = new List<KeyValuePair<int, string>>();
                    foreach (var groupBy in grp)
                    {
                        if (!htmlFieldSet.GroupList.Where(x => x.Value.Equals(groupBy.Value)).Any())
                        {
                            htmlFieldSet.GroupList.Add(groupBy);
                        }
                    }
                    //htmlFieldSet.groupList = grp;
                    htmlFieldSet.Html.Add(html);
                }
                else
                {
                    var t = obj.GetType();
                    if (!t.GetInterfaces().Contains(typeof(IPtfkLog)) &&
                    !t.GetInterfaces().Contains(typeof(IPtfkConfig)) &&
                    !t.GetInterfaces().Contains(typeof(IPtfkMedia)) &&
                    !t.GetInterfaces().Contains(typeof(IPtfkWorker)) &&
                    !t.GetInterfaces().Contains(typeof(IPtfkWorkflow<IPtfkForm>)))
                        CurrentPageConfig.Inconsistencies.Add(String.Format("Type {0} of Property {1} not available!" + Environment.NewLine + "Available type: {2}" + Environment.NewLine, item.Key.PropertyType.Name, t.Name + ":" + item.Key.Name, String.Join(Environment.NewLine, AvailableTypes)));
                }
            }
        }

        internal void SetHtmlValue(ref HtmlElement html, KeyValuePair<PropertyInfo, FormCaptionAttribute> item, IPtfkForm obj)
        {
            try
            {
                if (item.Value != null)
                {
                    html.ReadableType = item.Value.ReadableType;
                    html.Readable = IsReadable(item.Value.ReadableType, html);
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                if (item.Key != null)
                    html.MirroredOf = obj.GetMirroredOf(item.Key.Name);
            }
            catch (Exception)
            {

            }
            html.Value = "";
            try
            {
                try
                {
                    html.Value = item.Key?.GetValue(obj, null);
                    if (item.Key?.PropertyType == typeof(Boolean))
                    {
                        var b = Tools.GetBoolValue(html.Value?.ToString());
                        if (item.Value.MaxLength != 2)
                        {
                            if (b)
                                html.Value = "on";
                        }
                        else
                        {
                            if (b)
                                html.Value = "1";
                            else
                                html.Value = "0";
                        }
                    }
                    html.PlainValue = html.Value;
                }
                catch (System.Reflection.TargetException)
                {
                    html.Value = obj.GetType().GetProperty(item.Key.Name).GetValue(obj, null);
                    if (item.Key.PropertyType == typeof(Boolean))
                    {
                        var b = Tools.GetBoolValue(html.Value?.ToString());
                        if (item.Value.MaxLength != 2)
                        {
                            if (b)
                                html.Value = "on";
                        }
                        else
                        {
                            if (b)
                                html.Value = "1";
                            else
                                html.Value = "0";
                        }
                    }
                    html.PlainValue = html.Value;
                }
                if (html.Value != null && html.Value.GetType().Equals(typeof(System.String)))
                {
                    var val = Convert.ToString(html.Value);
                    if (!String.IsNullOrWhiteSpace(val))
                        html.Value = "[{" + Tools.EncodeBase64(val) + "}]";
                    html.PlainValue = val;
                }
            }
            catch (Exception)
            {
            }
        }

        private bool IsReadable(ReadableFieldType readableType, HtmlElement html)
        {
            html.ReadableType = readableType;
            switch (readableType)
            {
                case ReadableFieldType.Never:
                    return false;
                case ReadableFieldType.Always:
                case ReadableFieldType.WhenFilled:
                case ReadableFieldType.WhenFinalized:
                default:
                    return true;
            }
        }

        public string GetJsonForm(IPtfkForm obj)
        {
            return Tools.ToJsonFormGen(GetFormObject(obj));
        }

        public string GetJsonForm(PtfkFormStruct obj)
        {
            return Tools.ToJsonFormGen(obj);
        }

        private IQueryable<IPtfkForm> GetEmptyDataItem(IPtfkForm ifrm)
        {
            try
            {
                Assembly assembly = ifrm.GetType().Assembly;
                object obj = null;
                Type t = assembly.GetType(assembly.GetName().Name + "." + ifrm.GetType().Name);
                if (t == null)
                {
                    assembly = ifrm.GetType().BaseType.Assembly;
                    t = assembly.GetType(assembly.GetName().Name + "." + ifrm.GetType().Name);
                }

                if (t != null)
                {
                    var frm = Activator.CreateInstance(t);
                    if (frm.GetType().GetInterfaces().Contains(typeof(IPtfkForm)))
                    {
                        frm.GetType().GetProperty(ifrm.GetIdAttributeName()).SetValue(frm, -999);
                        obj = frm;
                    }
                }

                return new IPtfkForm[] { obj as IPtfkForm }.AsQueryable<IPtfkForm>();
            }
            catch (Exception ex)
            {
                ErrorTable.Err004(ifrm.GetType().Name.GetType().Name + " [DataItems()]", ex.Message, ex);
                return null;
            }
        }

        public string GetJsonControl(IPtfkForm obj, PtfkFormStruct dformRequest)
        {
            PtfkFormStruct form = new PtfkFormStruct();
            form.action = "";
            form.method = Constants.FormMethod.Get;
            form.html = new List<HtmlElement>();
            HtmlElement html = new HtmlElement();
            html.Name = dformRequest.name;
            html.Options = new List<Option>();

            //Retorno do datatables Server-Side
            if (dformRequest.method != null && dformRequest.method.ToLower().Equals(Constants.FormMethod.Options))
            {
                if (dformRequest.FilterObject == null)
                    ErrorTable.Err009();

                //var ass = obj.GetType().Assembly;
                //var filter = new PtfkFilter
                //{
                //    Session = obj.GetOwner(),
                //    LogType = Tools.GetIPtfkLogClass(ass),
                //    RestrictedBySession = obj.HasBusinessRestrictionsBySession(),
                //    FilteredProperties = null,
                //    FilteredValue = dformRequest.FilterObject.SearchValue ?? "",
                //    PageSize = dformRequest.FilterObject.Length == 0 ? 10 : dformRequest.FilterObject.Length,
                //    OrderByAscending = dformRequest.FilterObject.OrderAscending,
                //    OrderByColumnIndex = dformRequest.FilterObject.OrderingColumnIndex,
                //    PageIndex = dformRequest.FilterObject.Start / (dformRequest.FilterObject.Length == 0 ? 1 : dformRequest.FilterObject.Length),
                //    StopAfterFirstHtml = dformRequest.FilterObject.StopAfterFirstHtml
                //};
                var filter = obj.GetPtfkFilter(dformRequest.FilterObject);
                //var fromDB = obj.FilterDataItems(filter, obj);

                var dataList = GetHtmlItems(filter.Result.Items, filter);

                dformRequest.OutputDataFilter = filter;
                dformRequest.OutputHtmlElementFilter = dataList;

                if (dataList == null)
                {
                    var jsonNull = "[]";
                    var dataNull = @"{""draw"": {0},
    ""recordsTotal"": {1},
	""recordsFiltered"": {2},
	""data"": {3}}";
                    try
                    {
                        return dataNull
                                    .Replace("{0}", dformRequest.FilterObject.Draw.ToString())
                                    .Replace("{1}", "0")
                                    .Replace("{2}", "0")
                                    .Replace("{3}", jsonNull);
                    }
                    catch (Exception ex)
                    {
                        return @"{""error"":""" + ex.Message.Replace(@"""", @"'") + @"""}";
                    }
                }


                var dt = ToDataTable(dataList, obj);
                DataView view = new DataView(dt);
                if (filter?.Applied == false)
                    view.Sort = dt.Columns[dformRequest.FilterObject.OrderingColumnIndex + 1].ColumnName + " " + dformRequest.FilterObject.OrderDirection.ToLower();
                var jsonItems = DataTableToJson(view.ToTable());

                var dataItems = Tools.FromJson<List<JObject>>(jsonItems);

                StringBuilder tempProp = new StringBuilder();
                StringBuilder jsonTxt = new StringBuilder();
                List<String> arr = new List<string>();
                //List<HtmlElement> lstToReturn = new List<HtmlElement>();

                var recordsFiltered = 0;
                var count = dataItems.Count;
                var pageCount = (dformRequest.FilterObject.Start + dformRequest.FilterObject.Length);
                if (pageCount > count)
                    pageCount = dformRequest.FilterObject.Length + (count - dformRequest.FilterObject.Length);
                if (pageCount < 0)
                    pageCount = count;
                var result = filter.Result.Items.ToList();
                if (filter?.Applied == false && dformRequest.FilterObject.Start <= count && String.IsNullOrWhiteSpace(dformRequest.FilterObject.SearchValue))
                {
                    for (int i = dformRequest.FilterObject.Start; i < pageCount; i++)
                    {
                        var item = dataItems[i];

                        tempProp = new StringBuilder();
                        int id = 0;
                        int.TryParse(item["__id"].Value<String>(), out id);
                        var findSearchValue = false;

                        var iform = result.Where(x => x.Id.Equals(id)).FirstOrDefault();

                        foreach (var jItem in item.Properties())
                        {
                            var valor = "";
                            try
                            {
                                valor = jItem.First().Value<string>();
                                if (iform.GetType().GetProperty(jItem.Name).PropertyType != typeof(String) && //Propriedades do tipo String não servem para a consulta
                                    (
                                    iform.GetType().GetProperty(jItem.Name).PropertyType.GetInterfaces().Contains(typeof(IPtfkEntity)) ||
                                    iform.GetType().GetProperty(jItem.Name).PropertyType.GetInterfaces().Any(x => x.IsGenericType &&

                            (x.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                            x.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
                            )

                                    valor = (jItem.Name.Equals("__id") ? jItem.Value : Tools.ToJsonNoBackslash(iform.GetType().GetProperty(jItem.Name).GetValue(iform))).ToString().Trim();
                            }
                            catch (Exception ex)
                            { }

                            if (string.IsNullOrWhiteSpace(valor))
                                valor = "";
                            //Search REGEX não implementado
                            if (Convert.ToString(valor).ToLower().Contains(dformRequest.FilterObject.SearchValue.ToLower()))
                                findSearchValue = true;

                            tempProp.Append(valor.StartsWith("{") || valor.StartsWith("[") ? @"""" + jItem.Name + @"""" + " : " + valor + "," : @"""" + jItem.Name + @""" : """ + (valor == null ? "" : valor.ToString().Replace(@"""", @"'")) + @"""" + ",");
                        }
                        if (findSearchValue)
                        {
                            recordsFiltered++;
                            if (recordsFiltered > 1)
                                jsonTxt.Append(",");
                            jsonTxt.Append("{ " + tempProp.ToString().Substring(0, tempProp.ToString().Length - 1) + " }");
                            //lstToReturn.Add(dataList.Where(x => x.Id.Equals(id)).FirstOrDefault());
                        }
                    }
                }
                else
                    if (filter?.Applied == true || !String.IsNullOrWhiteSpace(dformRequest.FilterObject.SearchValue))//Consulta todos os registros da base
                {
                    for (int i = 0; i < count; i++)
                    {
                        var item = dataItems[i];

                        tempProp = new StringBuilder();
                        int id = 0;
                        int.TryParse(item["__id"].Value<String>(), out id);
                        var findSearchValue = false;

                        var iform = result.Where(x => x.Id.Equals(id)).FirstOrDefault();

                        foreach (var jItem in item.Properties())
                        {
                            var valor = jItem.First().Value<string>();
                            try
                            {
                                var p = iform.GetType().GetProperty(jItem.Name);
                                if (p != null && p.PropertyType.GetInterfaces().Contains(typeof(IPtfkEntity)))
                                    valor = (jItem.Name.Equals("__id") ? jItem.Value : Tools.ToJsonNoBackslash(iform.GetType().GetProperty(jItem.Name).GetValue(iform))).ToString().Trim();
                            }
                            catch (Exception ex)
                            { }

                            if (string.IsNullOrWhiteSpace(valor))
                                valor = "";
                            //Search REGEX não implementado
                            if (Convert.ToString(valor).ToLower().Contains(dformRequest.FilterObject.SearchValue.ToLower()))
                                findSearchValue = true;

                            tempProp.Append(valor.StartsWith("{") || valor.StartsWith("[") ? @"""" + jItem.Name + @"""" + " : " + valor + "," : @"""" + jItem.Name + @""" : """ + (valor == null ? "" : valor.ToString().Replace(@"""", @"'")) + @"""" + ",");
                        }
                        if (filter?.Applied == true || findSearchValue)
                        {
                            recordsFiltered++;
                            if (recordsFiltered > 1)
                                jsonTxt.Append(",");
                            jsonTxt.Append("{ " + tempProp.ToString().Substring(0, tempProp.ToString().Length - 1) + " }");
                            //lstToReturn.Add(dataList.Where(x => x.Id.Equals(id.ToString())).FirstOrDefault());
                        }
                    }
                }

                var json = '[' + jsonTxt.ToString() + ']';
                var data = @"{""draw"": {0},
    ""recordsTotal"": {1},
	""recordsFiltered"": {2},
	""data"": {3}}";
                try
                {
                    var ret = data
                                .Replace("{0}", dformRequest.FilterObject.Draw.ToString())
                                .Replace("{1}", filter.Result.TotalCount.ToString())
                                .Replace("{2}", (!String.IsNullOrWhiteSpace(dformRequest.FilterObject.SearchValue) ? recordsFiltered.ToString() : filter.Result.TotalCount.ToString()))
                                .Replace("{3}", json);
                    dformRequest.OutputDatatableJson = ret;
                    return ret;
                }
                catch (Exception ex)
                {
                    return @"{""error"":""" + ex.Message.Replace(@"""", @"'") + @"""}";
                }
            }
            else
            if (dformRequest.method != null && dformRequest.method.ToLower().Equals(Constants.FormMethod.Connect))//Retorno do controle customizável
            {
                html.TypeSetter = ElementType.container;
                html.Options.Add(new Option { Description = "custom" });
            }
            else
            {

                bool hasDescription = false;
                foreach (var opt in obj.GetSelectOptions())
                {
                    var desc = opt.Description;
                    var curOption = new Option { Value = opt.Value, Html = opt.Text };
                    if (!String.IsNullOrWhiteSpace(desc))
                    {
                        hasDescription = true;
                        curOption.Description = desc;
                        html.TypeSetter = ElementType.radiobuttons;
                    }
                    if (opt.Selected)
                        curOption.Selected = "selected";
                    html.Options.Add(curOption);
                }
                if (!hasDescription)
                {
                    html.Options.Insert(0, new Option { Value = "0", Html = Constants.DefaultListSelectOption });
                }
            }
            form.html.Add(html);
            return Tools.ToJsonFormGen(form);

        }

        private DataTable ToDataTable(List<HtmlElement> dataItems, IPtfkForm obj)
        {
            DataTable table = new DataTable();
            table.Columns.Add("__id");
            StringBuilder str = new StringBuilder();
            int count = 0;
            foreach (var item in dataItems)
            {
                var values = new object[item.Html.Count() + 1];
                values[0] = item.Id;
                var idx = 1;
                foreach (var jItem in item.Html)
                {
                    var valor = (!String.IsNullOrWhiteSpace(jItem.Text) ? jItem.Text : jItem.Value);
                    if (count == 0)
                    {
                        if (valor != null)
                            table.Columns.Add(jItem.Name, valor.GetType());
                        else
                            table.Columns.Add(jItem.Name, typeof(String));
                    }
                    values[idx] = valor;
                    idx++;
                }
                table.Rows.Add(values);
                count++;
            }
            return table;
        }

        private Type GetDataType(IPtfkForm obj, string name)
        {
            var type = obj.GetType().GetProperty(name).PropertyType;
            if (Nullable.GetUnderlyingType(type) != null)
                return type.GetGenericArguments()[0];
            else
            {
                if (type.IsGenericType)
                    return typeof(string);
                return type;
            }
        }

        private Type GetDataType(object value)
        {
            var toString = Convert.ToString(value).Trim();
            if (toString.Contains('.') || toString.Contains(','))
            {
                Double n;
                bool isNumeric = Double.TryParse(toString, out n);
                if (isNumeric)
                    return typeof(Double);
            }
            else
            {
                Int64 n;
                bool isNumeric = Int64.TryParse(toString, out n);
                if (isNumeric)
                    return typeof(Int64);
            }
            return typeof(String);
        }

        private String DataTableToJson(DataTable dt)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (DataRow dr in dt.DefaultView.Table.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row.Add(col.ColumnName, dr[col]);
                }
                rows.Add(row);
            }
            return Tools.ToJson(rows);
        }

        private DataTable ToDataTable(IList<JObject> data)
        {
            DataTable table = new DataTable();
            int count = 0;
            foreach (JObject item in data)
            {
                var values = new object[item.Properties().Count()];
                var idx = 0;
                foreach (var prop in item.Properties())
                {
                    if (count == 0)
                        table.Columns.Add(prop.Name);
                    values[idx] = prop.Value;
                    idx++;
                }
                table.Rows.Add(values);
                count++;
            }
            return table;
        }

        private InputType[] GetCurrentInputTypes()
        {
            List<InputType> lst = new List<InputType>();

            lst.Add(InputType.NoListing);
            lst.Add(InputType.WithListing);
            lst.Add(InputType.External);
            lst.Add(InputType.Hidden);

            switch (_ACTION)
            {
                case TypeDef.Action.CREATE:
                    break;
                case TypeDef.Action.READ:
                    lst.Add(InputType.SelectOne);
                    lst.Add(InputType.ReadOnly);
                    break;
                case TypeDef.Action.UPDATE:
                    lst.Add(InputType.OnlyOnEdit);
                    lst.Add(InputType.OnlyOnEditForInsert);
                    lst.Add(InputType.SelectOne);
                    lst.Add(InputType.ReadOnly);
                    break;
                case TypeDef.Action.DELETE:
                    break;
                case TypeDef.Action.LIST:
                    break;
                case TypeDef.Action._NONE:
                    break;
                default:
                    break;
            }

            return lst.ToArray();
        }

        private struct InputDefs
        {
            public List<KeyValuePair<PropertyInfo, FormCaptionAttribute>> ValidInputs { get; set; }
            public List<KeyValuePair<PropertyInfo, FormCaptionAttribute>> HiddenInputs { get; set; }
            public PropertyInfo PrimaryKey { get; set; }
            public String PrimaryKeyLabel { get; set; }
        }

        private InputDefs InputConfig = new InputDefs();

        private List<HtmlElement> GetHtmlItems(IQueryable<IPtfkForm> dataTableSource, PtfkFilter filter = null)
        {
            var tasks = new List<System.Threading.Tasks.Task>();
            if (dataTableSource != null && dataTableSource.Any())
            {
                var item = dataTableSource.FirstOrDefault();

                var validInputs = item.GetInputs(InputType.WithListing, InputType.OnlyForListing).ToList();
                var hiddenInputs = item.GetInputs(InputType.Hidden).ToList();
                hiddenInputs.ForEach(x => x.Value.IsImplicit = true);
                try
                {
                    var values = InputType.GetValues(typeof(InputType)).OfType<InputType>().ToList();
                    validInputs.AddRange(item.GetInputs(values.ToArray()).Where(x => x.Value.IsImplicit));
                }
                catch (Exception ex)
                {
                }

                InputConfig.HiddenInputs = hiddenInputs;
                InputConfig.ValidInputs = validInputs.Distinct().ToList();
                InputConfig.PrimaryKey = item.GetType().GetProperty(item.GetIdAttributeName());
                InputConfig.PrimaryKeyLabel = Tools.EncodeBase64(item.GetLabel(InputConfig.PrimaryKey.Name));

                if (InputConfig.HiddenInputs.Where(x => x.Key.Name.Equals(InputConfig.PrimaryKey.Name)).Any())
                {
                    InputConfig.ValidInputs.Where(x => x.Key.Name.Equals(InputConfig.PrimaryKey.Name)).FirstOrDefault().Value.IsImplicit = true;
                    InputConfig.HiddenInputs.Remove(InputConfig.HiddenInputs.Where(x => x.Key.Name.Equals(InputConfig.PrimaryKey.Name)).FirstOrDefault());
                }
                bool hasFilter = false;
                if (CurrentPageConfig?.CurrDForm?.FilterObject != null && filter != null)
                {
                    if (!filter.Applied)
                        dataTableSource = item.ApplyFilter(filter, dataTableSource);
                    hasFilter = true;
                }

                var list = new List<HtmlElement>();
                _ListDatatableItems = new List<IPtfkEntity>();
                int count = 0;
                foreach (var obj in dataTableSource.ToList().Distinct())
                {
                    tasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() => NewHtml(obj, _ListDatatableItems, list), System.Threading.Tasks.TaskCreationOptions.LongRunning));
                    if (filter != null && filter.StopAfterFirstHtml)
                        break;
                }
                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

                var t = item.GetType();
                if (!String.IsNullOrWhiteSpace(list.FirstOrDefault()?.Id) && Convert.ToInt64(list.FirstOrDefault()?.Id) > 0 && !t.GetInterfaces().Contains(typeof(IPtfkLog)) &&
                !t.GetInterfaces().Contains(typeof(IPtfkConfig)) &&
                !t.GetInterfaces().Contains(typeof(IPtfkMedia)) &&
                !t.GetInterfaces().Contains(typeof(IPtfkWorker)) &&
                !t.GetInterfaces().Contains(typeof(IPtfkWorkflow<IPtfkForm>)))
                    PtfkCache.GetOrSetWorkflowEntitiesList(item.ClassName, list);

                if (hasFilter)
                {
                    var oList = dataTableSource.Select(x => x.Id.ToString()).ToList();
                    list = list.OrderBySequence(oList, doc => doc.Id).ToList();
                }

                return list;
            }
            return null;
        }

        private void NewHtml(IPtfkForm obj, List<IPtfkEntity> listDatatableItems, List<HtmlElement> list)
        {
            var primaryKey = InputConfig.PrimaryKey;
            var ID = Convert.ToInt64(primaryKey.GetValue(obj, null));
            var html = new HtmlElement();
            html.Id = ID.ToString();
            html.Html = new List<HtmlElement>();

            _ListDatatableItems.Add(obj as IPtfkEntity);

            var currHtml = new HtmlElement
            {
                Caption = InputConfig.PrimaryKeyLabel,
                Name = primaryKey.Name,
                Value = ID
            };
            if (!InputConfig.ValidInputs.Where(x => x.Key.Name.Equals(InputConfig.PrimaryKey.Name) && x.Value.IsImplicit).Any())
                html.Html.Add(currHtml);

            var culture = Tools.CurrentFormatProvider as System.Globalization.CultureInfo;
            foreach (var item in InputConfig.ValidInputs)
            {
                currHtml = new HtmlElement
                {
                    Caption = Tools.EncodeBase64(obj.GetLabel(item.Key.Name)),
                    Name = item.Key.Name,
                    Value = obj.GetType().GetProperty(item.Key.Name).GetValue(obj, null),
                    Mode = item.Value.IsImplicit ? "is-implicit" : String.Empty
                };

                if (item.Key.PropertyType == typeof(bool))
                {
                    var d = Convert.ToBoolean(currHtml.Value);
                    if (culture.Name.ToLower().StartsWith("pt"))
                        currHtml.Value = d.ToString("Sim", "Não");
                    else
                        currHtml.Value = d.ToString(culture);
                }
                else
                 if (Nullable.GetUnderlyingType(item.Key.PropertyType) != null && Nullable.GetUnderlyingType(item.Key.PropertyType) == typeof(bool))
                {
                    var d = (bool?)currHtml.Value;
                    if (d.HasValue)
                        if (culture.Name.ToLower().StartsWith("pt"))
                            currHtml.Value = d.ToString("Sim", "Não");
                        else
                            currHtml.Value = d.Value.ToString(culture);
                    else
                        currHtml.Value = string.Empty;
                }
                else
                    switch (item.Value.MaskType)
                    {
                        case MaskTypeEnum.date:
                            try
                            {
                                var dt = Convert.ToDateTime(currHtml.Value);
                                if (dt != DateTime.MinValue)
                                    currHtml.Value = dt.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
                                else
                                    currHtml.Value = string.Empty;
                            }
                            catch (Exception ex)
                            {
                            }
                            break;
                        case MaskTypeEnum.time:
                            try
                            {
                                var dt = Convert.ToDateTime(currHtml.Value); if (dt != DateTime.MinValue)
                                    currHtml.Value = dt.ToString(culture.DateTimeFormat.ShortTimePattern, culture);
                            }
                            catch (Exception ex)
                            {
                            }
                            break;
                        case MaskTypeEnum.date_time:
                            try
                            {
                                var dt = Convert.ToDateTime(currHtml.Value); if (dt != DateTime.MinValue)
                                    currHtml.Value = dt.ToString(culture);
                                else
                                    currHtml.Value = string.Empty;
                            }
                            catch (Exception ex)
                            {
                            }
                            break;
                        default:
                            break;
                    }

                try
                {
                    if (currHtml.Value != null)
                        if (currHtml.Value.GetType().IsGenericType)
                        {
                            var iObj = ((IEnumerable<IPtfkForm>)currHtml.Value).FirstOrDefault<IPtfkForm>();
                            currHtml.Default = iObj.GetInputs(InputType.SelectOneText).FirstOrDefault().Key.Name;
                        }
                        else
                            currHtml.Default = ((IPtfkForm)currHtml.Value).GetInputs(InputType.SelectOneText).FirstOrDefault().Key.Name;
                }
                catch (Exception ex)
                {
                }

                if (!String.IsNullOrWhiteSpace(item.Value.OptionsContext))
                {
                    if (item.Value.OptionsType == RequestMode.ServerSide)
                    {
                        var listaOpts = ManagerListItems(item.Key.Name, obj);
                        var currID = obj.GetType().GetProperty(item.Key.Name).GetValue(obj, null);
                        int iID = 0;
                        String sID = Convert.ToString(currID);
                        int.TryParse(sID, out iID);
                        bool hasValue = false;
                        if (iID > 0 || !String.IsNullOrWhiteSpace(sID))
                            hasValue = true;

                        if (currID != null && hasValue)
                            try
                            {
                                if (listaOpts != null && listaOpts.Any())
                                    currHtml.Text = listaOpts.Where(x => x.Value.Equals(currID.ToString())).SingleOrDefault().Text;
                            }
                            catch (Exception ex)
                            {

                            }
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(item.Value.EntityName) && !_config.ServerSideSearchMode)
                        {
                            Assembly assembly = obj.GetType().Assembly;
                            Type t = assembly.GetType(assembly.GetName().Name + "." + item.Value.EntityName);
                            if (t == null)
                            {
                                assembly = obj.GetType().BaseType.Assembly;
                                t = assembly.GetType(assembly.GetName().Name + "." + item.Value.EntityName);
                            }
                            var frm = Activator.CreateInstance(t);

                            primaryKey = FormCaptionAttribute.GetPrimaryKey(frm);
                            if (primaryKey == null)
                                ErrorTable.Err006(item.Value.EntityName);
                            var currID = obj.GetType().GetProperty(item.Key.Name).GetValue(obj, null);

                            Type ts = Nullable.GetUnderlyingType(primaryKey.PropertyType) ?? primaryKey.PropertyType;
                            if (primaryKey.GetSetMethod() != null)
                            {
                                object safeValue = (currID == null) ? null : Convert.ChangeType(currID, ts);
                                frm.GetType().GetProperty(primaryKey.Name).SetValue(frm, safeValue, null);
                            }

                            var listaOpts = frm.GetType().GetMethod(nameof(IPtfkEntity.ItemsList)).Invoke(frm, null) as List<ListItem>;
                            try
                            {
                                currHtml.Text = listaOpts.Where(x => x.Value.Equals(currID.ToString())).SingleOrDefault().Text;
                            }
                            catch (Exception ex)
                            {
                                currHtml.Text = String.Empty;
                            }
                        }
                        else
                            if (!_config.ServerSideSearchMode)
                            ErrorTable.Err007(item.Key.Name);
                    }
                }

                html.Html.Add(currHtml);
            }
            list.Add(html);
        }
        private readonly List<Type> AvailableTypes = new List<Type> {
                typeof(System.Boolean),
                typeof(System.Byte),
                typeof(System.SByte),
                typeof(System.Char),
                typeof(System.Decimal),
                typeof(System.Double),
                typeof(System.Single),
                typeof(System.Int32),
                typeof(System.UInt32),
                typeof(System.Int64),
                typeof(System.UInt64),
                typeof(System.Object),
                typeof(System.Int16),
                typeof(System.UInt16),
                typeof(System.String),
                typeof(System.DateTime),
                typeof(int?),
                typeof(decimal?),
                typeof(DateTime?),
                typeof(List<ListItem>),
                typeof(byte[]),
                typeof(PtfkFileInfo)   ,
                typeof(List<PtfkFileInfo>),
                typeof(List<String>)         };
        private bool ChechkValidFormType(Type type)
        {


            if (AvailableTypes.Contains(type))
            {
                return true;
            }
            else

                return false;

        }
    }
}
