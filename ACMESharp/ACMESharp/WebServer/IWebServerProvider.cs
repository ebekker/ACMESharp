using System;
using System.IO;

namespace ACMESharp.WebServer
{
    public interface IWebServerProvider
    {
        void UploadFile(Uri fileUrl, Stream s);
    }
}
