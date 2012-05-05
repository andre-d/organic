using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic.Plugins
{
    public class HandleParameterEventArgs : EventArgs
    {
        public string Parameter { get; set; }
        public string[] Arguments { get; set; }
        /// <summary>
        /// Set to TRUE if the plugin handled the parameter correctly.
        /// </summary>
        public bool Handled { get; set; }
        /// <summary>
        /// Set to TRUE if Organic should quit after reading this parameter.
        /// </summary>
        public bool StopProgram { get; set; }
        public int Index { get; set; }

        public HandleParameterEventArgs(string Parameter)
        {
            this.Parameter = Parameter;
        }
    }
}
