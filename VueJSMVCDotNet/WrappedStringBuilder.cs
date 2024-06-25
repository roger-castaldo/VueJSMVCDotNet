namespace VueJSMVCDotNet
{
    internal class WrappedStringBuilder(bool minimize)
    {
        private readonly StringBuilder sb = new();

        public new string ToString()
            => (minimize ? JSMinifier.Minify(sb.ToString(),ignoreComments:true) : sb.ToString());

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
            => sb.Append((minimize ? $"{JSMinifier.StripComments(value.Trim())}{Environment.NewLine}" : value));

        public int Length
        {
            get { return sb.Length; }
            set { sb.Length = value; }
        }
    }
}
