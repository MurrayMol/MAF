namespace MAF.Security.Core
{
    public class SecurityConst
    {
        /// <summary>
        ///  常量
        /// </summary>
        public const string SID = "SID";  // SecurityId
        public const string CID = "CID";  // ClientId
        public const string ROOT_URL = "RootUrl";
        public const string PAGE_SIZE = "PageSize";
        public const string VALIDATE_CODE = "MyValidateCode";
        public const string MAINTAIN_MODE = "MaintainMode";
        public const string YES = "yes";
    }

    public class SecurityKey
    {
        private SecurityKey(string key)
        {
            Value = key;
        }

        public string Value { get; private set; }

        public override string ToString()
        {
            return Value;
        }

        static SecurityKey()
        {
            SID = new SecurityKey("SID");
            CID = new SecurityKey("CID");
        }

        public static SecurityKey SID { get; private set; }
        public static SecurityKey CID { get; private set; }
    }
}
