using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    /// <summary>
    /// Represents all possible errors that could affect a line of code.
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// No error.
        /// </summary>
        Success = 0,
        /// <summary>
        /// The specified label has invalid characters.
        /// </summary>
        InvalidLabel = 1,
        /// <summary>
        /// The specified value already exists.
        /// </summary>
        DuplicateName = 2,
        /// <summary>
        /// The opcode used is invalid, such as NOTVALID A, B
        /// </summary>
        InvalidOpcode = 3,
        /// <summary>
        /// The parameter specified is invalid.
        /// </summary>
        InvalidParameter = 4,
        /// <summary>
        /// .orgASM was unable to parse the given expression.
        /// </summary>
        IllegalExpression = 5,
        /// <summary>
        /// The specified preprocessor directive was invalid.
        /// </summary>
        InvalidDirective = 6,
        /// <summary>
        /// There were not enough parameters to complete the requested operation.
        /// For example, "#include" or "SET A" would cause this.
        /// </summary>
        InsufficientParamters = 7,
        /// <summary>
        /// There were too many parameters provided for the requested operation.
        /// </summary>
        TooManyParamters = 8,
        /// <summary>
        /// A #end statement does not have a matching #if, #ifdef, or #ifndef statement.
        /// </summary>
        UncoupledEnd = 9,
        /// <summary>
        /// A file was referenced in code that was not found on the disk.
        /// </summary>
        FileNotFound = 10,
        /// <summary>
        /// A referenced label or equate was not defined.
        /// </summary>
        UndefinedReference = 11,
    }

    /// <summary>
    /// Represents all possible warnings that could affect a line of code.
    /// </summary>
    public enum WarningCode
    {
        /// <summary>
        /// No warning.
        /// </summary>
        None = 0,
        /// <summary>
        /// The assembled statement, such as "SET A, A", was redundant.
        /// </summary>
        RedundantStatement = 1,
        /// <summary>
        /// The code attempted to assign a value to a literal, which fails silently on DCPU.
        /// Example: "SET 5, A"
        /// </summary>
        AssignToLiteral = 2
    }
}
