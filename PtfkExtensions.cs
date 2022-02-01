using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Petaframework.Interfaces;
using Petaframework.Middlewares;
using Petaframework.POCO;
using Petaframework.Strict;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Petaframework.Middlewares.JwtMiddleware;

namespace Petaframework
{
    public static class PtfkExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddPetaframework(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<PtfkServicesParameter> parameter)
        {
            var parameters = new PtfkServicesParameter();
            parameter.Invoke(parameters);
            Petaframework.Strict.ConfigurationManager.Set(parameters.Configuration);
            PtfkConsole.Set(
                        () => parameters.Configuration.GetValue<bool>(Constants.AppSettings.DebugByPtfkConsole, false),
                        () => parameters.Configuration.GetValue<string>(Constants.AppSettings.DebugSessionIds, "*"));

            if (Petaframework.PtfkEnvironment.CurrentEnvironment == null)
                Petaframework.PtfkEnvironment.CurrentEnvironment = new PtfkEnvironment(parameters.Configuration, parameters.WebHostEnvironment, parameters.Logger);
            Petaframework.PtfkEnvironment.CurrentEnvironment.ConsumerClass = parameters.ConsumerClass;

            return services;
        }

        public static IApplicationBuilder UsePetaframework<PtfkDbContext, PtfkLogType, PtfkMediaType>
                                                                                                    (this IApplicationBuilder app) where PtfkDbContext : IPtfkDbContext, new()
                                                                                                                                   where PtfkLogType : IPtfkLog, new()
                                                                                                                                   
                                                                                                                                   where PtfkMediaType : IPtfkMedia, new()
                                                                                                                                   
        {
            //SetEnvironment(env);
            PtfkEnvironment.CurrentEnvironment.AddDbContext(new PtfkDbContext());
            PtfkEnvironment.CurrentEnvironment.AddLogClass(new PtfkLogType());
            PtfkEnvironment.CurrentEnvironment.AddMediaClass(new PtfkMediaType());
            return app;
        }

        public static IApplicationBuilder UsePetaframework<PtfkDbContext, PtfkLogType, PtfkWorkerType, PtfkMediaType, PtfkEntityJoinType>
                                                                                                      (this IApplicationBuilder app) where PtfkDbContext : IPtfkDbContext, new()
                                                                                                                                     where PtfkLogType : IPtfkLog, new()
                                                                                                                                     where PtfkWorkerType : IPtfkWorker, new()
                                                                                                                                     where PtfkMediaType : IPtfkMedia, new()
                                                                                                                                     where PtfkEntityJoinType : IPtfkEntityJoin, new()
        {
            //SetEnvironment(env);
            UsePetaframework<PtfkDbContext, PtfkLogType, PtfkMediaType>(app);
            PtfkEnvironment.CurrentEnvironment.AddWorkerClass(new PtfkWorkerType());
            PtfkEnvironment.CurrentEnvironment.AddEntityJoinClass(new PtfkEntityJoinType());
            return app;
        }

        /// <summary>
        /// Returns a report object from the Petaframework environment
        /// </summary>
        /// <param name="response">The from HttpContext</param>
        /// <param name="owner">The owner of request</param>
        /// <param name="onSearchingSession">The function to be performed to return the user's session through the login parameter</param>
        /// <returns></returns>
        public static Portal.Reporter GetReport(this HttpResponse response, IPtfkSession owner, Func<String, IPtfkSession> onSearchingSession)
        {
            try
            {
                var reportType = response.HttpContext.Request.Query["t"].ToString();
                var q = response.HttpContext.Request.Query["q"].ToString();
                var request = Tools.FromJson<Petaframework.POCO.FormRequest>(Tools.DecodeBase64(q));
                var json = Tools.GetPtfkFormStruct(request?.Json, owner, response, onSearchingSession);

                DateTime dt = DateTime.MinValue;
                var lac = Tools.DecodeBase64(response.HttpContext.Request.Query["lac"].ToString());
                DateTime.TryParse(lac, out dt);
                if (dt != DateTime.MinValue)
                    json.ActivitiesAfterThisDate = Convert.ToDateTime(lac);
                IPtfkEntity ipeta = CreateInstance(owner.Current);
                if (Petaframework.PtfkEnvironment.CurrentEnvironment.HasPtfkDbContext())
                    ipeta = Tools.GetIPtfkEntityByClassName(Petaframework.PtfkEnvironment.CurrentEnvironment.PtfkDbContext.GetType().Assembly, request?.EntityType, owner.Current, Petaframework.PtfkEnvironment.CurrentEnvironment.Logger, json.ID);
                return new Portal.Reporter(reportType, ipeta, json, onSearchingSession);

            }
            catch (Exception ex)
            {
                PtfkEnvironment.CurrentEnvironment.Log.Error(Tools.ToJson(ex, true));
                throw;
            }
        }

        private static IPtfkEntity CreateInstance(IPtfkSession owner)
        {
            if (PtfkEnvironment.CurrentEnvironment.ConsumerClass == null)
                return null;
            IPtfkEntity t = Activator.CreateInstance(PtfkEnvironment.CurrentEnvironment.ConsumerClass.GetType()) as IPtfkEntity;
            t.Owner = owner;
            return t;
        }

        /// <summary>
        /// Returns a submitter object from the Petaframework environment
        /// </summary>
        /// <param name="response">The from HttpContext</param>
        /// <param name="owner">The owner of request</param>
        /// <param name="onSearchingSession">The function to be performed to return the user's session through the login parameter</param>
        /// <returns></returns>
        public static Portal.Submitter GetSubmitter(this HttpResponse response, IPtfkSession owner, Func<String, IPtfkSession> onSearchingSession)
        {
            var type = "";
            var request = new SubmitRequest(owner, response, onSearchingSession);
            string body = "";
            IFormCollection formCollection = null;
            IFormFileCollection fileCollection = null;
            ArchiveRequest file = null;
            try
            {

                response.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                if (!String.IsNullOrWhiteSpace(response.HttpContext.Request.ContentType) && response.HttpContext.Request.ContentType.ToLower().Contains("multipart/"))
                {
                    var streamContent = new StreamContent(response.HttpContext.Request.Body);
                    if (response.HttpContext.Request.ContentType != null)
                        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(response.HttpContext.Request.ContentType);

                    var provider = streamContent.ReadAsMultipartAsync().Result;

                    StringBuilder str = new();
                    foreach (var item in provider.Contents.Where(x => x.Headers.ContentType == null))
                        str.AppendLine(String.Concat(item.Headers.ContentDisposition.Name, ":\"", item.ReadAsStringAsync().Result, "\","));

                    body = String.Concat("{", str.ToString(), "}");
                    file = Tools.FromKeyValuePair<ArchiveRequest>(response.HttpContext.Request.Query.ToList());
                }
                else
                {
                    using StreamReader stream = new(response.HttpContext.Request.Body);
                    body = stream.ReadToEndAsync().Result;
                }
                if (response.HttpContext.Request.Method == HttpMethods.Get)
                    file = Tools.FromKeyValuePair<ArchiveRequest>(response.HttpContext.Request.Query.ToList());
                else
                {
                    formCollection = response.HttpContext.Request.Form;
                    fileCollection = formCollection?.Files;
                }

            }
            catch (Exception e)
            {

            }
            if (String.IsNullOrWhiteSpace(body) && file == null)
            {
                var msg = string.Format("The HTTP Request could not be read. {0} not found! Make sure that it configured as Transient Service", nameof(PtfkPipelineFilter));
                var e = new Exception(msg);
                PtfkConsole.WriteError(msg);
                throw e;
            }

            FormRequest form = Tools.FromJson<FormRequest>(Tools.DecodeBase64(body));
            if (form != null)
            {
                type = Constants.SubmitterType.Form;
                form.FormStruct = Tools.GetPtfkFormStruct(form.Json, owner, response, onSearchingSession);
                var ipeta = Tools.GetIPtfkEntityByClassName(Petaframework.PtfkEnvironment.CurrentEnvironment.PtfkDbContext.GetType().Assembly, form.EntityType, owner.Current, Petaframework.PtfkEnvironment.CurrentEnvironment.Logger, form.GetFormStruct().ID);
                request.Entity = ipeta;
            }
            if (file != null)
                type = Constants.SubmitterType.File;
            else
                file = formCollection == null ? null : file.UpdatePropertiesFromForm(Tools.FromKeyValuePair<ArchiveRequest>(response.HttpContext.Request.Query.ToList()));
            if (file != null)
                file.UpdatePropertiesFromForm(formCollection, fileCollection);
            if (file?.Attachment != null)
                type = Constants.SubmitterType.File;
            request.Form = form;
            request.File = file;

            var data = formCollection == null ? null : Tools.FromKeyValuePair<DatatableRequest>(formCollection.ToList());
            if (data != null && !String.IsNullOrWhiteSpace(data.FormType))
                type = Constants.SubmitterType.Datatable;
            request.Datatable = data;
            request.Type = type;

            return new Portal.Submitter(request, onSearchingSession);
        }

        internal static ArchiveRequest UpdatePropertiesFromForm(this ArchiveRequest arch, IFormCollection formCollection, IFormFileCollection fileCollection)
        {
            if (formCollection == null)
            {
                arch = null;
                return arch;
            }
            var el = Tools.FromKeyValuePair<ArchiveRequest>(formCollection.ToList());
            arch.Qquuid = el.Qquuid;
            arch.Attachment = fileCollection.FirstOrDefault();
            return arch;
        }
        internal static ArchiveRequest UpdatePropertiesFromForm(this ArchiveRequest arch, ArchiveRequest fromMultiPart)
        {
            if (arch != null)
            {
                arch.Qquuid = fromMultiPart.Qquuid ?? arch.Qquuid;
                arch.FileName = fromMultiPart.FileName ?? arch.FileName;
                arch.Size = fromMultiPart.Size ?? arch.Size;
            }
            return arch;
        }

        internal static HttpContext PtfkEnableSubmitter(this HttpContext context)
        {
            context.Request.EnableBuffering();
            var owner = context.Request.HttpContext.RequestServices.GetRequiredService<IPtfkSession>();

            return context;
        }

        /// <summary>
        /// This configuration must be informed in the HTTP request pipeline. It validates the session user's credentials by Bearer Authentication.
        /// </summary>
        /// <param name="context">The HttpContext</param>
        /// <param name="nextRoutine">The routine to execute after the authentication challenge.</param>
        /// <param name="settings">The Settings from OpenId Connect</param>
        /// <param name="oidcAuthenticationSchemeName">The OpenId Connect Authentication Scheme Name</param>
        /// <param name="authenticationDefaultScheme">Authentication Scheme Name</param>
        /// <param name="statusCodesToVerify">The status codes from the response considered to validate authentication. Default value: StatusCodes.Status302Found</param>
        /// <returns></returns>
        public static async Task PtfkBearerAuthenticationAsync(this HttpContext context, Func<Task> nextRoutine, string oidcAuthenticationSchemeName = "oidc", String authenticationDefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme, params int[] statusCodesToVerify)
        {
            List<int> responseCodeCheck = (statusCodesToVerify?.Length > 0 ? statusCodesToVerify : new int[] { (int)StatusCodes.Status302Found }).ToList();

            responseCodeCheck.Add(StatusCodes.Status401Unauthorized);

            var bearerAuth = context.Request.Headers["Authorization"]
                        .FirstOrDefault()?.StartsWith("Bearer ") ?? false;            
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized
                && !context.User.Identity.IsAuthenticated
                && !bearerAuth)
            {
                context.ChallengeAsync(oidcAuthenticationSchemeName).Wait();
            }
            else
                if (responseCodeCheck.Contains(context.Response.StatusCode)
                && !context.User.Identity.IsAuthenticated
                && bearerAuth)
            {
                var settings = context.Request.HttpContext.RequestServices.GetRequiredService<PtfkPipelineFilter>();
                if (settings == null)
                    throw new Exception(nameof(PtfkPipelineFilter) + " not found! Make sure that it configured as Transient Service");
                Petaframework.Middlewares.JwtMiddleware.Challenge(context, settings.OIdCSettings, authenticationDefaultScheme);
                if (context.Response.StatusCode != StatusCodes.Status401Unauthorized)
                    await nextRoutine();                
            }
        }

        internal static PtfkFilter GetPtfkFilter(this IPtfkForm form, PtfkFormStruct.Filter structFilter)
        {
            if (PtfkEnvironment.CurrentEnvironment.LogClass == null)
            {
                ErrorTable.Err019(nameof(PtfkEnvironment.CurrentEnvironment.LogClass));
            }
            return form.FilterDataItems(new PtfkFilter
            {
                Session = form.GetOwner(),
                LogType = PtfkEnvironment.CurrentEnvironment.LogClass,
                RestrictedBySession = form.HasBusinessRestrictionsBySession(),
                FilteredProperties = null,
                FilteredValue = structFilter.SearchValue ?? "",
                PageSize = structFilter.Length == 0 ? 10 : structFilter.Length,
                OrderByAscending = structFilter.OrderAscending,
                OrderByColumnIndex = structFilter.OrderingColumnIndex,
                PageIndex = structFilter.Start / (structFilter.Length == 0 ? 1 : structFilter.Length),
                StopAfterFirstHtml = structFilter.StopAfterFirstHtml
            }, form);
        }

        public static bool IsPtfkClass(this object form)
        {
            var t = form.GetType();
            return (t.GetInterfaces().Contains(typeof(IPtfkLog)) ||
            t.GetInterfaces().Contains(typeof(IPtfkConfig)) ||
            t.GetInterfaces().Contains(typeof(IPtfkMedia)) ||
            t.GetInterfaces().Contains(typeof(IPtfkWorker)) ||
            t.GetInterfaces().Contains(typeof(IPtfkWorkflow<IPtfkForm>)));
        }

        public static bool IsPtfkWorkflowClass(this object form)
        {
            var t = form.GetType();
            return (t.GetInterfaces().Contains(typeof(IPtfkWorker)) ||
            t.GetInterfaces().Contains(typeof(IPtfkWorkflow<IPtfkForm>)));
        }

        /// <summary>
        /// Sets workflow permissions to current User Session. Mandatory for workflow use.
        /// </summary>
        /// <param name="session">Current User Session</param>
        public static void SetOwnerBag(this IPtfkSession session)
        {
            try
            {
                if (session.Bag == null)
                    session.Bag = new Dictionary<string, object>();
                var ables = Tools.GetUserAbleWorkflows(session).ToArray();
                if (!session.Bag.ContainsKey(Petaframework.Constants.UserAbleWorkflows))
                    session.Bag.TryAdd(Petaframework.Constants.UserAbleWorkflows, ables ?? new string[] { });
                if (!session.Bag.ContainsKey(Petaframework.Constants.WorkflowAdmin))
                    session.Bag.TryAdd(Petaframework.Constants.WorkflowAdmin, Petaframework.Strict.ConfigurationManager.GetCurrentPermissionsAsync.Result?.Where(x => x.Value.Where(p => p.IsAdmin == true && p.EnabledTo != null && p.EnabledTo.Contains(session?.Current?.Login)).Any()).Select(x => x.Key).ToArray());
            }
            catch (Exception ex)
            {
                PtfkConsole.WriteLine(Tools.ToJson(ex, true));
            }
        }

        public static void InitSession(this IPtfkSession owner)
        {
            FileInfo[] files = ((IEnumerable<FileInfo>)PtfkFileInfo.GetDirectoryInfo(owner).GetFiles("*.*", SearchOption.AllDirectories)).ToArray<FileInfo>();
            new Thread((ThreadStart)(() => PtfkFileInfo.DeleteFiles(files))).Start();
            foreach (string entityName in ((IEnumerable<Type>)owner.GetType().Assembly.GetTypes()).Where<Type>((Func<Type, bool>)(t => ((IEnumerable<Type>)t.GetInterfaces()).Contains<Type>(typeof(IPtfkEntity)))).Select<Type, Type>((Func<Type, Type>)(t => t.UnderlyingSystemType)).Select<Type, string>((Func<Type, string>)(x => x.Name)))
                PtfkFileInfo.ClearFiles(entityName, owner);
        }
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string columnName, bool isAscending = true)
        {
            if (String.IsNullOrEmpty(columnName))
            {
                return source;
            }

            ParameterExpression parameter = Expression.Parameter(source.ElementType, "");

            MemberExpression property = Expression.Property(parameter, columnName);
            LambdaExpression lambda = Expression.Lambda(property, parameter);

            string methodName = isAscending ? "OrderBy" : "OrderByDescending";

            Expression methodCallExpression = Expression.Call(typeof(Queryable), methodName,
                                  new Type[] { source.ElementType, property.Type },
                                  source.Expression, Expression.Quote(lambda));

            return source.Provider.CreateQuery<T>(methodCallExpression);
        }

        public static IEnumerable<T> OrderBySequence<T, TId>(
       this IEnumerable<T> source,
       IEnumerable<TId> order,
       Func<T, TId> idSelector)
        {
            var lookup = source.ToLookup(idSelector, t => t);
            foreach (var id in order)
            {
                foreach (var t in lookup[id])
                {
                    yield return t;
                }
            }
        }
    }
}
