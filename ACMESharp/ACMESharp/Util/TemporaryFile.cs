using System;
using System.IO;

namespace ACMESharp.Util
{
    /// <summary>
    /// Wrapper for GetTempFileName, which deletes the file when it's disposed
    /// </summary>
    public sealed class TemporaryFile : IDisposable
    {
        public TemporaryFile()
        {
            FileName = Path.GetTempFileName();
        }

        public void Dispose()
        {
            File.Delete(FileName);
        }

        public string FileName { get; }
    }
}