using System;
using System.Text.RegularExpressions;

namespace ACMESharp.HTTP
{
    /// <summary>
    /// Represents a Link header value that represents the well-defined HTTP response
    /// entity header
    /// <see cref="https://en.wikipedia.org/wiki/List_of_HTTP_header_fields#Response_fields"><c>Link</c></see>
    /// and more fully specified in
    /// <see cref="https://tools.ietf.org/html/rfc5988#section-5">RFC 5988 Section 5</see>.
    /// </summary>
    /// <remarks>
    /// This class only implements a subset of the mechanics and nuances of the Link header
    /// field as necessary for implementing the ACME protocol.
    /// </remarks>
    public class Link
    {
        /// <summary>
        /// Regex pattern to match and extract the components of an HTTP related link header.
        /// </summary>
        public static readonly Regex LINK_HEADER_REGEX = new Regex("<(.+)>;rel=\"(.+)\"");

        public const string LINK_HEADER_FMT = "<{0}>;rel={1}";

        public Link(string value)
        {
            Value = value;

            var m = LINK_HEADER_REGEX.Match(value);
            if (!m.Success)
                throw new ArgumentException("Invalid Link header format", nameof(value));

            Uri = m.Groups[1].Value;
            Relation = m.Groups[2].Value;
        }

        public Link(string uri, string rel)
        {
            // This will parse the URI to make sure it's well-formed
            var parsed = new Uri(uri);

            Uri = uri;
            Relation = rel;
            Value = string.Format(LINK_HEADER_FMT, uri, rel);
        }

        public string Value
        { get; private set; }

        public string Uri
        { get; private set; }

        public string Relation
        { get; private set; }
    }
}
