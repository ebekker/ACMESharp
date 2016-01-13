using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.ACME
{
    public abstract class ChallengeAnswer
    {
        private readonly Dictionary<string, object> _fieldValues = new Dictionary<string, object>();

        public object this[string field]
        {
            get { return _fieldValues[field]; }
            protected set { _fieldValues[field] = value; }
        }

        //public IReadOnlyDictionary<string, object> ToResponseMessage()
        //{
        //    return _fieldValues;
        //}

        public IEnumerable<string> GetFields()
        {
            return _fieldValues.Keys;
        }

        protected void Remove(string field)
        {
            _fieldValues.Remove(field);
        }
    }

    public class DnsChallengeAnswer : ChallengeAnswer
    {
        public string KeyAuthorization
        {
            get { return this[nameof(KeyAuthorization)] as string; }
            set { this[nameof(KeyAuthorization)] = value; }
        }
    }

    public class HttpChallengeAnswer : ChallengeAnswer
    {
        public string KeyAuthorization
        {
            get { return this[nameof(KeyAuthorization)] as string; }
            set { this[nameof(KeyAuthorization)] = value; }
        }
    }

    public class TlsSniChallengeAnswer : ChallengeAnswer
    {
        public string KeyAuthorization
        {
            get { return this[nameof(KeyAuthorization)] as string; }
            set { this[nameof(KeyAuthorization)] = value; }
        }
    }

    public class PriorKeyChallengeAnswer : ChallengeAnswer
    {
        public string Authorization
        {
            get { return this[nameof(KeyAuthorization)] as string; }
            set { this[nameof(KeyAuthorization)] = value; }
        }

        public string KeyAuthorization
        {
            get { return this[nameof(KeyAuthorization)] as string; }
            set { this[nameof(KeyAuthorization)] = value; }
        }

        // TODO:  WORK IN PROGRESS!!!
        //    https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.4
        public class Authz
        {
            public string Payload
            { get; set; }

            public string Signature
            { get; set; }

            public string HeaderAlg
            { get; set; }

            public object HeaderJwk
            { get; set; }
        }
    }
}
