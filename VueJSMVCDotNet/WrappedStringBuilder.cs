namespace VueJSMVCDotNet
{
    internal class WrappedStringBuilder
    {
        private readonly StringBuilder sb;
        private readonly bool minimize;

        public WrappedStringBuilder(bool minimize)
        {
            sb = new();
            this.minimize = minimize;
        }

        public new string ToString()
            =>(minimize ? JSMinifier.Minify(sb.ToString()) : sb.ToString());

        public void AppendLine(string line)
        {
            if (minimize)
                Append(line);
            else
                sb.AppendLine(line);
        }

        internal void Append(string value)
            => WrappedAppend(value);

        private void WrappedAppend(string value)
            => sb.Append((minimize ? JSMinifier.StripComments(value.Trim()) : value));

        public int Length
        {
            get { return sb.Length; }
            set { sb.Length = value; }
        }
    }
}
