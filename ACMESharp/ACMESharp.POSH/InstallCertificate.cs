using ACMESharp.Installer;
using ACMESharp.PKI;
using ACMESharp.PKI.EC;
using ACMESharp.PKI.RSA;
using ACMESharp.POSH.Util;
using ACMESharp.Util;
using ACMESharp.Vault.Model;
using ACMESharp.Vault.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">Invokes an Installer to install a PKI certificate.</para>
    /// <para type="description">
    ///   Use this cmdlet to invoke an instance of an Installer Provider defined
    ///   by a profile stored in an ACMESharp Vault.  The profile will specify
    ///   the provider name as well as any parameters that will be applied during
    ///   the instance creation and invocation.  Alternatively, you can specify
    ///   all the details captured in a profile "inline" during this cmdlet's
    ///   invocation.
    /// </para>
    /// <para type="link">Get-InstallerProfile</para>
    /// <para type="link">Set-InstallerProfile</para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Install, "Certificate")]
    public class InstallCertificate : Cmdlet
    {
        public const string PSET_INSTALLER_INLINE = "InstallerInline";
        public const string PSET_INSTALLER_PROFILE = "InstallerProfile";

        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously retrieved and resolved
        ///     Certificated provided by an ACME CA Server.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string CertificateRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies a reference (ID or alias) to a previously defined
        ///     Installer profile in the associated Vault that defines the Installer
        ///     provider and associated instance parameters that should be used to
        ///     install the certificate.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = PSET_INSTALLER_PROFILE)]
        public string InstallerProfileRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies the Installer instance provider that will be used to
        ///     install the associated certificate.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = PSET_INSTALLER_INLINE)]
        public string Installer
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies the parameters that will be passed to the Installer
        ///     instance that will be used to install the associated certificate.
        /// </para>
        /// <para type="description">
        ///     If this cmdlet is invoked *in-line*, then these are the only parameters
        ///     that will be passed to the handler.  If this cmdlet is invoked with a
        ///     handler profile reference, then these parameters are merged with, and
        ///     override, whatever parameters are already defined within the profile.
        /// </para>
        /// </summary>
        [Parameter]
        public Hashtable InstallerParameters
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies a Vault profile name that will resolve to the Vault instance to be
        ///     used for all related operations and storage/retrieval of all related assets.
        /// </para>
        /// </summary>
        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage();
                var v = vlt.LoadVault();

                if (v.Certificates == null || v.Certificates.Count < 1)
                    throw new InvalidOperationException("No certificates found");

                var ci = v.Certificates.GetByRef(CertificateRef, throwOnMissing: false);
                if (ci == null)
                    throw new Exception("Unable to find a Certificate for the given reference");

                IssuerCertificateInfo ici = null;
                if (!string.IsNullOrEmpty(ci.IssuerSerialNumber))
                    v.IssuerCertificates.TryGetValue(ci.IssuerSerialNumber, out ici);

                PrivateKey pk = null;
                Crt crt = null;
                Crt issCrt = null;

                var keyAsset = vlt.GetAsset(Vault.VaultAssetType.KeyPem, ci.KeyPemFile);
                var crtAsset = vlt.GetAsset(Vault.VaultAssetType.CrtPem, ci.CrtPemFile);
                var issCrtAsset = ici != null
                        ? vlt.GetAsset(Vault.VaultAssetType.IssuerPem, ici.CrtPemFile)
                        : null;


                // Resolve details from inline or profile attributes
                string installerName = null;
                IReadOnlyDictionary<string, object> installerParams = null;
                IReadOnlyDictionary<string, object> cliInstallerParams = null;

                if (InstallerParameters?.Count > 0)
                    cliInstallerParams = (IReadOnlyDictionary<string, object>
                                    )PoshHelper.Convert<string, object>(InstallerParameters);

                if (!string.IsNullOrEmpty(InstallerProfileRef))
                {
                    var ppi = v.InstallerProfiles.GetByRef(InstallerProfileRef, throwOnMissing: false);
                    if (ppi == null)
                        throw new ItemNotFoundException("no Installer profile found for the given reference")
                                .With(nameof(InstallerProfileRef), InstallerProfileRef);

                    var ppAsset = vlt.GetAsset(Vault.VaultAssetType.InstallerConfigInfo,
                            ppi.Id.ToString());
                    InstallerProfile ip;
                    using (var s = vlt.LoadAsset(ppAsset))
                    {
                        ip = JsonHelper.Load<InstallerProfile>(s);
                    }

                    installerName = ip.InstallerProvider;
                    installerParams = ip.InstanceParameters;
                    if (cliInstallerParams != null)
                    {
                        WriteVerbose("Override Installer parameters specified");
                        if (installerParams?.Count == 0)
                        {
                            WriteVerbose("Profile does not define any parameters, using override parameters only");
                            installerParams = cliInstallerParams;
                        }
                        else
                        {
                            WriteVerbose("Merging Installer override parameters with profile");
                            var mergedParams = new Dictionary<string, object>();

                            foreach (var kv in ip.InstanceParameters)
                                mergedParams[kv.Key] = kv.Value;
                            foreach (var kv in cliInstallerParams)
                                mergedParams[kv.Key] = kv.Value;

                            installerParams = mergedParams;
                        }
                    }
                }
                else
                {
                    installerName = Installer;
                    installerParams = cliInstallerParams;
                }


                using (var pki = PkiHelper.GetPkiTool(v.PkiTool))
                {
                    // Load the Private Key
                    // TODO:  This is UGLY, but it works for now!
                    using (var s = vlt.LoadAsset(keyAsset))
                    {
                        try
                        {
                            pk = pki.ImportPrivateKey<RsaPrivateKey>(EncodingFormat.PEM, s);
                        }
                        catch { }
                    }
                    if (pk == null)
                    {
                        using (var s = vlt.LoadAsset(keyAsset))
                        {
                            try
                            {
                                pk = pki.ImportPrivateKey<EcKeyPair>(EncodingFormat.PEM, s);
                            }
                            catch { }
                        }
                    }
                    if (pk == null)
                    {
                        throw new NotSupportedException("unknown or unsupported private key format");
                    }

                    // Load the Certificate
                    using (var s = vlt.LoadAsset(crtAsset))
                    {
                        crt = pki.ImportCertificate(EncodingFormat.PEM, s);
                    }

                    // Load the Issuer Certificate
                    if (issCrtAsset != null)
                    {
                        using (var s = vlt.LoadAsset(issCrtAsset))
                        {
                            issCrt = pki.ImportCertificate(EncodingFormat.PEM, s);
                        }
                    }

                    // Finally, instantiate and invoke the installer
                    var installerProvider = InstallerExtManager.GetProvider(installerName);
                    using (var installer = installerProvider.GetInstaller(installerParams))
                    {
                        var chain = new Crt[0];
                        if (issCrt != null)
                            chain = new[] { issCrt };
                        installer.Install(pk, crt, chain, pki);
                    }
                }


                //try
                //{
                //}
                //catch (AcmeClient.AcmeWebException ex)
                //{
                //    ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ci));
                //    return;
                //}
            }
        }
    }
}
