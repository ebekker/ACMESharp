using System;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    public class BaseCmdlet : Cmdlet
    {
        public object CommandResult { get; set; }

        public object ExecuteCommand()
        {
            this.ProcessRecord();
            return this.CommandResult;
        }

        public new void WriteObject(object sendToPipeline)
        {
            this.CommandResult = sendToPipeline;
            try
            {
                base.WriteObject(sendToPipeline);
            }
            catch (NotImplementedException)
            {
            }
        }

        public new void WriteVerbose(string msg)
        {
            //log
            System.Diagnostics.Debug.WriteLine(msg);

            try
            {
                base.WriteVerbose(msg);
            }
            catch (NotImplementedException) { }
        }
    }
}