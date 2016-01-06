using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using Amazon.Runtime;

namespace ACMESharp.Providers.AWS
{
    /// <summary>
    /// Represents the set of mandatory or possible common parameters
    /// that may be shared across all AWS providers.
    /// </summary>
    public class AwsCommonParams
    {
        #region -- Constants --

        public static readonly ParameterDetail ACCESS_KEY_ID = new ParameterDetail(
                nameof(AwsCommonParams.AwsAccessKeyId),
                ParameterType.TEXT, label: "IAM Access Key ID",
                desc: "Access Key ID of the IAM credential to use");
        public static readonly ParameterDetail SECRET_ACCESS_KEY = new ParameterDetail(
                nameof(AwsCommonParams.AwsSecretAccessKey),
                ParameterType.TEXT, label: "IAM Secret Key",
                desc: "Secret Access Key of the IAM credential to use");
        public static readonly ParameterDetail SESSION_TOKEN = new ParameterDetail(
                nameof(AwsCommonParams.AwsSessionToken),
                ParameterType.TEXT, label: "IAM Session Token",
                desc: "Session Token of the IAM credential to use");

        public static readonly ParameterDetail PROFILE_NAME = new ParameterDetail(
                nameof(AwsCommonParams.AwsProfileName),
                ParameterType.TEXT, label: "AWS Stored Credential Profile Name",
                desc: "The name of the stored credential profile");
        public static readonly ParameterDetail PROFILE_LOCATION = new ParameterDetail(
                nameof(AwsCommonParams.AwsProfileLocation),
                ParameterType.TEXT, label: "AWS Stored Credential Profile Location",
                desc: "Overrides the default location to search for a stored credential profile");

        public static readonly ParameterDetail IAM_ROLE = new ParameterDetail(
                nameof(AwsCommonParams.AwsIamRole),
                ParameterType.TEXT, label: "IAM Role",
                desc: "Indicates an IAM Role to be used to resolve credentials; specify '*' to use first Role found");

        public static readonly ParameterDetail REGION = new ParameterDetail(
                nameof(AwsCommonParams.AwsRegion),
                ParameterType.TEXT, label: "API Endpoint Region",
                desc: "Region of the API endpoint to call");

        public const string IAM_ROLE_ANY = "*";

        #endregion -- Constants --

        #region -- Properties --

        public string AwsAccessKeyId
        { get; set; }

        public string AwsSecretAccessKey
        { get; set; }

        public string AwsSessionToken
        { get; set; }

        public string AwsProfileName
        { get; set; }

        public string AwsProfileLocation
        { get; set; }

        public string AwsIamRole
        { get; set; }

        public string AwsRegion
        {
            get { return RegionEndpoint == null ? null : RegionEndpoint.SystemName; }
            set { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value); }
        }

        public Amazon.RegionEndpoint RegionEndpoint
        { get; set; } = Amazon.RegionEndpoint.USEast1;

        #endregion -- Properties --

        #region -- Methods --

        public void InitParams(IReadOnlyDictionary<string, object> initParams)
        {
            if (initParams.ContainsKey(ACCESS_KEY_ID.Name))
                AwsAccessKeyId = (string)initParams[ACCESS_KEY_ID.Name];
            if (initParams.ContainsKey(SECRET_ACCESS_KEY.Name))
                AwsSecretAccessKey = (string)initParams[SECRET_ACCESS_KEY.Name];
            if (initParams.ContainsKey(SESSION_TOKEN.Name))
                AwsSessionToken = (string)initParams[SESSION_TOKEN.Name];

            if (initParams.ContainsKey(PROFILE_NAME.Name))
                AwsProfileName = (string)initParams[PROFILE_NAME.Name];
            if (initParams.ContainsKey(PROFILE_LOCATION.Name))
                AwsProfileLocation = (string)initParams[PROFILE_LOCATION.Name];

            if (initParams.ContainsKey(IAM_ROLE.Name))
                AwsIamRole = (string)initParams[IAM_ROLE.Name];

            if (initParams.ContainsKey(REGION.Name))
                AwsRegion = (string)initParams[REGION.Name];

            // Validate IAM cred parts - either both provided or both missing
            if (string.IsNullOrEmpty(AwsAccessKeyId) != string.IsNullOrEmpty(AwsSecretAccessKey))
                throw new InvalidOperationException("Access Key ID and Secret Access Key are inconsistent");
        }

        /// <summary>
        /// Resolves the set of <see cref="AWSCredentials">AWS Credentials</see> based on the
        /// combination of credential-related parameters that are specified.
        /// </summary>
        /// <remarks>
        /// The order of resolution is as follows:
        /// <list>
        /// <item>
        /// 1.  If AccessKeyId is found
        ///     <item>a.  If Session Token is found, returns Session AWS Credential</item>
        ///     <item>b.  If no Session Token, returns a Base AWS Credential</item>
        /// </item>
        /// <item>
        /// 2.  If Profile Name is found, return a Stored Profile AWS Credential, with
        ///     an optional, overridden Profile Location
        /// </item>
        /// <item>
        /// 3.  If an IAM Role Name is specified, get the credentials from the local
        ///     EC2 instance IAM Role environment; if the special name '*' is used,
        ///     it uses the first IAM Role found in the current EC2 environment
        /// </item>
        /// <item>
        /// 4.  Otherwise, assume credentials are specified in environment variables
        ///     accessible to the hosting process and retrieve them from the following
        ///     variables:
        ///     <item><code>AWS_ACCESS_KEY_ID</code></item>
        ///     <item><code>AWS_SECRET_ACCESS_KEY</code></item>
        ///     <item><code></code>AWS_SESSION_TOKEN</code> (optional)</code></item>
        /// </item>
        /// </list>
        /// </remarks>
        public AWSCredentials ResolveCredentials()
        {
            AWSCredentials cr;

            if (!string.IsNullOrEmpty(AwsAccessKeyId))
            {
                if (!string.IsNullOrEmpty(AwsSessionToken))
                {
                    cr = new SessionAWSCredentials(AwsAccessKeyId, AwsSecretAccessKey, AwsSessionToken);
                }
                else
                {
                    cr = new Amazon.Runtime.BasicAWSCredentials(AwsAccessKeyId, AwsSecretAccessKey);
                }
            }
            else if (!string.IsNullOrEmpty(AwsProfileName))
            {
                cr = new StoredProfileAWSCredentials(AwsProfileName, AwsProfileLocation);
            }
            else if (!string.IsNullOrEmpty(AwsIamRole))
            {
                if (AwsIamRole == IAM_ROLE_ANY)
                    cr = new InstanceProfileAWSCredentials();
                else
                    cr = new InstanceProfileAWSCredentials(AwsIamRole);
            }
            else
            {
                cr = new EnvironmentVariablesAWSCredentials();
            }

            return cr;
        }

        #endregion -- Methods --
    }
}
