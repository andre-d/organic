using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    public class ListEntry
    {
        public ListEntry(string LineValue, string File, int LineNumber)
        {
            this.LineValue = LineValue;
            this.File = File;
            this.LineNumber = LineNumber;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ErrorCode ErrorCode)
            : this(LineValue, File, LineNumber)
        {
            this.ErrorCode = ErrorCode;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output)
            : this(LineValue, File, LineNumber)
        {
            this.Output = Output;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ErrorCode ErrorCode)
            : this(LineValue, File, LineNumber, Output)
        {
            this.Output = Output;
        }

        public string LineValue;
        public string File;
        public int LineNumber;
        public ushort[] Output;
        public ErrorCode ErrorCode;

        public static string GetFriendlyErrorMessage(ListEntry Entry)
        {
            string message = Entry.File + " (line " + Entry.LineNumber + "): Error: ";
            switch (Entry.ErrorCode)
            {
                case ErrorCode.WhitespaceInLabel:
                    message += "Whitespace is not allowed in labels.";
                    break;
                case ErrorCode.DuplicateName:
                    message += "Duplicate name.";
                    break;
                case ErrorCode.InvalidOpcode:
                    message += "Invalid opcode.";
                    break;
                case ErrorCode.InvalidParameter:
                    message += "Invalid parameter.";
                    break;
                case ErrorCode.IllegalExpression:
                    message += "Illegal expression.";
                    break;
                default:
                    message += Entry.ErrorCode.ToString() + ".";
                    break;
            }
            return message;
        }
    }
}
