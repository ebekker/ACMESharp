
#cd C:\prj\letsencrypt\solutions\letsencrypt-win\letsencrypt-win\LetsEncrypt.ACME.POSH
#Add-Type -Path .\bin\Debug\ACMESharp.POSH.dll

<#
Configure/install certs

Install-ACMECertificateToAWS -Ref <cert-ref>
	-IAMPath <path> - optional, prefix with /cloudfront/ to use with CloudFront
	-IAMName <path> - required
	-ELBName <elb-name> - optional to install on ELB
	-ELBPort <elb-port> - required if elb-name is specified
	-VaultProfile <vp>
#>

## We need the AWS POSH Module
Import-Module AWSPowerShell

## TODO:  We'll need to either "assume" that the user has
## already imported the module or explicitly re-import it
## and we'll also have to address the Default Noun Prefix
##Import-Module ACMESharp

function Install-CertificateToAWS {
	param(
		[Parameter(Mandatory=$true)]
		[string]$Certificate,
		[Parameter(Mandatory=$true)]
		[string]$IAMName,
		[string]$IAMPath,
		[switch]$UseWithCloudFront,
		[switch]$IAMReplace,
		[string]$ELBName,
		[int]$ELBPort,

		## AWS POSH Base Params
		[object]$Region,
		[string]$AccessKey,
		[string]$SecretKey,
		[string]$SessionToken,
		[string]$ProfileName,
		[string]$ProfilesLocation,
		[Amazon.Runtime.AWSCredentials]$Credentials
	)

	$vpParams = @{}
	if ($VaultProfile) {
		$vpParams.VaultProfile = $VaultProfile
	}

	## This switch is just a flag that we need to check for the IAM Server
	## Certificate path to match some specific naming convention
	if ($UseWithCloudFront) {
		if (-not $IAMPath.StartsWith('/cloudfront/')) {
			throw "IAM Server Certificate path must start with '/cloudfront/' to use with CloudFront"
		}
	}

	$ci = Get-ACMECertificate @vpParams -Ref $Certificate
	if ($ci.IssuerSerialNumber) {
		$ic = Get-ACMEIssuerCertificate @vpParams -SerialNumber $ci.IssuerSerialNumber
		if ($ic) {
			if (-not $ic.CrtPemFile) {
				throw "Unable to resolve Issuer Certificate PEM file"
			}
		}
	}

	if (-not $ci.KeyPemFile) {
		throw "Unable to resolve Private Key PEM file"
	}
	if (-not $ci.CrtPemFile) {
		throw "Unable to resolve Certificate PEM file"
	}

	$privKeyFile = [System.IO.Path]::GetTempFileName()
	$certBodyFile = [System.IO.Path]::GetTempFileName()

	Get-ACMECertificate @vpParams	-Ref $Certificate `
			-ExportKeyPEM $privKeyFile `
			-ExportCertificatePEM $certBodyFile

	$privKey = [System.IO.File]::ReadAllText($privKeyFile)
	$certBody = [System.IO.File]::ReadAllText($certBodyFile)
	if ($ic) {
		$certChainFile = [System.IO.Path]::GetTempFileName()
		Get-ACMEIssuerCertificate @vpParams -ExportCertificatePEM $certChainFile
		$certChain = [System.IO.File]::ReadAllText($certChainFile)
		del $certChainFile
	}
	del $privKeyFile
	del $certBodyFile


	## Assemble AWS POSH Base Args to pass along for authentication
	$awsBaseArgs = @{
		Region = $Region
		AccessKey = $AccessKey
		SecretKey = $SecretKey
		SessionToken = $SessionToken
		ProfileName = $ProfileName
		ProfilesLocation = $ProfilesLocation
		Credentials = $Credentials
	}

	## -Certificate 1 -Verbose -ProfileName auto@aws3 -IAMName le1
	## -Certificate 1 -Verbose -ProfileName auto@aws3 -IAMName le2 -ELBName foo
	## -Certificate 1 -Verbose -ProfileName auto@aws3 -IAMName le2 -ELBName foo -ELBPort 8443 -Region us-east-1
	## -Certificate 1 -Verbose -ProfileName auto@aws3 -IAMName le2 -ELBName STAGE-PP-MTB -ELBPort 8443 -Region us-east-1

	$awsCert = $null
	$awsCertMeta = $null
	if ($IAMName) {
		try {
			## See if a cert for that name already exists
			$awsCert = Get-IAMServerCertificate -ServerCertificateName $IAMName -ErrorAction Ignore @awsBaseArgs
			if ($awsCert) {
				$awsCertMeta = $awsCert.ServerCertificateMetadata
			}
		} catch { }

		if ($awsCertMeta) {
			if ($IAMReplace) {
				Remove-IAMServerCertificate -ServerCertificateName $IAMName -Force @awsBaseArgs
				$awsCert = $null
				$awsCertMeta = $null
			}
			elseif ($awsCert.CertificateBody.Trim() -ne $certBody.Trim()) {
				throw "Non-matching certificate already installed under referenced Server Certificate Name"
			}
			else {
				Write-Verbose "Matching certificate already installed under referenced Server Certificate Name"
			}
		}

		if (-not $awsCertMeta -or $IAMReplace) {
			$apiArgs = @{
				PrivateKey = $privKey
				CertificateBody = $certBody
				CertificateChain = $certChain
				ServerCertificateName = $IAMName
			}
			if ($IAMPath) {
				$apiArgs.Path = $IAMPath
			}
			$awsCertMeta = Publish-IAMServerCertificate @apiArgs @awsBaseArgs
		}
	}

	if ($ELBName) {
		if (-not $ELBPort -or $ELBPort -lt 1) {
			throw "Invalid or missing ELB port"
		}

		$apiArgs = @{
			LoadBalancerName = $ELBName
			LoadBalancerPort = $ELBPort
			SSLCertificateId = $awsCertMeta.Arn
		}
		echo "SSLCertificate   = $($awsCert)"
		echo "SSLCertificateM  = $($awsCertMeta)"
		echo "SSLCertificateId = $($awsCertMeta.Arn)"
		Set-ELBLoadBalancerListenerSSLCertificate @apiArgs @awsBaseArgs
	}
}

Export-ModuleMember -Function Install-CertificateToAWS
