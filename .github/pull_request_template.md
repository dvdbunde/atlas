# Pull Request

## Summary

- What is changing and why?

## Contract Governance

### Contract Changes

- [ ] OpenAPI spec updated (`openapi/atlas-api.yaml`)
- [ ] Breaking change? (check with `openapi diff`)
- [ ] Version bumped? (MAJOR for breaking, MINOR for new features, PATCH for fixes)

### Regeneration

- [ ] `dotnet build` passes (regenerates artifacts)
- [ ] Generated artifacts committed (`GeneratedControllers.g.cs`, `AtlasContracts.g.cs`)
- [ ] No manual edits to generated files

### Manual Implementation

- [ ] Partial controllers updated (`*Controller.partial.cs`)
- [ ] DTO mappings updated (`DtoMappingExtensions.cs`)
- [ ] Integration tests updated
- [ ] Documentation updated (if needed)

### Validation

- [ ] `scripts/validate-contract.ps1` passes locally
- [ ] CI validation passes
- [ ] No merge conflicts

## Links

- ADR/PRD/design doc: (link to docs/ADRs or docs/PRDs or docs/design)

## Quality & Policy Checks

- [ ] Branch/PR rules followed (see `.github/copilot-instructions.md`)
- [ ] Tests added/updated; critical paths & errors at 100% (see Quality Policy)
- [ ] Docs updated (if applicable)
- [ ] Security and privacy considerations noted

## Screenshots / Demos (optional)

## Notes for Reviewers

- Risks, trade-offs, or areas needing extra attention
- Ensure generated files match `openapi/atlas-api.yaml`
- Check for breaking changes (major version bump if breaking)
- Verify partial controllers implement all interface methods


