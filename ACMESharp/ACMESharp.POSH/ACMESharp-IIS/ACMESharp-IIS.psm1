
#cd C:\prj\letsencrypt\solutions\letsencrypt-win\letsencrypt-win\ACMESharp.POSH
#Add-Type -Path .\bin\Debug\ACMESharp.POSH.dll

<#
Configure/install certs

Install-ACMECertificateToIIS -Ref <cert-ref>
	-ComputerName <target-server> - optional (defaults to local)
	-Website <website-name> - optional (defaults to 'Default Web Site')
	-HostHeader <hostheader-name> - optional (defaults to none)
	-IPAddress <ip-address> - optional (defaults to all)
	-Port <port-num> - optional (defaults to 443)
	-VaultProfile <vp>
#>

function Install-CertificateToIIS {
	param(
		[Parameter(Mandatory=$true)]
		[string]$Certificate,
		[string]$WebSite = "Default Web Site",
		[string]$IPAddress,
		[int]$Port,
		[string]$SNIHostname,
		[switch]$SNIRequired,
		[switch]$Replace,
		[string]$VaultProfile,

		[System.Management.Automation.Runspaces.PSSession]$RemoteSession
	)

	## TODO:  We'll need to either "assume" that the user has
	## already imported the module or explicitly re-import it
	## and we'll also have to address the Default Noun Prefix
	##Import-Module ACMESharp

	$vpParams = @{}
	if ($VaultProfile) {
		$vpParams.VaultProfile = $VaultProfile
	}

	$ci = Get-ACMECertificate @vpParams -Ref $Certificate
	if ($ci.IssuerSerialNumber) {
		$ic = Get-ACMEIssuerCertificate @vpParams -SerialNumber $ci.IssuerSerialNumber
		if ($ic) {
			if (-not $ic.CrtPemFile) {
				throw "Unable to resolve Issuer Certificate PEM file $($ci.IssuerSerialNumber)"
			}
		}
	}

	if (-not $ci.KeyPemFile) {
		throw "Unable to resolve Private Key PEM file"
	}
	if (-not $ci.CrtPemFile) {
		throw "Unable to resolve Certificate PEM file"
	}

	## Export out the PFX to a local temp file
	$pfxTemp = [System.IO.Path]::GetTempFileName()
	$crt = Get-ACMECertificate @vpParams -Ref $Certificate -ExportPkcs12 $pfxTemp -Overwrite
	if (-not $crt.Thumbprint) {
		throw "Unable to resolve certificate Thumbprint"
	}

	## Assemble a number of arguments and
	## settings based on input parameters
	$webBindingArgs = @{
		Name = $WebSite
		Protocol = "https"
	}
	$sslBinding = @{
		Host = "0.0.0.0"
		Port = "443"
	}

	if ($IPAddress) {
		$webBindingArgs.IPAddress = $IPAddress
		$sslBinding.Host = $IPAddress
	}
	if ($Port) {
		$webBindingArgs.Port = $Port
		$sslBinding.Port = "$Port"
	}
	if ($SNIHostname) {
		$webBindingArgs.HostHeader = $SNIHostname
	}
	
	## We craft a ScriptBlock to do the real work in such a way that we can invoke
	## it locally or remotely based on the right combination of input parameters
	[scriptblock]$script = {
		param(
			[string]$CrtThumbprint,
			[string]$pfxTemp,
			[byte[]]$pfxBytes,
			[bool]$SNIRequired,
			[bool]$Replace,
			[hashtable]$webBindingArgs,
			[hashtable]$sslBinding
		)

		Write-Warning "Params:"
		Write-Warning "  * $CrtThumbprint"
		Write-Warning "  * $pfxTemp"
		Write-Warning "  * $($pfxBytes.Length)"
		Write-Warning "  * $SNIRequired"
		Write-Warning "  * $Replace"
		Write-Warning "  * $webBindingArgs"
		Write-Warning "  * $sslBinding"

		## If we're running locally, then the PFX temp file already exists
		## If we're running remotely, we need to save the PFX bytes to a temp file
		if ($pfxBytes) {
			if (-not $pfxTemp) {
				$pfxTemp = [System.IO.Path]::GetTempFileName()
			}
			[System.IO.File]::WriteAllBytes($pfxTemp, $pfxBytes);
			Write-Verbose "Exported PFX bytes to temp file [$pfxTemp]"
		}

		## Import the PFX file to the local machine store and make sure its there
		## NOTE:  instead of using the native PKI Cert path provider and cmdlets, we're using the
		##        .NET framework directly because it will work on older platforms (Win2008, PS3)
		$crtStore = new-object System.Security.Cryptography.X509Certificates.X509Store "My","LocalMachine"
		$crtBytes = [System.IO.File]::ReadAllBytes($pfxTemp)
		$crt = new-object System.Security.Cryptography.X509Certificates.X509Certificate2
		$crt.Import($crtBytes, $null, "Exportable,PersistKeySet”)
		Write-Verbose "Using certificate [$($crt.Thumbprint)]"
		$crtStore.Open("MaxAllowed")
		$exists = $crtStore.Certificates | ? { $_.Thumbprint -eq $crt.Thumbprint }
		if (-not $exists) {
			Write-Verbose "Importing certificate from PFX [$pfxTemp]"
			$crtStore.Add($crt)
			$exists = $crtStore.Certificates | ? { $_.Thumbprint -eq $crt.Thumbprint }
			if (-not $exists) {
	    		throw "Failed to import Certificate or import was misplaced"
			}
		}
		else {
			Write-Verbose "Existing certificate with matching Thumbprint found; SKIPPING"
		}
		$crtStore.Close()

		## This is used later on for creating the SSL Binding
		$crtPath = "Cert:\LocalMachine\My\$($CrtThumbprint)"

		if (Test-Path $pfxTemp) {
			del $pfxTemp
		}

		## We need the MS Web Admin Module
		Import-Module WebAdministration

		## General guidelines for this procedure were borrowed from:
		##    http://www.iis.net/learn/manage/powershell/powershell-snap-in-configuring-ssl-with-the-iis-powershell-snap-in

		## See if there is already a matching Web Binding
		Write-Verbose "Testing for existing Web Binding"
		$existingWebBinding = Get-WebBinding @webBindingArgs
		if ($existingWebBinding) {
			Write-Warning "Existing Web Binding found matching specified parameters; SKIPPING"
		}
		else {
			$webBindingCreateArgs = @{}
			if ($SNIRequired) {
				$webBindingCreateArgs.SslFlags = 1
			}

			Write-Verbose "Creating Web Binding..."
			New-WebBinding @webBindingArgs @webBindingCreateArgs
			$newWebBinding = Get-WebBinding @webBindingArgs
			if (-not $newWebBinding) {
				throw "Failed to create new Web Binding"
			}
			Write-Verbose "Web Binding was created"
		}

		## See if there is already a matching SSL Binding
		Write-Verbose "Testing for existing SSL Binding"
		$sslBindingPath = "IIS:\SslBindings\$($sslBinding.Host)!$($sslBinding.Port)"
		Write-Verbose "  ...testing for [$sslBindingPath]"
		if (Test-Path -Path $sslBindingPath) {
			if ($Replace) {
				Write-Warning "Deleting existing SSL Binding";
				Remove-Item $sslBindingPath
			}
			else {
				throw "Existing SSL Binding found"
			}
		}
		Write-Verbose "Creating SSL Binding..."
		Write-Verbose "  ...at path [$sslBindingPath]"
		Get-Item $crtPath | New-Item $sslBindingPath
		$newSslBinding = Get-Item $sslBindingPath
		if (-not $newSslBinding) {
			throw "Failed to create new SSL Binding"
		}
		Write-Verbose "SSL Binding was created"
	}

	if ($RemoteSession)
	{
		$pfxBytes = [System.IO.File]::ReadAllBytes($pfxTemp);
		$invArgs = @(
			,$ci.Thumbprint
			,$null ## $pfxTemp
			,$pfxBytes
			,$SNIRequired.IsPresent
			,$Replace.IsPresent
			,$webBindingArgs
			,$sslBinding
		)
		Invoke-Command -Session $RemoteSession -ArgumentList $invArgs -ScriptBlock $script
	}
	else {
		$invArgs = @(
			,$ci.Thumbprint
			,$pfxTemp
			,$null ## $pfxBytes
			,$SNIRequired.IsPresent
			,$Replace.IsPresent
			,$webBindingArgs
			,$sslBinding
		)
		$script.Invoke($invArgs)
	}

	## Delete the local PFX temp file
	if (Test-Path $pfxTemp) {
		del $pfxTemp
	}
}

Export-ModuleMember -Function Install-CertificateToIIS
