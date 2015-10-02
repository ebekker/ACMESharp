using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsCommon.Set, "ServerDirectory")]
    public class SetServerDirectory : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_SINGLE_RES_ENT = "SingleResourceEntry";

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public string IssuerCert
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public Hashtable ResourceMap
        { get; set; }


        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public bool? GetInitialDirectory
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public bool? UseRelativeInitialDirectory
        { get; set; }

        [Parameter(ParameterSetName = PSET_SINGLE_RES_ENT, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Resource
        { get; set; }

        [Parameter(ParameterSetName = PSET_SINGLE_RES_ENT, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider())
            {
                vp.OpenStorage();
                var v = vp.LoadVault();

                if (GetInitialDirectory.HasValue)
                    v.GetInitialDirectory = GetInitialDirectory.Value;

                if (UseRelativeInitialDirectory.HasValue)
                    v.UseRelativeInitialDirectory = UseRelativeInitialDirectory.Value;

                if (!string.IsNullOrEmpty(IssuerCert))
                {
                    SetResEntry(v.ServerDirectory, AcmeServerDirectory.RES_ISSUER_CERT, IssuerCert);
                }

                if (!string.IsNullOrEmpty(Resource) && !string.IsNullOrEmpty(Resource))
                {
                    SetResEntry(v.ServerDirectory, Resource, Path);
                }

                if (ResourceMap != null)
                {
                    foreach (var ent in ResourceMap)
                    {
                        var dent = (DictionaryEntry)ent;
                        SetResEntry(v.ServerDirectory, dent.Key as string, dent.Value as string);
                    }
                }

                vp.SaveVault(v);
            }
        }

        private void SetResEntry(AcmeServerDirectory dir, string res, string path)
        {
            if (dir == null)
                throw new ArgumentNullException("dir", "Server Directory is required");
            if (string.IsNullOrEmpty(res))
                throw new ArgumentNullException("res", "Resource name is required");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path", "Resource path is required");

            // We can only set resources that are already
            // defined in the server directory mapping
            if (!dir.Contains(res))
                throw new ArgumentOutOfRangeException("res", "Resource name is invalid or unknown");

            dir[res] = path;
        }
    }
}
