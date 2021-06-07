# ACMESharp

An [ACME](https://github.com/letsencrypt/acme-spec) client library and PowerShell client for the .NET platform.

:star: I appreciate your star, it helps me decide to which OSS projects I should allocate my spare time.

---

> Interested in **ACME v2** or **.NET Standard** support?  Check out **[ACMESharp Core](https://github.com/PKISharp/ACMESharpCore)!**

---

Jump To:
* [Overview](#overview)
* [Quick Start](https://github.com/ebekker/ACMESharp/wiki/Quick-Start)
* [Build Status](#build-status)
* [Downloads](#downloads)
* [Current State](#current-state)
* [Related Links](#related-links)

---

**For NEW Documentation for the v0.9.x releases, please see the [new documentation](https://pkisharp.github.io/ACMESharp-docs/)**

For OLD documentation and getting started, go to the [wiki](https://github.com/ebekker/ACMESharp/wiki).

Also, see the the [FAQ](https://github.com/ebekker/ACMESharp/wiki/FAQ).

For announcements and discussions please see one of these:

| | |
|-|-|
| [![Join the chat at https://gitter.im/ebekker/letsencrypt-win](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/ebekker/letsencrypt-win?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) | by Gitter |
| [![Join the forums at http://groups.google.com/group/acmesharp](https://img.shields.io/badge/forums-join_group-4FB999.svg)](http://groups.google.com/group/acmesharp) | by Google Groups |


## Overview

This project implements a *client library* and *PowerShell client* for the ACME protocol.
* ACMESharp is interoperable with the [CA server](https://github.com/letsencrypt/boulder) used by the [Let's Encrypt](https://letsencrypt.org/) project which is the reference implementation for the server-side ACME protocol.
* ACMESharp includes features comparable to the official Let's Encrypt [client](https://github.com/letsencrypt/letsencrypt) which is the reference implementation for the client-side ACME protocol.

The ACMESharp client implementation is broken up into layers that build upon each other:
* basic tools and services required for implementing the ACME protocol and its semantics (JSON Web Signature (JWS), PKI operations, client-side persistence)
* low-level ACME protocol client library that can interoperate with a compliant ACME server
* PowerShell module that implements a powerful client, that functions equally well as a manual tool or a component of a larger automation process, for managing ACME Registrations, Identifiers and Certificates
* collection of *Provider* extensions that implement Challenge Handlers and Installers for various servers/services.

Some of the Providers available for handling ACME challenges and installing certificates include:
* Microsoft IIS 7.0+
* Microsoft Windows (Cert Store, DNS)
* AWS (S3, Route 53, ELB, IAM)
* CloudFlare

## Build Status

| | |
|-|-|
| [![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/acmesharp) | <a href="https://scan.coverity.com/projects/acmesharp"><img alt="Coverity Scan Build Status" src="https://scan.coverity.com/projects/7030/badge.svg"/></a> |

| PS3 | PS4 | PS5 |
|-|-|-|
| [![PS3 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs3\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) | [![PS4 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs4\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) | [![PS5 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs5\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) |

## Downloads

### ACMESharp PowerShell Client Modules

* **If you just want to use ACMESharp to request and install certificates**,
then you want the *ACMESharp PowerShell client*.

* See the
[instructions](https://github.com/ebekker/ACMESharp/wiki/%5BWIP%5D-Installation:-ACMESharp-PowerShell-client)
for installing the PowerShell client and the list of available
[modules](https://github.com/ebekker/ACMESharp/wiki/%5BWIP%5D-Downloads:-PowerShell-Modules).

### ACMESharp NuGet Packages

* **If you are a developer** who wants to embed ACMESharp client libraries in your
own projects or want to develop extensions for ACMESharp, see the list of available
[NuGet Packages](https://github.com/ebekker/ACMESharp/wiki/%5BWIP%5D-Downloads:-NuGet-Packages).

---

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
