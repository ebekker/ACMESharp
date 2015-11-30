namespace ACMESharp.PKI
{
    public enum EncodingFormat
    {
        /// <summary>
        /// Format encoding suitable for human-friendly printing.
        /// </summary>
        PRINT = 0,

        /// <summary>
        /// PEM text encoding.
        /// </summary>
        PEM = 1,

        /// <summary>
        /// DER binary encoding.
        /// </summary>
        DER = 2,
    }

    public enum ArchiveFormat
    {
        /// <summary>
        /// The PCKS#12 (.PFX) format.
        /// </summary>
        PKCS12 = 3,
    }
}
