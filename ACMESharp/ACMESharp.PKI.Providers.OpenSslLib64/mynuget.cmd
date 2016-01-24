
@SETLOCAL

@SET THIS=%0
@SET THIS_DIR=%~dp0
@SET NUGET=%THIS_DIR%..\nuget-build.cmd

"%NUGET%" ACMESharp.PKI.Providers.OpenSslLib64
