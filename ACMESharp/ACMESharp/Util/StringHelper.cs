namespace ACMESharp.Util
{
    public static class StringHelper
    {
        public static string IfNullOrEmpty(string s, string v1 = null)
        {
            if (string.IsNullOrEmpty(s))
                return v1;
            return s;
        }
    }
}
