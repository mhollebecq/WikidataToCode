using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikidataToCode.Models
{
    public class Property
    {
        public string Id { get; }
        public string Label { get; }

        public Property(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }
}
