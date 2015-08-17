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
        void Init();

        void Save(Stream stream);

        void Load(Stream stream);

        object ExportJwk();

        byte[] Sign(byte[] raw);
    }
}
