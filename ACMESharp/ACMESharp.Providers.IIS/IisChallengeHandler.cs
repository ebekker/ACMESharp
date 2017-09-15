using ACMESharp.ACME;
using ACMESharp.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace ACMESharp.Providers.IIS
{
	/// <summary>
	/// Implements a <see cref="IChallengeHandler">Challenge Handler</see> that handles
	/// HTTP challenges by managing responses served up by the local IIS server.
	/// </summary>
	/// <remarks>
	/// Much of the implementation of this handler was initially adapted from the
	/// similarly-intentioned <see
	/// cref="https://github.com/Lone-Coder/letsencrypt-win-simple/blob/master/letsencrypt-win-simple/Plugin/IISPlugin.cs"
	/// ><code>IISPlugin</code></see> class from the <see
	/// cref="https://github.com/Lone-Coder/letsencrypt-win-simple"
	/// >letsencrypt-win-simple</see> project -- thank you, <see
	/// cref="https://github.com/Lone-Coder">Bryan</see>!
	/// </remarks>
	public class IisChallengeHandler : IChallengeHandler
    {
        #region -- Properties --

        public string WebSiteRef
        { get; set; }

        public string OverrideSiteRoot
        { get; set; }

        public bool SkipLocalWebConfig
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public void Handle(ChallengeHandlingContext ctx)
        {
            AssertNotDisposed();
            var httpChallenge = (HttpChallenge)ctx.Challenge;
            EditFile(httpChallenge, false, ctx.Out);
        }

        public void CleanUp(ChallengeHandlingContext ctx)
        {
            AssertNotDisposed();
            var httpChallenge = (HttpChallenge)ctx.Challenge;
            EditFile(httpChallenge, true, ctx.Out);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("IIS Challenge Handler is disposed");
        }

        private void EditFile(HttpChallenge httpChallenge, bool delete, TextWriter msg)
        {
            IisWebSiteBinding site = IisHelper.ResolveSingleSite(WebSiteRef,
                    IisHelper.ListDistinctHttpWebSites());

            var siteRoot = site.SiteRoot;
            if (!string.IsNullOrEmpty(OverrideSiteRoot))
                siteRoot = OverrideSiteRoot;
            if (string.IsNullOrEmpty(siteRoot))
                throw new InvalidOperationException("missing root path for resolve site")
                        .With(nameof(IisWebSiteBinding.SiteId), site.SiteId)
                        .With(nameof(IisWebSiteBinding.SiteName), site.SiteName);

            // IIS-configured Site Root can use env vars
            siteRoot = Environment.ExpandEnvironmentVariables(siteRoot);

            // Make sure we're using the canonical full path
            siteRoot = Path.GetFullPath(siteRoot);

            // We need to strip off any leading '/' in the path
            var filePath = httpChallenge.FilePath;
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1);

            var fullFilePath = Path.Combine(siteRoot, filePath);
            var fullDirPath = Path.GetDirectoryName(fullFilePath);
            var fullConfigPath = Path.Combine(fullDirPath, "web.config");

            // This meta-data file will be placed next to the actual
            // Challenge answer content file and it captures some details
            // that we need in order to properly clean up the handling of
            // this Challenge after it has been submitted
            var fullMetaPath = $"{fullFilePath}-acmesharp_meta";

            // Check if user is running with elevated privs and warn if not
            if (!IisHelper.IsAdministrator())
            {
                Console.Error.WriteLine("WARNING:  You are not running with elelvated privileges.");
                Console.Error.WriteLine("          Write access may be denied to the destination.");
            }

            if (delete)
            {
                bool skipLocalWebConfig = SkipLocalWebConfig;
                List<string> dirsCreated = null;

                // First see if there's a meta file there to help us out
                if (File.Exists(fullMetaPath))
                {
                    var meta = JsonHelper.Load<IisChallengeHandlerMeta>(
                            File.ReadAllText(fullMetaPath));

                    skipLocalWebConfig = meta.SkippedLocalWebConfig;
                    dirsCreated = meta.DirsCreated;
                }

				// Get rid of the Challenge answer content file
				if (File.Exists(fullFilePath))
				{
					File.Delete(fullFilePath);
					msg.WriteLine("* Challenge response content has been removed from local file path");
					msg.WriteLine("    at:  [{0}]", fullFilePath);
				}

				// Get rid of web.config if necessary
				if (!skipLocalWebConfig && File.Exists(fullConfigPath))
				{
					File.Delete(fullConfigPath);
					msg.WriteLine("* Local web.config has been removed from local file path");
					msg.WriteLine("    at:  [{0}]", fullFilePath);
				}

				// Get rid of the meta file so that we can clean up the dirs
				if (File.Exists(fullMetaPath))
                    File.Delete(fullMetaPath);

                // Walk up the tree if needed
                if (dirsCreated?.Count > 0)
                {
					var dirsDeleted = new List<string>();
                    dirsCreated.Reverse();
                    foreach (var dir in dirsCreated)
                    {
                        if (Directory.Exists(dir))
                        {
                            if (Directory.GetFileSystemEntries(dir).Length == 0)
                            {
                                Directory.Delete(dir);
								dirsDeleted.Add(dir);
                            }
                        }
                    }

					if (dirsDeleted.Count > 0)
					{
						msg.WriteLine("* Removed the following directories:");
						foreach (var dd in dirsDeleted)
							msg.WriteLine("  - [{0}]", dd);
					}
                }
            }
            else
            {
                // Figure out which dirs we have to create so
                // we can capture and clean it up later on
                var meta = new IisChallengeHandlerMeta
                {
                    WasAdmin = IisHelper.IsAdministrator(),
                    SkippedLocalWebConfig = SkipLocalWebConfig,
                    DirsCreated = new List<string>(),
                };

                // In theory this ascending of the dir path should work
                // just fine, but just in case, this path segment counter
                // should gaurd against the possibility of an infinite loop
                var dirLimit = 100;

                var testDir = fullDirPath;
                while (!Directory.Exists(testDir))
                {
                    // Sanity check against an infinite loop
                    if (--dirLimit <= 0)
                        throw new Exception("Unexpected directory path segment count reached")
                                .With(nameof(dirLimit), "100")
                                .With(nameof(fullDirPath), fullDirPath)
                                .With(nameof(testDir), testDir)
                                .With($"first-{nameof(meta.DirsCreated)}",
                                        meta.DirsCreated[0])
                                .With($"last-{nameof(meta.DirsCreated)}",
                                        meta.DirsCreated[meta.DirsCreated.Count - 1]);

                    if (Path.GetFullPath(testDir) == siteRoot)
                        break;

                    // Add to the top of the list
                    meta.DirsCreated.Insert(0, testDir);
                    // Move to the parent
                    testDir = Path.GetDirectoryName(testDir);
                }

                foreach (var dir in meta.DirsCreated)
                    Directory.CreateDirectory(dir);

                File.WriteAllText(fullFilePath, httpChallenge.FileContent);
                File.WriteAllText(fullMetaPath, JsonHelper.Save(meta));

				msg.WriteLine("* Challenge response content has been written to local file path");
				msg.WriteLine("    at:  [{0}]", fullFilePath);
				msg.WriteLine("* Challenge response should be accessible with a MIME type of [text/json]");
				msg.WriteLine("    at:  [{0}]", httpChallenge.FileUrl);

				if (!SkipLocalWebConfig)
				{
					var t = typeof(IisChallengeHandler);
					var r = $"{t.Namespace}.{t.Name}-WebConfig";
					using (Stream rs = t.Assembly.GetManifestResourceStream(r))
					{
						using (var fs = new FileStream(fullConfigPath, FileMode.Create))
						{
							rs.CopyTo(fs);
						}
					}
					msg.WriteLine("* Local web.config has been created to serve response file as JSON");
					msg.WriteLine("  however, you may need to adjust this file for your environment");
					msg.WriteLine("    at: [{0}]", httpChallenge.FileUrl);
				}
				else
				{
					msg.WriteLine("* Local web.config file creation has been skipped!");
					msg.WriteLine("  You may need to manually adjust your configuration to serve the");
					msg.WriteLine("  Challenge Response file with a MIME type of [text/json]");
				}
            }
        }

        #endregion -- Methods --

        #region -- Types --

        public class IisChallengeHandlerMeta
        {
            public bool WasAdmin
            { get; set; }

            public bool SkippedLocalWebConfig
            { get; set; }

            public List<string> DirsCreated
            { get; set; }
        }

        #endregion -- Types --
    }
}
