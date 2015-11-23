using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Util
{
    public abstract class BaseManualProvider
    {
        public const string STD_OUT = "OUT";
        public const string STD_ERR = "ERR";

        protected string _WriteOutPath = STD_OUT;
        protected TextWriter _writer = Console.Out;

        public string WriteOutPath
        {
            get { return _WriteOutPath; }
            set
            {
                TextWriter newWriter = null;

                if (string.IsNullOrEmpty(value) || value == STD_OUT)
                    newWriter = Console.Out;
                else if (value == STD_ERR)
                    newWriter = Console.Error;
                else
                {
                    newWriter = new StreamWriter(_WriteOutPath, true);
                }

                if (_writer != null && newWriter != _writer
                        && _writer != Console.Out && _writer != Console.Error)
                {
                    _writer.Close();
                }
                _writer = newWriter;
            }
        }
    }
}
