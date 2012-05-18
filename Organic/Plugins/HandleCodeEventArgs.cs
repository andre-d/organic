using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic.Plugins
{
    public class HandleCodeEventArgs
    {
        public string Code { get; set; }
        public bool Handled { get; set; }
        public ListEntry Output { get; set; }
    }
}
