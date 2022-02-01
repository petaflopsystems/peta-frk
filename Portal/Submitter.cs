using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;
using Petaframework.POCO;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.POCO.OutboundData;
using static Petaframework.Portal.IPortalBase;

namespace Petaframework.Portal
{
    public class Submitter : PortalBase, IPortalBase
    {
        internal Submitter(SubmitRequest request, Func<String, IPtfkSession> onSearchingSession)
        {
            Entity = request.Entity;
            OnSearchingSession = onSearchingSession;
            _request = request;
            Owner = request.Owner;
        }

        internal readonly SubmitRequest _request;


        PtfkSubmitterEvents _Events;
        public PtfkSubmitterEvents Events
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
            /*Type (t) options:
             *       -> [default]                 needs {  }                          returns { StatusCodes.Status404NotFound }
             *  form -> [Form]                    needs { q.type, q.json.__id }       returns { StatusCodes.Status200OK(Archive) }
             *  data -> [Datatable]               needs { q.type }                    returns { [wfs], xml }
             *  file -> [File]                    needs { }                           returns { StatusCodes.Status200OK(Archive) | 
             *                                                                                  StatusCodes.Status200OK(UploaderFile[])
             */
            switch (Tools.GetPtfkEnvironmentStatus())
            {
                case Enums.EnvironmentStatus.Online:
                    break;
                case Enums.EnvironmentStatus.MaintenanceMode:
                    var ex1 = new PtfkException(PtfkException.ExceptionCode.PtfkSystemUnderMaintenance, "System under maintenance. Try again in a few minutes or contact your system administrator.");
                    return new OkObjectResult(GetErrorFormResponse(ex1));
                case Enums.EnvironmentStatus.Offline:
                    var ex = new PtfkException(PtfkException.ExceptionCode.PtfkSystemOffline, "Offline system. Try again in a few minutes or contact your system administrator.");
                    return new OkObjectResult(GetErrorFormResponse(ex));
            }
            ActionResult ret;
            switch (this._request.Type)
            {
                case Constants.SubmitterType.Form:
                    var form = DoPostForm();
                    ret = form;
                    break;
                case Constants.SubmitterType.Datatable:
                    var data = DoPostDatatable();
                    ret = data;
                    break;
                case Constants.SubmitterType.File:
                    var file = DoPostFile();
                    ret = new OkObjectResult(file);
                    break;
                default:
                    ret = new NotFoundObjectResult("");
                    break;
            }
            return ret;
        }

        private PtfkFormStruct GetErrorFormResponse(Exception ex) {
            PtfkFormStruct form = new()
            {
                ID = -1,
                action = "error",
                caption = Tools.GetExceptionMessage(ex)
            };
            return form;
        }

        private object DoPostFile()
        {
            try
            {
                Tools.CheckUserSimulation(Owner, _request.File.SimulatedUser, _request.Response);
                var session = Owner.Current;

                var pcontext = new FileStageResponseContext(this._request?.File);
                InvokeAction(() => this.Events?.FileSubmitting?.OnPreValidateResponse?.Invoke(pcontext), pcontext);

                Int64 id = 0;
                Int64.TryParse(_request.File.Id, out id);

                var filesList = PtfkFileInfo.GetFiles(session, _request.File.Entity, _request.File.PropertyName, id);

                var di = PtfkFileInfo.GetDirectoryInfo(session);

                //Delete
                if (_request.File.MarkedAsDeleted)
                {
                    if (_request.File.MarkedAsDeleted)
                    {
                        PtfkFileInfo.DeleteFile(session, _request.File.Qquuid);

                        return new Archive { Success = true };
                    }
                }

                //List
                if (_request.File.IsList && id > 0)
                {
                    var files = new List<UploaderFile>();
                    foreach (var item in filesList.Where(x => x != null && x.ParentID.Equals(id)))
                    {
                        if (item.FileInfo.Exists)
                        {
                            var path = Path.Combine(PtfkFileInfo.GetServerPath(session.Login).Replace("~", String.Empty), item.FileInfo.Name);
                            files.Add(new UploaderFile
                            {
                                Name = item.Name,
                                Size = item.FileInfo.Length,
                                Uuid = item.UID,
                                Path = path
                            });
                        }
                    }
                    var ucontext = new ListFileStageResponseContext(this._request?.File, files);
                    InvokeAction(() => this.Events?.FileSubmitting?.OnListingResponse?.Invoke(ucontext), ucontext);
                    files = ucontext.Files;

                    return Tools.ToJson(files);
                }
                else
                {
                    string fileName = Guid.NewGuid().ToString();
                    if (!string.IsNullOrWhiteSpace(_request.File.Qquuid))
                    {
                        var file = GetFile(di, _request.File.Attachment).Result;

                        var ptfkFile = new PtfkFileInfo
                        {
                            FileInfo = file,
                            OwnerID = session.Login,
                            UID = _request.File.Qquuid,
                            EntityName = _request.File.Entity,
                            ParentID = id,
                            Name = file.Name,
                            EntityProperty = _request.File.PropertyName
                        };

                        var ucontext = new UploadFileStageResponseContext(this._request?.File, ptfkFile);
                        InvokeAction(() => this.Events?.FileSubmitting?.OnUploadResponse?.Invoke(ucontext), ucontext);
                        ptfkFile = ucontext.File;

                        PtfkFileInfo.AddFile(session, ptfkFile);

                        return new Archive { Success = true };
                    }
                    else
                    {
                        return new Archive();
                    }
                }
            }
            catch (Exception ex)
            {
                return new Archive();
            }
        }

        private async Task<FileInfo> GetFile(DirectoryInfo di, IFormFile qqfile)
        {
            var filename = qqfile.FileName.Trim('\"');
            var fi = Path.Combine(di.FullName, filename);
            using (var stream = new FileStream(fi, FileMode.Create))
            {
                await qqfile.CopyToAsync(stream);
            }

            return new FileInfo(fi);
        }

        private ActionResult DoPostDatatable()
        {
            PtfkFormStruct dform = new PtfkFormStruct();
            var req = _request.Datatable;
            dform.action = req.Action;
            dform.url = req.Url;
            dform.method = req.Method;

            var implicitCodes = Tools.FromJson<List<KeyValuePair<string, object>>>(Tools.DecodeBase64(Convert.ToString(req.ImplicitCodes)));

            dform.FilterObject = new PtfkFormStruct.Filter();
            dform.FilterObject.Draw = req.Draw;
            dform.FilterObject.Start = req.Start;
            dform.FilterObject.Length = req.PtfkFormLength;
            //dform.FilterObject.SearchRegex = req.SearchRegex;
            dform.FilterObject.SearchValue = String.IsNullOrWhiteSpace(req.SearchValue) ? String.Empty : req.SearchValue.Trim();
            dform.FilterObject.OrderDirection = req.OrderDirection;
            dform.FilterObject.OrderingColumnIndex = req.OrderIndex;
            dform.FilterObject.UseCash = true;
            dform.implicitCodes = implicitCodes;
            dform.simulated = req.SimulatedUser;

            dform.Session = Owner;

            return DoPostForm(new FormRequest { EntityType = req.FormType, Json = Tools.ToJson(dform), FormStruct = dform });
        }

        private ActionResult DoPostForm(FormRequest request = null)
        {
            var formRequest = request == null ? _request.Form : request;
            var _owner = _request.Owner;
            var json = formRequest.GetFormStruct();
            var type = formRequest.EntityType;
            try
            {
                var pcontext = new PreValidateContext(formRequest?.GetFormStruct());
                InvokeAction(() => this.Events?.FormSubmitting?.OnPreValidateResponse?.Invoke(pcontext), pcontext);

                json.AutoSave = json?.method?.ToLower() == Constants.FormMethod.AutoSave;

                if (json.html != null && json.html[0].Readonly && json.AutoSave)
                    return new OkObjectResult("");

                if (!_owner.Current.IsAdmin &&
                    !type.ToLower().Equals(PtfkEnvironment.CurrentEnvironment.LogClass.GetType().Name.ToLower()) &&
                    !type.ToLower().Equals(Constants.PtfkViewWorkflowsClassName.ToLower()) &&
                    (String.IsNullOrWhiteSpace(json.method) || json.method.ToLower() == Constants.FormMethod.Get))
                {
                    json.method = Constants.FormMethod.Form;
                }
                var ret = Tools.GetIPtfkEntityByClassName(PtfkEnvironment.CurrentEnvironment.PtfkDbContext.GetType().Assembly, type, _owner.Current, PtfkEnvironment.CurrentEnvironment.Logger).Run(json);
                return new OkObjectResult(ret);
            }
            catch (PtfkException ex)
            {
                var msg = Tools.ToJson(ex, true, false);
                Task.Run(() => PtfkEnvironment.CurrentEnvironment.Log.Error(msg));
                PtfkConsole.WriteLine(msg);
                PtfkFormStruct form = GetErrorFormResponse(ex);
                form.action = json.AutoSave ? "error" : "redirect";

                var pcontext = new PredictedExceptionContext(formRequest?.GetFormStruct(), form, ex);
                InvokeAction(() => this.Events?.FormSubmitting?.OnPredictedExceptionThrown?.Invoke(pcontext), pcontext, false);

                //form.caption = queryString;
                return new OkObjectResult(form);
            }
            catch (Exception ex)
            {
                if (PtfkException.GetLastOccurrence(_owner) != null)
                    ex = PtfkException.GetLastOccurrence(_owner);
                Task.Run(() =>
                {
                    var msg = Tools.ToJson(new { Message = ex.Message, StackTrace = ex.StackTrace, InnerExceptionMessage = ex.InnerException?.Message }, true, false);
                    PtfkConsole.WriteLine(msg);
                    PtfkEnvironment.CurrentEnvironment.Log.Error(msg);
                });
                PtfkFormStruct form = GetErrorFormResponse(ex);
                var pcontext = new UnpredictedExceptionContext(formRequest?.GetFormStruct(), form, ex);
                InvokeAction(() => this.Events?.FormSubmitting?.OnUnpredictedExceptionThrown?.Invoke(pcontext), pcontext, false);
                return new OkObjectResult(form);
            }
        }

        private void InvokeAction(Func<Task> task, SubmitContextBase context, bool enableThrowException = true)
        {
            SetContextParent(context);
            task.Invoke()?.Wait();
            GetContextParent(context);
            if (context.InternalException != null && enableThrowException)
                throw context.InternalException;
        }

        private void SetContextParent(ContextBase context)
        {
            context.Parent = this;
            context.Owner = this.Owner;
        }
    }

    public class PtfkSubmitterEvents : SubmitContextBase
    {
        public FormSubmittingEvent FormSubmitting { get; set; }

        //public DatatableSubmittingEvent DatatableSubmitting { get; set; }

        public FileSubmittingEvent FileSubmitting { get; set; }

    }

    public class FileStageResponseContext : SubmitContextBase
    {
        public FileStageResponseContext(ArchiveRequest request)
        {
            this.Request = request;
        }
        public ArchiveRequest Request { get; internal set; }

    }

    public class ListFileStageResponseContext : FileStageResponseContext
    {
        public List<UploaderFile> Files { get; set; }
        public ListFileStageResponseContext(ArchiveRequest request, List<UploaderFile> files) : base(request)
        {
            Files = files;
        }

    }

    public class UploadFileStageResponseContext : FileStageResponseContext
    {
        public PtfkFileInfo File { get; set; }
        public UploadFileStageResponseContext(ArchiveRequest request, PtfkFileInfo file) : base(request)
        {
            File = file;
        }

    }

    public class StageResponseContext : SubmitContextBase
    {
        //public IPtfkSession GetOwner() { return base.GetOwner(); }
        public PetaframeworkStd.Commons.ProcessTask CurrentTask() { return Parent?.Entity?.CurrentWorkflow?.GetCurrentTask(); }
        public bool HasFinishedTask() { return Parent?.Entity?.CurrentWorkflow?.Finished() ?? false; }

        public PtfkFormStruct Request { get; internal set; }

    }

    public class FileSubmittingEvent
    {
        public Func<FileStageResponseContext, Task> OnPreValidateResponse { get; set; }
        public Func<ListFileStageResponseContext, Task> OnListingResponse { get; set; }
        public Func<UploadFileStageResponseContext, Task> OnUploadResponse { get; internal set; }
    }

    public class DatatableSubmittingEvent
    {

    }

    public class FormSubmittingEvent
    {
        public Func<PreValidateContext, Task> OnPreValidateResponse { get; set; }
        public Func<PredictedExceptionContext, Task> OnPredictedExceptionThrown { get; set; }
        public Func<UnpredictedExceptionContext, Task> OnUnpredictedExceptionThrown { get; set; }
    }

    public class PreValidateContext : StageResponseContext
    {
        public PreValidateContext(PtfkFormStruct request)
        {
            this.Request = request;
        }

        internal PtfkException ThrowPtfkException { get; private set; }
        public void SetException(PtfkException exception)
        {
            ThrowPtfkException = exception;
            InternalException = exception;
        }
    }

    public class PredictedExceptionContext : StageResponseContext
    {
        public PredictedExceptionContext(PtfkFormStruct request, PtfkFormStruct response, PtfkException ptfkExceptionWasThrown)
        {
            SetException(ptfkExceptionWasThrown);
            this.Request = request;
            this.Response = response;

        }

        public PtfkException PtfkExceptionWasThrown { get; private set; }
        public void SetException(PtfkException exception)
        {
            PtfkExceptionWasThrown = exception;
            InternalException = exception;
        }

        public PtfkFormStruct Response { get; internal set; } = new PtfkFormStruct();

        public void SetClientRedirectPath(String path)
        {
            this.Response.caption = path;
        }

    }

    public class UnpredictedExceptionContext : StageResponseContext
    {
        public UnpredictedExceptionContext(PtfkFormStruct request, PtfkFormStruct response, Exception exceptionWasThrown)
        {
            SetException(exceptionWasThrown);
            this.Request = request;
            this.Response = response;

        }
        public void SetException(Exception exception)
        {
            ExceptionWasThrown = exception;
            InternalException = exception;
        }

        public Exception ExceptionWasThrown { get; set; }
        public PtfkFormStruct Response { get; internal set; } = new PtfkFormStruct();

    }
}
