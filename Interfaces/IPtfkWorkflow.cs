using PetaframeworkStd.Commons;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;

namespace Petaframework.Interfaces
{
    public interface IPtfkWorkflow<out T> where T : IPtfkForm
    {
        string GetWorkFlowTitle();

        ProcessTask GetCurrentTask();

        ProcessTask GetTask(string taskID);

        List<ProcessTask> ListAllTasks();

        List<ProcessTask> GetNextTasks();

        ProcessTask GetNextTask(PtfkFormStruct form);

        PtfkFormStruct GetCurrentTaskState(PtfkFormStruct form);

        ProcessTask GetBeforeTask();

        PtfkFormStruct GetBeforeTaskState(PtfkFormStruct form);

        PtfkFormStruct GetReadableState(PtfkFormStruct form);

        PtfkFormStruct GetNextTaskState(PtfkFormStruct form);

        PtfkFormStruct GetTaskState(ProcessTask task, PtfkFormStruct form);

        void OnTaskRouting(PtfkEventArgs<ProcessTask> Task);

        void GoTo(string taskID);

        void GoToEnd();

        bool Finished(long entityID = 0);

        DateTime? CreationDate();

        String CreatorID();

        List<string> GetDelegates(ProcessTask taskToGetDelegates = null);

        /// <summary>
        /// Returns workflow diagram with marked path taken
        /// </summary>
        /// <param name="hexFillColor">Task taken fill color</param>
        /// <param name="hexStrokeColor">Task taken stroke color</param>
        /// <param name="endedProcessColor">Taken path color</param>
        /// <returns>Diagram with marked path taken</returns>
        string GetCurrentDiagram(string hexFillColor = "#6495ed", string hexStrokeColor = "#fff", string endedProcessColor = "#23d160");

        /// <summary>
        /// Returns the workfow diagram
        /// </summary>
        /// <returns>Workfow diagram</returns>
        string GetDiagram();

        bool CheckPermissionOnCurrentTask(long entityID);

        List<string> GetPermissionsOnCurrentTask(ProcessTask taskToGetPermissions = null);

        List<string> GetLastTaskProfileOwner(ProcessTask t);

        bool IsAdmin();

        bool IsNotPrivateUser();

        bool HasHierarchyFlag(ProcessTask t = null);

        bool HasBusinessProcess();

        bool HasTasks();

        IEnumerable<ProcessTask> GetTraceRoute();

        ProcessTask GetCurrentTask(long entityID);

        List<string> GetInvisibleFieldsOnCurrentTask();

        bool DropLastStage();

        List<IPtfkSession> ListWorkflowUsers();

        IPtfkSession GetStartOwner();

        IPtfkSession GetLastTaskOwner();

        IPtfkSession GetLastOwner();

        System.Threading.Tasks.Task<bool> MarkAsRead();

        ProcessTask GetFirstTask();

    }
}
