﻿using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace VueJSMVCDotNet
{
    internal class JSMinifier
    {
        internal static string StripComments(string originalString)
        {
            string ret = "";
            for (int x = 0; x < originalString.Length; x++)
            {
                if ((originalString[x] == '/') && ret.EndsWith("/"))
                {
                    ret = ret[..^1];
                    while (x < originalString.Length)
                    {
                        if (originalString[x] == '\n')
                            break;
                        x++;
                    }
                }
                else if ((originalString[x] == '*') && ret.EndsWith("/"))
                {
                    ret = ret[..^1];
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
            StringBuilder emptyLines = new();
            foreach (string line in lines)
            {
                string s = line.Trim();
                if (s.Length > 0 && !s.StartsWith("//"))
                    emptyLines.AppendLine(s.Trim());
            }

            string ret = StripComments(emptyLines.ToString());
            Stream inMS = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(ret));
            ret = new JsMin().Minify((TextReader)new StreamReader(inMS));
            return ret;
        }

    }

    [ExcludeFromCodeCoverage]
    internal class JsMin
    {
        private const int Eof = -1;
        private TextReader _sr;
        private TextWriter _sw;
        private int _theA;
        private int _theB;
        private int _theLookahead = Eof;
        private int _theX = Eof;
        private int _theY = Eof;
        private int _retStatement = -1;
        private bool _start = false;

        public string Minify(TextReader reader)
        {
            _sr = reader;
            var sb = new StringBuilder();
            using (_sw = new StringWriter(sb))
            {
                ExecuteJsMin();
            }
            return sb.ToString();
        }

        /// <summary>
        /// jsmin -- Copy the input to the output, deleting the characters which are
        /// insignificant to JavaScript. Comments will be removed. Tabs will be
        /// replaced with spaces. Carriage returns will be replaced with linefeeds.
        /// Most spaces and linefeeds will be removed.
        /// </summary>
        private void ExecuteJsMin()
        {
            _start = false;

            if (Peek() == 0xEF)
            {
                Get();
                Get();
                Get();
            }
            _theA = '\n';
            Action(3);
            while (_theA != Eof)
            {
                switch (_theA)
                {
                    case ' ':
                        Action(JsMin.IsAlphanum(_theB) ? 1 : 2);
                        break;
                    case '\n':
                    case '\u2028':
                    case '\u2029':
                        switch (_theB)
                        {
                            case '{':
                            case '[':
                            case '(':
                            case '+':
                            case '-':
                            case '!':
                            case '~':
                                if (!_start)
                                {
                                    //this is the first write, we don't want to write a new line to begin,
                                    // read next
                                    Action(2);
                                    break;
                                }
                                //Maintain the line break
                                Action(1);
                                break;
                            case ' ':
                                Action(3);
                                break;
                            default:
                                if (!_start)
                                {
                                    //this is the first write, we don't want to write a new line to begin,
                                    // read next
                                    Action(2);
                                    break;
                                }
                                Action(JsMin.IsAlphanum(_theB) ? 1 : 2);
                                break;
                        }
                        break;
                    default:
                        switch (_theB)
                        {

                            case ' ':
                                Action(JsMin.IsAlphanum(_theA) ? 1 : 3);
                                break;
                            case '\n':
                            case '\u2028':
                            case '\u2029':
                                switch (_theA)
                                {
                                    case '}':
                                    case ']':
                                    case ')':
                                    case '+':
                                    case '-':
                                    case '"':
                                    case '\'':
                                    case '`':
                                        Action(1);
                                        break;
                                    default:
                                        Action(JsMin.IsAlphanum(_theA) ? 1 : 3);
                                        break;
                                }
                                break;
                            default:
                                Action(1);
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// action -- do something! What you do is determined by the argument:
        ///      1   Output A.Copy B to A.Get the next B.
        ///      2   Copy B to A. Get the next B. (Delete A).
        ///      3   Get the next B. (Delete B).
        /// </summary>
        /// <param name="d"></param>
        void Action(int d)
        {
            switch (d)
            {
                case 1:
                    Put(_theA);
                    _start = true;

                    //process unary operator
                    HandleUnaryOperator();

                    goto case 2;
                case 2:
                    _theA = _theB;

                    //process string literals or end of statement and track return statement
                    if (!HandleStringLiteral())
                        HandleEndOfStatement();

                    goto case 3;
                case 3:
                    _theB = NextCharExcludingComments();

                    //track return statement
                    TrackReturnStatement();

                    //Check for a regex literal and process it if it is found
                    HandleRegexLiteral();

                    goto default;
                default:
                    break;
            }
        }

        private bool HandleUnaryOperator()
        {
            const string operators = "+-*/";
            if ((_theY == '\n' || _theY == ' ') &&
                (operators.Contains((char)_theA)) && (operators.Contains((char)_theB)))
            {
                Put(_theY);
                return true;
            }
            return false;
        }

        private bool TrackReturnStatement()
        {
            const string r = "return";
            const string preReturn = ";){} ";
            if (_retStatement == -1 && _theA == 'r' &&
                (preReturn.Contains((char)_theY) || char.IsWhiteSpace((char)_theY) || _theY == 'r'))
            {
                _retStatement = 0;
                return true;
            }

            if (_retStatement >= (r.Length - 1))
            {
                //reset when there is a return statement and the next char is not whitespace
                if (!char.IsWhiteSpace((char)_theA))
                {
                    _retStatement = -1;
                    return false;
                }
                //currently there's only whitespace but there is a return statement so just exit
                return true;
            }
            if (_retStatement < 0) return false;

            _retStatement++;
            if (r[_retStatement] == _theA) return true;

            _retStatement = -1;
            return false;
        }

        /// <summary>
        /// If it's an end of statement char read over whitespace but not new lines
        /// </summary>
        private bool HandleEndOfStatement()
        {
            if (_theA != '}') return false;

            var peek = Peek();
            //NOTE: We don't skip over a new line, this is becase in some cases 
            // library managers don't put a semicolon after a } when they have defined a variable as a method,
            // in this case when minifying it might break because the next declaration won't be valid unless
            // there's a semicolon or a line break, so we'll leave line breaks.            
            while (peek != Eof && peek != '\n' && char.IsWhiteSpace((char)peek))
            {
                Get();
                peek = Peek();
            }
            return true;
        }

        /// <summary>
        /// Iterates through a string literal
        /// </summary>
        private bool HandleStringLiteral()
        {
            if (_theA != '\'' && _theA != '"' && _theA != '`')
                return false;

            //only allowed with template strings
            var allowLineFeed = _theA == '`';

            //write the start quote
            Put(_theA);
            _theA = Get(replaceCr: !allowLineFeed); //don't replace CR here, if we need to deal with that

            for (; ; )
            {
                //If the A matches B it means the string literal is done
                // since at this moment B was the original A string literal (" or ')
                if (_theA == _theB)
                {
                    //write the end quote
                    Put(_theA);

                    //reset, this essentially resets the process
                    _theA = ' ';
                    break;
                }

                var skipRead = false;

                switch (_theA)
                {
                    case '\r':
                    case '\n':
                        if (!allowLineFeed)
                            throw new Exception($"Error: JSMIN unterminated string literal: {_theA}\n");
                        //if we're allowing line feeds, then just continue to write it
                        break;
                    case '\\':
                        //check for escaped chars

                        //This scenario needs to cater for backslash line escapes (i.e. multi-line JS strings)
                        switch (Peek())
                        {
                            case '\n':
                                //this is a multi-line string so we don't want to insert a line break here,
                                // just get the next char that is not a line break/eof/or string termination
                                do
                                {
                                    _theA = Get();
                                } while (_theA == '\n' && _theA != Eof && _theA != _theB);
                                break;
                            default:
                                Put(_theA);         //write the backslash
                                _theA = Get();      //get the escaped char
                                if (_theA == Eof)
                                    throw new Exception($"Error: JSMIN unterminated string literal: {_theA}\n");
                                Put(_theA);         //write the escaped char
                                _theA = Get();
                                skipRead = true;    //go to beginning of loop
                                break;
                        }
                        break;
                    case '$':
                        //check for string templates (i.e. ${ } )
                        //this must be enclosed in a backtick string
                        if (_theB == '`' && Peek() == '{')
                        {
                            HandleStringTemplateBlock();
                            skipRead = true;    //go to beginning of loop
                        }
                        break;
                }

                if (_theA == Eof)
                {
                    throw new Exception($"Error: JSMIN unterminated string literal: {_theA}\n");
                }

                if (!skipRead)
                {
                    Put(_theA);
                    _theA = Get(replaceCr: !allowLineFeed); //don't replace CR here, if we need to deal with that    
                }
            }
            return true;
        }

        /// <summary>
        /// Iterates through a string template block - and caters for nested blocks
        /// </summary>
        private void HandleStringTemplateBlock()
        {
            //This is a string template block

            Put(_theA);     //write the $
            _theA = Get();  //get next (this will be { )

            for (; ; )
            {
                switch (_theA)
                {
                    case '}':
                        //write the end bracket and read
                        Put(_theA);
                        _theA = Get();
                        //exit!
                        return;
                    case '$':
                        //check for inner string templates (i.e. ${ } )
                        if (Peek() == '{')
                        {
                            //recurse
                            HandleStringTemplateBlock();
                        }
                        break;
                    case Eof:
                        throw new Exception($"Error: JSMIN unterminated string template block: {_theA}\n");
                }

                Put(_theA);
                _theA = Get();
            }
        }

        /// <summary>
        /// Used to iterate over and output the content of a Regex literal
        /// </summary>
        private bool HandleRegexLiteral()
        {
            if (_theB != '/') return false;
            //if (_theA == '/') return false;

            //The original testing for regex literals didn't actually work in many cases,
            // for example see these bug reports: 
            //  https://github.com/douglascrockford/JSMin/issues/11
            //  https://github.com/Shazwazza/ClientDependency/issues/73                    
            //  https://github.com/Shazwazza/JsMinSharp/issues/8
            //The original logic from JSMin doesn't cater for the above issues mentioned
            // We've now added these additional characters to be able to preceed a regex literal: +
            // And now we also track a return statement which can preceed a regex literal.
            // To fix the single line no-op  issue - we need to allow for a new line to precede a 
            // regex statement too but _theA will not be a newline char here, only _theY will be.
            const string toMatch = "(,=:[!&|?+-~*/{\n+;";
            if (toMatch.IndexOf((char)_theA) < 0 && (char)_theY != '\n' && _retStatement != 5)
                return false;

            Put(_theA);
            if (_theA == '/' || _theA == '*')
            {
                Put(' ');
            }
            Put(_theB);
            for (; ; )
            {
                _theA = Get();
                if (_theA == '[')
                {
                    for (; ; )
                    {
                        Put(_theA);
                        _theA = Get();
                        if (_theA == ']')
                        {
                            break;
                        }
                        if (_theA == '\\')
                        {
                            Put(_theA);
                            _theA = Get();
                        }
                        if (_theA == Eof)
                        {
                            throw new Exception($"Error: JSMIN Unterminated set in Regular Expression literal: {_theA}\n");
                        }
                    }
                }
                else if (_theA == '/')
                {
                    switch (Peek())
                    {
                        case 'i':
                        case 'g':
                            //regex modifiers, do we care?
                            break;
                        case ' ':
                            //skip the space
                            Put(_theA);
                            Get();
                            _theA = Get();
                            break;
                        case '/':
                        case '*':
                            throw new Exception($"Error: JSMIN Unterminated set in Regular Expression literal: {_theA}\n");
                    }
                    break;
                }
                else if (_theA == '\\')
                {
                    Put(_theA);
                    _theA = Get();
                }
                if (_theA == Eof)
                {
                    throw new Exception($"Error: JSMIN Unterminated Regular Expression literal: {_theA}\n");
                }
                Put(_theA);
            }
            _theB = NextCharExcludingComments();
            return false;
        }

        /// <summary>
        /// next -- get the next character, excluding comments. peek() is used to see
        ///  if a '/' is followed by a '/' or '*'.
        /// </summary>
        /// <returns></returns>
        private int NextCharExcludingComments()
        {
            int c = Get();
            if (c == '/')
            {
                switch (Peek())
                {
                    case '/':
                        //handle single line comments
                        for (; ; )
                        {
                            c = Get();
                            if (JsMin.IsLineSeparator(c))
                            {
                                break;
                            }
                        }
                        break;
                    case '*':
                        //handle multi-line comments
                        Get(); //move to *

                        for (; ; )
                        {
                            var exit = false;
                            c = Get(); //read next
                            switch (c)
                            {
                                case '*':
                                    var currPeek = Peek();
                                    if (currPeek == '/')
                                    {
                                        //we're at the end of the comment

                                        Get(); //move to /

                                        //In one very peculiar circumstance, if the JS value is like:
                                        // val(1 /* Calendar */.toString());
                                        // if we strip the comment out, JS will produce an error because
                                        // 1.toString() is not valid, however 1..toString() is valid and 
                                        // similarly keeping the comment is valid. So we can check if the next value
                                        // is a '.' and if the current value is numeric and perform this operation.
                                        // The reason why .. works is because the JS parser cannot do 1.toString() because it 
                                        // sees the '.' as a decimal

                                        if (char.IsDigit((char)_theY))
                                        {
                                            currPeek = Peek();
                                            if (currPeek == '.')
                                            {
                                                //we actually want to write another '.'
                                                return '.';
                                            }
                                        }

                                        c = Get(); //move past the comment
                                        exit = true;
                                    }
                                    break;
                                case Eof:
                                    throw new Exception("Error: JSMIN Unterminated comment.\n");
                            }

                            if (exit)
                                break;
                        }
                        break;
                }
            }

            _theY = _theX;
            _theX = c;
            return c;
        }

        /// <summary>
        /// peek -- get the next character without getting it.
        /// </summary>
        /// <returns></returns>
        private int Peek()
        {
            _theLookahead = Get();
            return _theLookahead;
        }

        /// <summary>
        /// get -- return the next character from stdin. Watch out for lookahead. If
        /// the character is a control character, translate it to a space or
        /// linefeed.
        /// </summary>
        /// <returns></returns>
        private int Get(bool replaceCr = true)
        {
            int c = _theLookahead;
            _theLookahead = Eof;
            if (c == Eof)
            {
                c = _sr.Read();
            }
            if (c >= ' ' || c == '\n' || c == Eof)
            {
                return c;
            }
            if (c == '\r' && !replaceCr)
            {
                return c;
            }
            if (c == '\r' && replaceCr)
            {
                return '\n';
            }
            if (c == '\u2028' || c == '\u2029')
            {
                return '\n';
            }
            return ' ';
        }

        private void Put(int c)
        {
            _sw.Write((char)c);
        }

        /// <summary>
        /// isAlphanum -- return true if the character is a letter, digit, underscore,
        /// dollar sign, or non-ASCII character.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsAlphanum(int c)
        {
            return ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || c == '\\' ||
                    c > 126);
        }

        private static bool IsLineSeparator(int c)
        {
            return c <= '\n' || c == '\u2028' || c == '\u2029';
        }

    }
}
