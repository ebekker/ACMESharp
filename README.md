# letsencrypt-win
An [ACME](https://github.com/letsencrypt/acme-spec) client library and PowerShell module interoperable with the [Let's Encrypt](https://letsencrypt.org/) reference implemention [ACME Server](https://github.com/letsencrypt/boulder) and comparable to the feature set of the corresponding [client](https://github.com/letsencrypt/letsencrypt). 

The PowerShell modules include installers for configuring:
* IIS 7.0+ either locally or remotely (over PSSession)
* AWS Server Certificates and ELB Listeners

--


[![Build status](https://ci.appveyor.com/api/projects/status/0knwrhni528xi2rs?svg=true)](https://ci.appveyor.com/project/ebekker/letsencrypt-win)


---

This ACME implementation is broken up into layers that build:
* Basic, minimum JSON Web Signature (JWS) support that is required for ACME
* A raw ACME protocol client that can interact properly with an ACME server
* PowerShell Cmdlet that can interact with an ACME server and configure a local IIS 7+ configuration


This ACME client is being developed against the [Boulder CA](https://github.com/letsencrypt/boulder) ACME server reference implementation.  See how to [quickly spin up your own instance](https://github.com/ebekker/letsencrypt-win/wiki/Setup-Boulder-CA-on-Amazon-Linux) in AWS on an **Amazon Linux AMI**.
