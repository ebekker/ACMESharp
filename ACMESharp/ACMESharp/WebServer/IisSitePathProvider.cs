using System;
using System.IO;
using System.Security.Principal;

namespace ACMESharp.WebServer
{
    /// <summary>
    /// Implements a file-based <see cref="IWebServerProvider">Web Server Provider</see>
    /// that constructs the necessary directory and file path under a given Web site
    /// root path. 
    /// </summary>
    /// <remarks>
    /// This provider should be configured with the path to the root folder of the
    /// target site that is being used to validate a DNS Identifier.  Under this root
    /// path, the provider will construct the necessary intermediate folders, and
    /// challenge response file containing the content that is provided.
    /// <para>
    /// Additionally, if the file path references a file without an extension, it will
    /// generate a local <code>web.config</code> file in the same folder as the target
    /// file which will enable an IIS site configured to operate under an integrated
    /// pipeline mode to successfully serve up the file as a JSON mime-type.  By default
    /// IIS will not serve up a file without an extension in this scenario.
    /// </para>
    /// </remarks>
    public class IisSitePathProvider : XXXIWebServerProvider
    {
        /// <summary>
        /// Path to the root directory of the target Web site.
        /// </summary>
        public string WebSiteRoot
        { get; set; }

        public void UploadFile(Uri fileUrl, Stream s)
        {
            if (string.IsNullOrWhiteSpace(WebSiteRoot))
                throw new InvalidOperationException("Web site root is unspecified or invalid");
            if (!Directory.Exists(WebSiteRoot))
                throw new DirectoryNotFoundException("Web site root is missing");

            var filePath = fileUrl.AbsolutePath;
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);
            filePath = filePath.Replace('/', '\\');

            var fullPath = Path.Combine(WebSiteRoot, filePath);
            var dirPath = Path.GetDirectoryName(fullPath);
            var fileName = Path.GetFileName(fullPath);

            // Check if user is running with elevated privs and warn if not
            if (!IsAdministrator())
            {
                Console.Error.WriteLine("WARNING:  You are not running with elelvated privileges.");
                Console.Error.WriteLine("          Write access may be denied to the destination.");
            }

            Directory.CreateDirectory(dirPath);
            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                s.CopyTo(fs);
            }

            // If the target file path is a file without an extension,
            // we need to tell IIS that it's OK to serve it up, and to
            // associate a default MIME type (text/json)
            if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
            {
                var t = typeof(IisSitePathProvider);
                var wcPath = Path.Combine(dirPath, "web.config");
                using (Stream rs = t.Assembly.GetManifestResourceStream(
                        $"{t.Namespace}.IisSitePathProvider-WebConfig"),
                        fs = new FileStream(wcPath, FileMode.Create))
                {
                    rs.CopyTo(fs);
                }
            }
        }

        public static bool IsAdministrator()
        {
            var winId = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(winId);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
