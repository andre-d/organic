using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Organic
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
            this.CodeType = CodeType.BasicInstruction;
            PostponedExpressions = new Dictionary<ushort, string>();
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort Address, bool Listed)
            : this(LineValue, File, LineNumber, Address)
        {
            this.Listed = Listed;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort Address, ErrorCode ErrorCode)
            : this(LineValue, File, LineNumber, Address)
        {
            this.ErrorCode = ErrorCode;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort Address, ErrorCode ErrorCode, bool Listed)
            : this(LineValue, File, LineNumber, Address, Listed)
        {
            this.ErrorCode = ErrorCode;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address)
            : this(LineValue, File, LineNumber, Address)
        {
            this.Output = Output;
        }

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address, bool Listed)
            : this(LineValue, File, LineNumber, Address)
        {
            this.Output = Output;
            this.Listed = Listed;
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

        public ListEntry(string LineValue, string File, int LineNumber, ushort[] Output, ushort Address, ErrorCode ErrorCode, bool Listed)
            : this(LineValue, File, LineNumber, Output, Address, Listed)
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
        /// The "A" value of an interpreted instruction.
        /// </summary>
        public Assembler.StringMatch ValueA;
        /// <summary>
        /// The "B" value of an interpreted instruction.
        /// </summary>
        public Assembler.StringMatch ValueB;
        /// <summary>
        /// The opcode matched.
        /// </summary>
        public Assembler.StringMatch Opcode;
        /// <summary>
        /// The type of code this line represents.
        /// </summary>
        public CodeType CodeType;
        /// <summary>
        /// Expressions that will be evaluated in the second pass.
        /// </summary>
        internal Dictionary<ushort, string> PostponedExpressions;

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
                case ErrorCode.UncoupledStatement:
                    return "Uncoupled END, ELSEIF, ELIF, or ELSE directive.";
                case ErrorCode.FileNotFound:
                    return "File not found.";
                case ErrorCode.UndefinedReference:
                    return "Undefined reference.";
                case ErrorCode.AlignToPast:
                    return "Attempted to .align to past address.";
                case ErrorCode.InvalidMacroDefintion:
                    return "Invalid macro definition.";
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
