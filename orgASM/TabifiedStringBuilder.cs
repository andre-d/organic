using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    /// <summary>
    /// Used to build strings with different portions always appearing at the same location
    /// </summary>
    public class TabifiedStringBuilder
    {
        public string Value { get; set; }

        public TabifiedStringBuilder()
        {
            Value = "";
        }

        public void WriteAt(string content, int position)
        {
            while (Value.Length < position)
                Value += " ";
            Value += content;
        }
    }
}
