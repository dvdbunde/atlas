# ATLAS Email Delivery — Azure Communication Services (Phase B)

This document describes the email delivery integration implemented in **Phase B** of the
email work. It builds on the Phase A email-template persistence (customized templates in
Blob Storage, source-code defaults) and replaces the old SMTP production path with
**Azure Communication Services (ACS) Email**.

## Email flow

```txt
ATLAS application
      |
      | IEmailTemplateStore
      v
Resolve customized template (Blob)  --fallback-->  source-code default
      |
      v
Render email (IEmailTemplateRenderer)
      |
      v
IEmailService (email sending abstraction)
      |
      v
Azure Communication Services (ACS Email)
      |
      v
Recipient
```

In Azure, the **Blazor App Service** performs the email send. It authenticates to ACS
using its **system-assigned Managed Identity** (`DefaultAzureCredential`) — no connection
string or access key is stored or used in production.

## Azure resources

- **Azure Communication Services** resource (`Microsoft.Communication/communicationServices`).
- **Email Service** + **Email Domain** (`Microsoft.Communication/emailServices/domains`)
  with `AzureManaged` domain management. The sender address is `DoNotReply@<domain>`.

## Managed Identity & RBAC

- The **API App Service** and **Blazor App Service** system-assigned identities are granted the
  built-in **"Communication and Email Service Owner"** role (`09976791-48a7-449e-bb21-39d1a415f350`)
  scoped to the ACS Email Service.
- The bootstrap applies this RBAC configuration idempotently. The Blazor application performs
  the actual email send; the API identity retains the configured ACS service role as part of
  the environment bootstrap.
- No client secrets or connection strings are introduced for production authentication.

## App Service settings

Both App Services receive:

- `Email__Acs__Endpoint` — the ACS resource endpoint.
- `Email__Acs__SenderAddress` — the verified sender address.

## Application changes

- **`IEmailService`** (Application layer) remains the email-sending abstraction. It is
  Azure-agnostic.
- **`AcsEmailService`** (Infrastructure) implements `IEmailService` using the ACS Email SDK.
- **`AcsEmailClient`** wraps `EmailClient` and authenticates with `DefaultAzureCredential`.
- **`IEmailClient`** is a narrow seam over the ACS SDK for testability.
- **`LocalEmailService`** is a development-only sink that captures emails deterministically
  (logs them) when ACS is not configured.

## SMTP

The old `SmtpEmailService` has been **removed**. There is no SMTP fallback in production.
When ACS is not configured in a development environment, the application uses
`LocalEmailService`.

## Environment guard

`LocalEmailService` is **development-only**. In any non-development environment the
application fails fast at startup if ACS configuration (`Email:Acs:Endpoint` /
`Email:Acs:SenderAddress`) is missing, rather than silently falling back to the local
sink or SMTP.

## Local development

To exercise email locally without production secrets:

1. Leave `Email:Acs:Endpoint` and `Email:Acs:SenderAddress` empty (the default in
   `appsettings.Development.json`).
2. The application resolves `LocalEmailService`, which logs each email (recipient, subject,
   body) so it can be inspected during development.

To test real ACS delivery locally, set `Email:Acs:Endpoint` and `Email:Acs:SenderAddress`
via user-secrets or environment variables (do not commit them).

## Manual Azure setup

- The ACS email domain must be **verified** in the Azure portal (DNS verification) before
  emails can be sent. This cannot be fully automated with Bicep.
- The sender address `DoNotReply@<domain>` must be a valid address on the verified domain.
