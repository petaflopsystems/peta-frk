using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace Petaframework
{
    public class ListItem : SelectListItem
    {
        public String Description { get; set; }

        public ListItem(String Text, String Value) : base(Text, Value) { }
    }
}
