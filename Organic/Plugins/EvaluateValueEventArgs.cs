using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic.Plugins
{
    public class EvaluateValueEventArgs : EventArgs
    {
        public EvaluateValueEventArgs(string Value)
        {
            this.Value = Value;
        }

        public string Value { get; set; }
        public ushort Result { get; set; }
        public bool Handled { get; set; }
    }
}
