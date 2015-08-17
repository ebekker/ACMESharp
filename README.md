# letsencrypt-win
An ACME client that can update Windows IIS7+ configurations.

This ACME implementation is broken up into layers that build:
* Basic, minimum JSON Web Signature (JWS) support that is required for ACME
* A raw ACME protocol client that can interact properly with an ACME server
* PowerShell Cmdlet that can interact with an ACME server and configure a local IIS 7+ configuration


This ACME client is being developed against the [Boulder CA](https://github.com/letsencrypt/boulder) ACME server reference implementation.  See how to [quickly spin up your own instance](https://github.com/ebekker/letsencrypt-win/wiki/Setup-Boulder-CA-on-Amazon-Linux) in AWS on an **Amazon Linux AMI**.
