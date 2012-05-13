using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Organic
{
    public static class Extensions
    {
        /// <summary>
        /// Given a string, such as "SET A, B ; test", it removes all comments from that string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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

        public static bool SafeContains(this string value, char needle)
        {
            value = value.Trim();
            bool inString = false, inChar = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == needle && !inString && !inChar)
                    return true;
                if (value[i] == '"' && !inChar)
                    inString = !inString;
                if (value[i] == '\'' && !inString)
                    inChar = !inChar;
            }
            return false;
        }

        /// <summary>
        /// Works the same as String.Split, but will not split if the requested characters are within
        /// a character or string literal.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="characters"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes unneccissary whitespace from a line of code.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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

        public static bool ContainsKey(this List<Label> value, string key)
        {
            foreach (var label in value)
            {
                if (label.Name.ToLower() == key.ToLower())
                    return true;
            }
            return false;
        }

        public static ushort GetValue(this List<Label> value, string key)
        {
            foreach (var label in value)
            {
                if (label.Name.ToLower() == key.ToLower())
                    return label.Address;
            }
            return 0;
        }

        /// <summary>
        /// Given a string with escaped characters, this will return the unescaped version.
        /// Example: "Test\\nstring" becomes "Test\nstring"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
                        case '0':
                            newvalue += "\0";
                            break;
                        case 'x':
                            if (i + 3 > value.Length)
                                return null;
                            string hex = value[i + 2].ToString() + value[i + 3].ToString();
                            i += 2;
                            try
                            {
                                newvalue += (char)Encoding.ASCII.GetBytes(new char[] { (char)byte.Parse(hex, NumberStyles.HexNumber) })[0];
                            }
                            catch
                            {
                                return null;
                            }
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
