using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.JOSE
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
