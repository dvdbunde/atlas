# Azure Infrastructure

This directory contains the Infrastructure as Code definitions and deployment support files for ATLAS.

## Deployment order

Infrastructure deployment consists of two sequential stages:

1. **Main infrastructure deployment**
   - Run `main.bicep`.
   - This provisions the Azure resources required by the application.

2. **Bootstrap**
   - Run the bootstrap only after `main.bicep` has completed successfully.
   - The bootstrap performs the required post-deployment initialization using the provisioned resources.

Do not run the bootstrap before the main Bicep deployment has completed.

## Directory structure

- `main.bicep` — Main Azure infrastructure deployment.
- `main.parameters.dev.json` — Development environment parameters.
- `bootstrap.ps1` — Post-deployment bootstrap and configuration script.
- `modules/` — Reusable Bicep modules for Azure resources and monitoring.
- `telemetry/` — Grafana dashboard and Azure Workbook definitions.

## Deployment procedure

### 1. Deploy the main infrastructure

Run `main.bicep` using the appropriate subscription, resource group, location, and parameter configuration.

[Exact project-specific command to be documented here.]

Confirm that the deployment completes successfully before continuing.

### 2. Run the bootstrap

After the main infrastructure deployment succeeds, run the bootstrap procedure.

[Exact project-specific bootstrap command to be documented here.]

The bootstrap must not be treated as a replacement for the `main.bicep` deployment; it is a subsequent initialization step.

## Related documentation

- [Environment setup](../docs/runbooks/environment-setup.md)
- [Azure infrastructure architecture](../docs/architecture/azure-infrastructure.md)
- [Operations runbook](../docs/runbooks/operations-runbook.md)
