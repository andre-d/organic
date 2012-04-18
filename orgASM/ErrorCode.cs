using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    public enum ErrorCode
    {
        Success = 0,
        WhitespaceInLabel = 1,
        DuplicateName = 2,
        InvalidOpcode = 3,
        InvalidParameter = 4,
        IllegalExpression = 5
    }
}
