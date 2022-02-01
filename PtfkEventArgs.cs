using Newtonsoft.Json;
using Petaframework.Interfaces;
using PetaframeworkStd.Commons;
using System;

namespace Petaframework
{
    public class PtfkEntityEventArgs<T> : EventArgs, ICloneable where T: IPtfkForm
    {
        public T Entity { get; private set; }
        public ProcessTask CurrentTask { get; private set; }
        [JsonIgnore]
        public IPtfkWorkflow<T> CurrentWorkflow;
        [JsonConstructor]
        public PtfkEntityEventArgs(ref T Entity)
        {
            this.Entity = Entity;
        }
        public PtfkEntityEventArgs(T Entity, ProcessTask currentTask, IPtfkWorkflow<T> workflow)
        {
            this.Entity = Entity;
            this.CurrentTask = currentTask;
            this.CurrentWorkflow = workflow;
        }

        public object Clone()
        {
            var json = Tools.ToJson(this);
            var ret =  Tools.FromJson<PtfkEntityEventArgs<T>>(json);
            ret.CurrentWorkflow = this.CurrentWorkflow;
            return ret;
        }

        public PtfkEntityEventArgs<T> Copy()
        {
            return Clone() as PtfkEntityEventArgs<T>;
        }
    }

    public class PtfkEventArgs<T> : EventArgs, ICloneable 
    {
        public T Element { get; private set; }
        public ProcessTask CurrentTask { get; private set; }
        [JsonConstructor]
        public PtfkEventArgs(ref T Entity)
        {
            this.Element = Entity;
        }


        public object Clone()
        {
            var json = Tools.ToJson(this);
            var ret = Tools.FromJson<PtfkEventArgs<T>>(json);
            return ret;
        }

        public PtfkEventArgs<T> Copy()
        {
            return Clone() as PtfkEventArgs<T>;
        }
    }
}
