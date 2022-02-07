using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Petaframework.Interfaces;
using Petaframework.Json;
using Petaframework.Strict;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Formatting = Newtonsoft.Json.Formatting;
using PetaframeworkStd;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Primitives;
using System.Dynamic;
using static Petaframework.POCO.OutboundData;
using static Petaframework.Enums;

namespace Petaframework
{
    public static class Tools
    {
        public static PtfkFormStruct GetPtfkFormStruct(
     string json,
     IPtfkSession user,
     HttpResponse response = null,
     Func<String, IPtfkSession> onSearchingSession = null)
        {
            PtfkFormStruct ptfkFormStruct = new PtfkFormStruct();
            if (string.IsNullOrWhiteSpace(json))
            {
                ptfkFormStruct.action = TypeDef.Action.READ.ToString();
            }
            else
            {
                ptfkFormStruct = JsonConvert.DeserializeObject<PtfkFormStruct>(json);
                if (string.IsNullOrWhiteSpace(ptfkFormStruct.action) && (ptfkFormStruct.html == null || ptfkFormStruct.html.Count<HtmlElement>() == 0))
                    ptfkFormStruct.action = TypeDef.Action.READ.ToString();
            }
            Tools.CheckUserSimulation(user, ptfkFormStruct.simulated ?? "", response, onSearchingSession);
            if (string.IsNullOrWhiteSpace(user.Login))
                ptfkFormStruct.simulated = string.Empty;
            return ptfkFormStruct;
        }

        public static string HtmlEncodeText(string txt)
        {
            var t = ExtractHtmlInnerText(txt);
            foreach (var item in t.ToCharArray().Distinct())
            {
                var enc = System.Web.HttpUtility.HtmlEncode(item);
                var dec = System.Web.HttpUtility.HtmlDecode(item.ToString());
                if (enc != dec)
                    txt = txt.Replace(item.ToString(), enc);
            }
            return txt;
        }

        public static IEnumerable<string> GetProfileFromWorkflow(IPtfkWorkflow<IPtfkForm> workflow, string taskId)
        {
            foreach (var task in workflow.ListAllTasks())
                if (task.ID.Equals(taskId))
                    foreach (var p in task.Profiles)
                        yield return p.ID;

            yield return "";
        }

        public static string ExtractHtmlInnerText(string htmlText)
        {
            Regex regex = new Regex("(<.*?>\\s*)+", RegexOptions.Singleline);

            string resultText = regex.Replace(htmlText, " ").Trim();

            return resultText;
        }

        public static string HtmlDecode(string txt)
        {
            return System.Web.HttpUtility.HtmlDecode(txt);
        }

        public static string ConvertMinutesToDuration(int? minutes)
        {
            if (!minutes.HasValue)
                return (string)null;
            IFormatProvider currentFormatProvider = Tools.CurrentFormatProvider;
            return Tools.GetDuration(new TimeSpan?(TimeSpan.FromMinutes((double)minutes.Value)));
        }

        public static T FromKeyValuePair<T>(List<KeyValuePair<string, StringValues>> list)
        {
            dynamic item = new ExpandoObject();
            var dItem = item as IDictionary<String, object>;
            foreach (var key in list)
            {
                dItem.Add(new KeyValuePair<String, object>(key.Key, key.Value.ToString()));
            }
            var json = ToJson(item);
            return FromJson<T>(json);
        }

        public static string GetDuration(TimeSpan? span)
        {
            return !span.HasValue ? (string)null : span.Value.ToString("d\\d\\ h\\h\\ mm\\m");
        }

        public static EnvironmentStatus GetPtfkEnvironmentStatus()
        {
            return PtfkEnvironment.CurrentEnvironment.Status;
        }

        public static string GetSimulatedUserJson(IPtfkSession user)
        {
            if (!user.Login.Equals(user.Current.Login))
            {
                var simulated = new UserSimulated
                {
                    Login = user.Current.Login,
                    Name = user.Current.Name,
                    Description = user.Current.Department != null ? String.Join(" - ", user.Current.Department.DepartmentalHierarchy) : null
                };
                return EncodeBase64(ToJson(simulated));
            }
            return string.Empty;
        }

        public static void CheckUserSimulation(
          IPtfkSession user,
          string simulated,
          HttpResponse response,
          Func<string, IPtfkSession> searchUserFunc = null
          )
        {
            if (!string.IsNullOrWhiteSpace(simulated) && simulated.ToLower().Equals("undefined"))
                simulated = "";
            string userFromCookie = DecodeBase64(response?.HttpContext?.Request?.Cookies[Constants.SimulatedUserCookieName]);
            Tools.UserSimulated userSimulated1;
            if (string.IsNullOrWhiteSpace(simulated))
            {
                if (string.IsNullOrWhiteSpace(userFromCookie))
                    return;
                userSimulated1 = Tools.FromJson<Tools.UserSimulated>(userFromCookie, false);
                if (string.IsNullOrWhiteSpace(userSimulated1.Login))
                    return;
                simulated = userSimulated1.Login;
            }
            IEnumerable<string> source1 = user.Bag == null || !user.Bag.ContainsKey(Constants.WorkflowAdmin) ? null : user.Bag[Constants.WorkflowAdmin] as IEnumerable<string>;
            if (source1 == null || source1.Count() <= 0)
                return;
            var source2 = UserRepository.List();
            var source3 = source2.Where(x => x.Login.Equals(simulated));
            if (!source3.Any() && searchUserFunc != null)
                source3 = new List<IPtfkSession> { searchUserFunc.Invoke(simulated) };
            if (source3.Count() == 0 && user.Bag.ContainsKey("email"))
                source3 = source2.Where<IPtfkSession>((Func<IPtfkSession, bool>)(x => x.Bag["email"].Equals((object)simulated)));
            if (source3.Count() > 0)
            {
                IPtfkSession owner = source3.FirstOrDefault();
                user.SetCurrentInstance(owner);
            }
            else if (!string.IsNullOrWhiteSpace(userFromCookie))
            {
                var t = Tools.FromJson<Tools.UserSimulated>(userFromCookie, false);
                source3 = source2.Where(x => x.Login.Equals(t.Login));
                if (!String.IsNullOrWhiteSpace(t.Login))
                    simulated = t.Login;
                if (source3.Any())
                {
                    IPtfkSession owner = source3.FirstOrDefault();
                    user.SetCurrentInstance(owner);
                }
                else
                    user.Login = simulated;
            }
            else
                user.Login = simulated;
            if (response == null)
                return;

            Tools.UserSimulated userSimulated2;
            if (source3.Any())
            {
                userSimulated1 = new Tools.UserSimulated();
                userSimulated1.Login = user.Current.Login;
                userSimulated1.Name = Tools.ToTitleCase(user.Current.Name);
                userSimulated1.Description = user.Department != null ? String.Join(" - ", user.Department.DepartmentalHierarchy) : "";
                userSimulated2 = userSimulated1;
            }
            else
            {
                userSimulated1 = new Tools.UserSimulated
                {
                    Login = simulated,
                    Name = "???"
                };
                userSimulated2 = userSimulated1;
            }
            string json2 = Tools.ToJson((object)userSimulated2);
            response.Cookies.Append(Constants.SimulatedUserCookieName, json2);


            var bearerAuth = response.HttpContext.Request.Headers["Authorization"]
.FirstOrDefault()?.StartsWith("Bearer ") ?? false;
            if (bearerAuth)
            {
                if (user.Bag == null)
                    user.Bag = new Dictionary<string, object>();
                if (user.Bag.ContainsKey(Constants.BearerRequestFlag))
                    user.Bag.Remove(Constants.BearerRequestFlag);
                user.Bag.Add(Constants.BearerRequestFlag, true);
            }

        }

        internal static bool DoesTypeWereSimilar(Type type, Type inter)
        {
            return (inter.Namespace.ToString() + inter.Name.ToString()).Equals(type.Namespace.ToString() + type.Name.ToString()) || inter.IsAssignableFrom(type) || ((IEnumerable<Type>)type.GetInterfaces()).Any<Type>((Func<Type, bool>)(i => i.IsGenericType && i.GetGenericTypeDefinition() == inter));
        }

        internal static string ToTitleCase(string input)
        {
            input = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input?.ToLower());
            return input;
        }

        public static PtfkFilter RunFilterEntities<T>(IPtfkDbContext context, PtfkFilter filterParam) where T : class, IPtfkForm
        {
            if (context.GetType().GetInterfaces().Contains(typeof(IMongoDbContext)))
            {

            }


            var Db = context as DbContext;

            //TODO format queries for databases other than SQLite and SQL Server.
            var entityType = Db.Model.FindEntityType(typeof(T));
            var logType = Db.Model.FindRuntimeEntityType(filterParam.LogType?.GetType());
            
			// Table info 
            var tableName = "[" + entityType.GetTableName() + "]";
            var logTableName = "[" + logType?.GetTableName() + "]";
            var tableSchema = entityType.GetSchema();


            var idProp = entityType.FindPrimaryKey().Properties.FirstOrDefault();
            var entityNameProp = logType?.FindProperty(nameof(filterParam.LogType.EntityName));
            var entityIdProp = logType?.FindProperty(nameof(filterParam.LogType.EntityId));
            var loginChangeProp = logType?.FindProperty(nameof(filterParam.LogType.LoginChange));
            var logChangeType = logType?.FindProperty(nameof(filterParam.LogType.LogType));

            var stmt = new System.Text.StringBuilder();
            var val = Tools.CleanInjection(filterParam.FilteredValue);
            var props = entityType.GetProperties().Where(x => filterParam.FilteredProperties.Contains(x.Name));
            // Column info 
            int count = 1;
            var orderColumn = "";
            if (!filterParam.FilteredProperties.Contains(idProp.Name))
                stmt.AppendFormat(" e.[{0}] LIKE '%{1}%'", idProp.GetColumnName(), val);
            if (filterParam.OrderByColumnIndex == 0)
                orderColumn = idProp.GetColumnName();
            foreach (var property in props)
            {
                var columnName = property.GetColumnName();
                stmt.AppendFormat(" OR e.[{0}] LIKE '%{1}%'", columnName, val);
                if (count == filterParam.OrderByColumnIndex)
                {
                    orderColumn = columnName;                
                }
                count++;
            };
            var sql = "SELECT e.* FROM " + tableName + " AS e WHERE " + stmt.ToString();

            if (!filterParam.Session.IsAdmin && filterParam.RestrictedBySession && logType != null)
            {
                sql = "SELECT e.* FROM " + tableName + " AS e JOIN " + logTableName + " AS l ON l.[" + entityNameProp.GetColumnName() + "] LIKE '" + typeof(T).Name + "' AND l.[" + entityIdProp.GetColumnName() + "] = e.[" + idProp.GetColumnName() + "] AND l.[" + loginChangeProp.GetColumnName() + "] LIKE '" + filterParam.Session.Login + "' AND l.[" + logChangeType.GetColumnName() + "] = '" + nameof(LogType.Create) + "'  WHERE " + stmt.ToString();
            }

            filterParam.SqlGenerated = sql;
            if (PtfkExtensions.IsPtfkWorkflowClass(Activator.CreateInstance<T>()))
            {

                if (filterParam.PageSize == 0)//All
                {

                    filterParam.SetResult(
                            Db.Set<T>().FromSqlRaw(sql).OrderBy(orderColumn, filterParam.OrderByAscending).AsNoTracking(),
                            Db.Set<T>().FromSqlRaw(sql).Count());
                }

                filterParam.SetResult(Db.Set<T>().FromSqlRaw(sql)
                            .OrderBy(orderColumn, filterParam.OrderByAscending)
                            .AsNoTracking()
                            .Skip(filterParam.PageIndex * filterParam.PageSize)
                            .Take(filterParam.PageSize)
                            .AsQueryable<T>(), Db.Set<T>().FromSqlRaw(sql).Count());
                return filterParam;
            }

            if (filterParam.PageSize == 0)//All
            {
                filterParam.SetResult(
                        Db.Set<T>().FromSqlRaw(sql)
                         .OrderBy(orderColumn, filterParam.OrderByAscending)
                        .AsNoTracking(),
                        Db.Set<T>().FromSqlRaw(sql).Count());
            }

            filterParam.SetResult(
                        Db.Set<T>().FromSqlRaw(sql)
                         .OrderBy(orderColumn, filterParam.OrderByAscending)
                           .AsNoTracking()
                           .Skip(filterParam.PageIndex * filterParam.PageSize)
                           .Take(filterParam.PageSize)
                           .AsQueryable<T>(),
                        Db.Set<T>().FromSqlRaw(sql).Count());

            filterParam.Applied = true;

            return filterParam;
        }
        public static string GetExceptionMessage(Exception ex)
        {
            if (ex.InnerException != null)
            {
                return GetExceptionMessage(ex.InnerException);
            }
            return ex.Message.Replace(PtfkEnvironment.CurrentEnvironment?.WebHostEnvironment?.ContentRootPath, "...");
        }

        public static bool WorkflowChecks<T>(T entity, params Petaframework.Enums.LogType[] logType) where T : class, IPtfkEntity
        {
            if (entity.Owner != null)
            {

            }

            if (logType != null && logType.Length > 0)
            {
                switch (logType[0])
                {
                    case Petaframework.Enums.LogType.Delete:
                        if (entity == null || entity.GetType().GetInterfaces().Contains(typeof(IPtfkLog)))
                            return false;
                        break;
                    case Petaframework.Enums.LogType.Update:
                        var i = (entity as PtfkForm<T>);
                        if (i.CurrentGenerator?.CurrentPageConfig?.CurrDForm?.AutoSave == true)
                            return false;
                        break;
                    case Petaframework.Enums.LogType.Create:
                        break;
                    default:
                        break;
                }
            }
            if (entity.CurrentWorkflow != null && entity.CurrentWorkflow.HasBusinessProcess())
            {
                if (String.IsNullOrWhiteSpace(entity.CurrentProcessTaskID))
                    throw new Exception(String.Format("Unknown Workflow Task ({0}.{1})!", entity.GetType().Name, entity.Id));
                if (String.IsNullOrWhiteSpace(entity.CurrentProcessTaskID))
                {
                    try
                    {
                        (entity as PtfkForm<T>).CurrentProcessTask = entity.CurrentWorkflow.GetCurrentTask();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            return true;
        }

        public static String ToJsonNoBackslash(object obj)
        {
            return Newtonsoft.Json.Linq.JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(obj)).ToString();
        }

        internal static String ToJsonFormGen(Object obj, params bool[] withoutBackslash)
        {
            if (withoutBackslash != null && withoutBackslash.Length > 0 && withoutBackslash[0])
                return Newtonsoft.Json.Linq.JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(obj)).ToString();
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static bool GetBoolValue(object val)
        {
            if (val == null)
                return false;
            if (val != null && val.GetType() == typeof(bool))
                return (bool)val;
            if (val.GetType() == typeof(string))
            {
                if (val.ToString().Trim().ToLower().Equals("on")
                    || val.ToString().Trim().ToLower().Equals("1")
                    || val.ToString().Trim().ToLower().Equals("true")
                    || val.ToString().Trim().ToLower().Equals("t")
                    || val.ToString().Trim().ToLower().Equals("yes")
                    || val.ToString().Trim().ToLower().Equals("y")
                    )
                    return true;
                return false;
            }

            long v = 0;
            long.TryParse(val.ToString(), out v);
            if (v > 0)
                return true;

            bool b = false;
            bool.TryParse(val.ToString(), out b);

            return b;
        }

        public static T GetConcreteClass<T, Interface>(Interface item) where T : IPtfkEntity
        {
            try
            {
                if (Tools.ImplementsInterface(item.GetType(), typeof(Interface)))
                {
                    var from = ToJson(item);
                    return FromJson<T>(from);
                }
                return default(T);
            }
            catch (Exception ex)
            {
                return default(T);
            }

        }

        public static IEnumerable<String> GetUserAbleWorkflows(IPtfkSession owner)
        {
            var all = Petaframework.Strict.ConfigurationManager.GetCurrentPermission;
            var perms = all.Where(x => x.Value.ToList().Where(y => (y.HierarchyFlag && owner.Department != null) || (y.EnabledTo != null && y.EnabledTo.Contains(owner.Login))).Any());
            return perms.Select(x => x.Key);
        }


        /// <summary>
        /// Compare two JSONs, and return true if equals
        /// </summary>
        /// <param name="expected">Json string</param>
        /// <param name="actual">Json string</param>
        /// <returns>True if equal or False if differents</returns>
        public static bool CompareJson(string expected, string actual)
        {
            if (String.IsNullOrWhiteSpace(expected) || String.IsNullOrWhiteSpace(actual))
            {
                return false;
            }


            JObject xpctJSON = JObject.Parse(expected);
            JObject actJSON = JObject.Parse(actual);

            return JToken.DeepEquals(xpctJSON, actJSON);

        }




        public class Variance
        {
            public string INFO { get; set; }
            public object FROM { get; set; }
            public object TO { get; set; }
        }

        public static String JsonDifferenceReport(String sourceJsonString,
             String targetJsonString)
        {
            JObject sourceJObject = JsonConvert.DeserializeObject<JObject>(sourceJsonString);
            JObject targetJObject = JsonConvert.DeserializeObject<JObject>(targetJsonString);
            List<Variance> result = new List<Variance>();

            if (sourceJObject != null && !JToken.DeepEquals(sourceJObject, targetJObject))
            {
                foreach (KeyValuePair<string, JToken> sourceProperty in sourceJObject)
                {
                    JProperty targetProp = targetJObject.Property(sourceProperty.Key);

                    if (!JToken.DeepEquals(sourceProperty.Value, targetProp.Value)
                        &&
                        !(String.IsNullOrWhiteSpace(sourceProperty.Value.ToString())
                          && String.IsNullOrWhiteSpace(targetProp.Value.ToString()))
                        )
                    {
                        var Diff = new Variance { INFO = sourceProperty.Key, TO = targetProp.Value.ToString(), FROM = sourceProperty.Value.ToString() };
                        result.Add(Diff);
                    }
                }
            }
            else
            {
                PtfkConsole.WriteLine("Objects are same");
            }

            String retorno = ToJson(result);


            return retorno;
        }


        public static String ToJson(Object text)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(text);
        }
        public static String ToJson(Object text, Boolean ignoreLoopHandling, Boolean identedOutput = false)
        {
            if (ignoreLoopHandling)
            {
                if (identedOutput)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(text, Formatting.Indented, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                return Newtonsoft.Json.JsonConvert.SerializeObject(text, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            }
            else
            {
                if (identedOutput)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(text, Formatting.Indented);
                else
                    return ToJson(text);
            }
        }

        public static String ToJson(Object text, JsonSerializerSettings settings)
        {
            if (settings == null)
            {
                settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(text,
                Newtonsoft.Json.Formatting.None,
                settings);
        }

        public static IPtfkConfig GetConfigByEntity(string entityName, List<IPtfkConfig> configs)
        {
            var name = entityName.ToLower().Replace("_", "").Trim();
            System.Collections.Generic.IEnumerable<string> tmp;
            foreach (var item in configs)
            {
                tmp = item.ProcessedTables.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Replace("_", "").Trim());
                if (tmp.Contains(name))
                    return item as IPtfkConfig;
            }
            return null;
        }

        public static string RemoveAccents(string text)
        {
            StringBuilder sbReturn = new StringBuilder();
            var arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }

        public static bool ZeroNullOrWhiteSpace(object val)
        {
            if (val == null)
                return true;
            double i = 0;
            double.TryParse(val.ToString(), out i);
            if (i == 0)
                return true;
            if (String.IsNullOrWhiteSpace(val.ToString()))
                return true;
            return false;
        }

        public static FileInfo MoveWithReplace(
          this FileInfo file,
          FileInfo newLocation,
          IPtfkSession session)
        {
            if (System.IO.File.Exists(newLocation.FullName))
                System.IO.File.Delete(newLocation.FullName);
            if (!newLocation.Directory.Exists)
                newLocation.Directory.Create();
            if (newLocation.Exists)
                newLocation.Delete();
            System.IO.File.Copy(file.FullName, newLocation.FullName);
            newLocation = new FileInfo(newLocation.FullName);
            PtfkFileInfo.RefreshFile(session, file, newLocation);
            file = newLocation;
            return newLocation;
        }

        public static IPtfkConfig GetConfigByEntity<T>(T entity, List<IPtfkConfig> configs) where T : class, IPtfkEntity
        {
            var name = entity.GetType().Name.ToLower().Replace("_", "").Trim();
            System.Collections.Generic.IEnumerable<string> tmp;
            foreach (var item in configs)
            {
                tmp = item.ProcessedTables.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Replace("_", "").Trim());
                if (tmp.Contains(name))
                    return item as IPtfkConfig;
            }
            return null;
        }

        public static String ToJsonPreserveReferences(Object text)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(text, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        }

        public static T FromJson<T>(string json, bool camelCaseOnly = false)
        {
            if (camelCaseOnly)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Converters = new List<JsonConverter> { new CamelCaseOnlyConverter() }
                };
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, settings);
            }
            else
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    return default(T);
                }
        }

        private static Task FindAndChangeRecursive(FileInfo file, string txt, string title, string stage, IEnumerable<KeyValuePair<String, HtmlElement>> captionValuePair)
        {
            var EntitiesNames = captionValuePair;

            var idxBloco = txt.IndexOf("@[");
            var toReplace = new KeyValuePair<int, int>(idxBloco, -1);
            if (idxBloco >= 0)
            {
                idxBloco = idxBloco + 2;
                toReplace = new KeyValuePair<int, int>(toReplace.Key, txt.Substring(idxBloco).IndexOf("]@"));
                var bloco = SetBloco(txt, idxBloco, toReplace.Value);
                StringBuilder strList = new StringBuilder();
                while (!String.IsNullOrWhiteSpace(bloco))
                {
                    foreach (var entity in EntitiesNames)
                    {
                        var v = entity.Value.PlainValue != null ? entity.Value.PlainValue.ToString() : "";
                        if (entity.Value.Type == ElementType.checkbox.ToString() || entity.Value.Type == ElementType.radio.ToString())
                        {

                            v = Tools.GetBoolValue(v) ? "true" : "false";
                            if (!String.IsNullOrWhiteSpace(v) && Convert.ToBoolean(v))
                                v = setCulture ? "Yes" : "Sim";
                            else
                                v = setCulture ? "No" : "Não";

                            strList.AppendLine(ReplaceValuesOnPatterns(bloco, title, entity.Key, v, entity.Value.Name, entity.Value.Stage));
                        }
                        else
                            if (entity.Value.Options != null && entity.Value.Options.Any())
                        {
                            string val = entity.Value.Options.Where(x => x.Value != null && x.Value.Equals(v)).FirstOrDefault()?.Html;
                            v = val ?? v;
                            if (v.Equals(Constants.DefaultListSelectOption))
                                v = "";
                            strList.AppendLine(ReplaceValuesOnPatterns(bloco, title, entity.Key, v, entity.Value.Name, entity.Value.Stage));
                        }
                        else
                            if (entity.Value.Mask == MaskTypeEnum.money.ToString())
                        {
                            double c = 0;
                            double.TryParse(entity.Value.PlainValue.ToString().Replace(",", "."), out c);
                            if (c == 0)
                                strList.AppendLine(ReplaceValuesOnPatterns(bloco, title, entity.Key, "", entity.Value.Name, entity.Value.Stage));
                            else
                                strList.AppendLine(ReplaceValuesOnPatterns(bloco, title, entity.Key, ToCurrency(v), entity.Value.Name, entity.Value.Stage));
                        }
                        else
                            strList.AppendLine(ReplaceValuesOnPatterns(bloco, title, entity.Key, v, entity.Value.Name, entity.Value.Stage));
                    }

                    txt = txt.Replace(txt.Substring(toReplace.Key - 2, txt.Substring(toReplace.Key - 2).IndexOf("]@") + 2), strList.ToString());
                    strList = new StringBuilder();

                    idxBloco = txt.IndexOf("@[");
                    if (idxBloco >= 0)
                    {
                        idxBloco = idxBloco + 2;
                        bloco = SetBloco(txt, idxBloco, txt.Substring(idxBloco).IndexOf("]@"));
                    }
                    else
                        bloco = String.Empty;
                }
            }
            return File.WriteAllTextAsync(file.FullName, ReplaceValuesOnPatterns(txt, title, "", "", "", stage, EntitiesNames));
        }

        public static string ToCurrency(string val)
        {
            double d = 0;
            double.TryParse(val, out d);
            return d.ToString("C", System.Globalization.CultureInfo.CurrentCulture);
        }

        internal static string SetBloco(string txt, int idxBloco, int length)
        {
            try
            {
                return txt.Substring(idxBloco, length);
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }


        private static bool setCulture = false;
        static IFormatProvider _CurrentFormatProvider = new System.Globalization.CultureInfo("pt-BR", false);
        public static IFormatProvider CurrentFormatProvider { get { return _CurrentFormatProvider; } set { _CurrentFormatProvider = value; setCulture = true; } }
        private static string ReplaceValuesOnPatterns(string patternSource, string title, string caption = "", string value = "", string prop = "", string stage = "", IEnumerable<KeyValuePair<string, HtmlElement>> properties = null)
        {
            var ret = patternSource;
            if (!string.IsNullOrEmpty(prop))
                ret = ret.Replace("{" + prop.ToLower() + "}", value);
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    if (item.Value?.Options?.Count > 0)
                        try
                        {
                            ret = ret.Replace("{" + item.Value.Name.ToLower() + "}", item.Value.PlainValue != null ? item.Value.Options.Where(x => x.Value.Equals(item.Value.PlainValue?.ToString())).FirstOrDefault()?.Html : "");
                        }
                        catch (Exception)
                        {
                            ret = ret.Replace("{" + item.Value.Name.ToLower() + "}", item.Value.PlainValue != null ? item.Value.PlainValue.ToString() : "");
                        }
                    else if (item.Value.Mask == MaskTypeEnum.money.ToString())
                    {
                        double c = 0;
                        double.TryParse(item.Value.PlainValue.ToString(), out c);
                        if (c == 0)
                            ret = ret.Replace("{" + item.Value.Name.ToLower() + "}", "");
                        else
                            ret = ret.Replace("{" + item.Value.Name.ToLower() + "}", item.Value.PlainValue != null ? ToCurrency(item.Value.PlainValue.ToString()) : "");
                    }
                    else
                        ret = ret.Replace("{" + item.Value.Name.ToLower() + "}", item.Value.PlainValue != null ? item.Value.PlainValue.ToString() : "");
                }
            }
            var culture = CurrentFormatProvider as System.Globalization.CultureInfo;
            ret = ret.Replace("{title}", title)
                         .Replace("{caption}", caption)
                         .Replace("{value}", value)
                         .Replace("{stage}", (!string.IsNullOrWhiteSpace(stage) ? "[" + stage + "]" : ""))
                         .Replace("{now}", DateTime.Now.ToString(CurrentFormatProvider))
                         .Replace("{today}", DateTime.Now.ToString(culture.DateTimeFormat.ShortDatePattern, culture));
            return ret;
        }

        public static FileInfo GetTemplate(string fileName, ILogger logger = null)
        {
            var assemblyPath = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

            FileInfo fi = new FileInfo(Path.Combine(assemblyPath.Directory.FullName, "wwwroot", "Templates", fileName));
            if (fi.Exists)
                return fi;
            else
                if (logger != null)
                logger.LogError("Template {0} not found!", fi.FullName);

            return new FileInfo(Path.Combine(assemblyPath.Directory.FullName, "Templates", fileName));
        }

        public static FileInfo HtmlToPDF(ExportableEntity exportableEntity, FileInfo headerImage, PtfkFormStruct form = null, PetaframeworkStd.Commons.ProcessTask task = null)
        {
            var assemblyPath = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (form != null)
            {
                var id = form.GetHtml(nameof(IEntity.Id));
                if (id.Value == null || String.IsNullOrWhiteSpace(id.Value.ToString()) || (long)id.Value == 0)
                    id.Value = form.ID;
                exportableEntity = Tools.GetEntityHtmlToExport(form, form.Session, GetTemplate("EntityExport.template"), task);
            }

            int c = 0;
            bool rerun = true;
            PetaframeworkStd.Commands.ResultClass result;

            var fi = new FileInfo(Path.Combine(assemblyPath.Directory.FullName, "_Converters", "HtmlToPdfConverter.dll"));
            do
            {
                var success = PetaframeworkStd.Shell.RunDotNetScript(fi, out result, exportableEntity.EntityHtmlFile.FullName, headerImage.FullName);
                if (success || c > 2)
                    rerun = false;
                c++;
            } while (rerun);

            var f = new FileInfo(exportableEntity.EntityHtmlFile.FullName + ".pdf");
            return f;
        }

        private static string GetHeader(FileInfo fi)
        {
            if (fi.Exists)
                return "<img src='file:///" + fi.FullName + "'></br></br>";
            return "";
        }

        internal class CustomAssemblyLoadContext : AssemblyLoadContext
        {
            public IntPtr LoadUnmanagedLibrary(string absolutePath)
            {
                return LoadUnmanagedDll(absolutePath);
            }
            protected override IntPtr LoadUnmanagedDll(String unmanagedDllName)
            {
                return LoadUnmanagedDllFromPath(unmanagedDllName);
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                throw new NotImplementedException();
            }
        }

        private static string GetWKTools()
        {
            var ext = ".dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ext = ".dll";
            else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                ext = ".so";
            else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                ext = ".dylib";
            return "libwkhtmltox" + ext;
        }

        public static PtfkFormStruct GetEntityToReport<T>(PtfkForm<T> entity) where T : IPtfkForm
        {

            var captions = entity.GetAllCaptions();
            if (captions != null)
            {
                var iEntity = entity as IEntity;
                PtfkFormStruct st = new PtfkFormStruct();
                st.html = new List<HtmlElement>();
                st.html.Add(new HtmlElement
                {
                    Name = "field",
                    Caption = entity.FormLabel,
                    TypeSetter = ElementType.text,
                    Id = iEntity.Id.ToString(),
                    Html = new List<HtmlElement>()
                });

                foreach (var item in captions)
                {
                    var html = new HtmlElement
                    {
                        Name = item.Key.Name,
                        Caption = item.Value.LabelText,
                        TypeSetter = ElementType.text
                    };
                    entity.CurrentGenerator.SetHtmlValue(ref html, item, entity);
                    st.html[0].Html.Add(html);
                }
                return st;
            }
            return null;
        }

        public static ExportableEntity GetEntityHtmlToExport(PtfkFormStruct toExport, IPtfkSession session, FileInfo htmlTemplateFile, PetaframeworkStd.Commons.ProcessTask task = null)
        {
            var defaultPattern = @"<style>
  tr:nth-child(even) {background: #FFF}
  tr:nth-child(odd) {background: #EEE; border-left: 10px solid #AAAAAA;}
</style>
<h1>
  <strong>{title}</strong><sub>{stage}</sub>
</h1>
<table>
  <tbody>
    @[
    <tr>
      <td>
        <strong>{caption}</strong>
      </td>
      <td>{value}</td>
    </tr>]@
  </tbody>
</table>";
            IEntity f;

            var i = toExport.html[0].Html.Where(x => x.Name != null && x.Name.Equals(nameof(f.Id))).FirstOrDefault();
            i.Value = toExport.ID;
            i.PlainValue = i.Value;

            List<KeyValuePair<String, HtmlElement>> lst = new List<KeyValuePair<string, HtmlElement>>();
            List<KeyValuePair<String, FileInfo>> files = new List<KeyValuePair<string, FileInfo>>();
            PopulateFromHtml(session, toExport.html, ref lst, ref files);

            var title = "Entity Report";
            if (lst.Any() && String.IsNullOrWhiteSpace(lst.FirstOrDefault().Key))
            {
                title = lst.FirstOrDefault().Value.Caption;
                lst.RemoveAt(0);
            }

            var tempFile = new FileInfo(Path.Combine(PtfkFileInfo.GetDirectoryInfo(session).FullName, Guid.NewGuid().ToString() + ".html"));

            if (htmlTemplateFile != null && htmlTemplateFile.Exists)
                defaultPattern = File.ReadAllText(htmlTemplateFile.FullName);

            FindAndChangeRecursive(tempFile, defaultPattern, title, task?.Name, lst).Wait();

            return new ExportableEntity { EntityHtmlFile = tempFile, Title = title, Attachments = files };
        }

        private static List<KeyValuePair<Type, String>> _IPtfkEntityClasses;
        public static KeyValuePair<Type, String>[] GetIPtfkEntity(Assembly assembly)
        {
            if (_IPtfkEntityClasses == null)
                _IPtfkEntityClasses = new List<KeyValuePair<Type, string>>();
            var lst = _IPtfkEntityClasses.Where(x => x.Key.Assembly.Equals(assembly)).ToArray();
            if (lst.Length == 0)
            {
                var q = from t in assembly.GetTypes()
                        where t.GetInterfaces().Contains(typeof(IPtfkEntity))
                        && !t.Name.StartsWith("Ptfk")
                        select t.UnderlyingSystemType;

                foreach (var item in q)
                {
                    var o = Activator.CreateInstance(item);
                    (o as IPtfkEntity).SetBusiness();
                    var label = o.GetType().GetProperty(nameof(Petaframework.PtfkForm<IPtfkForm>.FormLabel))?.GetValue(o)?.ToString();
                    _IPtfkEntityClasses.Add(new KeyValuePair<Type, String>(item, label));
                }

            }
            else
                return lst;

            return _IPtfkEntityClasses.Where(x => x.Key.Assembly.Equals(assembly)).ToArray();
        }

        internal static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) != null;

        internal static void SetValue(object inputObject, string propertyName, object propertyVal)
        {
            //find out the type
            Type type = inputObject.GetType();

            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(propertyName);

            //find the property type
            Type propertyType = propertyInfo.PropertyType;

            //Convert.ChangeType does not handle conversion to nullable types
            //if the property type is nullable, we need to get the underlying type of the property
            var targetType = IsNullable(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;

            //Returns an System.Object with the specified System.Type and whose value is
            //equivalent to the specified object.
            propertyVal = Convert.ChangeType(propertyVal, targetType);

            //Set the value of the property
            propertyInfo.SetValue(inputObject, propertyVal, null);

        }

        internal static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        private static void PopulateFromHtml(IPtfkSession session, List<HtmlElement> html, ref List<KeyValuePair<string, HtmlElement>> lst, ref List<KeyValuePair<string, FileInfo>> files)
        {
            if (html != null && html.Any())
                foreach (var item in html)
                {
                    if (!String.IsNullOrWhiteSpace(item.Caption) && !String.IsNullOrWhiteSpace(item.Type)
                        && !item.Type.Equals(ElementType.container.ToString())
                        && !item.Type.Equals(ElementType.reset.ToString()))
                    {
                        if (item.Value != null)
                        {
                            var minified = System.Text.RegularExpressions.Regex.Replace(item.Value.ToString(), "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1").Trim();
                            if (item.PlainValue == null && minified.StartsWith("[{"))
                            {
                                var o = item.Value.ToString();
                                var temp = o.Substring(0, o.Length - 2);
                                temp = temp.Substring(2).Trim();
                                try
                                {
                                    item.PlainValue = Tools.DecodeBase64(temp);
                                }
                                catch (Exception ex)
                                {

                                    item.PlainValue = temp;
                                }
                            }
                            else
                            {
                                if (item.PlainValue == null || item.Value == null)
                                    item.PlainValue = item.Value;
                            }
                        }
                        if (item.Type.Equals(ElementType.fieldset.ToString()))
                            lst.Add(new KeyValuePair<string, HtmlElement>(String.Empty, new HtmlElement { Caption = Tools.DecodeBase64(item.Caption) }));
                        else
                        if (item.Type.Equals(ElementType.uploader.ToString()) && item.PlainValue != null)
                        {
                            files.Add(new KeyValuePair<string, FileInfo>(Tools.DecodeBase64(item.Caption), GetPtfkFormStructFileInfo(item.PlainValue, session)));
                        }
                        else
                            lst.Add(new KeyValuePair<string, HtmlElement>(Tools.DecodeBase64(item.Caption), item));
                    }
                    PopulateFromHtml(session, item.Html, ref lst, ref files);
                }
        }

        public static bool IsJsonArray(string json)
        {
            try
            {

                var token = JToken.Parse(json);

                if (token is JArray)
                {
                    return true;
                }
                else if (token is JObject)
                {
                    return false;
                }
                return false;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static FileInfo GetPtfkFormStructFileInfo(object plainValue, IPtfkSession session)
        {
            UploaderFile file;
            try
            {
                try
                {
                    file = Tools.FromJson<UploaderFile>(plainValue.ToString());
                }
                catch (Exception ex)
                {
                    file = plainValue as UploaderFile;
                }
                if (plainValue is Petaframework.PtfkFileInfo)
                {
                    var f = (PtfkFileInfo)plainValue;
                    file = new UploaderFile { Name = f.Name };
                }

                var fi = new FileInfo(Path.Combine(PtfkFileInfo.GetDirectoryInfo(session).FullName, file.Name));
                if (fi.Exists)
                    return fi;
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        public static string GetPropertyName<T>(Expression<Func<T>> propertyLambda)
        {
            var me = propertyLambda.Body as MemberExpression;

            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
            }

            return me.Member.Name;
        }

        public static string GetEnumName(Enum val)
        {
            return val.GetType().Name + "." + Enum.GetName(val.GetType(), val);
        }

        public static ListItem NewListItem(string text, string value, bool selected, string desc)
        {
            var item = new ListItem(text, value);
            item.Selected = selected;
            item.Description = desc;
            return item;
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }


        [Serializable]
        private struct SmtpConfig
        {
            public String SmtpHost { get; set; }
            public int PortNumber { get; set; }
            public String Login { get; set; }
            public String Password { get; set; }
            public String UserAccountAddress { get; set; }
            private String _EmailToAdmin;
            public String EmailToAdmin
            {
                get
                {
                    if (String.IsNullOrWhiteSpace(_EmailToAdmin))
                        return "ti@iema.es.gov.br";
                    else
                        return _EmailToAdmin;
                }
                set { _EmailToAdmin = value; }
            }
        }

        private struct MailThreadParam
        {
            public String smtp { get; set; }
            public MailMessage mailMessage { get; set; }
            public String fromName { get; set; }
            public String bodyMsg { get; set; }
        }

        /// <summary>
        /// Method for sending email.It requires that the informed SMTP server be released to the IP where this call will be executed.
        /// </summary>
        /// <param name="smtpServerHost">SMTP server IP address</param>
        /// <param name="mailMessage">Message to send</param>
        /// <param name="senderName">Sender's  name</param>
        /// <returns></returns>
        public static bool SendMail(String smtpServerHost, MailMessage mailMessage, String senderName, bool threadEnable, params Boolean[] emailToAdmin)
        {
            if (threadEnable)
            {
                var param = new MailThreadParam
                {
                    fromName = senderName,
                    mailMessage = mailMessage,
                    smtp = smtpServerHost,
                    bodyMsg = mailMessage.Body
                };

                Thread t2 = new Thread(SendMail);
                t2.Start(param);
                return true;
            }
            else
            {
                return SendMail(smtpServerHost, mailMessage, senderName, emailToAdmin);
            }
        }

        private static void SendMail(object param)
        {
            var mailParam = (MailThreadParam)param;
            mailParam.mailMessage.Body = mailParam.bodyMsg;
            SendMail(mailParam.smtp, mailParam.mailMessage, mailParam.fromName);
        }

        internal static void FromDbToTemp(
         IPtfkForm obj,
         KeyValuePair<PropertyInfo, FormCaptionAttribute> item,
         PageConfig _config)
        {
            byte[] numArray = (byte[])null;
            try
            {
                numArray = obj.GetType().GetProperty(item.Value.MirroredOf).GetValue((object)obj, (object[])null) as byte[];
            }
            catch (Exception ex)
            {
            }
            if (numArray == null)
                return;
            string str = Guid.NewGuid().ToString();
            string path2 = str + MimeCheck.TranslateMimeToExtension(numArray);
            FileInfo fileInfo = new FileInfo(Path.Combine(PtfkFileInfo.GetDirectoryInfo(_config.Owner).FullName, path2));
            System.IO.File.WriteAllBytes(fileInfo.FullName, numArray);
            PtfkFileInfo.AddFile(_config.Owner, new PtfkFileInfo()
            {
                FileInfo = fileInfo,
                OwnerID = _config.Owner.Login,
                UID = str,
                EntityName = _config.PageType,
                ParentID = Convert.ToInt64(obj.GetType().GetProperty(obj.GetIdAttributeName()).GetValue((object)obj, (object[])null)),
                Name = path2,
                EntityProperty = item.Key.Name
            }, null);
        }

        public static string GetInnerExceptionMessage(Exception ex)
        {
            if (ex.InnerException != null)
                return GetInnerExceptionMessage(ex.InnerException);
            else
                return ex.Message;
        }

        private static Boolean SendMail(string smtpServerHost, MailMessage mailMessage, string nomeExibicaoRemetente, params bool[] emailToAdmin)
        {
            try
            {
                FileInfo fiConfig = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "//smtp.config");
                SmtpConfig config = new SmtpConfig();
                if (!fiConfig.Exists)
                {
                    config.Login = "noreply-iema@iema.es.gov.br";
                    config.Password = "13m4@noreply";
                    config.PortNumber = 587;
                    config.SmtpHost = "mx.correio.es.gov.br";
                    config.UserAccountAddress = "noreply-iema@iema.es.gov.br";

                    var conf = ToJson(config, true);


                    File.WriteAllText(fiConfig.FullName, conf);
                }
                else
                {
                    try
                    {
                        config = JsonConvert.DeserializeObject<SmtpConfig>(File.ReadAllText(fiConfig.FullName));
                    }
                    catch (Exception ex)
                    {
                        config = FromJson<SmtpConfig>(File.ReadAllText(fiConfig.FullName));
                    }
                }

                SmtpClient client = new SmtpClient();
                client.Host = config.SmtpHost;
                client.Credentials = new System.Net.NetworkCredential(config.Login, config.Password);
                if (config.PortNumber > 0)
                {
                    client.Port = config.PortNumber;
                }

                if (emailToAdmin != null && emailToAdmin.Length > 0 && emailToAdmin[0])
                {
                    mailMessage.To.Add(config.EmailToAdmin);
                }

                mailMessage.From = new MailAddress(config.UserAccountAddress, nomeExibicaoRemetente);
                client.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static String DecodeBase64(string str)
        {
            if (IsBase64(str))
            {
                var base64EncodedBytes = System.Convert.FromBase64String(str);
                return Encoding.GetEncoding("iso-8859-1").GetString(base64EncodedBytes);
            }
            return str;
        }

        public static String EncodeBase64(string str, bool checkAlreadyIsBase64 = false)
        {
            if (checkAlreadyIsBase64)
                if (IsBase64(str))
                    return str;
            if (String.IsNullOrWhiteSpace(str))
                return String.Empty;
            var plainTextBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(str);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static bool IsIntegerType(Type o)
        {
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsRealType(Type o)
        {
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsNumberType(Type o)
        {
            return IsRealType(o) || IsIntegerType(o);
        }

        public static IPtfkEntity GetIPtfkEntityByClassName(String className, IPtfkSession _session, ILogger _logger = null, long entityId = 0)
        {
            return GetIPtfkEntityByClassName(PtfkEnvironment.CurrentEnvironment?.PtfkDbContext?.GetType().Assembly, className, _session, _logger, entityId);
        }

        public static IPtfkEntity GetIPtfkEntityByClassName(Assembly assembly, String className, IPtfkSession _session, ILogger _logger = null, long entityId = 0)
        {
            if (String.IsNullOrWhiteSpace(className))
                className = Constants.PtfkViewWorkflowsClassName;
            var q = from t in assembly.GetTypes()
                    where t.GetInterfaces().Contains(typeof(IPtfkEntity))
                    && t.Name.Equals(className)
                    select t.UnderlyingSystemType;
            var tp = q.FirstOrDefault();

            if (tp != null)
            {
                var obj = Activator.CreateInstance(tp) as IPtfkEntity;
                if (_logger != null)
                    obj.Logger = _logger;
                if (_session != null)
                    obj.Owner = _session;
                obj.Id = entityId;
                return obj;
            }
            return null;
        }

        public static string GetHashMD5(FileInfo file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file.FullName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static string GetHashMD5(byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        internal static List<Type> GetIMediaClasses()
        {
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && ImplementsInterface(t, typeof(IPtfkMedia))
                    select t;
            return q.ToList();
        }

        internal static object GetIBusinessMediaClass(Assembly assembly, String ownerID)
        {
            if (PtfkFileInfo.BusinessClassMedia != null)
                return PtfkFileInfo.BusinessClassMedia;

            var iSession = (from t in assembly.GetTypes()
                            where t.IsClass && ImplementsInterface(t, typeof(IPtfkSession))
                            select t).FirstOrDefault();
            var sess = (Activator.CreateInstance(iSession) as IPtfkSession).Current;

            var q = from t in assembly.GetTypes()
                    where t.IsClass && ExtendsIMediaBusinessClass(t, typeof(IPtfkBusiness<>), sess)
                    select t;
            if (q.Any())
                return PtfkFileInfo.BusinessClassMedia;
            return null;
        }

        internal static object GetIBusinessEntityJoinClass(Assembly assembly)
        {
            if (PtfkEntityJoined.BusinessClassMedia != null)
                return PtfkEntityJoined.BusinessClassMedia;

            var iSession = (from t in assembly.GetTypes()
                            where t.IsClass && ImplementsInterface(t, typeof(IPtfkSession))
                            select t).FirstOrDefault();
            var sess = (Activator.CreateInstance(iSession) as IPtfkSession).Current;

            var q = from t in assembly.GetTypes()
                    where t.IsClass && ExtendsIPtfkEntityJoinBusinessClass(t, typeof(IPtfkBusiness<>), sess)
                    select t;
            if (q.Any())
                return PtfkEntityJoined.BusinessClassMedia;
            return null;
        }

        private static bool ImplementsInterface(Type type, Type equalsTo)
        {
            try
            {
                return type.GetInterfaces().Contains(equalsTo);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool ExtendsIMediaBusinessClass(Type toCheck, Type generic, IPtfkSession session)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                try
                {
                    var iface = toCheck.GetInterface(generic.Name, true);
                    if (generic.IsInterface && iface != null && iface.IsGenericType && ImplementsInterface(iface.GetGenericArguments()[0], typeof(IPtfkMedia)))
                    {

                        PtfkFileInfo.BusinessClassMedia = Activator.CreateInstance(toCheck, session.Current);
                        return true;
                    }
                }
                catch (Exception)
                {

                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private static bool ExtendsIPtfkEntityJoinBusinessClass(Type toCheck, Type generic, IPtfkSession session)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                try
                {
                    var iface = toCheck.GetInterface(generic.Name, true);
                    if (generic.IsInterface && iface != null && iface.IsGenericType && ImplementsInterface(iface.GetGenericArguments()[0], typeof(IPtfkEntityJoin)))
                    {

                        PtfkEntityJoined.BusinessClassMedia = Activator.CreateInstance(toCheck, session.Current);
                        return true;
                    }
                }
                catch (Exception)
                {

                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        internal static object GetIBusinessLogClass(Assembly assembly)
        {
            if (BaseWorkflow.HasResult)
            {
                if (BaseWorkflow.GetBusinessClassLogResult() != null)
                    return BaseWorkflow.GetBusinessClassLogResult();
            }

            var iSession = (from t in assembly.GetTypes()
                            where t.IsClass && ImplementsInterface(t, typeof(IPtfkSession))
                            select t).FirstOrDefault();
            var sess = (Activator.CreateInstance(iSession) as IPtfkSession).Current;

            var q = from t in assembly.GetTypes()
                    where t.IsClass && ExtendsILogBusinessClass(t, typeof(IPtfkBusiness<>), sess)
                    select t;
            if (q.Any())
                return BaseWorkflow.GetBusinessClassLogResult();
            return null;
        }

        internal static IPtfkLog GetIPtfkLogClass(Assembly assembly)
        {
            if (BaseWorkflow.ClassLog != null)
                return BaseWorkflow.ClassLog;

            var iLog = (from t in assembly.GetTypes()
                        where t.IsClass && ImplementsInterface(t, typeof(IPtfkLog))
                        select t).FirstOrDefault();
            var log = (Activator.CreateInstance(iLog) as IPtfkLog);

            if (log != null)
                BaseWorkflow.ClassLog = log;

            return BaseWorkflow.ClassLog;
        }

        private static bool ExtendsILogBusinessClass(Type toCheck, Type generic, IPtfkSession session)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                try
                {
                    var iface = toCheck.GetInterface(generic.Name, true);
                    if (generic.IsInterface && iface != null && iface.IsGenericType && ImplementsInterface(iface.GetGenericArguments()[0], typeof(IPtfkLog)))
                    {

                        BaseWorkflow.BusinessClassLog = Task.Factory.StartNew(() => Activator.CreateInstance(toCheck, session));
                        return true;
                    }
                }
                catch (Exception)
                {

                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        internal static object GetIBusinessWorkerClass(Assembly assembly, IPtfkSession session)
        {
            if (PtfkWorker.BusinessClassWorker != null)
                return PtfkWorker.BusinessClassWorker;

            var q = from t in assembly.GetTypes()
                    where t.IsClass && ExtendsIWorkerBusinessClass(t, typeof(IPtfkBusiness<>), session)
                    select t;
            if (q.Any())
            {
                PtfkWorker.BusinessClassWorker = Activator.CreateInstance(q.FirstOrDefault(), session);
                return PtfkWorker.BusinessClassWorker;
            }
            return null;
        }

        internal static bool ExtendsIWorkerBusinessClass(Type toCheck, Type generic, IPtfkSession session)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                try
                {
                    var iface = toCheck.GetInterface(generic.Name, true);
                    if (generic.IsInterface && iface != null && iface.IsGenericType && ImplementsInterface(iface.GetGenericArguments()[0], typeof(IPtfkWorker)))
                    {

                        PtfkWorker.BusinessClassWorker = Activator.CreateInstance(toCheck, session);
                        return true;
                    }
                }
                catch (Exception)
                {

                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        internal static bool IsProductionEnvironment
        {
            get
            {
                try
                {
                    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    return env.ToLower().StartsWith("pro");
                }
                catch (Exception ex)
                {
                    return true;
                }
            }
        }

        public static bool IsDevelopmentEnvironment() { return Petaframework.PtfkEnvironment.CurrentEnvironment.WebHostEnvironment.IsDevelopment(); }
        public static Logger Log { get { return Petaframework.PtfkEnvironment.CurrentEnvironment?.Log; } }


        internal static async Task<IPtfkWorkflow<T>> GetIWorkflow<T>(IPtfkSession session, T entity, IPtfkBusiness<T> businessClass) where T : IPtfkForm
        {
            try
            {
                if (entity == null)
                    return null;
                var obj = Activator.CreateInstance(PtfkWorkflow<T>.GetOrSetWorkflowBusiness(entity), session, entity, businessClass);

                return obj as IPtfkWorkflow<T>;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static void CheckPermission(IPtfkForm form, IPtfkSession session)
        {
            if (form == null || session == null)
                Petaframework.ErrorTable.Err014();
            var listPermissions = Strict.ConfigurationManager.GetPermissions(form.GetType().Name).ToList();
            if (listPermissions.Count == 0)
                Petaframework.ErrorTable.Err012(session.Login, form.GetType().Name);

            var enabledList = new List<string>();

            var permissions = listPermissions.Where(x => x.Profile.Equals("*") ||
                                                        (x.HierarchyFlag && !String.IsNullOrWhiteSpace(session.Department?.ID)) ||
                                                        (x.EnabledTo.Contains("*") ||
                                                        x.EnabledTo.Contains(session.Login) ||
                                                        x.EnabledTo.Contains(Constants.TOKEN_USER_ID + session.Login) ||
                                                        x.EnabledTo.Contains(Constants.TOKEN_DEPARTMENT_ID + session.Department?.ID))
                                                        );
            if (!permissions.Any())
            {
                Petaframework.ErrorTable.Err012(session.Login, form.GetType().Name);
            }
        }

        public static string CleanInjection(string text)
        {
            text = text.Replace("'", String.Empty);
            text = text.Replace("\"", String.Empty);
            text = text.Replace("´", String.Empty);
            text = text.Replace(";", String.Empty);
            text = text.Replace("--", String.Empty);
            text = text.Replace("/", "%");
            text = text.Replace("=", String.Empty);
            text = text.Replace("\"", "%");
            return text;
        }

        public static bool HasPermission(IPtfkForm form, IPtfkSession session)
        {
            try
            {
                CheckPermission(form, session);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string GetBuildVersion()
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(ConfigurationManager.CONFIG_PATH, "@Version"));
            return fileInfo.Exists ? System.IO.File.ReadAllText(fileInfo.FullName).Trim() : "0";
        }

        public static string GetAppVersion()
        {
            return string.Join('.', System.Environment.Version.Major, System.Environment.Version.Minor, GetBuildVersion());
        }

        public static string GetPetaFrkVersion()
        {
            return typeof(Tools).Assembly.GetName().Version.ToString();
        }

        internal struct UserSimulated
        {
            public string Login { get; set; }

            public string Name { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Description { get; set; }
        }

        #region PetaframeworkStd

        public static IEnumerable<KeyValuePair<string, object>> GetToSendParameters(IServiceParameter objectToListProperties)
        {
            foreach (var item in objectToListProperties.GetType().GetProperties())
            {
                if (!item.Name.Equals(nameof(IServiceParameter.Authorization)) &&
                    !item.Name.Equals(nameof(IServiceParameter.ToSendFile)) &&
                    !item.Name.Equals(nameof(IServiceParameter.ToSendParametersList)))
                    yield return new KeyValuePair<string, object>(item.Name, item.GetValue(objectToListProperties));
            }
        }

        public static bool IsBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0
               || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static string GetMacAddress()
        {
            var macAddr = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                           where nic.OperationalStatus == OperationalStatus.Up
                           select nic.GetPhysicalAddress().ToString()).FirstOrDefault();
            return macAddr;
        }

        public static string GetMacAddresses()
        {
            var macAddr = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                           where nic.OperationalStatus == OperationalStatus.Up
                           select nic.GetPhysicalAddress().ToString());
            return string.Join(",", macAddr);
        }

        public static bool ContainsMacAddress(string mac)
        {
            var macAddrs = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                            where nic.OperationalStatus == OperationalStatus.Up
                            select nic.GetPhysicalAddress().ToString()).ToList();
            return macAddrs.Contains(mac);
        }

        public static IPAddress GetCurrentIP()
        {
            IPAddress IP = IPAddress.Parse("127.0.0.1");
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = ip;
                    break;
                }
            }
            return IP;
        }

        public static IPAddress GetHostIP(String hostnameOrAddress)
        {
            IPAddress IP = IPAddress.Parse("127.0.0.1");
            var host = Dns.GetHostEntry(hostnameOrAddress);
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.Equals(IP))
                {
                    if (ip.ToString().Equals(IP.ToString()))
                        IP = GetCurrentIP();
                    else
                        IP = ip;
                    break;
                }
            }
            if (IP.ToString().Equals("127.0.0.1"))
            {
                return GetCurrentIP(); ;
            }
            return IP;
        }

        public class FileInfoContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                return objectType == typeof(FileInfo) ? (JsonContract)this.CreateISerializableContract(objectType) : base.CreateContract(objectType);
            }
        }

        #endregion

    }

    public static class IsLocalExtension
    {
        private const string NullIpAddress = "::1";

        public static bool IsLocal(this HttpRequest req)
        {
            var connection = req.HttpContext.Connection;
            if (connection.RemoteIpAddress.IsSet())
            {
                return connection.LocalIpAddress.IsSet()
                    ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                    : IPAddress.IsLoopback(connection.RemoteIpAddress);
            }

            return true;
        }

        private static bool IsSet(this IPAddress address)
        {
            return address != null && address.ToString() != NullIpAddress;
        }
    }
    public static class BoolExtensions
    {
        public static string ToString(this bool? v, string trueString, string falseString, string nullString = "Undefined")
        {
            return v == null ? nullString : v.Value ? trueString : falseString;
        }
        public static string ToString(this bool v, string trueString, string falseString)
        {
            return ToString(v, trueString, falseString, null);
        }
    }

    public static class IConfigurationSectionExtensions
    {
        public static Middlewares.JwtMiddleware.OIdCSettings ToOIdcSettings(this Microsoft.Extensions.Configuration.IConfigurationSection config)
        {
            var t = new Petaframework.Middlewares.JwtMiddleware.OIdCSettings();
            config.Bind(t);
            return t;
        }
    }

    public static class DateTimeJavaScript
    {
        private static readonly long DatetimeMinTimeTicks =
           (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        public static long ToJavaScriptMilliseconds(this DateTime dt)
        {
            return (long)((dt.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000);
        }
    }
}
