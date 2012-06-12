using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic
{
    public partial class Assembler
    {
        private void LoadInternalExpressionExtensions()
        {
            // isref(label)
            ExpressionExtensions.Add("isref", (string value) =>
                {
                    if (ReferencedValues.Contains(value.ToLower()))
                        return 1;
                    return 0;
                });
        }
    }
}
