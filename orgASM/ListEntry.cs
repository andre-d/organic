using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
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

        public string Code;
        public string FileName;
        public int LineNumber;
        public ushort[] Output;
        public ErrorCode ErrorCode;
        public WarningCode WarningCode;
        public bool Listed;
        public ushort Address;

        public static string GetFriendlyErrorMessage(ListEntry Entry)
        {
            switch (Entry.ErrorCode)
            {
                case ErrorCode.WhitespaceInLabel:
                    return "Whitespace is not allowed in labels.";
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
                    return Entry.ErrorCode.ToString() + ".";
            }
        }

        public static string GetFriendlyWarningMessage(ListEntry Entry)
        {
            switch (Entry.WarningCode)
            {
                case WarningCode.RedundantStatement:
                    return "Redundant statement.";
                case WarningCode.AssignToLiteral:
                    return "Attempted to assign to a literal.";
                default:
                    return Entry.WarningCode.ToString() + ".";
            }
        }
    }
}
