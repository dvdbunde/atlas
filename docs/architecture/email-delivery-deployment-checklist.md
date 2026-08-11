# Phase C — Azure Deployment & Configuration Checklist

This document captures the Phase C verification for the email milestone (Phases A and B)
before Phase D performs the actual Azure deployment and end-to-end testing.

## What was verified as already correct (left unchanged)

- **Storage Account**: the existing ATLAS Storage Account is reused. No new storage
  account is introduced.
- **`email-templates` container**: provisioned via `infra/modules/storage.bicep`, private
  (`publicAccess: None`), name matches `StorageOptions.EmailTemplatesContainer`
  (`"email-templates"`).
- **Storage RBAC**: `Storage Blob Data Contributor`
  (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) is assigned to both API and Blazor App Service
  managed identities, scoped to the storage account. This covers read/create/replace/delete
  of customized templates during Reset. Managed Identity is used; no storage account keys.
- **ACS resource**: `atlas-comm-<env>-<suffix>` via `infra/modules/communicationservices.bicep`.
  Includes the email service and an `AzureManaged` email domain.
- **ACS sender**: `DoNotReply@<domain>`. Emitted as `Email__Acs__SenderAddress`.
- **ACS RBAC**: the **Blazor App Service** (which performs the send) receives the Azure
  Communication Services Email Sender role (`c273bd1b-3068-4be7-9e8e-a081e4d5f5f4`), scoped
  to the ACS resource. The API App Service does **not** receive it.
- **App Service settings** (both App Services): `Storage__AccountName`,
  `Storage__EmailTemplatesContainer`, `Email__Acs__Endpoint`, `Email__Acs__SenderAddress`.
- **Managed Identity**: both App Services use a system-assigned identity.
- **No secrets**: no ACS/storage connection strings, access keys, or passwords are committed.
- **Bicep compiles**: `az bicep build` passes for `main.bicep` and all modules.

## Phase D deployment prerequisites

Before Phase D can consider ACS email fully verified, the following manual step must be
complete:

- **ACS email domain verification**: the `AzureManaged` email domain (default `atlas.com`)
  requires DNS verification in the Azure portal. This cannot be fully completed by Bicep.
  ACS email sending is **not considered fully verified until the required domain
  verification is complete** and the sender address `DoNotReply@<domain>` is operational on
  the verified domain.

## Files involved

- `infra/main.bicep`
- `infra/modules/storage.bicep`
- `infra/modules/communicationservices.bicep`
- `infra/modules/appservice.bicep`
- `infra/modules/names.bicep`
- `infra/main.parameters.dev.json`
