using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace orgASM
{
    public partial class Assembler
    {
        /// <summary>
        /// Given an expression, it will parse it and return the result as a nullable ushort
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ushort? ParseExpression(string value)
        {
            value = value.Trim();
            if (value.Contains("("))
                return EvaluateParenthesis(value);
            if (value.StartsWith("~"))
                return (ushort)(~ParseExpression(value.Substring(1)));
            if (!HasOperators(value))
            {
                // Parse value
                ushort result;
                if (value.StartsWith("0d"))
                    value = value.Substring(2);
                if (value.StartsWith("'")) // Character
                {
                    if (value.Length < 3)
                        return null;
                    value = value.Substring(1, value.Length - 2).Unescape();
                    if (value == null)
                        return null;
                    if (value.Length != 1)
                        return null;
                    return Encoding.ASCII.GetBytes(value)[0];
                }
                else if (value.StartsWith("0x")) // Hex
                {
                    value = value.Substring(2);
                    if (!ushort.TryParse(value.Replace("_", ""), NumberStyles.HexNumber, null, out result))
                        return null;
                    else
                    {
                        return result;
                    }
                }
                else if (value.StartsWith("0b")) // Binary
                {
                    value = value.Substring(2);
                    return ParseBinary(value.Replace("_", ""));
                }
                else if (value.StartsWith("0o"))
                {
                    value = value.Substring(2);
                    try
                    {
                        return Convert.ToUInt16(value.Replace("_", ""), 8);
                    }
                    catch { return null; }
                }
                else if (ushort.TryParse(value.Replace("_", ""), out result)) // Decimal
                {
                    return result;
                }
                else if (value == "$")
                {
                    return currentAddress;
                }
                else // Defined value or error
                {
                    if (Values.ContainsKey(value.ToLower()))
                    {
                        return Values[value.ToLower()];
                    }
                    else
                        return null;
                }
            }

            // Parse expression
            string[] operands = GetOperands(value);
            if (string.IsNullOrEmpty(operands[0]) && operands[1] == "-")
                return (ushort)-ParseExpression(operands[2]);
            if (operands == null)
                return null;
            ushort? left = ParseExpression(operands[0]);
            ushort? right = ParseExpression(operands[2]);
            if (left == null || right == null)
                return null;
            switch (operands[1])
            {
                case "*":
                    return (ushort)(left.Value * right.Value);
                case "/":
                    return (ushort)(left.Value / right.Value);
                case "+":
                    return (ushort)(left.Value + right.Value);
                case "-":
                    return (ushort)(left.Value - right.Value);
                case "<<":
                    return (ushort)(left.Value << right.Value);
                case ">>":
                    return (ushort)(left.Value >> right.Value);
                case "|":
                    return (ushort)(left.Value | right.Value);
                case "^":
                    return (ushort)(left.Value ^ right.Value);
                case "&":
                    return (ushort)(left.Value & right.Value);
                case "%":
                    return (ushort)(left.Value % right.Value);
                case "==":
                    return (ushort)(left.Value == right.Value ? 1 : 0);
                case "!=":
                    return (ushort)(left.Value != right.Value ? 1 : 0);
                case "<":
                    return (ushort)(left.Value < right.Value ? 1 : 0);
                case ">":
                    return (ushort)(left.Value > right.Value ? 1 : 0);
                case "<=":
                    return (ushort)(left.Value <= right.Value ? 1 : 0);
                case ">=":
                    return (ushort)(left.Value >= right.Value ? 1 : 0);
                default:
                    return null;
            }
        }

        private ushort? EvaluateParenthesis(string value)
        {
            while (value.Contains("("))
            {
                int openingParenthesis = -1, closingParenthesis = -1, parenCount = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == '(')
                    {
                        parenCount++;
                        if (parenCount == 1)
                            openingParenthesis = i;
                    }
                    if (value[i] == ')')
                    {
                        parenCount--;
                        if (parenCount == 0)
                        {
                            closingParenthesis = i;
                            break;
                        }
                    }
                }
                if (openingParenthesis == -1 || closingParenthesis == -1)
                    return null;
                ushort? subExpression = ParseExpression(value.Substring(openingParenthesis + 1, closingParenthesis - (openingParenthesis + 1)));
                if (subExpression == null)
                    return null;
                value = value.Remove(openingParenthesis) + subExpression.Value.ToString() + value.Substring(closingParenthesis + 1);
            }
            return ParseExpression(value);
        }

        private ushort? ParseBinary(string value)
        {
            ushort mask = 1;
            ushort result = 0;
            foreach (char c in value)
            {
                if (c == '1')
                    result |= mask;
                else if (c == '0') { }
                else
                    return null;
                mask <<= 1;
            }
            return result;
        }

        string[] MathOperators = new string[] { "*", "/", "+", "-", "<<", ">>", "|", "^", "&", "%", "==", "!=", ">", "<", ">=", "<=" };

        private string[] GetOperands(string value)
        {
            int firstIndex = int.MaxValue;
            string firstOperator = "";
            foreach (string s in MathOperators)
            {
                if (!value.Contains(s))
                    continue;
                bool instring = false, inchar = false;
                int index = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == s[index] && !instring && !inchar)
                    {
                        if (index == s.Length - 1)
                        {
                            // Split string
                            if (i < firstIndex && firstOperator.Length < s.Length)
                            {
                                firstIndex = i;
                                firstOperator = s;
                            }
                            continue;
                        }
                        index++;
                    }
                    else
                        index = 0;
                    if (value[i] == '"')
                        instring = !instring;
                    if (value[i] == '\'')
                        inchar = !inchar;
                }
            }
            if (firstIndex != int.MaxValue)
            {
                return new string[]
                {
                    value.Remove(firstIndex - (firstOperator.Length - 1)),
                    firstOperator,
                    value.Substring(firstIndex + 1)
                };
            }
            return null;
        }

        private bool HasOperators(string value)
        {
            foreach (string s in MathOperators)
            {
                if (!value.Contains(s))
                    continue;
                bool instring = false, inchar = false;
                int index = 0;
                foreach (char c in value)
                {
                    if (c == s[index] && !instring && !inchar)
                    {
                        if (index == s.Length - 1)
                            return true;
                        index++;
                    }
                    else
                        index = 0;
                    if (c == '"')
                        instring = !instring;
                    if (c == '\'')
                        inchar = !inchar;
                }
            }
            return false;
        }
    }
}
