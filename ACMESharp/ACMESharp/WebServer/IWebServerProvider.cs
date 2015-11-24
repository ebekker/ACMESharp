using System;
using System.IO;

namespace ACMESharp.WebServer
{
    public interface IWebServerProvider : IChallengeHandlingProvider
    {
        void UploadFile(Uri fileUrl, Stream s);
    }
}
