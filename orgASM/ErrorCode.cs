using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    public enum ErrorCode
    {
        Success = 0,
        InvalidLabel = 1,
        DuplicateName = 2,
        InvalidOpcode = 3,
        InvalidParameter = 4,
        IllegalExpression = 5,
        InvalidDirective = 6,
        InsufficientParamters = 7,
        TooManyParamters = 8,
        UncoupledEnd = 9
    }

    public enum WarningCode
    {
        None = 0,
        RedundantStatement = 1,
        AssignToLiteral = 2
    }
}
