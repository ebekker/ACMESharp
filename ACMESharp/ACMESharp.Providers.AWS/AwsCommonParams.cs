using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

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
                nameof(AwsCommonParams.AccessKeyId),
                ParameterType.TEXT, label: "IAM Access Key ID",
                desc: "Access Key ID of the IAM credential to use");
        public static readonly ParameterDetail SECRET_ACCESS_KEY = new ParameterDetail(
                nameof(AwsCommonParams.SecretAccessKey),
                ParameterType.TEXT, label: "IAM Secret Key",
                desc: "Secret Access Key of the IAM credential to use");
        public static readonly ParameterDetail REGION = new ParameterDetail(
                nameof(AwsCommonParams.Region),
                ParameterType.TEXT, label: "API Endpoint Region",
                desc: "Region of the API endpoint to call");

        #endregion -- Constants --

        #region -- Properties --

        public string AccessKeyId
        { get; set; }

        public string SecretAccessKey
        { get; set; }

        public string Region
        {
            get { return RegionEndpoint == null ? null : RegionEndpoint.SystemName; }
            set { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value); }
        }

        public Amazon.RegionEndpoint RegionEndpoint
        { get; set; } = Amazon.RegionEndpoint.USEast1;

        #endregion -- Properties --

        #region -- Methods --

        public void InitParams(IDictionary<string, object> initParams)
        {
            if (initParams.ContainsKey(ACCESS_KEY_ID.Name))
                AccessKeyId = (string)initParams[ACCESS_KEY_ID.Name];
            if (initParams.ContainsKey(SECRET_ACCESS_KEY.Name))
                SecretAccessKey = (string)initParams[SECRET_ACCESS_KEY.Name];
            if (initParams.ContainsKey(REGION.Name))
                Region = (string)initParams[REGION.Name];

            // Validate IAM cred parts - either both provided or both missing
            if (string.IsNullOrEmpty(AccessKeyId) != string.IsNullOrEmpty(SecretAccessKey))
                throw new InvalidOperationException("Access Key ID and Secret Access Key are inconsistent");
        }

        #endregion -- Methods --
    }
}
