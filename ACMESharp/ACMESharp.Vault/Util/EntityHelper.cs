using System;

namespace ACMESharp.Vault.Util
{
    public static class EntityHelper
    {
        public static Guid NewId()
        {
            return Guid.NewGuid();
        }
    }
}
