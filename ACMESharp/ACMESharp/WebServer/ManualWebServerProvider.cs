using System;
using System.IO;
using ACMESharp.Util;

namespace ACMESharp.WebServer
{
    public class ManualWebServerProvider : BaseManualProvider, XXXIWebServerProvider
    {
        public string FilePath
        { get; set; }

        public void UploadFile(Uri fileUrl, Stream s)
        {
            var path = FilePath;

            if (string.IsNullOrEmpty(path))
                path = Path.GetTempFileName();
            else
            {
                int index = 0;
                while (File.Exists(path))
                    path = string.Format("{0}.{1}", FilePath, ++index);
            }

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException("Missing folder in requested file path");
            
            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                s.CopyTo(fs);
            }

            _writer.WriteLine("Manually Upload Web Server File:");
            _writer.WriteLine("  *           URL:  [{0}]", fileUrl);
            _writer.WriteLine("  *  File Content:  [{0}]", path);
        }
    }
}
