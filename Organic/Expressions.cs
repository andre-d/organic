using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Organic.Plugins;

namespace Organic
{
    public partial class Assembler
    {
        public delegate ushort ExpressionExtension(string value);
        public Dictionary<string, ExpressionExtension> ExpressionExtensions;

        /// <summary>
        /// Given an expression, it will parse it and return the result as a nullable ushort
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ExpressionResult ParseExpression(string value)
        {
            ExpressionResult expressionResult = new ExpressionResult();
            expressionResult.Successful = true;
            expressionResult.References = new List<string>();
            value = value.Trim();
            if (HandleExpression != null)
            {
                HandleExpressionEventArgs heea = new HandleExpressionEventArgs(value);
                HandleExpression(this, heea);
                value = heea.Expression;
            }
            if (value.Contains("("))
            {
                // Check for advanced expression handlers
                foreach (var item in ExpressionExtensions)
                {
                    if (value.StartsWith(item.Key.ToLower() + "("))
                    {
                        string expr = value.Substring(value.IndexOf("(") + 1);
                        expr = expr.Remove(expr.Length - 1);
                        expressionResult.Value = item.Value(expr);
                        return expressionResult;
                    }
                }
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
                EvaluateValueEventArgs args = new EvaluateValueEventArgs(value);
                if (EvaluateExpressionValue != null)
                {
                    EvaluateExpressionValue(this, args);
                }
                if (args.Handled)
                {
                    expressionResult.Value = args.Result;
                    return expressionResult;
                }
                else if (value.StartsWith("0d"))
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
                else if (value.StartsWith("{") && value.EndsWith("}")) // instruction literal
                {
                    string instruction = value.Substring(1, value.Length - 2);
                    Assembler subAssembler = new Assembler();
                    List<ListEntry> assembly = subAssembler.Assemble(instruction);
                    if (assembly.Count == 0)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    if (assembly[0].Output == null)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    if (assembly[0].Output.Length == 0)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    expressionResult.Value = assembly[0].Output[0];
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
                    int nextLabelIndex = -1;
                    // Find the next relative label after the current line
                    for (int i = 0; i < RelativeLabels.Count; i++)
                    {
                        if (RelativeLabels.ElementAt(i).Key > LineNumbers.Peek())
                        {
                            nextLabelIndex = i;
                            break;
                        }
                    }
                    if (nextLabelIndex == -1)
                        nextLabelIndex = RelativeLabels.Count - 1; // If no such label is found, use the last one
                    bool initialPlus = true;
                    // For each plus, increment that label index, and each minus decrements it
                    foreach (char c in value)
                    {
                        if (c == '+') // The intial plus symbol is ignored
                        {
                            if (initialPlus)
                                nextLabelIndex++;
                            initialPlus = false;
                        }
                        else if (c == '-')
                            nextLabelIndex--;
                    }
                    if (nextLabelIndex < 0)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    if (nextLabelIndex > RelativeLabels.Count - 1)
                    {
                        expressionResult.Successful = false;
                        return expressionResult;
                    }
                    expressionResult.Value = RelativeLabels.ElementAt(nextLabelIndex).Value;
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
                    if (Values.ContainsKey(value.ToLower()))
                        expressionResult.Value = Values[value.ToLower()];
                    else if (LabelValues.ContainsKey(value.ToLower()))
                    {
                        expressionResult.Relocate = true;
                        expressionResult.Value = LabelValues.GetValue(value.ToLower());
                    }
                    else
                        expressionResult.Successful = false;
                    expressionResult.References.Add(value.ToLower());
                    if (!ReferencedValues.Contains(value.ToLower()))
                        ReferencedValues.Add(value.ToLower());
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
                expressionResult.Value = (ushort)-expressionResult.Value;
                return expressionResult;
            }
            if (operands == null)
            {
                expressionResult.Successful = false;
                return expressionResult;
            }
            ExpressionResult left = ParseExpression(operands[0]);
            ExpressionResult right = ParseExpression(operands[2]);
            expressionResult.References.AddRange(left.References.Concat(right.References));
            if ((!left.Successful || !right.Successful) && operands[1] != "===" && operands[1] != "!==")
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
                case "<>":
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
                case "===":
                    expressionResult.Value = (ushort)(operands[0].ToLower().Trim() == operands[2].ToLower().Trim() ? 1 : 0);
                    break;
                case "!==":
                    expressionResult.Value = (ushort)(operands[0].ToLower().Trim() != operands[2].ToLower().Trim() ? 1 : 0);
                    break;
                case "&&":
                    expressionResult.Value = (ushort)(left.Value > 0 && right.Value > 1 ? 1 : 0);
                    break;
                case "||":
                    expressionResult.Value = (ushort)(left.Value > 0 || right.Value > 1 ? 1 : 0);
                    break;
                case "^^":
                    expressionResult.Value = (ushort)(left.Value > 0 ^ right.Value > 1 ? 1 : 0); // between boolean operators, ^ is ^^ in C#
                    break;
                default:
                    expressionResult.Successful = false;
                    return expressionResult;
            }
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

        private List<CustomExpressionOperator> CustomOperators;

        string[] MathOperators = new string[] { "*", "/", "+", "-", "<<", ">>", "||", "&&", "^^", "|", "^", "&", "%", "===", "!==", "==", "!=", "<>", ">", "<", ">=", "<=" };

        private string[] GetOperands(string value)
        {
            int firstIndex = int.MaxValue;
            string firstOperator = "";
            foreach (string s in MathOperators)
            {
                if (!value.Contains(s))
                    continue;
                bool instring = false, inchar = false, ininstruction = false;
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
                    if (value[i] == '"' && !inchar && !ininstruction)
                        instring = !instring;
                    if (value[i] == '\'' && !instring && !ininstruction)
                        inchar = !inchar;
                    if (value[i] == '{' && !instring && !inchar)
                        ininstruction = true;
                    if (value[i] == '}' && !instring && !inchar)
                        ininstruction = false;
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
        /// The original expression
        /// </summary>
        public string Expression { get; set; }
        /// <summary>
        /// All values referenced by name in the expression.
        /// </summary>
        public List<string> References { get; set; }

        public bool Relocate { get; set; }
    }
}
