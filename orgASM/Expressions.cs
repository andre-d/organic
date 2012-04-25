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
        public ExpressionResult ParseExpression(string value)
        {
            return ParseExpression(value, false);
        }

        /// <summary>
        /// Given an expression, it will parse it and return the result as a nullable ushort
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ExpressionResult ParseExpression(string value, bool followReferences)
        {
            ExpressionResult expressionResult = new ExpressionResult();
            expressionResult.Successful = true;
            expressionResult.References = new string[0];
            value = value.Trim();
            if (value.Contains("("))
            {
                return EvaluateParenthesis(value);
            }
            if (value.StartsWith("~"))
            {
                expressionResult = ParseExpression(value.Substring(1));
                if (expressionResult.Successful)
                    expressionResult.Value = (ushort)~expressionResult.Value;
                return expressionResult;
            }
            if (!HasOperators(value))
            {
                // Parse value
                ushort result;
                if (value.StartsWith("0d"))
                    value = value.Substring(2);
                if (value.StartsWith("'")) // Character
                {
                    if (value.Length < 3)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    value = value.Substring(1, value.Length - 2).Unescape();
                    if (value == null)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    if (value.Length != 1)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    expressionResult.Value = (ushort)Encoding.ASCII.GetBytes(value)[0];
                    return expressionResult;
                }
                else if (value.StartsWith("0x")) // Hex
                {
                    value = value.Substring(2);
                    if (!ushort.TryParse(value.Replace("_", ""), NumberStyles.HexNumber, null, out result))
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    else
                    {
                        expressionResult.Value = result;
                        return expressionResult;
                    }
                }
                else if (value.StartsWith("0b")) // Binary
                {
                    value = value.Substring(2);
                    ushort? binResult = ParseBinary(value.Replace("_", ""));
                    if (binResult == null)
                        expressionResult.Successful = false;
                    else
                        expressionResult.Value = binResult.Value;
                    return expressionResult;
                }
                else if (value.StartsWith("0o"))
                {
                    value = value.Substring(2);
                    try
                    {
                        expressionResult.Value = Convert.ToUInt16(value.Replace("_", ""), 8);
                        return expressionResult;
                    }
                    catch
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                }
                else if (ushort.TryParse(value.Replace("_", ""), out result)) // Decimal
                {
                    expressionResult.Value = result;
                    return expressionResult;
                }
                else if (value == "$")
                {
                    if (LineNumbers.Count == 0)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    expressionResult.Value = currentAddress;
                    return expressionResult;
                }
                else if (value.StartsWith("$")) // Relative label
                {
                    if (LineNumbers.Count == 0)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    value = value.Substring(1);
                    int currentIndex = -1;
                    for (int i = 0; i < RelativeLabels.Count; i++)
                    {
                        if (RelativeLabels.ElementAt(i).Key > LineNumbers.Peek())
                        {
                            currentIndex = i - 1;
                            break;
                        }
                    }
                    if (currentIndex == -1)
                        currentIndex = RelativeLabels.Count;
                    foreach (char c in value)
                    {
                        if (c == '+')
                            currentIndex++;
                        else if (c == '-')
                            currentIndex--;
                    }
                    if (currentIndex < 0)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    if (currentIndex > RelativeLabels.Count)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    expressionResult.Value = RelativeLabels.ElementAt(currentIndex).Value;
                    return expressionResult;
                }
                else if (value.ToLower() == "true")
                {
                    expressionResult.Value = 1;
                    return expressionResult;
                }
                else if (value.ToLower() == "false")
                {
                    expressionResult.Value = 0;
                    return expressionResult;
                }
                else // Defined value or error
                {
                    if (followReferences)
                    {
                        if (Values.ContainsKey(value.ToLower()))
                        {
                            expressionResult.Value = Values[value.ToLower()];
                        }
                        else
                        {
                            expressionResult.Successful = false;
                        }
                    }
                    else
                    {
                        expressionResult.References = expressionResult.References.Concat(
                            new string[] { value.ToLower() }).ToArray();
                    }
                    return expressionResult;
                }
            }

            // Parse expression
            string[] operands = GetOperands(value);
            if (operands == null)
            {
                expressionResult.Successful = false;
                return expressionResult;
            }
            if (string.IsNullOrEmpty(operands[0]) && operands[1] == "-")
            {
                expressionResult = ParseExpression(operands[2]);
                return expressionResult;
            }
            if (operands == null)
            {
                expressionResult.Successful = false;
                return expressionResult;
            }
            ExpressionResult left = ParseExpression(operands[0], followReferences);
            ExpressionResult right = ParseExpression(operands[2], followReferences);
            if (!left.Successful || !right.Successful)
            {
                expressionResult.Successful = false;
                return expressionResult;
            }
            switch (operands[1])
            {
                case "*":
                    expressionResult.Value = (ushort)(left.Value * right.Value);
                    break;
                case "/":
                    expressionResult.Value =  (ushort)(left.Value / right.Value);
                    break;
                case "+":
                    expressionResult.Value =  (ushort)(left.Value + right.Value);
                    break;
                case "-":
                    expressionResult.Value =  (ushort)(left.Value - right.Value);
                    break;
                case "<<":
                    expressionResult.Value =  (ushort)(left.Value << right.Value);
                    break;
                case ">>":
                    expressionResult.Value =  (ushort)(left.Value >> right.Value);
                    break;
                case "|":
                    expressionResult.Value =  (ushort)(left.Value | right.Value);
                    break;
                case "^":
                    expressionResult.Value =  (ushort)(left.Value ^ right.Value);
                    break;
                case "&":
                    expressionResult.Value =  (ushort)(left.Value & right.Value);
                    break;
                case "%":
                    expressionResult.Value =  (ushort)(left.Value % right.Value);
                    break;
                case "==":
                    expressionResult.Value =  (ushort)(left.Value == right.Value ? 1 : 0);
                    break;
                case "!=":
                    expressionResult.Value =  (ushort)(left.Value != right.Value ? 1 : 0);
                    break;
                case "<":
                    expressionResult.Value =  (ushort)(left.Value < right.Value ? 1 : 0);
                    break;
                case ">":
                    expressionResult.Value =  (ushort)(left.Value > right.Value ? 1 : 0);
                    break;
                case "<=":
                    expressionResult.Value =  (ushort)(left.Value <= right.Value ? 1 : 0);
                    break;
                case ">=":
                    expressionResult.Value =  (ushort)(left.Value >= right.Value ? 1 : 0);
                    break;
                default:
                    expressionResult.Successful = false;
                    return expressionResult;
            }
            expressionResult.References = expressionResult.References
                .Concat(left.References).Concat(right.References).ToArray();
            return expressionResult;
        }

        private ExpressionResult EvaluateParenthesis(string value)
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
                {
                    var expressionResult = new ExpressionResult();
                    expressionResult.Successful = false;
                    return expressionResult;
                }
                ExpressionResult subExpression = ParseExpression(value.Substring(openingParenthesis + 1, closingParenthesis - (openingParenthesis + 1)));
                if (!subExpression.Successful)
                    return subExpression;
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
                bool instring = false, inchar = false, inrelative = false;
                int index = 0;
                foreach (char c in value)
                {
                    if (c == s[index] && !instring && !inchar && !inrelative)
                    {
                        if (index == s.Length - 1)
                            return true;
                        index++;
                    }
                    else
                        index = 0;
                    if (inrelative && !(c == '+' || c == '-'))
                        inrelative = false;
                    if (c == '$' && !instring && !inchar)
                        inrelative = true;
                    if (c == '"')
                        instring = !instring;
                    if (c == '\'')
                        inchar = !inchar;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// The result of evaluating an expression
    /// </summary>
    public class ExpressionResult
    {
        /// <summary>
        /// The result of the operation
        /// </summary>
        public ushort Value { get; set; }
        /// <summary>
        /// True if there were no errors
        /// </summary>
        public bool Successful { get; set; }
        /// <summary>
        /// A list of referenced values
        /// </summary>
        public string[] References { get; set; }
        /// <summary>
        /// The original expression
        /// </summary>
        public string Expression { get; set; }
    }
}
