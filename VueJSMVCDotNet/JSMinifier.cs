using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    /// <summary>
	/// Description of JSMinifier.
	/// </summary>
	internal class JSMinifier
    {
        internal static string StripComments(string originalString)
        {
            string ret = "";
            for (int x = 0; x < originalString.Length; x++)
            {
                if ((originalString[x] == '/') && ret.EndsWith("/"))
                {
                    ret = ret.Substring(0, ret.Length - 1);
                    while (x < originalString.Length)
                    {
                        if (originalString[x] == '\n')
                            break;
                        x++;
                    }
                }
                else if ((originalString[x] == '*') && ret.EndsWith("/"))
                {
                    ret = ret.Substring(0, ret.Length - 1);
                    x++;
                    if (x < originalString.Length)
                    {
                        string tmp = originalString[x].ToString();
                        while (x < originalString.Length)
                        {
                            if (originalString[x] == '/' && tmp.EndsWith("*"))
                            {
                                x++;
                                break;
                            }
                            tmp += originalString[x];
                            x++;
                        }
                    }
                }
                else if (originalString[x] == '\"')
                {
                    ret += originalString[x];
                    x++;
                    while (x < originalString.Length)
                    {
                        if ((originalString[x] == '\"') && !ret.EndsWith("\\"))
                            break;
                        ret += originalString[x];
                        x++;
                    }
                }
                else if (originalString[x] == '\'')
                {
                    ret += originalString[x];
                    x++;
                    while (x < originalString.Length)
                    {
                        if ((originalString[x] == '\'') && !ret.EndsWith("\\"))
                            break;
                        ret += originalString[x];
                        x++;
                    }
                }
                if (x < originalString.Length)
                    ret += originalString[x];
            }
            return ret;
        }

        public static string Minify(string js)
        {
            string[] lines = js.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder emptyLines = new StringBuilder();
            foreach (string line in lines)
            {
                string s = line.Trim();
                if (s.Length > 0 && !s.StartsWith("//"))
                    emptyLines.AppendLine(s.Trim());
            }

            string ret = StripComments(emptyLines.ToString());
            Stream outMS = new MemoryStream();
            Stream inMS = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(ret));
            new JSMinifier().Minify(ref inMS, ref outMS);
            return ASCIIEncoding.ASCII.GetString(((MemoryStream)outMS).ToArray()); ;
        }

        const int EOF = -1;

        StreamReader sr;
        StreamWriter sw;
        int theA;
        int theB;
        int theLookahead = EOF;

        private void Minify(ref Stream src, ref Stream dst)
        {
            sr = new StreamReader(src);
            sw = new StreamWriter(dst);
            jsmin();
            sr.Close();
            sw.Flush();
            sw.Close();
        }

        /* jsmin -- Copy the input to the output, deleting the characters which are
                insignificant to JavaScript. Comments will be removed. Tabs will be
                replaced with spaces. Carriage returns will be replaced with linefeeds.
                Most spaces and linefeeds will be removed.
        */
        private void jsmin()
        {
            theA = '\n';
            action(3);
            while (theA != EOF)
            {
                switch (theA)
                {
                    case ' ':
                        {
                            if (isAlphanum(theB))
                            {
                                action(1);
                            }
                            else
                            {
                                action(2);
                            }
                            break;
                        }
                    case '\n':
                        {
                            switch (theB)
                            {
                                case '{':
                                case '[':
                                case '(':
                                case '+':
                                case '-':
                                    {
                                        action(1);
                                        break;
                                    }
                                case ' ':
                                    {
                                        action(3);
                                        break;
                                    }
                                default:
                                    {
                                        if (isAlphanum(theB))
                                        {
                                            action(1);
                                        }
                                        else
                                        {
                                            action(2);
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            switch (theB)
                            {
                                case ' ':
                                    {
                                        if (isAlphanum(theA))
                                        {
                                            action(1);
                                            break;
                                        }
                                        action(3);
                                        break;
                                    }
                                case '\n':
                                    {
                                        switch (theA)
                                        {
                                            case '}':
                                            case ']':
                                            case ')':
                                            case '+':
                                            case '-':
                                            case '"':
                                            case '\'':
                                                {
                                                    action(1);
                                                    break;
                                                }
                                            default:
                                                {
                                                    if (isAlphanum(theA))
                                                    {
                                                        action(1);
                                                    }
                                                    else
                                                    {
                                                        action(3);
                                                    }
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        action(1);
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }
        /* action -- do something! What you do is determined by the argument:
                1   Output A. Copy B to A. Get the next B.
                2   Copy B to A. Get the next B. (Delete A).
                3   Get the next B. (Delete B).
           action treats a string as a single character. Wow!
           action recognizes a regular expression if it is preceded by ( or , or =.
        */
        private void action(int d)
        {
            if (d <= 1)
            {
                put(theA);
            }
            if (d <= 2)
            {
                theA = theB;
                if (theA == '\'' || theA == '"')
                {
                    for (; ; )
                    {
                        put(theA);
                        theA = get();
                        if (theA == theB)
                        {
                            break;
                        }
                        if (theA <= '\n')
                        {
                            throw new Exception(string.Format("Error: JSMIN unterminated string literal: {0} found at {1}\n", theA, sr.BaseStream.Position));
                        }
                        if (theA == '\\')
                        {
                            put(theA);
                            theA = get();
                        }
                    }
                }
            }
            if (d <= 3)
            {
                theB = next();
                if (theB == '/' && (theA == '(' || theA == ',' || theA == '=' ||
                                    theA == '[' || theA == '!' || theA == ':' ||
                                    theA == '&' || theA == '|' || theA == '?' ||
                                    theA == '{' || theA == '}' || theA == ';' ||
                                    theA == '\n'))
                {
                    put(theA);
                    put(theB);
                    for (; ; )
                    {
                        theA = get();
                        if (theA == '/')
                        {
                            break;
                        }
                        else if (theA == '\\')
                        {
                            put(theA);
                            theA = get();
                        }
                        else if (theA <= '\n')
                        {
                            throw new Exception(string.Format("Error: JSMIN unterminated Regular Expression literal : {0}.\n", theA));
                        }
                        put(theA);
                    }
                    theB = next();
                }
            }
        }
        /* next -- get the next character, excluding comments. peek() is used to see
                if a '/' is followed by a '/' or '*'.
        */
        private int next()
        {
            int c = get();
            if (c == '/')
            {
                switch (peek())
                {
                    case '/':
                        {
                            for (; ; )
                            {
                                c = get();
                                if (c <= '\n')
                                {
                                    return c;
                                }
                            }
                        }
                    case '*':
                        {
                            get();
                            for (; ; )
                            {
                                switch (get())
                                {
                                    case '*':
                                        {
                                            if (peek() == '/')
                                            {
                                                get();
                                                return ' ';
                                            }
                                            break;
                                        }
                                    case EOF:
                                        {
                                            throw new Exception("Error: JSMIN Unterminated comment.\n");
                                        }
                                }
                            }
                        }
                    default:
                        {
                            return c;
                        }
                }
            }
            return c;
        }
        /* peek -- get the next character without getting it.
        */
        private int peek()
        {
            theLookahead = get();
            return theLookahead;
        }
        /* get -- return the next character from stdin. Watch out for lookahead. If
                the character is a control character, translate it to a space or
                linefeed.
        */
        private int get()
        {
            int c = theLookahead;
            theLookahead = EOF;
            if (c == EOF)
            {
                c = sr.Read();
            }
            if (c >= ' ' || c == '\n' || c == EOF)
            {
                return c;
            }
            if (c == '\r')
            {
                return '\n';
            }
            return ' ';
        }

        private void put(int c)
        {
            sw.Write((char)c);
        }
        /* isAlphanum -- return true if the character is a letter, digit, underscore,
                dollar sign, or non-ASCII character.
        */
        private bool isAlphanum(int c)
        {
            return ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || c == '\\' ||
                c > 126);
        }

    }
}
