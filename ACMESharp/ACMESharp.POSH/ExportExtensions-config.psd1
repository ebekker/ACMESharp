@{
    HashManifest = "CHECKSUMS.md"
    HashPreamble = @"

# Computed Checksums for Provider Extensions Reference Docs

These checksums are computed using SHA256 over the auto-generated
reference documentation for Provider Extensions.

| Checksum                                                         |  File
|------------------------------------------------------------------|----------------------------------
"@
    RootPreamble = @"

# Provider Extensions

Here you will find the different types of Provider Extensions that are
currently available for use with ACMESharp.

"@


    HandlersDir = "handlers"
    HandlersPreamble = @"

# Provider Extensions for Challenge Handler

Challenge Handlers are used by ACMESharp to fulfill *Challenges* issued by
an ACME CA server to prove ownership and control of a DNS *Identifier*
(domain name) before the server can generate a signed certificate for that
Identifier.

The ACME protocol defines several standard *Identifier Validation* types
which each have their own protocol and set of procedures to satisfy the
Challenge.  Each of these Validations is also versioned in order to support
evolution of these protocols and procedures.  Examples of some of these
Validations are ``dns-01`` and ``http-01``.

A Provider Extension for Challenge Handling defines which Validation types
it supports and optionally if it supports *cleaning up* after itself --
this may entail removing any residual artifacts or reverting state that
it may have produced in order to complete the Challenge.

"@

    InstallersDir = "installers"
    InstallersPreamble = @"

# Provider Extensions for Installers

Installers are used by ACMESharp to install PKI certificates obtained from
an ACME CA server.  These installers can install the certificate into any
server (such as a Web server, mail server or proxy server) or into a
service (such as a cloud-hosted Web site, CDN service or load balancer).

Installers may optionally support uninstallation as well.

Certificate installation is not technically a part of the ACME protocol as
it takes place after an ACME certificate is issued and exchanged, however
it is a natural extension of the entire process, and therefore *completes
the story* of automated certificate management and exchange.

"@

    VaultsDir = "vaults"
    VaultsPreamble = @"

# Provider Extensions for Vault Storage

*(todo:  describe me)*
"@

    DecodersDir = "decoders"
    DecodersPreamble = @"

# Provider Extensions for Challenge Decoders

*(todo:  describe me)*
"@

    PkiToolsDir = "pkitools"
    PkiToolsPreamble = @"

# Provider Extensions for PKI Tools

*(todo:  describe me)*
"@

}