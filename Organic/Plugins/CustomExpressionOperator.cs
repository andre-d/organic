using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic.Plugins
{
    public class CustomExpressionOperator
    {
        public string Operator { get; set; }
        public delegate ushort Handler(ushort op1, ushort op2);
    }
}
