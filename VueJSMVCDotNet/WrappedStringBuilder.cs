namespace VueJSMVCDotNet
{
    internal class WrappedStringBuilder(bool minimize)
    {
        private readonly StringBuilder sb = new();

        public new string ToString()
            => (minimize ? JSMinifier.Minify(sb.ToString(), ignoreComments: true) : sb.ToString());

        public void AppendLine(string line)
            => sb.AppendLine(line);

        internal void Append(string value)
            => sb.Append(value);
    }
}
