using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACMESharp.Messages
{
    public class ChallengePart
    {
        [JsonExtensionData]
        private Dictionary<string, JToken> _expando =
                new Dictionary<string, JToken>();

        public JToken this[string name]
        {
            get { return _expando[name]; }
            set { _expando[name] = value; }
        }

        public string Type
        { get; set; }

        public string Uri
        { get; set; }

        public string Token
        { get; set; }

        public string Status
        { get; set; }

        public DateTime? Validated
        { get; set; }

        public IDictionary<string, string> Error
        { get; set; }
    }


    //public class ChallengePart : Dictionary<string, object>
    //{
    //    public string Type
    //    {
    //        get { return this[nameof(Type)] as string; }
    //        set { this[nameof(Type)] = value; }
    //    }

    //    public string Uri
    //    {
    //        get { return this[nameof(Uri)] as string; }
    //        set { this[nameof(Uri)] = value; }
    //    }

    //    public string Token
    //    {
    //        get { return this[nameof(Token)] as string; }
    //        set { this[nameof(Token)] = value; }
    //    }

    //    public string Status
    //    {
    //        get { return this[nameof(Status)] as string; }
    //        set { this[nameof(Status)] = value; }
    //    }

    //    public DateTime? Validated
    //    {
    //        get { return this[nameof(Validated)] as DateTime?; }
    //        set { this[nameof(Validated)] = value; }
    //    }

    //    public IDictionary<string, string> Error
    //    {
    //        get { return this[nameof(Error)] as IDictionary<string, string>; }
    //        set { this[nameof(Error)] = value; }
    //    }
    //}
}
