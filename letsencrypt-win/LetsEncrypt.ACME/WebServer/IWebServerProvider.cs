using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.WebServer
{
    public interface IWebServerProvider
    {
        void UploadFile(Uri fileUrl, Stream s);
    }
}
