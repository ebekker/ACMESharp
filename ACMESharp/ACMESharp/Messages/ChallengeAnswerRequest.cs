using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;

namespace ACMESharp.Messages
{
    public class ChallengeAnswerRequest : RequestMessage, IReadOnlyDictionary<string, object>
    {
        private readonly Dictionary<string, object> _fieldValues = new Dictionary<string, object>();

        private ChallengeAnswerRequest(ChallengeAnswer answer)
                : base("challenge")
        {
            Answer = answer;

            // Have to reproduce base properties
            // since this class is a dictionary
            _fieldValues[nameof(Resource)] = base.Resource;

            foreach (var field in answer.GetFields())
            {
                _fieldValues[field] = answer[field];
            }
        }

        protected ChallengeAnswer Answer
        { get; private set; }

        public static ChallengeAnswerRequest CreateRequest(ChallengeAnswer answer)
        {
            return new ChallengeAnswerRequest(answer);
        }

        #region -- Explicit Implementation Members --

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _fieldValues.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fieldValues.GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count
                => _fieldValues.Count;

        bool IReadOnlyDictionary<string, object>.ContainsKey(string key)
        {
            return _fieldValues.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return _fieldValues.TryGetValue(key, out value);
        }

        object IReadOnlyDictionary<string, object>.this[string key]
        {
            get { return _fieldValues[key]; }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys
                => _fieldValues.Keys;

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values
                => _fieldValues.Values;
        
        #endregion -- Explicit Implementation Members --
    }
}
