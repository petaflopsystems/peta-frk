using Microsoft.AspNetCore.Mvc;
using Petaframework.Interfaces;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework.Portal
{
    public abstract class PortalBase
    {
        internal bool ExportAsBase64 { get; set; } = false;
        public Func<String, IPtfkSession> OnSearchingSession { get; protected set; }
        public IPtfkEntity Entity { get; protected set; }

        public IPtfkSession Owner { get; set; }

        public IPtfkSession GetOwner()
        {
            if (Entity == null || Entity.Owner == null)
                return Owner.Current;
            return Entity.Owner.Current;
        }

        protected void GetContextParent(ContextBase context)
        {
            this.ExportAsBase64 = context.ExportAsBase64;
        }
    }

    public abstract class ContextBase
    {
        public bool ExportAsBase64 { internal get; set; } = false;
        internal IPortalBase Parent { get; set; }
        public IPtfkEntity GetEntity()
        {
            return Parent.Entity;
        }
        public IPtfkSession Owner { get; set; }

        public IPtfkSession GetOwner()
        {
            if (Parent.Entity == null || Parent.Entity.Owner == null)
                return Owner.Current;
            return Parent.Entity.Owner.Current;
        }

        public async Task<ActionResult> ToActionResultAsync()
        {
            return await Task.Factory.StartNew(() => this.Parent.ToActionResult());
        }

        public ActionResult ToActionResult()
        {
            return this.Parent.ToActionResult();
        }
    }

    public interface IPortalBase
    {
        IPtfkEntity Entity { get; }
        IPtfkSession GetOwner();
        ActionResult ToActionResult();
        Func<String, IPtfkSession> OnSearchingSession { get; }
    }

    public interface IContextBase
    {
        IPtfkSession GetOwner();
        PetaframeworkStd.Commons.ProcessTask CurrentTask();

        bool HasFinishedTask();

    }

    public class SubmitContextBase : ContextBase
    {
        internal Exception InternalException { get; set; } = null;
    }

    internal interface ISubmitContext : IContextBase
    {
        PtfkFormStruct ResponseFormStruct();
    }

    internal interface IInvoke
    {
        void Invoke(Task t);
    }


    public class ReportContextBase : ContextBase
    {
        public PetaframeworkStd.Commons.ProcessTask CurrentTask() { return Parent?.Entity?.CurrentWorkflow?.GetCurrentTask(); }
        public bool HasFinishedTask() { return Parent?.Entity?.CurrentWorkflow?.Finished() ?? false; }
    }

    internal interface IReportContext : IContextBase
    {
        object ResponseObject();
    }
}

