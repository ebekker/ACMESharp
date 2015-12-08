using System;

namespace ACMESharp.Vault
{
    public interface IIdentifiable
    {
        Guid Id
        { get; }

        string Alias
        { get; }
    }
}
