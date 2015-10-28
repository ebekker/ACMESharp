# letsencrypt-win
An [ACME](https://github.com/letsencrypt/acme-spec) client for the Windows platform.

>:bangbang: **NOTE:**  At present, due to a limitation a dependency on OpenSSL, you must use the **32-bit version of PowerShell** for any of the PS modules.


[![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/letsencrypt-win)


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
