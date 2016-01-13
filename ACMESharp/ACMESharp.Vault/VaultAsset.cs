using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault
{
    public class VaultAsset
    {
        public virtual string Name
        { get; protected set; }

        public virtual VaultAssetType Type
        { get; protected set; }

        public virtual bool IsSensitive
        { get; protected set; }
    }
}
