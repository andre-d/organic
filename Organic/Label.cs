using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic
{
    public class Label
    {
        public ushort Address { get; set; }
        public string Name { get; set; }
        public int LineNumber { get; set; }
        internal int RootLineNumber { get; set; }
    }
}
