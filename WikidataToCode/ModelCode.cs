using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikidataToCode.Models;

namespace WikidataToCode
{
    partial class Model
    {
        private readonly InstanceOfItem instanceOfItem;
        private readonly IEnumerable<Property> properties;

        public Model(InstanceOfItem instanceOfItem, IEnumerable<Property> properties)
        {
            this.instanceOfItem = instanceOfItem;
            this.properties = properties;
        }

        private string ToClassName(string baseString)
        {
            var split = baseString.Replace("'", "")
                                  .Replace("(", "_")
                                  .Replace(")", "_")
                                  .Replace("/", "_")
                                  .Replace("-", "_")
                                  .Split(' ', ',');

            return string.Join("", split.Select(s => FirstLetterUpper(s)));
        }

        private string FirstLetterUpper(string baseString)
        {
            StringBuilder sb = new StringBuilder();
            CharEnumerator enumerator = baseString.GetEnumerator();
            if (enumerator.MoveNext())
                sb.Append(char.ToUpperInvariant(enumerator.Current));

            while (enumerator.MoveNext())
                sb.Append(enumerator.Current);

            return sb.ToString();
        }
    }
}
