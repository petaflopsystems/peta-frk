using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;

namespace Petaframework
{
    public class PageConfig
    {
        public String PageType { get; set; }
        public Boolean ServerSideSearchMode { get; set; }
        public IPtfkSession Owner { get; set; }
        public string Action { get; set; }

        public PtfkFormStruct CurrDForm { get; private set; }

        public List<String> Inconsistencies { get; internal set; } = new List<string>();

        internal PageConfig(IPtfkSession session)
        {
            Owner = session;
        }

        public PageConfig(String pageType, string jsonRequest, IPtfkSession owner) : this(pageType, Tools.GetPtfkFormStruct(jsonRequest, owner), owner)
        {

        }
        internal bool SkipCache = false;
        public PageConfig(String pageType, PtfkFormStruct jsonRequest, IPtfkSession owner)
        {
            this.CurrDForm = jsonRequest;
            if (jsonRequest?.method?.ToLower() == Constants.FormMethod.AutoSave)
                this.CurrDForm.AutoSave = true;
            this.PageType = pageType;
            this.ServerSideSearchMode = CurrDForm.serverSideSearch;
            this.Owner = owner;
            if (this.CurrDForm.method != null && this.CurrDForm.method.ToLower().Equals(Constants.FormMethod.Delete))
                CurrDForm.action = TypeDef.Action.DELETE.ToString();
            if (String.IsNullOrWhiteSpace(CurrDForm.action))
                this.Action = String.Empty;
            else
                this.Action = CurrDForm.action;
        }
    }
}
