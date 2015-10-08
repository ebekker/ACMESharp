# letsencrypt-win
An [ACME](https://github.com/letsencrypt/acme-spec) client for the Windows platform.

--

Jump To:
* [Overview](#overview)
* 

This project implements an ACME client library and PowerShell modules interoperable with the [Let's Encrypt](https://letsencrypt.org/) ACME [CA server](https://github.com/letsencrypt/boulder) reference implemention and includes features comparable to the Let's Encrypt [client](https://github.com/letsencrypt/letsencrypt) reference implementation.

The PowerShell modules include installers for configuring:
* IIS 7.0+ either locally or remotely (over PSSession)
* AWS Server Certificates and ELB Listeners

--

[![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/letsencrypt-win)


---

## Overview

This ACME client implementation is broken up into layers that build upon each other:
* Basic tools and service required for implementing ACME protocol (JSON Web Signature (JWS), persistence, PKI operations via OpenSSL) (.NET assembly)
* A low-level ACME protocol client that can interoperate with a proper ACME server (.NET assembly)
* A PowerShell Module that implements a "local vault" for managing ACME Registrations, Identifiers and Certificates (PS Binary Module)
* A set of PowerShell Modules that implement installers for various servers/services (PS Script Modules)
  * IIS Installer
  * AWS Installer
  * Futuer Installers...

This ACME client is being developed against the [Boulder CA](https://github.com/letsencrypt/boulder) ACME server reference implementation.  See how to [quickly spin up your own instance](https://github.com/ebekker/letsencrypt-win/wiki/Setup-Boulder-CA-on-Amazon-Linux) in AWS on an **Amazon Linux AMI**.

---

## Current State

This client is now operable and can successfully interact with the Let's Encrypt  [staging CA](https://acme-staging.api.letsencrypt.org/) to initialize new Registrations, authorize DNS Identifiers and issue Certificates.  Further, it can succussfully install and configure the certificate and related SSL settings for a local or remote IIS 7.0+ server or an AWS environment.



## Example Operations

Here is a typical usage scenario based on the _current_ state of the project.

The PS Module uses a local _Vault_ for its state peristenct and management.  The Vault root folder should have appropriate ACLs applied to guard the contents within which include sensitive elements, such as PKI private keys.  The following examples are executed from a PowerShell console:
```powershell
mkdir c:\Vault
cd c:\Vault
Import-Module ACMEPowerShell
```

You initialize the Vault and can optionally specify a base URL endpoint for the ACME Server.  If unspecified, it defaults to the current LE staging CA (after final release, this will default to the LE production CA).
```PowerShell
Initialize-ACMEVault -BaseURI https://acme-staging.api.letsencrypt.org/
```

The first step is to create a new **Registration** with the ACME server, a root account that will own all associated DNS Identifiers and issued Certificates.  Currently it is _assumed_ that there is only one active Registraion in the Vault.  In the future we may support multiple and you'll be able to indiacate a default and/or active one.

The only required argument for a Registration is one or more contacts.  Email contacts must resolve to valid addresses with a valid MX domain.
```
New-ACMERegistration -Contacts mailto:user@domain.com
```

You will get a Terms-of-Service (TOS) Agreement URI which you should review, and then agree to.
```
Update-ACMERegistration -AcceptTOS
```

Next, you need to authorize an **Identifier** with the ACME server which associates it with your Registration and allows you to request certificates.  The only Identifier type supported by the current ACME spec is a DNS-based one, and thus you will only be able to request Domain-Validated (DV) server certificates afterwards.
```
New-ACMEIdentifer -Dns example.com -Alias dns1 -Label "My First DNS Identifier" -Memo "A sample DNS domain"
```
This example also demonstrates the use of a few common options available with most of the POSH module cmdlets that allow you to create or update artifacts in the Vault:
* **```-Alias```** - allows you to assign a unique name for the entity, must conform to regex  ```[A-Za-z][A-Za-z0-9_-/+]{0,49}```
* **```-Label```** - an optional, more-descriptive display name for the entity
* **```-Memo```** - an optional, free-form attribute of notes and comments for the entity

The **Alias** allows you to reference the associated entity in subsequent operations.  Besides the user-assigned Alias, a system generated ID (GUID) is assigned and returned when the entity is created or updated.  To use the ID as a reference identifier, specify it prefixed with the ```=``` character.  Lastly, an entity may also be referenced via its sequential index (zero-based) relative to its create order.  In the example above, we assigned the unique Alias ```dns1``` to the first Identifier we want to authorize.

After you create the new Identifier, it is immediately submitted to the ACME server which responds back with a list of one or more **Challenges** which must be completed in order to prove your ownership and authority over the requested DNS name.  You will also get a list of _combinations_ which indicate what combination of Challenges need to be completed for a succesful authorization.  Today, this ACME client supports the ```dns``` and ```simpleHttp``` Challenge types as described in the ACME spec, _however_, please note that first release of the Let's Encrypt CA (Boulder) implementation will only support ```simpleHttp``` and ```dvsni``` Challenge types, so only the simpleHttp is in common between the LE server and this client.

In order to complete a given Challenge, this client supports the notion of **Providers** which can make necessary configuration changes to DNS or Web servers to satisfy the Challenge.  For each of the two Challenge types that we support (dns and simpleHttp), this client currently supports two Providers.  The first Provider for each is a _manual_ Provider which simply prints out the necessary details that must be manually implemented by the operator.  The other Providers implemented offer an automated approach to completing the Challenge by making use of the AWS Route 53 and S3 services.

To make use of any Provider, you need to create an instance of it and adjust the configuration settings associated with that instance.  In this example, we create an instance of each of the four supported Providers across the two different Challenge types.
```PowerShell
New-ACMEProviderConfig -DnsProvider Manual -Alias manualDnsProvider
New-ACMEProviderConfig -DnsProvider AwsRoute53 -Alias r53DnsProvider
New-ACMEProviderConfig -WebServerProvider Manual -Alias manualHttpProvider
New-ACMEProviderConfig -WebServerProvider AwsS3 -Alias s3HttpProvider
```

When you create a Provider instance it will return back a file path to a configuration file (JSON format) that you should update with the necessary details to let that Provider function.  The manual Providers generally don't have any configuration as they simply print out details that must be configured to the console output.  For the others, you need to provide details such as credentials and paths so that they can execute properly.

You can always see all Providers defined, as well as the current configuration file path of an existing Provider in the Vault.
```
Edit-ACMEProviderConfig -List
Edit-ACMEProviderConfig -Ref s3HttpProvider
```

Here is an example Provider configuration file for the ```AwsS3``` Provider.  After you creat an instance you should edit the configuration file and update as necessary.
```JSON
{
    "Provider": {
        "$type": "LetsEncrypt.ACME.WebServer.AwsS3WebServerProvider, LetsEncrypt.ACME",
        "BucketName": "acmetesting.sample.com",
        "AccessKeyId": "IAM-Account-AccessKey",
        "SecretAccessKey": "IAM-Account-SecretKey",
        "Region": "us-east-1",
        "DnsProvider": {
            "$type": "LetsEncrypt.ACME.DNS.AwsRoute53DnsProvider, LetsEncrypt.ACME",
            "HostedZoneId": "Route53-Hosted-Zone-ID",
            "AccessKeyId": "IAM-Account-AccessKey",
            "SecretAccessKey": "IAM-Account-SecretKey",
            "Region": "us-east-1"
        },
        "DnsCnameTarget":  "star.acmetesting.sample.com"
    }
}
```

Once, the Provider(s) are created and configured, you can complete the Challenges posed by the ACME server.
```PowerShell
Get-ACMEIdentifier -Ref dns1
Complete-ACMEIdentifier -Ref dns1 -Challenge simpleHttp -ProviderConfig s3HttpProvider
```

After you've completed all the Challenges you need to satisfy, you submit your responses for each Challenge type for validation by the ACME server.
```PowerShell
Submit-ACMEChallenge -Ref dns1 -Challenge simpltHttp
```

You can check on the status of a particular Identifier, and you should see the status change from 'pending' to 'valid' if all the Challenges have been satisfied.
```PowerShell
Update-ACMEIdentifier -Ref dns1
```

After an Identifier is authorized, you can create a new certificate request against it.  You can either provide your private key and CSR in PEM format, or have the PS module create new ones for you.
```PowerShell
## Either import existing key/csr PEM files...
New-ACMECertificate -Identifier dns1 -KeyPemFile path\to\key.pem -CsrPemFile path\to\csr.pem -Alias cert1

## ...or generate new ones
New-ACMECertificate -Identifier dns1 -Generate -Alias cert1
```
Then you submit the request and it either gets approved (or denied) immediately, or gets deferred and you can refresh the status after some delay.
```PowerShell
Submit-ACMECertificate -Ref cert1
Update-ACMECertificate -Ref cert1
```

At this point you should have your issued (signed) certificate in the Vault.  You can get at it any time and export various elements in a few different formats.
```PowerShell
Get-ACMECertificate -Ref cert1 `
    -ExportKeyPEM cert1-key.pem `
    -ExportCsrPEM cert1-csr.pem `
    -ExportCertificatePEM cert1-crt.pem `
    -ExportCertificateDER cert1-crt.der `
    -ExportPkcs12 cert1-all.pfx
```

### Installing Certificates

Once you have a certificate issued, you can export the various components as shown in the last example and you can use those any way necessary to install the certificate.  However, this project also includes some automation-supporting installation cmdlets that cater to a few specific server/service use cases.

#### Windows IIS

For IIS 7.0 and greater on Windows 2008 and greater, you can use the IIS installer cmdlet to automatically install the SSL certificate and configure and endpoint on a Web Site.

TODO:

#### Amazon Web Services (AWS)

In AWS, there are several services that make use of customer-provided SSL certificates to host customer content over an SSL/TLS interface.  Some of these include, the Elastic Load Balancer (ELB) service, the CloudFront service, the Elastic Beanstalk service and the OpsWorks service.  (See [here](http://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_server-certs.html) for more details.)

For all of these services, AWS maintains a customer-managed repository of SSL certificates inside the AWS IAM service.  Once a certificate is installed into IAM, it can be referenced by any of the other services listed above.

This ACME client package includes a PowerShell Script Module that allows you to install a PKI certificate into IAM, and optionally to configure an existing ELB listener endpoint to use it.  (The other services need to be manually configured to use an IAM server certificate.)  Note, this module _requires_ the AWSPowerShell module, which is installed as part of the AWS .NET SDK.

```PowerShell
## Make sure you cd to your local Vault root directory
cd c:\Vault

Import-Module ACMEPowerShell
Import-Module ACMEPowerShell-AWS

Install-ACMECertificateToAWS -Certificate cert1 `
        -IAMName myFirstAwsAcmeCert -IAMPath /Optional/Path `
        -ELBName MY-FIRST-ELB -ELBPort 8443
```
Additionally, the installation cmdlet also accepts various combinations of parameters that resolve the user's authentication to the AWS services, the same as all other AWSPowerShell Cmdlets, such as ```-AccessKey```, ```-SecretKey```, ```-Region``` or ```-ProfileName```.
