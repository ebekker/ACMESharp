using ACMESharp.POSH.Util;
using ACMESharp.Vault.Model;
using ACMESharp.Vault.Util;
using System;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///   Creates and submits a new Identifier to be verified to the ACME CA Server.
    /// </para>
    /// <para type="description">
    ///   Currently, the only Identifier type supported is the DNS type.
    /// </para>
    /// <para type="link">Get-Identifier</para>
    /// <para type="link">Update-Identifier</para>
    /// <para type="link">Complete-Challenge</para>
    /// <para type="link">Submit-Challenge</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Identifier")]
    [OutputType(typeof(AuthorizationState))]
    public class NewIdentifier : Cmdlet
    {
        /// <summary>
        /// <para type="description">
        ///   Specifies the DNS name to be submitted for verification.
        /// </para>
        /// </summary>
        [Parameter]
        public string Dns
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional, unique alias to assign to the Identifier for future
        ///   reference.
        /// </para>
        /// </summary>
        [Parameter]
        public string Alias
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional, human-friendly label to assign to the Identifier for
        ///   easy recognition.
        /// </para>
        /// </summary>
        [Parameter]
        public string Label
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional, arbitrary text field to capture any notes or details
        ///   associated with the Identifier.
        /// </para>
        /// </summary>
        [Parameter]
        public string Memo
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

                if (v.Registrations == null || v.Registrations.Count < 1)
                    throw new InvalidOperationException("No registrations found");

                var ri = v.Registrations[0];
                var r = ri.Registration;

                AuthorizationState authzState = null;
                var ii = new IdentifierInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    RegistrationRef = ri.Id,
                    Dns = Dns,
                };

                try
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        authzState = c.AuthorizeIdentifier(Dns);
                        ii.Authorization = authzState;

                        if (v.Identifiers == null)
                            v.Identifiers = new EntityDictionary<IdentifierInfo>();

                        v.Identifiers.Add(ii);
                    }
                }
                catch (AcmeClient.AcmeWebException ex)
                {
                    ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ii));
                    return;
                }

                vlt.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
