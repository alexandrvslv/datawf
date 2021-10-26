namespace Doc.Odf
{
    public class FormatedString
    {
        public FormatedString(string value)
        {
            this.Value = value;
        }
        public FormatedString(string value, int index, int length)
            : this(value)
        {
            this.Index = index;
            this.Length = length;
        }
        public FormatedString(string value, int index, int length, string format)
            : this(value, index, length)
        {
            this.Format = format;
        }
        public string Format = string.Empty;
        public string Value = string.Empty;
        public int Index = -1;
        public int Length = -1;
    }

}
