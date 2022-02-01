using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Petaframework
{
    [Serializable]
    public class ExportableEntity
    {
        public string Title { get; set; }
        public FileInfo EntityHtmlFile { get; set; }
        public List<KeyValuePair<String, FileInfo>> Attachments { get; set; }
    }
}
