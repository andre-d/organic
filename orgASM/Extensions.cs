using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace orgASM
{
    public static class Extensions
    {
        public static string TrimComments(this string value)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == ';' && !inString && !inChar)
                    return value.Remove(i).Trim();
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return value.Trim();
        }

        public static string[] SafeSplit(this string value, params char[] characters)
        {
            string[] result = new string[1];
            result[0] = "";
            bool inString = false, inChar = false;
            foreach (char c in value)
            {
                bool foundChar = false;
                if (!inString && !inChar)
                {
                    foreach (char haystack in characters)
                    {
                        if (c == haystack)
                        {
                            foundChar = true;
                            result = result.Concat(new string[] { "" }).ToArray();
                            break;
                        }
                    }
                }
                if (!foundChar)
                {
                    result[result.Length - 1] += c;
                    if (c == '"' && !inChar)
                        inString = !inString;
                    if (c == '\'' && !inString)
                        inChar = !inChar;
                }
            }
            return result;
        }

        public static string TrimExcessWhitespace(this string value)
        {
            string newvalue = "";
            value = value.Trim().Replace('\t', ' ');
            bool inString = false, inChar = false, previousWhitespace = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (!(char.IsWhiteSpace(value[i]) && previousWhitespace) || inString || inChar)
                    newvalue += value[i];
                if (char.IsWhiteSpace(value[i]))
                    previousWhitespace = true;
                else
                    previousWhitespace = false;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return newvalue.Trim();
        }

        public static string Unescape(this string value)
        {
            if (value == null)
                return null;
            string newvalue = "";
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\')
                    newvalue += value[i];
                else
                {
                    if (i + 1 == value.Length)
                        return null;
                    switch (value[i + 1])
                    {
                        case 'a':
                            newvalue += "\a";
                            break;
                        case 'b':
                            newvalue += "\b";
                            break;
                        case 'f':
                            newvalue += "\f";
                            break;
                        case 'n':
                            newvalue += "\n";
                            break;
                        case 'r':
                            newvalue += "\r";
                            break;
                        case 't':
                            newvalue += "\t";
                            break;
                        case 'v':
                            newvalue += "\v";
                            break;
                        case '\'':
                            newvalue += "\'";
                            break;
                        case '"':
                            newvalue += "\"";
                            break;
                        case '\\':
                            newvalue += "\\";
                            break;
                        default:
                            return null;
                    }
                    i++;
                }
            }
            return newvalue;
        }
    }
}
