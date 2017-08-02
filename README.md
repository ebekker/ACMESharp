# ACMESharp

An [ACME](https://github.com/letsencrypt/acme-spec) library and client for the .NET platform.

---

Jump To:
* [Overview](#overview)
* [Quick Start](https://github.com/ebekker/ACMESharp/wiki/Quick-Start)
* [Current State](#current-state)
* [Related Links](#related-links)

---

For documentation and getting started, go to the [wiki](https://github.com/ebekker/ACMESharp/wiki) which includes the [FAQ](https://github.com/ebekker/ACMESharp/wiki/FAQ).

For announcements and discussions please see one of these:
| | |
|-|-|
| [![Join the chat at https://gitter.im/ebekker/letsencrypt-win](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/ebekker/letsencrypt-win?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) | by Gitter |
| [![Join the forums at http://groups.google.com/group/acmesharp](https://img.shields.io/badge/forums-join_group-4FB999.svg)](http://groups.google.com/group/acmesharp) | by Google Groups |


## Downloads


### PowerShell Modules

| Stable Modules | (powershellgallery.com) |
|-|-|
| [![Powershellgallery Badge][psgallery-badge]][psgallery-status] | ACMESharp - complete 0.8.1 distribution

[psgallery-badge]: https://img.shields.io/badge/PowerShell_Gallery-LATEST-green.svg
[psgallery-status]: https://www.powershellgallery.com/packages/ACMESharp

| Early Access | (myget.org) |
|-|-|
| [![MyGet](https://img.shields.io/myget/acmesharp-posh-staging/v/ACMESharp.svg)](https://www.myget.org/feed/acmesharp-posh-staging/package/nuget/ACMESharp) | ACMESharp base module
| [![MyGet](https://img.shields.io/myget/acmesharp-posh-staging/v/ACMESharp.Providers.AWS.svg)](https://www.myget.org/feed/acmesharp-posh-staging/package/nuget/ACMESharp.Providers.AWS) | Provider Module - AWS
| [![MyGet](https://img.shields.io/myget/acmesharp-posh-staging/v/ACMESharp.Providers.CloudFlare.svg)](https://www.myget.org/feed/acmesharp-posh-staging/package/nuget/ACMESharp.Providers.CloudFlare) | Provider Module - CloudFlare
| [![MyGet](https://img.shields.io/myget/acmesharp-posh-staging/v/ACMESharp.Providers.IIS.svg)](https://www.myget.org/feed/acmesharp-posh-staging/package/nuget/ACMESharp.Providers.IIS) | Provider Module - Microsoft IIS
| [![MyGet](https://img.shields.io/myget/acmesharp-posh-staging/v/ACMESharp.Providers.Windows.svg)](https://www.myget.org/feed/acmesharp-posh-staging/package/nuget/ACMESharp.Providers.Windows) | Provider Module - Microsoft Windows

### .NET Packages - Client Libs for developers

| Stable Packages | (nuget.org) |
|-|-|
| [![NuGet](https://img.shields.io/nuget/v/ACMESharp.svg)](https://www.nuget.org/packages/ACMESharp) | ACMESharp client library
| [![NuGet](https://img.shields.io/nuget/v/ACMESharp.Vault.svg)](https://www.nuget.org/packages/ACMESharp.Vault) | ACMESharp Vault library
| [![NuGet](https://img.shields.io/nuget/v/ACMESharp.POSH.svg)](https://www.nuget.org/packages/ACMESharp.POSH) | ACMESharp POSH library
| [![NuGet](https://img.shields.io/nuget/v/ACMESharp.PKI.Providers.OpenSslLib32.svg)](https://www.nuget.org/packages/ACMESharp.PKI.Providers.OpenSslLib32) | ACMESharp 32-bit dependency for OpenSSL Cert Provider
| [![NuGet](https://img.shields.io/nuget/v/ACMESharp.PKI.Providers.OpenSslLib64.svg)](https://www.nuget.org/packages/ACMESharp.PKI.Providers.OpenSslLib64) | ACMESharp 64-bit dependency for OpenSSL Cert Provider

| Early Access | (myget.org) |
|-|-|
[![MyGet](https://img.shields.io/myget/acmesharp/v/ACMESharp.svg)](https://www.myget.org/feed/acmesharp/package/nuget/ACMESharp) | Coming Soon!

## Build Status

| | |
|-|-|
| [![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/acmesharp) | <a href="https://scan.coverity.com/projects/acmesharp"><img alt="Coverity Scan Build Status" src="https://scan.coverity.com/projects/7030/badge.svg"/></a> |

| PS3 | PS4 | PS5 |
|-|-|-|
| [![PS3 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs3\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) | [![PS4 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs4\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) | [![PS5 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs5\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) |

---

## Overview

This project implements an ACME client library and PowerShell modules interoperable with the [Let's Encrypt](https://letsencrypt.org/) ACME [CA server](https://github.com/letsencrypt/boulder) reference implementation and includes features comparable to the Let's Encrypt [client](https://github.com/letsencrypt/letsencrypt) reference implementation.

This ACME client implementation is broken up into layers that build upon each other:
* Basic tools and service required for implementing ACME protocol (JSON Web Signature (JWS), persistence, PKI operations via OpenSSL) (.NET assembly)
* A low-level ACME protocol client that can interoperate with a proper ACME server (.NET assembly)
* A PowerShell Module that implements a "local vault" for managing ACME Registrations, Identifiers and Certificates (PS Binary Module)
* A set of PowerShell Modules that implement installers for various servers/services (PS Script Modules)
  * IIS Installer
  * AWS Installer
  * Future Installers...

The PowerShell modules include installers for configuring:
* IIS 7.0+ either locally or remotely (over PSSession)
* AWS Server Certificates and ELB Listeners

## Current State

This client is fully operable and can successfully interact with the Let's Encrypt production and staging servers to:
* Initialize new Registrations
* Authorize DNS Identifiers
* Issue Certificates

Further, it can successfully install and configure the certificate and related SSL/TLS settings for various local (e.g. IIS, Cert Store) or remote (e.g. AWS, CloudFlare) servers or services.

*All documentation is still work-in-progress.*

## Quick Start

You can find an example of how to get started quickly [here](https://github.com/ebekker/ACMESharp/wiki/Quick-Start).

---

## Related Links

Check out these other related projects and resources:
* For a great intro and overview of Let's Encrypt, ACME and related tech, check out this [Changelog podcast](https://changelog.com/podcast/243) with [Jacob Hoffman-Andrews](https://github.com/jsha), the lead developer of LE
* An [alternative simple ACME client for Windows](https://github.com/Lone-Coder/letsencrypt-win-simple) which features:
  * simple usage for common scenarios
  * IIS support
  * automatic renewals
* A [GUI interface](https://github.com/webprofusion/Certify) to this project's PowerShell module
* The official [python ACME client](https://github.com/letsencrypt/letsencrypt) of the [Let's Encrypt] project
* The [ACME specification](https://github.com/ietf-wg-acme/acme) which brings this all together (under development)
* See other [contributions](https://github.com/ebekker/ACMESharp/wiki/Contributions)

This ACME client is being developed against the [Boulder CA](https://github.com/letsencrypt/boulder) ACME server reference implementation.  See how to [quickly spin up your own instance](https://github.com/ebekker/ACMESharp/wiki/Setup-Boulder-CA-on-Amazon-Linux) in AWS on an **Amazon Linux AMI**.

---

*Please note, this project was formerly named* **`letsencrypt-win`**.
