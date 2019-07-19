using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class WrappedStringBuilder
    {
        private StringBuilder _sb;
        private bool _minimize;

        public WrappedStringBuilder(bool minimize)
        {
            _sb = new StringBuilder();
            _minimize = minimize;
        }

        public string ToString()
        {
            return (_minimize ? JSMinifier.Minify(_sb.ToString()) : _sb.ToString());
        }

        public void AppendLine(string line)
        {
            if (_minimize)
                _sb.Append(line);
            else
                _sb.AppendLine(line);
        }

        internal void AppendFormat(string format, object arg0)
        {
            _sb.AppendFormat(format, arg0);
        }

        internal void AppendFormat(string format, object arg0, object arg1)
        {
            _sb.AppendFormat(format, arg0, arg1);
        }

        internal void AppendFormat(string format,object arg0,object arg1,object arg2)
        {
            _sb.AppendFormat(format, arg0, arg1, arg2);
        }

        internal void AppendFormat(string format,object[] args)
        {
            _sb.AppendFormat(format, args);
        }

        internal void Append(string value)
        {
            _sb.Append(value);
        }

        public int Length
        {
            get { return _sb.Length; }
            set { _sb.Length = value; }
        }
    }
}
