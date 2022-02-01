using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Petaframework.Interfaces;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework.POCO
{
    public class FormRequest
    {
        [JsonProperty("type")]
        public String EntityType { get; set; }
        [JsonProperty("json")]
        public String Json { get; set; }

        public bool IsEmpty()
        {
            return String.IsNullOrWhiteSpace(EntityType) && String.IsNullOrWhiteSpace(Json);
        }

        [JsonIgnore]
        internal PtfkFormStruct FormStruct { get; set; }

        public PtfkFormStruct GetFormStruct()
        {
            return FormStruct;
            //return Tools.FromJson<PtfkFormStruct>(Json);
        }
    }

    public class DatatableRequest
    {
        [JsonProperty("draw")]
        public int Draw { get; set; }

        private int? _orderIndex = null;
        private string _orderDirection = "desc";

        [JsonProperty("order[0][column]")]
        [Obsolete]
        public string OrderIdx { get; set; }

        [JsonProperty("order[0][dir]")]
        [Obsolete]
        public string OrderDir { get; set; }

        [JsonProperty("orderIndex")]
        public int OrderIndex
        {
            get
            {
                int idx = -1;
                int.TryParse(OrderIdx, out idx);
                return idx >= 0 ? idx : _orderIndex ?? 0;
            }
            set
            {
                _orderIndex = value;
            }
        }

        [JsonProperty("orderDirection")]
        public string OrderDirection
        {
            get
            {
                return !String.IsNullOrWhiteSpace(OrderDir) ? OrderDir : _orderDirection;
            }
            set
            {
                _orderDirection = value;
            }
        }

        [JsonProperty("start")]
        public int Start { get; set; }
        [JsonProperty("search[value]")]
        public string SearchValue { get; set; }
        [JsonProperty("ptfk_form[type]")]
        public string FormType { get; set; }
        [JsonProperty("ptfk_form[url]")]
        public string Url { get; set; }
        [JsonProperty("ptfk_form[method]")]
        public string Method { get; set; }
        [JsonProperty("ptfk_form[action]")]
        public string Action { get; set; }
        [JsonProperty("ptfk_form[implicitCodes]")]
        public string ImplicitCodes { get; set; }
        [JsonProperty("ptfk_form[length]")]
        public int PtfkFormLength { get; set; }
        [JsonProperty("ptfk_form[sim]")]
        public string SimulatedUser { get; set; }
    }

    public class ArchiveRequest
    {
        [JsonProperty("list")]
        public bool IsList { get; set; }
        [JsonProperty("entity")]
        public string Entity { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("p")]
        public string PropertyName { get; set; }
        [JsonProperty("sim")]
        public string SimulatedUser { get; set; }
        [JsonProperty("qquuid")]
        public string Qquuid { get; set; }
        [JsonProperty("delete")]
        public bool MarkedAsDeleted { get; set; }
        [JsonProperty("qqfile")]
        public IFormFile Attachment { get; set; }

        [JsonProperty("qqfilename")]
        public string FileName { get; set; }

        [JsonProperty("qqtotalfilesize")]
        public string Size { get; set; }
    }

    public class SubmitRequest
    {
        public SubmitRequest() { }
        internal SubmitRequest(IPtfkSession owner, HttpResponse response, Func<String, IPtfkSession> onSearchingSession)
        {
            Tools.CheckUserSimulation(owner, null, response, onSearchingSession);
            this.Owner = owner;
            this.Response = response;
        }
        public FormRequest Form { get; set; }
        public DatatableRequest Datatable { get; set; }
        public ArchiveRequest File { get; set; }
        public string Type { get; set; }
        internal IPtfkSession Owner { get; set; }
        internal IPtfkEntity Entity { get; set; }

        internal HttpResponse Response { get; set; }
    }
}
