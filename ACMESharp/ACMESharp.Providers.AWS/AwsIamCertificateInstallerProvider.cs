using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.Providers.AWS
{
    [InstallerProvider("aws-iam",
        Aliases = new[] { "awsIam" },
        Label = "AWS IAM Server Certificate",
        Description = "Provider for uploading a certificate"
                + " to IAM as a Server Certificate.")]
    public class AwsIamCertificateInstallerProvider : IInstallerProvider
    {
        public static readonly ParameterDetail SERVER_CERTIFICATE_NAME = new ParameterDetail(
                nameof(AwsIamCertificateInstaller.ServerCertificateName),
                ParameterType.TEXT, isRequired: true, label: "Server Certificate Name",
                desc: "The unique Name that will identify the Server Certificate");

        public static readonly ParameterDetail PATH = new ParameterDetail(
                nameof(AwsIamCertificateInstaller.Path),
                ParameterType.TEXT, isRequired: false, label: "IAM Certificate Path",
                desc: "Path under IAM to organize the Server Certificate (special considerations"
                        + " when used with CloudFront)");

        public static readonly ParameterDetail USE_WITH_CLOUDFRONT = new ParameterDetail(
                nameof(AwsIamCertificateInstaller.UseWithCloudFront),
                ParameterType.BOOLEAN, isRequired: false, label: "Use With CloudFront",
                desc: "Flag to indicate if the Server Certificate will be used with CloudFront");

        internal static readonly ParameterDetail[] PARAMS =
        {
            SERVER_CERTIFICATE_NAME,
            PATH,
            USE_WITH_CLOUDFRONT,

            AwsCommonParams.ACCESS_KEY_ID,
            AwsCommonParams.SECRET_ACCESS_KEY,
            AwsCommonParams.SESSION_TOKEN,

            AwsCommonParams.PROFILE_NAME,
            AwsCommonParams.PROFILE_LOCATION,

            AwsCommonParams.IAM_ROLE,

            AwsCommonParams.REGION,

        };

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public IInstaller GetInstaller(IReadOnlyDictionary<string, object> initParams)
        {
            return AwsIamCertificateInstallerProvider.GetNewInstaller(initParams);
        }


        public static AwsIamCertificateInstaller GetNewInstaller(IReadOnlyDictionary<string, object> initParams)
        {
            var inst = new AwsIamCertificateInstaller();

            if (initParams == null)
                initParams = new Dictionary<string, object>();

            // Required params
            if (!initParams.ContainsKey(SERVER_CERTIFICATE_NAME.Name))
                throw new KeyNotFoundException($"missing required parameter [{SERVER_CERTIFICATE_NAME.Name}]");
            inst.ServerCertificateName = (string)initParams[SERVER_CERTIFICATE_NAME.Name];

            // Optional params
            if (initParams.ContainsKey(PATH.Name))
                inst.Path = (string)initParams[PATH.Name];
            if (initParams.ContainsKey(USE_WITH_CLOUDFRONT.Name))
                inst.UseWithCloudFront = (bool)initParams[USE_WITH_CLOUDFRONT.Name];

            // Process the common params
            inst.CommonParams.InitParams(initParams);

            // Some validation
            if (!string.IsNullOrEmpty(inst.Path))
            {
                if (!inst.Path.StartsWith(AwsIamCertificateInstaller.PATH_REQUIRED_PREFIX))
                    throw new ArgumentException($"optional Path argument must start with leading"
                            + $" '{AwsIamCertificateInstaller.PATH_REQUIRED_PREFIX}'");
                if (!inst.Path.EndsWith(AwsIamCertificateInstaller.PATH_REQUIRED_SUFFIX))
                    throw new ArgumentException($"optional Path argument must end with trailing"
                            + $" '{AwsIamCertificateInstaller.PATH_REQUIRED_SUFFIX}'");
            }

            if (inst.UseWithCloudFront)
            {
                if (!string.IsNullOrEmpty(inst.Path))
                {
                    if (!inst.Path.StartsWith(AwsIamCertificateInstaller.PATH_REQUIRED_CLOUDFRONT_PREFIX,
                            StringComparison.InvariantCultureIgnoreCase))
                        throw new ArgumentException($"optional Path argument must start with leading"
                                + $" '{AwsIamCertificateInstaller.PATH_REQUIRED_CLOUDFRONT_PREFIX}'");
                }
                else
                {
                    inst.Path = AwsIamCertificateInstaller.PATH_REQUIRED_CLOUDFRONT_PREFIX;
                }
            }

            return inst;
        }
    }
}
