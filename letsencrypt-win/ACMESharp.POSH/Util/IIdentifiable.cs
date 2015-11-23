using System;

namespace ACMESharp.POSH.Util
{
    public interface IIdentifiable
    {
        Guid Id
        { get; }

        string Alias
        { get; }
    }
}
