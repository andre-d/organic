using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic.Plugins
{
    /// <summary>
    /// Used to handle custom expression evaluation
    /// </summary>
    public class HandleExpressionEventArgs : EventArgs
    {
        public HandleExpressionEventArgs(string Expression)
        {

        }

        /// <summary>
        /// The expression being parsed.  Changes you make to this are applied before vanilla evaluation continues.
        /// </summary>
        public string Expression { get; set; }
    }
}
