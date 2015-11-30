# ACMESharp

An [ACME](https://github.com/letsencrypt/acme-spec) library and client for the .NET platform.

[![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/letsencrypt-win)
[![Join the chat at https://gitter.im/ebekker/letsencrypt-win](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/ebekker/letsencrypt-win?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
<a href="https://scan.coverity.com/projects/acmesharp">
  <img alt="Coverity Scan Build Status"
       src="https://scan.coverity.com/projects/7030/badge.svg"/>
</a>

---

Please note, this project was formerly named **`letsencrypt-win`**.

---

## Related

Also check out these other related projects:

* An [alternative simple ACME client for Windows](https://github.com/Lone-Coder/letsencrypt-win-simple) which features:
  * simple usage for common scenarios
  * IIS support
  * automatic renewals
* A [GUI interface](http://webprofusion.com/apps/certify) to this project's PowerShell module
* The official [python ACME client](https://github.com/letsencrypt/letsencrypt) of the [Let's Encrypt] project
* The [ACME specification](https://github.com/ietf-wg-acme/acme) which brings this all together (under development)

---

Jump To:
* [Overview](#overview)
* [Current State](#current-state)
* [Example Usage](#example-usage)

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
  * Futuer Installers...

This ACME client is being developed against the [Boulder CA](https://github.com/letsencrypt/boulder) ACME server reference implementation.  See how to [quickly spin up your own instance](https://github.com/ebekker/letsencrypt-win/wiki/Setup-Boulder-CA-on-Amazon-Linux) in AWS on an **Amazon Linux AMI**.

## Current State

This client is now operable and can successfully interact with the Let's Encrypt  [staging CA](https://acme-staging.api.letsencrypt.org/) to initialize new Registrations, authorize DNS Identifiers and issue Certificates.  Further, it can succussfully install and configure the certificate and related SSL/TLS settings for a local or remote IIS 7.0+ server or an AWS environment.

## Example Usage

The example usage has been moved to its own [wiki](https://github.com/ebekker/letsencrypt-win/wiki/Example-Usage).
