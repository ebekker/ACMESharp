using System;
using System.IO;

namespace ACMESharp.JOSE
{
    public interface ISigner : IDisposable
    {
        string JwsAlg { get; }

        void Init();

        void Save(Stream stream);

        void Load(Stream stream);

        object ExportJwk(bool canonical = false);

        byte[] Sign(byte[] raw);
    }
}
