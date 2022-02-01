using Newtonsoft.Json;
using Petaframework.Interfaces;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Petaframework
{
    public class PtfkFormStruct
    {
        internal bool _SkipProfileCheck = false;

        [JsonIgnore]
        internal PtfkFilter OutputDataFilter { get; set; }

        [JsonIgnore]
        internal List<HtmlElement> OutputHtmlElementFilter { get; set; }

        [JsonIgnore]
        internal String OutputDatatableJson { get; set; }

        [JsonIgnore]
        internal DateTime ActivitiesAfterThisDate { get; set; }

        public PtfkFormStruct() { }

        public class Filter
        {
            public int Draw { get; set; }
            public int Start { get; set; }
            public int Length { get; set; }
            public String SearchValue { get; set; }
            public bool SearchRegex { get; set; }
            public int OrderingColumnIndex { get; set; }
            public string OrderDirection { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Boolean UseCash { get; set; }

            public Boolean OrderAscending
            {
                get
                {
                    return (OrderDirection ?? "").ToLower().Equals("asc");
                }
            }
            [JsonIgnore]
            internal bool StopAfterFirstHtml { get; set; } = false;
            
        }
        public override string ToString()
        {
            return Tools.ToJsonFormGen(this).Replace(System.Environment.NewLine, String.Empty).Replace(@"\", @"/");
        }
        public Boolean HasImplicitCode(String implicitPropertyName)
        {
            try
            {
                return implicitCodes.Exists(x => x.Key.Equals(implicitPropertyName));
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public Object GetImplicitValue(String implicitPropertyName)
        {
            try
            {
                return implicitCodes.Find(x => x.Key.Equals(implicitPropertyName)).Value;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private long _id = 0;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long __id { get { return _id; } set { _id = value; ID = value; } }

        public string action { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string method { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool readable { get; set; } = false;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string caption { get; set; }
        public List<HtmlElement> html { get; set; }
        public long ID { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ownerID { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore]
        public IPtfkSession Session { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string parentEntity { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Filter FilterObject { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<KeyValuePair<String, object>> implicitCodes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<HtmlElement> dataItems { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        internal bool? maintenanceMode { get; set; }

        public bool serverSideSearch { get; set; } = false;

        public void SetImplicitCode(String name, Object value)
        {
            if (implicitCodes == null)
                implicitCodes = new List<KeyValuePair<string, object>>();
            var element = implicitCodes.Find(x => x.Key.Equals(name));
            if (!String.IsNullOrWhiteSpace(element.Key))
                implicitCodes.Remove(element);
            implicitCodes.Add(new KeyValuePair<string, object>(name, value));
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string parentID { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object parentObject { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string url { get; set; }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        private List<String> _ChangedInputs { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string GUID { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string message { get; set; }


        [JsonIgnore]
        public bool AutoSave { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string simulated { get; set; }


        private List<UserRole> _userRoles;
        /// <summary>
        /// Returns User Roles, only if exists Stage
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<UserRole> userRoles
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(html?[0].Stage))
                    return _userRoles;
                else
                    return null;
            }
            set { _userRoles = value; }
        }

        [JsonIgnore]
        internal List<IPtfkMedia> MediaFiles { get; private set; } = new List<IPtfkMedia>();

        internal void DistinctMediaFiles()
        {
            MediaFiles = MediaFiles.GroupBy(p => p.Hash).Select(g => g.Last()).ToList();
        }

        bool DoesTypeWereSimilar(Type type, Type inter)
        {
            if ((inter.Namespace.ToString() + inter.Name.ToString()).Equals(type.Namespace.ToString() + type.Name.ToString()))
                return true;
            if (inter.IsAssignableFrom(type))
                return true;
            if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == inter))
                return true;
            return false;
        }

        [JsonIgnore]
        private PtfkGen CurrentGenerator;
        internal bool IsIntegrationRun = false;
        public T GenerateBase<T>(IPtfkSession Owner, System.Threading.Tasks.Task<IPtfkWorkflow<T>> workflow, PtfkGen generator) where T : PtfkForm<T>, new()
        {
            T toSave = new T();
            toSave.Owner = Owner;
            (toSave as IEntity).Id = ID;

            CurrentGenerator = generator;

            SecurityCheck(toSave, workflow);

            html.Add(new HtmlElement { Name = toSave.GetIdAttributeName(), Value = ID });

            foreach (var item in implicitCodes)
            {
                var valor = item.Value;
                foreach (var caption in toSave.Captions.Where(x => x.Value.IsImplicit && !String.IsNullOrWhiteSpace(x.Value.OptionsContext)))
                {
                    if (caption.Key.Name.Equals(item.Key))
                    {
                        valor = GetHtml(caption.Value.OptionsContext, html.ToArray()).Value;
                        break;
                    }
                }
                html.Add(new HtmlElement { Name = item.Key, Value = valor });
            }
            _ChangedInputs = new List<String>();
            foreach (var item in toSave.GetType().GetProperties())
            {
                var htmlElem = GetHtml(item.Name, html.ToArray());
                var nameTest = item.Name;
                if (htmlElem != null && htmlElem.Value != null)
                {
                    if (htmlElem.Type == ElementType.subform.ToString())
                    {
                        if (htmlElem.Mode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLower().Trim()).Contains("selectone"))
                        {
                            var propType = (Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType);
                            if (DoesTypeWereSimilar(propType, typeof(System.Collections.Generic.ICollection<>)))
                            {
                                var genericType = propType.GetGenericArguments()[0];
                                var listType = typeof(List<>);
                                var constructedListType = listType.MakeGenericType(genericType);
                                var instance = (IList)Activator.CreateInstance(constructedListType);
                                var elemType = htmlElem.Value.GetType();
                                if (elemType == typeof(Newtonsoft.Json.Linq.JObject))
                                {
                                    var objDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject((htmlElem.Value as Newtonsoft.Json.Linq.JObject).ToString(), genericType);
                                    instance.Add(objDeserialized);
                                    item.SetValue(toSave, instance, null);
                                    _ChangedInputs.Add(item.Name);
                                }
                                else if (elemType == typeof(Newtonsoft.Json.Linq.JArray))
                                {
                                    var arr = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>((htmlElem.Value as Newtonsoft.Json.Linq.JArray).ToString());
                                    foreach (var objDeserialized in arr)
                                    {
                                        instance.Add(objDeserialized.ToObject(genericType));
                                    }
                                    if (instance.Count > 0)
                                    {
                                        item.SetValue(toSave, instance, null);
                                        _ChangedInputs.Add(item.Name);
                                    }
                                }
                            }
                        }
                        else
                        {
                            item.SetValue(toSave, item.GetValue(toSave, null), null);
                            _ChangedInputs.Add(item.Name);
                        }
                    }
                    else
                    if (htmlElem.Type == ElementType.selectmultiple.ToString())
                    {
                        var arr = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>((htmlElem.Value as Newtonsoft.Json.Linq.JArray).ToString());
                        var listType = typeof(List<>);
                        var propType = (Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType);
                        var genericType = propType.GetGenericArguments()[0];
                        var constructedListType = listType.MakeGenericType(genericType);
                        var instance = (IList)Activator.CreateInstance(constructedListType);
                        foreach (var objDeserialized in arr)
                        {
                            instance.Add(objDeserialized.ToObject(genericType));
                        }
                        if (instance.Count > 0)
                        {
                            item.SetValue(toSave, instance, null);
                            _ChangedInputs.Add(item.Name);
                        }
                    }
                    else
                    if (!CheckIfNull(htmlElem))
                    {
                        Type t = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;

                        if (item.GetSetMethod() != null && !String.IsNullOrWhiteSpace(htmlElem.Value.ToString()))
                        {
                            object safeValue = null;
                            try
                            {
                                safeValue = (htmlElem.Value == null) ? null : Convert.ChangeType(htmlElem.Value, t);
                                if (safeValue.GetType().Equals(typeof(System.String)) && (htmlElem.Mask != null && !htmlElem.Mask.Equals(MaskTypeEnum.money.ToString()) || !Tools.IsDigitsOnly(htmlElem.Value.ToString())))
                                {
                                    safeValue = Tools.DecodeBase64(Convert.ToString(safeValue));
                                }
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    if (Nullable.GetUnderlyingType(item.PropertyType) == null)
                                        safeValue = (htmlElem.Value == null) ? null : Convert.ChangeType(Tools.DecodeBase64(Convert.ToString(htmlElem.Value)), t);
                                    else
                                    {
                                        if (t == typeof(Int32) ||
                                            t == typeof(Int64))//Se for inteiro e nullable verificar se o valor é zero
                                        {
                                            var id = Convert.ToInt64((htmlElem.Value == null) ? null : Convert.ChangeType(Tools.DecodeBase64(Convert.ToString(htmlElem.Value)), t));
                                            if (id != 0)
                                                safeValue = (htmlElem.Value == null) ? null : Convert.ChangeType(Tools.DecodeBase64(Convert.ToString(htmlElem.Value)), t);
                                        }
                                        if (t == typeof(bool))
                                            safeValue = Tools.GetBoolValue(Tools.DecodeBase64(Convert.ToString(htmlElem.Value)));
                                        else
                                            safeValue = (htmlElem.Value == null) ? null : Convert.ChangeType(Tools.DecodeBase64(Convert.ToString(htmlElem.Value)), t);
                                    }
                                }
                                catch (Exception cex)
                                {
                                    throw new ConvertionException(htmlElem.Name);
                                }
                            }
                            if (t.Equals(typeof(System.String)) && htmlElem.Type == ElementType.checkbox.ToString())
                            {
                                if (safeValue.ToString().Trim().ToLower().Equals("on") || safeValue.ToString().Trim().Contains("b24=") || safeValue.ToString().Trim().Contains("T04="))//on
                                    safeValue = "True";
                                else
                                    safeValue = "False";
                            }
                            item.SetValue(toSave, safeValue, null);
                            htmlElem.Value = htmlElem.PlainValue = safeValue;
                            _ChangedInputs.Add(item.Name);
                        }
                        else
                        {
                            ICollection<PtfkFileInfo> fromMemory = null;
                            var doesSimilar = false;
                            bool isSingleFile = true;
                            if (DoesTypeWereSimilar(t, typeof(PtfkFileInfo)))
                            {
                                fromMemory = new List<PtfkFileInfo>();
                                fromMemory.Add(item.GetValue(toSave, null) as PtfkFileInfo);
                                doesSimilar = true;
                            }

                            if (DoesTypeWereSimilar(t, typeof(ICollection<PtfkFileInfo>)))
                            {
                                fromMemory = item.GetValue(toSave, null) as ICollection<PtfkFileInfo>;
                                doesSimilar = true;
                                isSingleFile = false;
                            }

                            if (doesSimilar)
                            {
                                IList<PtfkFileInfo> fromJson;
                                if (!Tools.IsJsonArray(htmlElem.Value.ToString()))
                                {
                                    fromJson = new List<PtfkFileInfo>();
                                    fromMemory.Add(Tools.FromJson<PtfkFileInfo>(htmlElem.Value.ToString()));
                                }
                                else
                                    fromJson = Tools.FromJson<IList<PtfkFileInfo>>(htmlElem.Value.ToString());
                                var idx = 0;
                                foreach (var file in fromMemory.Where(x => x != null && x.ParentID.Equals(ID)))
                                {
                                    if (fromJson.Where(x => x.UID.Equals(file.UID)).Count() == 0)
                                    {
                                        file.ToDelete = true;
                                    }
                                    idx++;
                                }
                                var currInput = toSave.GetInputs().Where(x => x.Key.Name.Equals(item.Name)).FirstOrDefault();
                                if (isSingleFile && !String.IsNullOrWhiteSpace(currInput.Value.MirroredOf))
                                {
                                    object path = new object();
                                    try
                                    {
                                        var toSaveFile = fromJson.Where(x => !x.ToDelete && fromMemory.Where(y => y.UID != null && y.UID.Equals(x.UID)).Count() == 0).LastOrDefault();
                                        if (toSaveFile == null)
                                            toSaveFile = fromMemory.FirstOrDefault();
                                        if (toSaveFile != null && toSaveFile.FileInfo == null)
                                            toSaveFile = PtfkFileInfo.GetFiles(Owner, toSave.ClassName, currInput.Value.MirroredOf, (toSave as IPtfkEntity).Id).LastOrDefault(); //PtfkFileInfo.GetFiles(Owner).Where(x => x.UID.Equals(toSaveFile.UID)).FirstOrDefault();
                                        var prop = toSave.GetType().GetProperty(currInput.Value.MirroredOf);
                                        try
                                        {
                                            if (toSaveFile != null)
                                                path = new
                                                {
                                                    path = toSaveFile.FileInfo?.FullName,
                                                    exists = toSaveFile.FileInfo?.Exists,
                                                    found = new System.IO.FileInfo(toSaveFile.FileInfo?.FullName).Exists,
                                                    fromMemory = fromMemory.Count(),
                                                    fromJson = fromJson.Count()
                                                };
                                        }
                                        catch (Exception)
                                        {

                                        }

                                        if (toSaveFile != null && (toSaveFile.EntityProperty.Equals(currInput.Key.Name) || toSaveFile.EntityProperty.Equals(prop.Name)))
                                        {
                                            var iMediaBusiness = Tools.GetIBusinessMediaClass(toSave.GetType().Assembly, ownerID);

                                            if (iMediaBusiness != null)
                                            {
                                                var json = new string[] { toSaveFile.Hash };

                                                PtfkMedia media = new PtfkMedia();
                                                media.Bytes = System.IO.File.ReadAllBytes(toSaveFile.FileInfo.FullName);
                                                media.EntityName = toSaveFile.EntityName;
                                                media.Extension = toSaveFile.FileInfo.Extension;
                                                media.Hash = toSaveFile.Hash;
                                                media.Name = toSaveFile.Name;
                                                media.Path = "*";
                                                media.Size = toSaveFile.FileInfo.Length;
                                                media.EntityProperty = prop.Name;
                                                MediaFiles.Add(media);

                                                prop.SetValue(toSave, Convert.ChangeType(Tools.ToJson(json), prop.PropertyType), null);
                                            }
                                            else
                                                prop.SetValue(toSave, Convert.ChangeType(System.IO.File.ReadAllBytes(toSaveFile.FileInfo.FullName), prop.PropertyType), null);
                                            _ChangedInputs.Add(item.Name);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        toSave.Log(Petaframework.Enums.Log.Error, "Error on materalize Media {0}:{1} - Path:{2} ", ex, toSave.ClassName, currInput.Key.Name, Tools.ToJson(path));
                                        PtfkException.SetLastOccurrence(Owner, new Exception("Media Error: " + currInput.Value.LabelText));
                                        throw PtfkException.GetLastOccurrence(Owner);
                                    }

                                }

                                //TODO is not single File - Is List<PtfkFileInfo>
                            }
                        }
                    }
                }
            }
            //TODO verificar resposta em auto salvamento
            if (toSave?.CurrentGenerator?.CurrentPageConfig?.SkipCache == true)
            {
                return toSave;
            }

            if (ID > 0)
            {
                if (_EnabledInputs != null && _EnabledInputs.Result != null)
                {
                    T valid = toSave.BusinessClass.Read(ID).Result;
                    //Recreate relations
                    valid.Captions = toSave.Captions;
                    valid.Validates = toSave.Validates;
                    valid.Logger = toSave.Logger;
                    valid.CustomCaptions = toSave.CustomCaptions;
                    valid.BusinessClass = toSave.BusinessClass;
                    valid.FormLabel = toSave.FormLabel;
                    valid.Owner = toSave.Owner;
                    valid.ParentID = toSave.ParentID;
                    foreach (var item in _EnabledInputs.Result)
                    {
                        var property = valid.GetType().GetProperty(item.Key.Name);
                        var getter = toSave.GetType().GetProperty(item.Key.Name);
                        if (property.SetMethod != null && getter != null)
                            property.SetValue(valid, toSave.GetType().GetProperty(item.Key.Name).GetValue(toSave, null));
                    }
                    return valid;
                }
                var type = new T();
                var prop = toSave.GetType().GetProperty(type.GetIdAttributeName());
                prop.SetValue(toSave, Convert.ChangeType(ID, prop.PropertyType), null);
            }
            else
            {
                foreach (var item in toSave.GetInputs().Where(x => x.Key.PropertyType.Equals(typeof(bool))))
                {
                    var prop = toSave.GetType().GetProperty(item.Key.Name);
                    if (prop.GetValue(toSave) == null)
                        //if (Tools.IsNullable(prop.PropertyType))
                        Tools.SetValue(toSave, prop.Name, false);
                    //else
                    // prop.SetValue(toSave, Convert.ChangeType(false, prop.PropertyType));
                }
            }
            SecurityCheck(toSave, workflow, false);

            return toSave;
        }

        private void SecurityCheck<T>(T t, System.Threading.Tasks.Task<IPtfkWorkflow<T>> workflow, bool preCheck = true) where T : PtfkForm<T>, new()
        {
            if (preCheck)//Verifica se a entidade em alteraçao pode ser alterada pelo usuário
            {
                if (ID <= 0)
                    return;

                var lista = t.GetDataItems();
                bool found = false;
                foreach (IPtfkEntity item in lista)
                {
                    if (item.Id.Equals(ID))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    throw new NotAccessibleEntityException();


                TypeDef.Action action = TypeDef.GetAction(this.action);
                switch (action)
                {
                    case TypeDef.Action._NONE:
                        if (ID > 0)
                            action = TypeDef.Action.UPDATE;
                        else
                            action = TypeDef.Action.CREATE;
                        break;

                }

                if (workflow.Result != null)
                    _EnabledInputs = System.Threading.Tasks.Task.Factory.StartNew(() => SetEnabledInputs<T>(workflow.Result, t));
                else
                {
                    //var validInputTypes = TypeDef.GetEditFlags(action);
                    //var inputs = t.GetInputs().Where(x => x.Value.InputType.GetUniqueFlags().Where(y => validInputTypes.HasValue && validInputTypes.Value.HasFlag(y)).Count() > 0);

                    //this._EnabledInputs = inputs;
                }
            }
            else //Verifica se existe campos não permitidos para edição que foram alterados
            {

            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore)]
        private System.Threading.Tasks.Task<IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>>> _EnabledInputs;
        private IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> SetEnabledInputs<T>(IPtfkWorkflow<T> workflow, T t) where T : PtfkForm<T>, new()
        {
            var currStruct = workflow.GetCurrentTaskState(CurrentGenerator.GetFormObject(t.BusinessClass.Read(ID).Result as IPtfkForm));
            var valids = currStruct.html[0].Html.Where(x => !x.Readonly);
            if (IsIntegrationRun)
                valids = currStruct.html[0].Html;
            var all = valids.Select(x => x.Name).Union(valids.Select(x => x.MirroredOf)).Distinct();
            var inputs = t.GetInputs().Where(x => all.Contains(x.Key.Name));
            return inputs;
        }

        private bool CheckIfNull(HtmlElement html)
        {
            if (html.Value == null)
                return true;
            if (html.Value.GetType() == typeof(String))
            {
                return String.IsNullOrWhiteSpace(html.ToString());
            }
            return false;
        }

        private bool Contains(string name)
        {
            return GetHtml(name) != null;
        }

        internal HtmlElement GetHtml(string name, params HtmlElement[] list)
        {
            HtmlElement obj = null;

            if (list == null || list.Count() == 0)
                list = html.ToArray();

            foreach (var item in list)
            {
                if (!String.IsNullOrWhiteSpace(item.Name) && item.Name.ToLower().Equals(name.ToLower()))
                    return item;
                else
                {
                    if (item.Html != null && item.Html.Count > 0)
                        obj = GetHtml(name, item.Html.ToArray());
                }
                if (obj != null)
                    return obj;
            }

            return null;
        }

        internal HtmlElement GetMirroredHtml(string name, params HtmlElement[] list)
        {
            HtmlElement obj = null;

            if (list == null || list.Length == 0)
                list = html.ToArray();

            foreach (var item in list)
            {
                if (!String.IsNullOrWhiteSpace(item.Name) && item.MirroredOf != null && item.MirroredOf.ToLower().Equals(name.ToLower()))
                    return item;
                else
                {
                    if (item.Html != null && item.Html.Count > 0)
                        obj = GetMirroredHtml(name, item.Html.ToArray());
                }
                if (obj != null)
                    return obj;
            }

            return null;
        }

        public class UserRole
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string name { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string guid { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool flag { get; set; }
        }
    }

}