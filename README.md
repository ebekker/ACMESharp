# letsencrypt-win
An [ACME](https://github.com/letsencrypt/acme-spec) client library and PowerShell module interoperable with the [Let's Encrypt](https://letsencrypt.org/) reference implemention [ACME Server](https://github.com/letsencrypt/boulder) and comparable to the feature set of the corresponding [client](https://github.com/letsencrypt/letsencrypt). 

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
```
mkdir c:\Vault
cd c:\Vault
Import-Module ACMEPowerShell
```

You initialize the Vault and can optionally specify a base URL endpoint for the ACME Server.  If unspecified, it defaults to the current LE staging CA (after final release, this will default to the LE production CA).
```
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

