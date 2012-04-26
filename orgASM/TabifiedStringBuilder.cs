using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    /// <summary>
    /// Used to build strings with different portions always appearing at the same index
    /// </summary>
    public class TabifiedStringBuilder
    {
        /// <summary>
        /// The working value of the string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Creates a TabifiedStringBuilder with an empty string
        /// </summary>
        public TabifiedStringBuilder()
        {
            Value = "";
        }

        /// <summary>
        /// Writes the value at the given index, and pads it with spaces.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="content"></param>
        public void WriteAt(int position, string content)
        {
            while (Value.Length < position)
                Value += " ";
            Value += content;
        }
    }
}
