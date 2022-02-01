using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Petaframework.Interfaces
{
    public interface IPtfkForm : ICloneable, IEntity
    {
        int ParentID { get; set; }

        string GetIdAttributeName();

        List<string> GetPasswordMaskAttributeNames();

        string GetLabel(string attributeName);

        string GetTooltip(string attributeName);

        string GetMask(string attributeName);

        int? GetMaxLength(string attributeName);

        string GetMirroredOf(string attributeName);

        string GetSubformEntityName(string attributeName);

        Validate GetValidate(string attributeName);

        string GetClientSideContext(string attributeName);

        List<ListItem> GetSelectOptions(string attributeName);

        string GetContext(string attributeName);

        IQueryable<IPtfkForm> GetDataItems(IPtfkForm entity);

        PtfkFilter FilterDataItems(PtfkFilter filterParam, IPtfkForm entity);

        IQueryable<IPtfkForm> ApplyFilter(
          PtfkFilter filter,
          IQueryable<IPtfkForm> source);

        PtfkFilter CurrentFilter { get; }

        List<String> OutputMessage { get; }

        List<ListItem> GetSelectOptions();

        void AddCustomMode(string propertyName, TypeDef.InputType mode);

        IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> GetInputs(
          params TypeDef.InputType[] types);

        IEnumerable<KeyValuePair<string, object>> GetImplicits();

        IEnumerable<KeyValuePair<string, object>> GetReadables(
          ReadableFieldType readableFieldType);

        IEnumerable<KeyValuePair<PropertyInfo, FormCaptionAttribute>> GetSubforms(
          params TypeDef.InputType[] types);

        void SetBusinessClass<T>(IPtfkBusiness<T> business);
        bool HasBusinessRestrictionsBySession();

        [JsonIgnore]
        [NotMapped]
        string FormLabel { get; }

        [NotMapped]
        new ILogger Logger { set; get; }

        [JsonIgnore]
        [NotMapped]
        bool IsCache { set; }

        bool GetCacheFlag();

        IPtfkSession GetOwner();
    }
}
