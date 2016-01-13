using System;
using System.IO;

namespace ACMESharp.WebServer
{
    public interface XXXIWebServerProvider
    {
        void UploadFile(Uri fileUrl, Stream s);
    }
}
