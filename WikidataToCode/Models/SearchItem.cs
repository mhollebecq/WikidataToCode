using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikidataToCode.Models
{
    public class SearchItem
    {
        public SearchItem(string id, string label, string description)
        {
            Id = id;
            Label = label;
            Description = description;
        }

        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
    }
}
