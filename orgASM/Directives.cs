using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    public partial class Assembler
    {
        #region Preprocessor Directives

        private void ParseDirectives(List<ListEntry> output, string line)
        {
            string directive = line.Substring(1);
            string[] parameters = directive.Split(' ');
            if (directive == "endif" || directive == "end")
            {
                if (IfStack.Count == 1)
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.UncoupledStatement));
                else
                {
                    IfStack.Pop();
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                }
            }
            else if (directive.StartsWith("elseif") || directive.StartsWith("elif"))
            {
                if (IfStack.Count == 1)
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.UncoupledStatement));
                else
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        var result = ParseExpression(line.Substring(3), true);
                        if (result.Successful)
                        {
                            if (result.Value > 0)
                                IfStack.Push(!IfStack.Pop());
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                        }
                        else
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                    }
                }
            }
            else if (directive == "else")
            {
                if (IfStack.Count == 1)
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.UncoupledStatement));
                else
                    IfStack.Push(!IfStack.Pop());
            }
            else if (IfStack.Peek())
            {
                if (directive == "region" || directive == "endregion") { } // Allowed but ignored
                else if (directive == "nolist")
                {
                    noList = true;
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                }
                else if (directive == "list")
                {
                    noList = false;
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                }
                else if ((directive.StartsWith("dat ") || directive.StartsWith("dw ")))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        List<ushort> binOutput = new List<ushort>();
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                {
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                }
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    foreach (byte b in Encoding.ASCII.GetBytes(str))
                                        binOutput.Add(b);
                                }
                            }
                            else
                            {
                                ExpressionResult value = ParseExpression(data.Trim());
                                if (!value.Successful)
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                else
                                    binOutput.Add(value.Value);
                            }
                        }
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), binOutput.ToArray(), currentAddress, !noList));
                        if (!noList)
                            currentAddress += (ushort)binOutput.Count;
                    }
                }
                else if (directive.StartsWith("echo"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        string consoleOutput = "";
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                {
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                }
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    consoleOutput += str;
                                }
                            }
                            else
                            {
                                ExpressionResult value = ParseExpression(data.Trim(), true);
                                if (!value.Successful)
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                else
                                    consoleOutput += "0x" + value.Value.ToString("x").ToUpper();
                            }
                        }
                        Console.Write(consoleOutput + "\n");
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                        output.Add(new ListEntry(consoleOutput, FileNames.Peek(), LineNumbers.Peek(), currentAddress));
                    }
                }
                else if (directive.StartsWith("ascii"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        List<ushort> binOutput = new List<ushort>();
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                {
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                }
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    foreach (byte b in Encoding.ASCII.GetBytes(str))
                                        binOutput.Add(b);
                                }
                            }
                            else
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        }
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), binOutput.ToArray(), currentAddress, !noList));
                        if (!noList)
                            currentAddress += (ushort)binOutput.Count;
                    }
                }
                else if (directive.StartsWith("asciip"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        List<ushort> binOutput = new List<ushort>();
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                {
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                }
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    binOutput.Add((ushort)str.Length);
                                    foreach (byte b in Encoding.ASCII.GetBytes(str))
                                        binOutput.Add(b);
                                }
                            }
                            else
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        }
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), binOutput.ToArray(), currentAddress, !noList));
                        if (!noList)
                            currentAddress += (ushort)binOutput.Count;
                    }
                }
                else if (directive.StartsWith("asciic") || directive.StartsWith("asciiz"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        string[] dataStrings = directive.Substring(directive.IndexOf(" ")).SafeSplit(',');
                        List<ushort> binOutput = new List<ushort>();
                        foreach (string data in dataStrings)
                        {
                            if (data.Trim().StartsWith("\""))
                            {
                                if (!data.Trim().EndsWith("\""))
                                {
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                                }
                                else
                                {
                                    string str = data.Trim().Substring(1, data.Trim().Length - 2).Unescape();
                                    foreach (byte b in Encoding.ASCII.GetBytes(str))
                                        binOutput.Add(b);
                                    binOutput.Add(0);
                                }
                            }
                            else
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        }
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), binOutput.ToArray(), currentAddress, !noList));
                        if (!noList)
                            currentAddress += (ushort)binOutput.Count;
                    }
                }
                else if (directive.StartsWith("org")) // .orgASM's namesake :)
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else if (parameters.Length > 2)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    }
                    else
                    {
                        ExpressionResult value = ParseExpression(parameters[1]);
                        if (value == null)
                        {
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        }
                        else
                        {
                            currentAddress = value.Value;
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                        }
                    }
                }
                else if (directive.StartsWith("ifdef"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else if (parameters.Length > 2)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    }
                    else
                    {
                        if (Values.ContainsKey(parameters[1].ToLower()))
                            IfStack.Push(true);
                        else
                            IfStack.Push(false);
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                    }
                }
                else if (directive.StartsWith("ifndef"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else if (parameters.Length > 2)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                    }
                    else
                    {
                        if (Values.ContainsKey(parameters[1].ToLower()))
                            IfStack.Push(false);
                        else
                            IfStack.Push(true);
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                    }
                }
                else if (directive.StartsWith("if"))
                {
                    if (parameters.Length == 1)
                    {
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    }
                    else
                    {
                        var result = ParseExpression(line.Substring(3), true);
                        if (result.Successful)
                        {
                            if (result.Value > 0)
                                IfStack.Push(true);
                            else
                                IfStack.Push(false);
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                        }
                        else
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                    }
                }
                else if (directive.StartsWith("equ") || directive.StartsWith("define") || directive.StartsWith("equate"))
                {
                    if (parameters.Length > 1)
                    {
                        if (Values.ContainsKey(parameters[1].ToLower()))
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.DuplicateName));
                        else
                        {
                            if (parameters.Length == 2)
                            {
                                Values.Add(parameters[1].ToLower(), 1);
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                            }
                            else if (parameters.Length > 2)
                            {
                                string expression = directive.TrimExcessWhitespace();
                                expression = expression.Substring(expression.IndexOf(' ') + 1);
                                expression = expression.Substring(expression.IndexOf(' ') + 1);
                                ExpressionResult value = ParseExpression(expression, true); // TODO: find a way to forward reference
                                if (value != null)
                                {
                                    Values.Add(parameters[1].ToLower(), value.Value);
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, !noList));
                                }
                                else
                                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                            }
                            else
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                        }
                    }
                }
                else if (directive.StartsWith("undef"))
                {
                    if (parameters.Length != 2)
                    {
                        if (Values.ContainsKey(parameters[1].ToLower()))
                        {
                            // Resolve all references using this value thus far
                            for (int i = 0; i < output.Count; i++)
                            {
                                if (output[i].Expression == null)
                                    continue;
                                for (int j = 0; j < output[i].Expression.References.Length; j++)
                                {
                                    if (output[i].Output.Length > 1)
                                    {
                                        string reference = output[i].Expression.References[j];
                                        if (reference.ToLower() == parameters[1].ToLower())
                                        {
                                            ushort originalValue = output[i].Output[1];
                                            ushort value = (ushort)(Values[reference.ToLower()] + originalValue);
                                            output[i].Output[1] = value;
                                        }
                                    }
                                }
                            } // TODO: Allow optimization here?
                            Values.Remove(parameters[1].ToLower());
                        }
                        else
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.UndefinedReference));
                    }
                    else if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                }
                else if (directive.StartsWith("pad") || directive.StartsWith("fill")) // .pad length, value
                {
                    parameters = line.SafeSplit(',', ' ');
                    string[] fixedParams = new string[0];
                    foreach (string parameter in parameters)
                        if (!string.IsNullOrEmpty(parameter))
                            fixedParams = fixedParams.Concat(new string[] { parameter }).ToArray();
                    parameters = fixedParams;
                    if (parameters.Length == 3)
                    {
                        var length = ParseExpression(parameters[1], true);
                        var value = ParseExpression(parameters[2], true);
                        if (!length.Successful || !value.Successful)
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        else
                        {
                            ushort[] padding = new ushort[length.Value];
                            for (int i = 0; i < padding.Length; i++)
                                padding[i] = value.Value;
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), padding, currentAddress, !noList));
                            if (!noList)
                                currentAddress += (ushort)padding.Length;
                        }
                    }
                    else if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                }
                else if (directive.StartsWith("reserve"))
                {
                    if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else
                    {
                        var expression = directive.Substring(7).Trim();
                        var result = ParseExpression(expression);
                        if (result.Successful)
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), new ushort[result.Value], currentAddress, !noList));
                        else
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                    }
                }
                else if (directive.StartsWith("align")) // .align addr
                {
                    if (parameters.Length == 2)
                    {
                        var addr = ParseExpression(parameters[1], true);
                        if (!addr.Successful)
                            output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.IllegalExpression));
                        else
                        {
                            if (currentAddress > addr.Value)
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.AlignToPast));
                            else
                            {
                                var amount = (ushort)(addr.Value - currentAddress);
                                ushort[] padding = new ushort[amount];
                                output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), padding, currentAddress, !noList));
                                if (!noList)
                                    currentAddress = addr.Value;
                            }
                        }
                    }
                    else if (parameters.Length == 1)
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InsufficientParamters));
                    else
                        output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.TooManyParamters));
                }
                else
                {
                    output.Add(new ListEntry(line, FileNames.Peek(), LineNumbers.Peek(), currentAddress, ErrorCode.InvalidDirective));
                }
            }
        }

        #endregion
    }
}
