using ACMESharp.Ext;
using ACMESharp.Vault;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;

namespace ACMESharp.POSH.Util
{
    public static class PoshHelper
    {
        static PoshHelper()
        {
            // We have to override the base directory used to search for
            // Extension assemblies because under PowerShell, the base
            // directory happens to be where the PowerShell binary is
            // running from, not where the ACMESharp PS Module lives

            var baseDir = Path.GetDirectoryName(typeof(VaultExtManager).Assembly.Location);

            ExtCommon.BaseDirectoryOverride = baseDir;
            ExtCommon.RelativeSearchPathOverride = string.Empty;
        }

        /// <summary>
        /// This routine must be invoked from any CMDLET that relies on the Ext
        /// mechanism when running under POSH, but does not make use of Vault.
        /// </summary>
        public static void BeforeExtAccess()
        {
            // This is a no-op routine but by accessing this from a POSH cmdlet
            // it will force the class constructor to be called which will make
            // sure the Ext mechanism is properly initilized for use under POSH
        }

        public static IDictionary<K,V> Convert<K, V>(this Hashtable h, IDictionary<K, V> d = null)
        {
            if (h == null)
                return d;

            if (d == null)
                d = new Dictionary<K, V>();

            foreach (var k in h.Keys)
                d.Add((K)k, (V)h[k]);

            return d;
        }

        /// <summary>
        /// Defines the well-defined ACME problem detail type URN prefix.
        /// </summary>
        public const string PROBLEM_DETAIL_TYPE_URN = "urn:acme:error:";

        /// <summary>
        /// Defines a mapping from well-defined ACME problem detail type URNs to
        /// POSH <see cref="ErrorCategory">error categories</see>.
        /// </summary>
        /// <remarks>
        /// This list was compiled from:  https://ietf-wg-acme.github.io/acme/#rfc.section.5.4
        /// </remarks>
        public static readonly ReadOnlyDictionary<string, ErrorCategory> PROBLEM_DETAIL_TYPE_TO_ERROR_CATEGORY =
                new ReadOnlyDictionary<string, ErrorCategory>(new Dictionary<string, ErrorCategory>
                        {
                            [$"{PROBLEM_DETAIL_TYPE_URN}badCSR"]         /**/ = ErrorCategory.InvalidArgument,
                            [$"{PROBLEM_DETAIL_TYPE_URN}badNonce"]       /**/ = ErrorCategory.InvalidArgument,
                            [$"{PROBLEM_DETAIL_TYPE_URN}connection"]     /**/ = ErrorCategory.ReadError,
                            [$"{PROBLEM_DETAIL_TYPE_URN}dnssec"]         /**/ = ErrorCategory.SecurityError,
                            [$"{PROBLEM_DETAIL_TYPE_URN}malformed"]      /**/ = ErrorCategory.InvalidData,
                            [$"{PROBLEM_DETAIL_TYPE_URN}serverInternal"] /**/ = ErrorCategory.OperationStopped,
                            [$"{PROBLEM_DETAIL_TYPE_URN}tls"]            /**/ = ErrorCategory.ReadError,
                            [$"{PROBLEM_DETAIL_TYPE_URN}unauthorized"]   /**/ = ErrorCategory.PermissionDenied,
                            [$"{PROBLEM_DETAIL_TYPE_URN}unknownHost"]    /**/ = ErrorCategory.ObjectNotFound,
                            [$"{PROBLEM_DETAIL_TYPE_URN}rateLimited"]    /**/ = ErrorCategory.ResourceBusy,
                        });

        /// <summary>
        /// Constructs an <see cref="ErrorRecord"/> from an <see cref="AcmeClient.AcmeWebException"/>,
        /// populating as much detail as can be derived.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="targetObject"></param>
        /// <returns></returns>
        public static ErrorRecord CreateErrorRecord(AcmeClient.AcmeWebException ex, object targetObject = null)
        {
            // Resolve Error ID
            var errorId = "(n/a)";
            if ((bool)ex.Data?.Contains(nameof(errorId)))
                errorId = ex.Data[nameof(errorId)] as string;
            else if (ex.Response?.ProblemDetail != null)
                errorId = $"{ex.Response.ProblemDetail.Type} ({ex.Response.ProblemDetail.Status})";

            // Resolve Error Category
            var errorCategory = ErrorCategory.NotSpecified;
            var problemType = ex?.Response?.ProblemDetail?.Type;
            if ((bool)ex.Data?.Contains(nameof(errorCategory)))
                errorCategory = (ErrorCategory)ex.Data[nameof(errorCategory)];
            else if (PROBLEM_DETAIL_TYPE_TO_ERROR_CATEGORY.ContainsKey(problemType))
                errorCategory = PROBLEM_DETAIL_TYPE_TO_ERROR_CATEGORY[problemType];

            // Resolve any inner/deeper error message
            ErrorDetails errorDetails = null;
            if (!string.IsNullOrEmpty(ex.Response?.ProblemDetail?.Detail))
                errorDetails = new ErrorDetails(ex.Response.ProblemDetail.Detail);
            else if (ex.InnerException != null)
                errorDetails = new ErrorDetails(ex.InnerException.Message);

            return new ErrorRecord(ex, errorId, errorCategory, targetObject)
            {
                ErrorDetails = errorDetails
            };
        }

        /*
        PS D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp.POSH\bin\TestVault> $Error[0]
        Update-ACMEIdentifier : Unexpected error
        At line:1 char:1
        + Update-ACMEIdentifier -VaultProfile p1 1
        + ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        + CategoryInfo          : NotSpecified: (:) [Update-ACMEIdentifier], AcmeWebException
        + FullyQualifiedErrorId : ACMESharp.AcmeClient+AcmeWebException,ACMESharp.POSH.UpdateIdentifier

        PS D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp.POSH\bin\TestVault> $Error[0] | Format-list -Force


        Exception             : ACMESharp.AcmeClient+AcmeWebException: Unexpected error ---> System.Net.WebException: The remote server returned an error:
                        (404) Not Found.
                            at System.Net.HttpWebRequest.GetResponse()
                            at ACMESharp.AcmeClient.RequestHttpGet(Uri uri) in
                        D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp\AcmeClient.cs:line 629
                            --- End of inner exception stack trace ---
                            at ACMESharp.AcmeClient.RefreshIdentifierAuthorization(AuthorizationState authzState, Boolean useRootUrl) in
                        D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp\AcmeClient.cs:line 314
                            at ACMESharp.POSH.UpdateIdentifier.ProcessRecord() in
                        D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp.POSH\UpdateIdentifier.cs:line 149
                            at System.Management.Automation.Cmdlet.DoProcessRecord()
                            at System.Management.Automation.CommandProcessor.ProcessRecord()
        TargetObject          :
        CategoryInfo          : NotSpecified: (:) [Update-ACMEIdentifier], AcmeWebException
        FullyQualifiedErrorId : ACMESharp.AcmeClient+AcmeWebException,ACMESharp.POSH.UpdateIdentifier
        ErrorDetails          :
        InvocationInfo        : System.Management.Automation.InvocationInfo
        ScriptStackTrace      : at <ScriptBlock>, <No file>: line 1
        PipelineIterationInfo : {}
        PSMessageDetails      :



        PS D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp.POSH\bin\TestVault> $Error[0].Exception.Response


        StatusCode      : NotFound
        Headers         : {Replay-Nonce, Connection, Content-Length, Content-Type...}
        Links           : {}
        RawContent      : {123, 34, 116, 121...}
        ContentAsString : {"type":"urn:acme:error:malformed","detail":"Expired authorization","status":404}
        IsError         : True
        Error           : System.Net.WebException: The remote server returned an error: (404) Not Found.
                        at System.Net.HttpWebRequest.GetResponse()
                        at ACMESharp.AcmeClient.RequestHttpGet(Uri uri) in D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp\AcmeClient.cs:line
                    629
        ProblemDetail   : ACMESharp.Messages.ProblemDetailResponse



        PS D:\prj\letsencrypt\projects\ACMESharp\ACMESharp\ACMESharp.POSH\bin\TestVault> $Error[0].Exception.Response.ProblemDetail


        Type           : urn:acme:error:malformed
        Title          :
        Status         : 404
        Detail         : Expired authorization
        Instance       :
        OrignalContent :
        */

    }
}
