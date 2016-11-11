using ACMESharp.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.Providers.AWS
{
    [InstallerProvider("aws-elb",
        Aliases = new[] { "awsElb" },
        Label = "AWS Elastic Load Balancer",
        Description = "Provider for handling certificate"
                + " installation to AWS Elastic Load Balancer.")]
    public class AwsElbInstallerProvider : IInstallerProvider
    {
        public static readonly ParameterDetail ELB_NAME = new ParameterDetail(
                nameof(AwsElbInstaller.LoadBalancerName),
                ParameterType.TEXT, isRequired: true, label: "ELB Instance Name",
                desc: "The unique Name identifying the target ELB instance");

        public static readonly ParameterDetail LIS_PORT = new ParameterDetail(
                nameof(AwsElbInstaller.LoadBalancerPort),
                ParameterType.NUMBER, isRequired: true, label: "ELB Listening Port",
                desc: "The listening port on the target ELB where the certificate will be installed");

        public static readonly ParameterDetail LIS_PROTO = new ParameterDetail(
                nameof(AwsElbInstaller.LoadBalancerProtocol),
                ParameterType.TEXT, isRequired: false, label: "ELB Listening Protocol",
                desc: "The protocol used on the listening port on the target ELB, one of:  HTTPS, SSL");

        public static readonly ParameterDetail INST_PORT = new ParameterDetail(
                nameof(AwsElbInstaller.InstancePort),
                ParameterType.NUMBER, isRequired: true, label: "ELB Listening Port",
                desc: "The listening port on the back-end instances of the ELB");

        public static readonly ParameterDetail INST_PROTO = new ParameterDetail(
                nameof(AwsElbInstaller.InstanceProtocol),
                ParameterType.TEXT, isRequired: false, label: "ELB Listening Protocol",
                desc: "The protocol used  by the back-end instances of the ELB, one of:  HTTP, HTTPS, SSL, TCP");

        public static readonly ParameterDetail EXISTING_SERVER_CERTIFICATE_NAME = new ParameterDetail(
                nameof(AwsElbInstaller.ExistingServerCertificateName),
                ParameterType.TEXT, isRequired: false, label: "Existing IAM Server Certificate Name",
                desc: "An existing IAM Server Certificate name to install; either this *OR* the IAM"
                        + " Server Certificate installer parameters must be specified.");

        internal static readonly ParameterDetail[] PARAMS = (new[]
        {
            ELB_NAME,
            LIS_PORT,
            LIS_PROTO,
            INST_PORT,
            INST_PROTO,
            EXISTING_SERVER_CERTIFICATE_NAME,
        }).Concat(AwsIamCertificateInstallerProvider.PARAMS).ToArray();

        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return PARAMS;
        }

        public IInstaller GetInstaller(IReadOnlyDictionary<string, object> initParams)
        {
            var inst = new AwsElbInstaller();

            if (initParams == null)
                initParams = new Dictionary<string, object>();

            // Required params
            if (!initParams.ContainsKey(ELB_NAME.Name))
                throw new KeyNotFoundException($"missing required parameter [{ELB_NAME.Name}]");
            inst.LoadBalancerName = (string)initParams[ELB_NAME.Name];
            if (!initParams.ContainsKey(LIS_PORT.Name))
                throw new KeyNotFoundException($"missing required parameter [{LIS_PORT.Name}]");
            inst.LoadBalancerPort = (int)((long)initParams[LIS_PORT.Name]);

            // Optional params
            if (initParams.ContainsKey(LIS_PROTO.Name))
            {
                inst.LoadBalancerProtocol = ((string)initParams[LIS_PROTO.Name])?.ToUpper();
                if (!AwsElbInstaller.VALID_LIS_PROTOS.Contains(inst.LoadBalancerProtocol))
                    throw new ArgumentException("invalid listener protocol specified");

                if (initParams.ContainsKey(INST_PORT.Name))
                    inst.InstancePort = (int)((long)initParams[INST_PORT.Name]);
                else
                    inst.InstancePort = inst.LoadBalancerPort;

                if (initParams.ContainsKey(INST_PROTO.Name))
                    inst.InstanceProtocol = ((string)initParams[INST_PROTO.Name])?.ToUpper();
                else
                    inst.InstanceProtocol = inst.LoadBalancerProtocol;
                if (!AwsElbInstaller.VALID_INST_PROTOS.Contains(inst.InstanceProtocol))
                    throw new ArgumentException("invalid instance protocol specified");

            }

            if (initParams.ContainsKey(EXISTING_SERVER_CERTIFICATE_NAME.Name))
            {
                inst.ExistingServerCertificateName = (string)initParams[EXISTING_SERVER_CERTIFICATE_NAME.Name];
            }
            else
            {
                inst.CertInstaller = AwsIamCertificateInstallerProvider.GetNewInstaller(initParams);
            }

            // Process the common params
            inst.CommonParams.InitParams(initParams);

            return inst;
        }
    }
}
