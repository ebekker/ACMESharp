using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Ext
{
    /// <summary>
    /// Defines meta data used to describe parameters that may be provided for
    /// various stages and entry points in the extension mechanisms.
    /// </summary>
    public class ParameterDetail
    {
        public ParameterDetail(string name, ParameterType type,
                bool isRequired = false, bool isMultiValued = false,
                string label = null, string desc = null)
        {
            Name = name;
            Type = type;
            IsRequired = isRequired;
            IsMultiValued = isMultiValued;
            Label = label;
            Description = desc;
        }

        public string Name
        { get; private set; }

        public ParameterType Type
        { get; private set; }

        public bool IsRequired
        { get; private set; }

        public bool IsMultiValued
        { get; private set; }

        public string Label
        { get; private set; }

        public string Description
        { get; private set; }
    }

    /// <summary>
    /// Defines the different logical types that are supported
    /// for parameters in the extension mechanisms.
    /// </summary>
    public enum ParameterType
    {
        TEXT = 0x1,
        NUMBER = 0x2,
        BOOLEAN = 0x3,

        // TODO:  We were going to support a Secret type
        // that would be passed around as a SecuritString
        // instance, but looking into the future, it looks
        // like that might be going away because of the
        // complexity of supporting it on multiple platforms:
        //    https://github.com/dotnet/corefx/issues/1387
        //SECRET = 0xA,

        /// <summary>
        /// Key-Value pair, with a Text value (and key).
        /// </summary>
        KVP_TEXT = 0x10,
    }
}
