using Newtonsoft.Json;
using Petaframework.Interfaces;
using System;
using System.Linq;

namespace Petaframework
{
    public class PtfkFilter
    {
        /// <summary>
        /// The type of Log class
        /// </summary>
        internal IPtfkLog LogType { get; set; }
        /// <summary>
        /// Flag indicates 
        /// </summary>
        internal PetaframeworkStd.Interfaces.IPtfkSession Session { get; set; }
        
        public int PageSize { get; set; } = 10;
        public int PageIndex { get; set; } = 0;
        public String FilteredValue { get; set; }
        public int OrderByColumnIndex { get; set; } = 0;
        public bool OrderByAscending { get; set; } = true;
        public String[] FilteredProperties { get; set; }
        internal String SqlGenerated { get; set; }
        public bool RestrictedBySession { get; set; } = false;

        public PtfkFilterResult Result { get; private set; }
        [JsonIgnore]
        public bool Applied { get; internal set; }

        [JsonIgnore]
        internal bool StopAfterFirstHtml { get; set; } = false;

        [JsonIgnore]
        internal DateTime PreFilterDatetime { get; set; }

        public void SetResult(IQueryable<IPtfkForm> filterResult, int totalCount)
        {
            this.Result = new PtfkFilterResult
            {
                Items = filterResult,
                TotalCount = totalCount
            };
        }

    }

    public class PtfkFilterResult
    {
        /// <summary>
        /// Resulting query with the filter and informed pagination
        /// </summary>
        public IQueryable<IPtfkForm> Items { get; set; }
        /// <summary>
        /// Total number of items in the database with the informed filter
        /// </summary>
        public int TotalCount { get; set; }
    }
}
