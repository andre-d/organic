using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM.Plugins
{
    public class AssemblyCompleteEventArgs : EventArgs
    {
        public List<ListEntry> Output { get; set; }

        public AssemblyCompleteEventArgs(List<ListEntry> Output)
        {
            this.Output = Output;
        }
    }
}
