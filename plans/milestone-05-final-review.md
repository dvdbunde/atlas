# ATLAS Milestone 05 final review

Documentation Review Findings
ADRs reviewed: ADR-001, ADR-002, ADR-005, ADR-008, ADR-012, ADR-013
PRD sections: F-01 through F-07 (citizen requirements)
Design docs: Context diagram, container diagram, domain model, core entities, data flows, extension points
OpenAPI: Full yaml reviewed — all endpoints, schemas, operationIds
NSwag contracts: AtlasContracts.g.cs fully read — ApplicationSummaryResponse, ApplicationDetailResponse, PermitTypeSummaryResponse, FieldDefinitionResponse, PermitTypeResponse, ApplicationSummaryResponseStatus, FieldDefinitionResponseType
Blazor project: Full structure reviewed — Program.cs, _Imports.razor, App.razor, Routes.razor, MainLayout.razor, NavMenu.razor, .csproj, existing pages
Plans: atlas-foundation-plan.md, Milestone-5-phase-D.md, ROADMAP.md

Key finding for API contract sharing: The NSwag-generated contracts in AtlasContracts.g.cs live in namespace ATLAS.API.Contracts.Generated — inside the ATLAS.API project. The Blazor project currently references ATLAS.Domain and ATLAS.Application but NOT ATLAS.API. This means Blazor cannot directly use the generated contracts without either:

Adding a project reference to ATLAS.API
Creating a shared contracts library
Duplicating DTOs in Blazor
Key finding for PermitType detail retrieval: GET /api/permittypes/{id} returns PermitTypeResponse which does NOT include fields. PermitTypeResponse has: id, name, description, fee, isActive. No Fields array. Only PermitTypeSummaryResponse (from GET /api/permit-types/active) includes fields. This means ApplicationCreate cannot use the getPermitTypeById endpoint for field definitions.

Concern-by-Concern Analysis
CONCERN 1 — API Client Architecture
Evidence:

ATLAS.Blazor.csproj references ATLAS.Domain and ATLAS.Application only — NOT ATLAS.API
AtlasContracts.g.cs lives in namespace ATLAS.API.Contracts.Generated — inside the API project
Blazor _Imports.razor has @using System.Net.Http and @using System.Net.Http.Json — base HTTP is available
No existing API service or typed HttpClient in Blazor
Finding: NSwag-generated clients are NOT directly consumable from Blazor because they're in a different project. A generic ApiClient that deserializes HttpResponseMessage to models manually would require Blazor to define its own DTOs — duplicating what NSwag already generated.

Recommendation: MODIFY

Do NOT create a generic ApiClient with duplicate DTOs
Instead, add a project reference from Blazor to the API project (or extract contracts to a shared class library)
Then create typed service classes (PermitTypeService, ApplicationService) that use HttpClient and return the NSwag-generated types directly

The existing NSwag-generated contracts remain in ATLAS.API for Milestone 5. Extraction to a dedicated ATLAS.Contracts project is deferred until a future milestone demonstrates a clear need for contract sharing beyond the current solution boundaries.

Blazor Server pages communicate directly with the Application layer
through MediatR.

The API layer remains available for external consumers and future
integrations but is not used as the primary communication mechanism
for the Blazor Server UI.

Repositories remain inaccessible from the UI.
All business operations must flow through Commands and Queries.

CONCERN 2 — Route Consistency
Evidence:

Approved plan routes: /permits, /applications/create/{permitTypeId}, /applications/edit/{id}, /dashboard, /applications/{id}, /applications/confirmation/{id}
Current Phase D plan routes: /apply, /apply/{permitTypeId}, /my-applications, /my-applications/{id}, /my-applications/{id}/edit, /confirmation/{id}
Existing Blazor pages use simple single-word routes: /counter, /weather, / (Home)
PRD terminology: "Citizen Dashboard" (F-04), "applications", "permit types"
API endpoint path: /api/applications/citizen/dashboard
Finding: The approved Milestone 5 plan uses /dashboard and /applications/.... The current Phase D plan uses /my-applications and /apply. Neither is inherently wrong, but the Phase D routes deviate from the approved plan without justification. The approved plan's routes are more consistent with PRD terminology (F-04: "view list of their applications").

Recommendation: MODIFY — Use the approved plan routes:

/permits → PermitSelection
/applications/create/{PermitTypeId:guid} → ApplicationCreate
/applications/edit/{ApplicationId:guid} → ApplicationEdit
/dashboard → CitizenDashboard
/applications/{ApplicationId:guid} → ApplicationDetail
/applications/confirmation/{ApplicationId:guid} → ConfirmationPage
CONCERN 3 — AuthService Necessity
Evidence:

Program.cs registers AddAuthenticationCore(), AddAuthorizationCore(), and AddCascadingAuthenticationState()
_Imports.razor does NOT include Microsoft.AspNetCore.Components.Authorization — but AddCascadingAuthenticationState() makes CascadingParameter(AuthenticationState) available
No existing Blazor component uses AuthorizeView or [Authorize]
AuthenticationStateProvider is registered automatically by AddAuthenticationCore()
Finding: AuthenticationStateProvider + CascadingAuthenticationState is already sufficient. An AuthService wrapping AuthenticationStateProvider would be a thin (potentially unnecessary) wrapper. Blazor Server already has the auth infrastructure. Per ADR-005 and ADR-008, authentication flows via Entra ID with role claims — the standard Blazor pattern of AuthorizeView + [Authorize] already handles this.

Recommendation: REJECT AuthService.cs. Use Blazor's built-in:

AuthorizeView with Roles="Citizen" for component-level conditional rendering
[Authorize] attribute on pages for URL-level access control
CascadingParameter(AuthenticationState) for programmatic access to claims
CONCERN 4 — Permit Type Detail Retrieval
Evidence from OpenAPI:

GET /api/permittypes/{id} returns PermitTypeResponse:

GET /api/permit-types/active returns PermitTypeSummaryResponse:

FieldDefinitionResponse:

Finding: GET /api/permittypes/{id} returns PermitTypeResponse which does NOT include field definitions. This endpoint is useless for DynamicFormGenerator. However, GET /api/permit-types/active returns PermitTypeSummaryResponse which DOES include fields.

Recommendation: The ApplicationCreate and ApplicationEdit pages should NOT call GET /api/permittypes/{id}. Instead, they should call GET /api/permit-types/active and filter the result client-side for the specific permitTypeId. This is NOT a blocker — the API surface is sufficient.

CONCERN 5 — DTO / Contract Validation
Evidence from AtlasContracts.g.cs:

✅ ApplicationSummaryResponse — exists (Id, ApplicationNumber, Status as ApplicationSummaryResponseStatus, SubmittedDate, CitizenId, PermitTypeId, CitizenName, PermitTypeName)
✅ ApplicationDetailResponse — exists (inherits ApplicationSummaryResponse + reviewedDate, citizenNotes, officerNotes, documents, reviews, officerName)
✅ PermitTypeSummaryResponse — exists (Id, Name, Description, Fee, Fields as ICollection(FieldDefinitionResponse)
✅ FieldDefinitionResponse — exists (Name, Type as FieldDefinitionResponseType with JsonStringEnumConverter, IsRequired, DefaultValue)
✅ ApplicationSummaryResponseStatus — exists as enum (Draft, Submitted, UnderReview, Approved, Rejected, InfoRequested, Resubmitted)
✅ FieldDefinitionResponseType — exists as enum (Text, MultilineText, Number, Date, Boolean, Dropdown)
✅ CreateDraftRequest — exists (PermitTypeId, CitizenNotes, FieldValues as ICollection(FieldValueRequest))
✅ UpdateDraftRequest — exists (CitizenNotes, FieldValues)
✅ FieldValueRequest — exists (FieldName, Value, SortOrder)
❌ NO C# NSwag-generated client class exists (no partial class ApplicationsClient or similar)
Finding: All referenced DTOs exist. However, they exist in namespace ATLAS.API.Contracts.Generated — inside the ATLAS.API project. The Blazor project does not reference ATLAS.API. They cannot be used without a project reference or shared library.

Recommendation: MODIFY — Add ATLAS.API project reference to ATLAS.Blazor.csproj, or extract contracts to a shared library. The NSwag-generated types are exactly what Blazor needs.

CONCERN 6 — Dashboard Route Strategy
Evidence:

API endpoint: GET /api/applications/citizen/dashboard — uses "dashboard" terminology
PRD F-04: "Citizens can view a list of their submitted applications" — uses "list" terminology
PRD UC4: "Citizen Views Application List" — uses "Application List"
NavMenu currently uses simple single-word routes: /counter, /weather
Approved plan uses /dashboard
Phase D plan uses /my-applications
Finding: The API endpoint is named "dashboard" (citizen dashboard). The PRD describes it as an application list. The approved plan uses /dashboard. "Dashboard" is concise and maps directly to the API operation name ("getCitizenDashboard").

Recommendation: MODIFY — Use /dashboard (from the approved plan), which aligns with the API operationId getCitizenDashboard and is shorter. Alternatively, /applications could work if this is the primary list view.

CONCERN 7 — Dynamic Validation Strategy
Evidence:

FieldDefinitionResponse has IsRequired (boolean) and DefaultValue (string, nullable)
Field definitions come from API response (dynamic), not from data annotations on a compile-time model
DataAnnotationsValidator requires compile-time [Required] attributes on model properties
Dynamic forms render controls based on FieldDefinitionResponseType enum — the fields are not known at compile time
Finding: DataAnnotationsValidator alone is NOT sufficient for dynamic field validation. Required fields are determined by permit type configuration at runtime. You cannot put [Required] on a model property when the model properties are unknown at compile time.

Recommendation: MODIFY — Implement custom validation:

Blazor EditForm with OnValidSubmit and custom ValidationMessageStore
Iterate List(FieldDefinitionResponse) and for each field where IsRequired == true, check if the corresponding value in Dictionary<string, string> is non-empty
Report validation errors via ValidationMessageStore
This is a common pattern for dynamic forms in Blazor
CONCERN 8 — ApplicationTimeline Feasibility
Evidence:

ApplicationDetailResponse has reviews array (type: List(ReviewResponse))
ReviewResponse has: id, officerId, decision (enum: 1=Approved, 2=Rejected, 3=RequestInfo), reasonCode, comments, reviewedDate
Domain model has Application.Status state machine with 7 states
The domain tracks only CURRENT status (not historical transitions)
No StatusHistory entity or StatusChangedEvent collection exists in the domain
AuditLog records contain action type + timestamp, but are for all system actions, not specifically status transitions
Finding: A true timeline of status transitions cannot be rendered from existing API data. The reviews collection captures officer decisions (approve, reject, request info) but there is no record of Draft→Submitted or Submitted→UnderReview transitions. The domain stores only the current status, not the history.

Recommendation: MODIFY — Do NOT implement ApplicationTimeline as a status history component. Instead:

Implement a simpler review activity component that shows officer reviews (approve/reject/request-info entries from the reviews collection)
Display current status prominently using StatusBadge
Add a note: "Status history tracking requires domain event persistence (future enhancement)"
CONCERN 9 — Authorization Enforcement
Evidence:

Program.cs: AddAuthenticationCore(), AddAuthorizationCore(), AddCascadingAuthenticationState()
No existing Blazor component uses AuthorizeView or [Authorize]
Citizen pages must be citizen-only
ADR-005: Blazor Server with Entra ID authentication
ADR-008: Entra ID-first identity
Entra ID roles: Citizen, Officer, Admin
Finding: AddCascadingAuthenticationState() enables both AuthorizeView and [Authorize]. The standard Blazor Server pattern is:

[Authorize(Roles = "Citizen")] on the page component for URL-level access control
AuthorizeView inside the page for conditional rendering of citizen-specific content
Recommendation: GO — Use BOTH patterns:

[Authorize(Roles = "Citizen")] on all citizen pages (blocks non-citizens from navigating to the URL)
AuthorizeView in NavMenu.razor to show/hide citizen links based on auth state
CONCERN 10 — Build Validation Strategy
Evidence:

Solution file: ATLAS.slnx
Multiple projects: Domain, Application, Infrastructure, API, Blazor, + 5 test projects
Repository uses dotnet build --property WarningLevel=0 (from conversation history)
Existing test projects use xUnit
Recommendation:

Build: dotnet build src/ATLAS.Blazor/ATLAS.Blazor.csproj (isolated Blazor build)
Full solution: dotnet build --property WarningLevel=0 (verify no regressions)
Test: dotnet test tests/ATLAS.IntegrationTests/ATLAS.IntegrationTests.csproj (API integration tests)
Verify specific branch: dotnet build (without WarningLevel=0 to catch warnings)
Required Changes Before Phase D Starts

## Change Source Impact

1 Add API project reference to Blazor (or extract shared contracts library) Concern 1, 5 Required — Blazor needs access to AtlasContracts.g.cs types
2 Fix route paths to match approved plan (/dashboard, /applications/...) Concern 2, 6 Required — consistency with approved plan and API naming
3 Remove AuthService.cs from plan Concern 3 Recommended — use built-in Blazor auth framework
4 Update ApplicationCreate to use GET /api/permit-types/active (not GET /api/permittypes/{id}) Concern 4 Required — PermitTypeResponse doesn't have fields
5 Replace DataAnnotationsValidator with custom dynamic validation Concern 7 Required — compile-time attributes can't validate runtime field definitions
6 Replace ApplicationTimeline with simpler review activity display Concern 8 Required — status history data doesn't exist yet
7 Use [Authorize(Roles = "Citizen")] on all citizen pages Concern 9 Required — authorization enforcement
Updated Risk Assessment
Risk Likelihood Impact Status
Blazor can't access NSwag contracts without project reference Certain High 🔴 Must fix — add API reference to Blazor .csproj
ApplicationCreate tries to use endpoint without fields Certain Blocker 🔴 Must fix — use getActivePermitTypes instead
DataAnnotationsValidator fails on dynamic fields Certain Medium 🔴 Must fix — implement custom ValidationMessageStore
ApplicationTimeline has no data to render Certain Medium 🔴 Must fix — switch to review activity component
Routes conflict between plan versions Decided Low 🟡 Must align — use approved plan routes
AuthService duplicates framework Probable Low 🟢 Reject — use built-in Blazor auth
Final GO / NO-GO Decision
NO-GO — Requires Remediation Before Phase D Begins
7 changes are required before implementation can start. The most critical are:

API contract access — Blazor has no way to consume NSwag-generated DTOs without a project reference change
Endpoint mismatch — PermitTypeResponse lacks field definitions; DynamicFormGenerator depends on them
Validation approach — DataAnnotationsValidator cannot handle runtime-defined fields
Timeline feasibility — Status transition history doesn't exist in the current data model
Route alignment — Phase D routes deviate from the approved Milestone 5 plan
The answer to the 6 validation questions:

Question Answer
Can Phase D begin immediately? No
Are any API changes required? No — API surface is sufficient (with correct endpoint usage)
Are any OpenAPI changes required? No
Are any DTO changes required? No — all DTOs exist in NSwag contracts (access is the issue)
Are any missing endpoints required? No — GET /api/permit-types/active provides fields (just use it instead of GET /api/permittypes/{id})
Summary

Concerns resolved: 3 (AuthService usage), 10 (build validation)
Concerns requiring modification: 1 (API client architecture), 2 (routes), 4 (permit type detail), 5 (contract access), 6 (dashboard route), 7 (validation), 8 (timeline), 9 (authorization)
Concerns blocking implementation: 4 (would use wrong endpoint), 7 (validation would silently fail at runtime), 8 (component would render empty)
