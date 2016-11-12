using ACMESharp.POSH.Util;
using ACMESharp.Util;
using System;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsData.Update, "Registration", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(AcmeRegistration))]
    public class UpdateRegistration : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter UseBaseUri
        { get; set; }

        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        [ValidateCount(1, 100)]
        public string[] Contacts
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter AcceptTos
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionaly, set or update the unique alias assigned to the Certificate
        ///   for future reference.  To remove the alias, use the empty string.
        /// </para>
        /// </summary>
        [Parameter]
        public string NewAlias
        { get; set; }

        [Parameter]
        public string Label
        { get; set; }

        [Parameter]
        public string Memo
        { get; set; }

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

                // If we're renaming the Alias, do that
                // first in case there are any problems
                if (NewAlias != null)
                {
                    v.Registrations.Rename("0", NewAlias);
                    ri.Alias = NewAlias == "" ? null : NewAlias;
                }

                if (!LocalOnly)
                {
                    try
                    {
                        using (var c = ClientHelper.GetClient(v, ri))
                        {
                            c.Init();
                            c.GetDirectory(true);

                            r = c.UpdateRegistration(UseBaseUri, AcceptTos, Contacts);
                            ri.Registration = r;
                        }
                    }
                    catch (AcmeClient.AcmeWebException ex)
                    {
                        ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ri));
                        return;
                    }

                    vlt.SaveVault(v);
                }

                ri.Label = StringHelper.IfNullOrEmpty(Label);
                ri.Memo = StringHelper.IfNullOrEmpty(Memo);

                vlt.SaveVault(v);

                WriteObject(r);
            }
        }
    }
}
