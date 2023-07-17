using System;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet
{
    internal class WrappedStringBuilder
    {
        private readonly StringBuilder _sb;
        private readonly bool _minimize;

        public WrappedStringBuilder(bool minimize)
        {
            _sb = new();
            _minimize = minimize;
        }

        public new string ToString()
        {
            return (_minimize ? JSMinifier.Minify(_sb.ToString()) : _sb.ToString());
        }

        public void AppendLine(string line)
        {
            if (_minimize)
                Append(line);
            else
                _sb.AppendLine(line);
        }

        internal void Append(string value)
        {
            WrappedAppend(value);
        }

        private void WrappedAppend(string value)
        {
            _sb.Append((_minimize ? JSMinifier.StripComments(value.Trim()) : value));
        }

        public int Length
        {
            get { return _sb.Length; }
            set { _sb.Length = value; }
        }
    }
}
