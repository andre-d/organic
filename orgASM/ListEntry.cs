using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    /// <summary>
    /// Represents an entry in a listing output from assembling code.
    /// </summary>
    public class ListEntry
    {
        public ListEntry(string LineValue, string File, int LineNumber, ushort Address)
        {
            this.Code = LineValue;
            this.FileName = File;
            this.LineNumber = LineNumber;
            this.Listed = true;
            this.Address = Address;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort Address, ErrorCode ErrorCode)
            : this(LineValue, File, LineNumber, Address)
        {
            this.ErrorCode = ErrorCode;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address)
            : this(LineValue, File, LineNumber, Address)
        {
            this.Output = Output;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address, bool Listed, WarningCode WarningCode)
            : this(LineValue, File, LineNumber, Address)
        {
            this.Output = Output;
            this.Listed = Listed;
            this.WarningCode = WarningCode;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address, ErrorCode ErrorCode)
            : this(LineValue, File, LineNumber, Output, Address)
        {
            this.Output = Output;
            this.ErrorCode = ErrorCode;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address, WarningCode WarningCode)
            : this(LineValue, File, LineNumber, Output, Address)
        {
            this.Output = Output;
            this.WarningCode = WarningCode;
        }

        /// <summary>
        /// The trimmed and uncommented code that was parsed.
        /// </summary>
        public string Code;
        /// <summary>
        /// The name of the file the code is contained in.
        /// </summary>
        public string FileName;
        /// <summary>
        /// The line number in the file the code is contained in.
        /// </summary>
        public int LineNumber;
        /// <summary>
        /// The global line number of this code when #include adds it to the file.
        /// </summary>
        public int RootLineNumber;
        /// <summary>
        /// The assembled output.
        /// </summary>
        public ushort[] Output;
        /// <summary>
        /// The error produced by assembling this code.
        /// </summary>
        public ErrorCode ErrorCode;
        /// <summary>
        /// The warning produced by assembling this code.
        /// </summary>
        public WarningCode WarningCode;
        /// <summary>
        /// True if this code is listed, false if not.  This changes with #nolist and #list.
        /// </summary>
        public bool Listed;
        /// <summary>
        /// The address this code is located at.
        /// </summary>
        public ushort Address;
        /// <summary>
        /// A list of referenced values this code uses, such as label names and equates.
        /// </summary>
        public string[] References;

        /// <summary>
        /// Given an error code, this returns a user-friendly message.
        /// </summary>
        /// <param name="Entry"></param>
        /// <returns></returns>
        public static string GetFriendlyErrorMessage(ErrorCode Entry)
        {
            switch (Entry)
            {
                case ErrorCode.InvalidLabel:
                    return "Invalid label name.";
                case ErrorCode.DuplicateName:
                    return "Duplicate name.";
                case ErrorCode.InvalidOpcode:
                    return "Invalid opcode.";
                case ErrorCode.InvalidParameter:
                    return "Invalid parameter.";
                case ErrorCode.IllegalExpression:
                    return "Illegal expression.";
                case ErrorCode.InvalidDirective:
                    return "Invalid preprocessor directive.";
                case ErrorCode.InsufficientParamters:
                    return "Insufficient parameters.";
                case ErrorCode.TooManyParamters:
                    return "Too many parameters.";
                case ErrorCode.UncoupledEnd:
                    return "Uncoupled END directive.";
                default:
                    return Entry.ToString() + ".";
            }
        }

        /// <summary>
        /// Given a warningcode, this returns a user-friendly message.
        /// </summary>
        /// <param name="Entry"></param>
        /// <returns></returns>
        public static string GetFriendlyWarningMessage(WarningCode Entry)
        {
            switch (Entry)
            {
                case WarningCode.RedundantStatement:
                    return "Redundant statement.";
                case WarningCode.AssignToLiteral:
                    return "Attempted to assign to a literal.";
                default:
                    return Entry.ToString() + ".";
            }
        }
    }
}
