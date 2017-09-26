using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ACMESharp.ACME.Providers
{
    public class ManualChallengeHandler : IChallengeHandler
    {
        #region -- Constants --

        public const string STD_OUT = "OUT";
        public const string STD_ERR = "ERR";
        public const string STD_DBG = "DBG";

        private static readonly TextWriter DEBUG_WRITER =
                new StreamWriter(new DebugStream(), Encoding.UTF8);

        #endregion -- Constants --

        #region -- Fields --

        private Stream _stream = null;
		private TextWriter _writer = null;

        #endregion -- Fields --

        #region -- Properties --

        public string WriteOutPath
        { get; private set; }

        public bool Append
        { get; private set; }

        public bool Overwrite
        { get; private set; }

		public bool OutputJson
		{ get; set; }

        public bool IsDisposed
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public void SetOut(string path, bool append = false, bool overwrite = false)
        {
            var priorS = _stream;
            var priorW = _writer;

            switch (path ?? string.Empty)
            {
				case "":
					_writer = null;
					_stream = null;
					break;

                case STD_OUT:
                    _writer = Console.Out;
                    _stream = null;
                    break;

                case STD_ERR:
                    _writer = Console.Error;
                    _stream = null;
                    break;

                case STD_DBG:
                    _writer = DEBUG_WRITER;
                    _stream = null;
                    break;

                default:
                    _writer = null;

                    if (append)
                        _stream = new FileStream(path, FileMode.Append);
                    else if (overwrite)
                        _stream = new FileStream(path, FileMode.Create);
                    else
                        _stream = new FileStream(path, FileMode.CreateNew);

                    _writer = new StreamWriter(_stream);
                    break;
            }

            if (priorS != null)
            {
                try
                {
                    priorW.Dispose();
                    priorS.Dispose();
                }
                catch (Exception)
                {
                    // TODO: failure to clean up the prior Out
                    // should do what???
                }
            }
        }

        public void Handle(ChallengeHandlingContext ctx)
        {
            AssertNotDisposed();

            var dnsChallenge = ctx.Challenge as DnsChallenge;
            var httpChallenge = ctx.Challenge as HttpChallenge;
            var tlsSniChallenge = ctx.Challenge as TlsSniChallenge;

            if (dnsChallenge != null)
            {
				var json = new
				{
					Label = "Manual Challenge Handler - DNS",
					HandleTime = DateTime.Now,
					ChallengeToken = dnsChallenge.Token,
					ChallengeType = "DNS",
					Description = new[]
					{
						"To complete this Challenge please create a new Resource",
						"Record (RR) with the following characteristics:",
					},
					DnsDetails = new
					{
						RRType = "TXT",
						RRName = dnsChallenge.RecordName,
						RRValue = dnsChallenge.RecordValue,
					}
				};

				// Compute the output message
				var message = OutputJson
					? JsonConvert.SerializeObject(json, Formatting.Indented)
					: $@"== {json.Label} ==
  * Handle Time:      [{json.HandleTime}]
  * Challenge Token:  [{json.ChallengeToken}]

{string.Join(Environment.NewLine, json.Description)}
  * RR Type:  [{json.DnsDetails.RRType}]
  * RR Name:  [{json.DnsDetails.RRName}]
  * RR Value: [{json.DnsDetails.RRValue}]
------------------------------------
";

				ctx.Out.WriteLine(message);
				_writer?.WriteLine(message);
                _writer?.Flush();
            }
            else if (httpChallenge != null)
            {
				var json = new
				{
					Label = "Manual Challenge Handler - HTTP",
					HandleTime = DateTime.Now,
					ChallengeToken = httpChallenge.Token,
					ChallengeType = "HTTP",
					Description = new[]
					{
						"To complete this Challenge please create a new file",
						"under the server that is responding to the hostname",
						"and path given with the following characteristics:",
					},
					HttpDetails = new
					{
						HttpUrl = httpChallenge.FileUrl,
						FilePath = httpChallenge.FilePath,
						FileContent = httpChallenge.FileContent,
						MimeType = "text/plain",
					}
				};

				// Compute the output message
				var message = OutputJson
					? JsonConvert.SerializeObject(json, Formatting.Indented)
					: $@"== {json.Label} ==
  * Handle Time:      [{json.HandleTime}]
  * Challenge Token:  [{json.ChallengeToken}]

{string.Join(Environment.NewLine, json.Description)}
  * HTTP URL:     [{json.HttpDetails.HttpUrl}]
  * File Path:    [{json.HttpDetails.FilePath}]
  * File Content: [{json.HttpDetails.FileContent}]
  * MIME Type:    [{json.HttpDetails.MimeType}]
------------------------------------										
";

				ctx.Out.WriteLine(message);
				_writer?.WriteLine(message);
				_writer?.Flush();
            }
            else if (tlsSniChallenge != null)
            {
                var json = new
                {
                    Label = "Manual Challenge Handler - TLS-SNI",
                    HandleTime = DateTime.Now,
                    ChallengeToken = tlsSniChallenge.Token,
                    ChallengeType = "TLS-SNI",
                    Description = new[]
                    {
                        "To complete this Challenge please configure your web server",
                        "with TLS-SNI self-signed certificates for the following token:",
                    },
                    TlsSniDetails = new
                    {
                        Token = tlsSniChallenge.Token,
                        IterationCount = tlsSniChallenge.IterationCount,
                    }
                };

                // Compute the output message
                var message = OutputJson
                    ? JsonConvert.SerializeObject(json, Formatting.Indented)
                    : $@"== {json.Label} ==
  * Handle Time:      [{json.HandleTime}]
  * Challenge Token:  [{json.ChallengeToken}]

{string.Join(Environment.NewLine, json.Description)}
  * Token:        [{json.TlsSniDetails.Token}]
  * Iterations:   [{json.TlsSniDetails.IterationCount}]
------------------------------------										
";

                ctx.Out.WriteLine(message);
                _writer?.WriteLine(message);
                _writer?.Flush();
            }
            else
            {
                var ex = new InvalidOperationException("unsupported Challenge type");
                ex.Data["challengeType"] = ctx.GetType();
                throw ex;
            }
        }

        public void CleanUp(ChallengeHandlingContext ctx)
        {
            AssertNotDisposed();

            var dnsChallenge = ctx.Challenge as DnsChallenge;
            var httpChallenge = ctx.Challenge as HttpChallenge;
            var tlsSniChallenge = ctx.Challenge as TlsSniChallenge;

            if (dnsChallenge != null)
            {
				var json = new
				{
					Label = "Manual Challenge Handler - DNS",
					CleanUpTime = DateTime.Now,
					ChallengeToken = dnsChallenge.Token,
					ChallengeType = "DNS",
					Description = new[]
					{
						"The Challenge has been completed -- you can now remove the",
						"Resource Record created previously with the following",
						"characteristics:",
					},
					DnsDetails = new
					{
						RRType = "TXT",
						RRName = dnsChallenge.RecordName,
						RRValue = dnsChallenge.RecordValue,
					}
				};

				// Compute the output message
				var message = OutputJson
					? JsonConvert.SerializeObject(json, Formatting.Indented)
					: $@"== {json.Label} ==
  * CleanUp Time:     [{json.CleanUpTime}]
  * Challenge Token:  [{json.ChallengeToken}]

{string.Join(Environment.NewLine, json.Description)}
  * RR Type:  [{json.DnsDetails.RRType}]
  * RR Name:  [{json.DnsDetails.RRName}]
  * RR Value: [{json.DnsDetails.RRValue}]
------------------------------------
";

				ctx.Out.WriteLine(message);
				_writer?.WriteLine(message);
                _writer?.Flush();
            }
            else if (httpChallenge != null)
            {
				var json = new
				{
					Label = "Manual Challenge Handler - HTTP",
					CleanUpTime = DateTime.Now,
					ChallengeToken = httpChallenge.Token,
					ChallengeType = "HTTP",
					Description = new[]
					{
						"The Challenge has been completed -- you can now remove the",
						"file created previously with the following characteristics:",
					},
					HttpDetails = new
					{
						HttpUrl = httpChallenge.FileUrl,
						FilePath = httpChallenge.FilePath,
						FileContent = httpChallenge.FileContent,
						MimeType = "text/plain",
					}
				};

				// Compute the output message
				var message = OutputJson
					? JsonConvert.SerializeObject(json, Formatting.Indented)
					: $@"== {json.Label} ==
  * CleanUp Time:     [{json.CleanUpTime}]
  * Challenge Token:  [{json.ChallengeToken}]

{string.Join(Environment.NewLine, json.Description)}
  * HTTP URL:     [{httpChallenge.FileUrl}]
  * File Path:    [{httpChallenge.FilePath}]
  * File Content: [{httpChallenge.FileContent}]
  * MIME Type:    [text/plain]
------------------------------------
";
				ctx.Out.WriteLine(message);
				_writer?.WriteLine(message);
				_writer?.Flush();
            }
            else if (tlsSniChallenge != null)
            {
                var json = new
                {
                    Label = "Manual Challenge Handler - TLS-SNI",
                    CleanUpTime = DateTime.Now,
                    ChallengeToken = httpChallenge.Token,
                    ChallengeType = "TLS-SNI",
                    Description = new[]
                    {
                        "The Challenge has been completed -- you can now remove the",
                        "self-signed certificates and SNI configuration on your",
                        "web server for the following token:",
                    },
                    TlsSniDetails = new
                    {
                        Token = tlsSniChallenge.Token,
                        IterationCount = tlsSniChallenge.IterationCount,
                    }
                };

                // Compute the output message
                var message = OutputJson
                    ? JsonConvert.SerializeObject(json, Formatting.Indented)
                    : $@"== {json.Label} ==
  * CleanUp Time:     [{json.CleanUpTime}]
  * Challenge Token:  [{json.ChallengeToken}]

{string.Join(Environment.NewLine, json.Description)}
  * Token:        [{json.TlsSniDetails.Token}]
  * Iterations:   [{json.TlsSniDetails.IterationCount}]
------------------------------------
";
                ctx.Out.WriteLine(message);
                _writer?.WriteLine(message);
                _writer?.Flush();
            }
            else
            {
                var ex = new InvalidOperationException("unsupported Challenge type");
                ex.Data["challengeType"] = ctx.GetType();
                throw ex;
            }
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                try
                {
                    _writer.Dispose();
                    _stream.Dispose();
                }
                catch (Exception)
                {
                    // TODO: failure to clean up the prior Out
                    // should do what???
                }
            }

            IsDisposed = true;
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("Manual Challenge Handler is disposed");
        }

        #endregion -- Methods --

        #region -- Types --

        private class DebugStream : Stream
        {
            public override bool CanRead
            { get { return false; } }
            public override bool CanSeek
            { get { return false; } }

            public override bool CanWrite
            { get { return true; } }


            public override long Length
            { get { return 0; } }

            public override long Position
            { get; set; }

            public override void Flush()
            {
                Debug.Flush();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Debug.Write(Encoding.UTF8.GetString(buffer, offset, count));
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }

        #endregion -- Types --
    }
}
