using System.Collections.Generic;

namespace ACMESharp.PKI
{

    public class CsrDetails
    {
        /// <summary>X509 'CN'</summary>
        public string CommonName { get; set; }

        // <summary>X509 SAN extension</summary>
        public IEnumerable<string> AlternativeNames { get; set; }

        /// <summary>X509 'C'</summary>
        public string Country { get; set; }

        /// <summary>X509 'ST'</summary>
        public string StateOrProvince { get; set; }

        /// <summary>X509 'L'</summary>
        public string Locality { get; set; }

        /// <summary>X509 'O'</summary>
        public string Organization { get; set; }

        /// <summary>X509 'OU'</summary>
        public string OrganizationUnit { get; set; }

        /// <summary>X509 'D'</summary>
        public string Description { get; set; }

        /// <summary>X509 'S'</summary>
        public string Surname { get; set; }

        /// <summary>X509 'G'</summary>
        public string GivenName { get; set; }

        /// <summary>X509 'I'</summary>
        public string Initials { get; set; }

        /// <summary>X509 'T'</summary>
        public string Title { get; set; }

        /// <summary>X509 'SN'</summary>
        public string SerialNumber { get; set; }

        /// <summary>X509 'UID'</summary>
        public string UniqueIdentifier { get; set; }

        /// <summary>X509 'emailAddress'</summary>
        public string Email { get; set; }
    }
}
