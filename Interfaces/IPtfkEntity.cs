using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PetaframeworkStd.Commons;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Petaframework.Interfaces
{
    public interface IPtfkEntity : IEntity, IPtfkForm
    {
        long Id { get; set; }
        
        /// <summary>
        /// This is the list of items for the display as selection options
        /// </summary>
        /// <returns>List of ListItem for options</returns>
        List<ListItem> ItemsList();

        void SetBusiness();

        String Run(PtfkFormStruct formJson);

        List<Dictionary<String, object>> GetFilteredDataByFields(IEnumerable<string> fields, IEnumerable<long> onlyThisIds = null);

        String Make(PageConfig config);

        void OnFileUploaded(PtfkEventArgs<PtfkFileInfo> e);

        [JsonIgnore]
        ProcessTask CurrentProcessTask { get; }

        [JsonIgnore]
        IPtfkWorkflow<IPtfkForm> CurrentWorkflow { get; }

        bool IsAutoSave();

        [JsonIgnore]
        IPtfkSession Owner { set; get; }

        String CurrentProcessTaskID { get; }

        string GetSelectedRole();

        bool HasPermitionOnCurrentTask(long entityID);

        List<IPtfkEntityJoin> GetJoinedEntities(String propertyName, long entityToId);
        List<IPtfkEntityJoin> GetJoinedEntities(String propertyName);
        void SetJoinedEntities(List<String> toEntityIds, String propertyName);
        List<IPtfkEntityJoin> ListJoinedEntities();
        void ClearJoinedEntities(List<IPtfkEntityJoin> startList);

        PtfkGen CurrentGenerator { get; }
    }

}
