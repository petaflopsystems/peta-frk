using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PetaframeworkStd.Commons
{
    [Serializable]
    public class File
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty("bytes", NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Bytes { get; set; }
        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public int Length { get; set; }
        [JsonProperty("extension", NullValueHandling = NullValueHandling.Ignore)]
        public String Extension { get; set; }
    }

    [Serializable]
    public class HtmlAndPdfModel
    {
        [JsonProperty("listFiles", NullValueHandling = NullValueHandling.Ignore)]
        public FileList ListPdfs { get; set; }
        [JsonProperty("listHtml", NullValueHandling = NullValueHandling.Ignore)]
        public HtmlList ListHtml { get; set; }
    }

    [Serializable]
    public class FileList
    {
        public FileList() { }

        public List<File> Files { get; set; } = new List<File>();
    }

    [Serializable]
    public class HtmlList
    {
        public String[] Htmls { get; set; }
    }
}
