# ACMESharp

An [ACME](https://github.com/letsencrypt/acme-spec) library and client for the .NET platform.

--

For documentation and getting started, go to the [wiki](https://github.com/ebekker/ACMESharp/wiki) which includes the [FAQ](https://github.com/ebekker/ACMESharp/wiki/FAQ).

For announcements and discussions please see go to the **[Community Forums](http://groups.google.com/group/acmesharp)**.

---

[![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/acmesharp)
[![Join the chat at https://gitter.im/ebekker/letsencrypt-win](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/ebekker/letsencrypt-win?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
<a href="https://scan.coverity.com/projects/acmesharp">
  <img alt="Coverity Scan Build Status"
       src="https://scan.coverity.com/projects/7030/badge.svg"/>
</a>


| PS3 | PS4 | PS5 |
------|-----|------
[![PS3 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs3\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) | [![PS4 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs4\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp) | [![PS5 Test Status](https://build.powershell.org/app/rest/builds/buildType:\(id:ACMESharp_InstallTestOnPs5\)/statusIcon.svg)](https://build.powershell.org/externalStatus.html?projectId=ACMESharp)


---

*Please note, this project was formerly named* **`letsencrypt-win`**.

## Related

Check out these other related projects:

* An [alternative simple ACME client for Windows](https://github.com/Lone-Coder/letsencrypt-win-simple) which features:
  * simple usage for common scenarios
  * IIS support
  * automatic renewals
* A [GUI interface](https://github.com/webprofusion/Certify) to this project's PowerShell module
* The official [python ACME client](https://github.com/letsencrypt/letsencrypt) of the [Let's Encrypt] project
* The [ACME specification](https://github.com/ietf-wg-acme/acme) which brings this all together (under development)
* See other [contributions](https://github.com/ebekker/ACMESharp/wiki/Contributions)

---

Jump To:
* [Overview](#overview)
* [Current State](#current-state)
* [Quick Start](https://github.com/ebekker/ACMESharp/wiki/Quick-Start)

This project implements an ACME client library and PowerShell modules interoperable with the [Let's Encrypt](https://letsencrypt.org/) ACME [CA server](https://github.com/letsencrypt/boulder) reference implemention and includes features comparable to the Let's Encrypt [client](https://github.com/letsencrypt/letsencrypt) reference implementation.

The PowerShell modules include installers for configuring:
* IIS 7.0+ either locally or remotely (over PSSession)
* AWS Server Certificates and ELB Listeners

---

## Overview

This ACME client implementation is broken up into layers that build upon each other:
* Basic tools and service required for implementing ACME protocol (JSON Web Signature (JWS), persistence, PKI operations via OpenSSL) (.NET assembly)
* A low-level ACME protocol client that can interoperate with a proper ACME server (.NET assembly)
* A PowerShell Module that implements a "local vault" for managing ACME Registrations, Identifiers and Certificates (PS Binary Module)
* A set of PowerShell Modules that implement installers for various servers/services (PS Script Modules)
  * IIS Installer
  * AWS Installer
  * Future Installers...

This ACME client is being developed against the [Boulder CA](https://github.com/letsencrypt/boulder) ACME server reference implementation.  See how to [quickly spin up your own instance](https://github.com/ebekker/ACMESharp/wiki/Setup-Boulder-CA-on-Amazon-Linux) in AWS on an **Amazon Linux AMI**.

## Current State

This client is now operable and can successfully interact with the Let's Encrypt  [staging CA](https://acme-staging.api.letsencrypt.org/) to initialize new Registrations, authorize DNS Identifiers and issue Certificates.  Further, it can successfully install and configure the certificate and related SSL/TLS settings for a local or remote IIS 7.0+ server or an AWS environment.

## Quick Start

You can find an example of how to get started quickly [here](https://github.com/ebekker/ACMESharp/wiki/Quick-Start).

*Please note, all documentation is still work-in-progress.*

