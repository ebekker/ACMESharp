param(
    [string]$OutputDir
)

$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'

if (-not $OutputDir) {
    $OutputDir = "$PWD\ext_docs"
}

$configFile = (Split-Path -Leaf $PSCommandPath) -replace '.ps1','-config.psd1'
$config = Import-LocalizedData -BaseDirectory $PSScriptRoot -FileName $configFile

## Define each of the extension types we support and their specific attributes
$extMap = [ordered]@{
    handlers = @{
        label    = "Challenge Handlers"
        listCmd  = { ACMESharp\Get-ChallengeHandlerProfile -ListChallengeHandlers }
        subDir   = $config.HandlersDir
        preamble = $config.HandlersPreamble
        formatCmd = { param($ext) Export-ACMEExtensionAsMarkdown -ChallengeHandler $ext }
    }
    installers = @{
        label    = "Installers"
        listCmd  = { ACMESharp\Get-InstallerProfile -ListInstallers }
        subDir   = $config.InstallersDir
        preamble = $config.InstallersPreamble
        formatCmd = { param($ext) Export-ACMEExtensionAsMarkdown -Installer $ext }
    }
    vault = @{
        label    = "Vault Storage"
        listCmd  = {
            ## Forces the load of Vault-related assemblies
            ACMESharp\Get-VaultProfile -ListProfiles | Out-Null
            [ACMESharp.Vault.VaultExtManager]::GetProviderInfos() | Select-Object -ExpandProperty Name
        }
        subDir   = $config.VaultsDir
        preamble = $config.VaultsPreamble
        formatCmd = { param($ext) Export-ACMEExtensionAsMarkdown -Vault $ext }
    }
    decoders = @{
        label    = "Challenge Decoders"
        listCmd  = { ACMESharp\Get-ChallengeHandlerProfile -ListChallengeTypes }
        subDir   = $config.DecodersDir
        preamble = $config.DecodersPreamble
        formatCmd = { param($ext) Export-ACMEExtensionAsMarkdown -ChallengeDecoder $ext }
    }
    pkiTool = @{
        label    = "PKI Tools"
        listCmd  = {
            ## Forces the load of Vault-related assemblies
            ACMESharp\Get-ChallengeHandlerProfile -ListChallengeTypes | Out-Null
            [ACMESharp.PKI.PkiToolExtManager]::GetProviderInfos() | Select-Object -ExpandProperty Name
        }
        subDir   = $config.PkiToolsDir
        preamble = $config.PkiToolsPreamble
        formatCmd = { param($ext) Export-ACMEExtensionAsMarkdown -PkiTool $ext }
    }
}

mkdir $OutputDir -ErrorAction SilentlyContinue | Out-Null
$rootNdx = "$OutputDir\README.md"
Write-Output "ACMESharp Provider Extensions" > $rootNdx
Write-Output $config.RootPreamble >> $rootNdx

$hashNdx = $config.HashManifest
if ($hashNdx) {
    $hashNdx = "$OutputDir\$hashNdx"
    Write-Output $config.HashPreamble > $hashNdx
}

foreach ($extType in $extMap.Keys) {
    $extLabel  = $extMap[$extType].label
    $extList   = & $extMap[$extType].listCmd
    $subDir    = $extMap[$extType].subDir
    $preamble  = $extMap[$extType].preamble
    $formatCmd = $extMap[$extType].formatCmd

    if ($extList) {
        $outDir = "$OutputDir\$subDir"
        mkdir $outDir -ErrorAction SilentlyContinue | Out-Null
        $extNdx = "$outDir\README.md"
        Write-Output "[ACMESharp Provider Extensions](../) > $extLabel" > $extNdx
        Write-Output $preamble >> $extNdx
        Write-Output "* [$extLabel]($subDir/README.md)" >> $rootNdx

        foreach ($ext in $extList) {
            $fname = "$outDir\$ext.md"
            Write-Output "[ACMESharp Provider Extensions](../) > [$extLabel](./) > $ext" > $fname
            & $formatCmd $ext >> $fname
            if ($hashNdx) {
                "``$(Get-FileHash -Algorithm SHA256 $fname |
                        Select-Object -ExpandProperty Hash)`` | ``$subDir/$ext.md``" >> $hashNdx
            }

            Write-Output "* [``$($ext)``]($($ext).md)" >> $extNdx
        }

        if ($hashNdx) {
            "``$(Get-FileHash -Algorithm SHA256 $extNdx |
                    Select-Object -ExpandProperty Hash)`` | ``$subDir/README.md``" >> $hashNdx
        }
    }
}

if ($hashNdx) {
    Write-Output "" >> $rootNdx
    Write-Output "" >> $rootNdx
    Write-Output "[CHECKSUMS]($($config.HashManifest))" >> $rootNdx
    "``$(Get-FileHash -Algorithm SHA256 $rootNdx |
            Select-Object -ExpandProperty Hash)`` | ``README.md``" >> $hashNdx
}
