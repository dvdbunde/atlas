using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ATLAS.API.Auth
{
    /// <summary>
    /// Applies [Authorize] policies to NSwag-generated controller actions based on
    /// OpenAPI security requirements. Survives regeneration because it acts on the
    /// ASP.NET Core controller model at startup, not via attributes on generated code.
    /// 
    /// Mapping rules (mirrors openapi/atlas-api.yaml security blocks):
    /// - Applications: GET, POST -> Authenticated; Approve, Reject, RequestInfo, Assign -> OfficerOrAdmin
    /// - PermitTypes: GET (list + single) -> Authenticated; POST, PUT, DELETE -> Admin
    /// - Users: ALL -> Admin
    /// - AuditLogs: ALL -> Admin
    /// - Documents: ALL -> Authenticated
    /// </summary>
    public class GeneratedControllerAuthorizationConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                ApplyActionPolicy(action);
            }
        }

        private static void ApplyActionPolicy(ActionModel action)
        {
            // Skip if [AllowAnonymous] or specific [Authorize] already applied
            if (HasExplicitAuthAttribute(action))
                return;

            var policy = ResolvePolicy(action.Controller.ControllerName, action.ActionName);
            if (policy != null)
            {
                action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
            }
        }

        private static bool HasExplicitAuthAttribute(ActionModel action)
        {
            return action.Attributes.Any(a =>
                a is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute ||
                a is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
        }

        private static string? ResolvePolicy(string controller, string action)
        {
            return (controller.ToLowerInvariant(), action.ToLowerInvariant()) switch
            {
                // Applications
                ("applications", "applicationsget")  => "Authenticated",
                ("applications", "applicationspost") => "Authenticated",
                ("applications", "approve")          => "OfficerOrAdmin",
                ("applications", "reject")           => "OfficerOrAdmin",
                ("applications", "requestinfo")      => "OfficerOrAdmin",
                ("applications", "assign")           => "OfficerOrAdmin",

                // Milestone 5 - Draft workflow
                ("applications", "draft") => "Citizen",           // POST /api/applications/draft (createDraft)
                ("applications", "applicationsput") => "Citizen",  // PUT /api/applications/{id} (updateDraft)
                ("applications", "submit") => "Citizen",           // POST /api/applications/{id}/submit (submitDraft)
                ("applications", "resubmit") => "Citizen",         // POST /api/applications/{id}/resubmit (resubmitApplication)
                ("applications", "dashboard") => "Citizen",        // GET /api/applications/citizen/dashboard (getCitizenDashboard)                

                // PermitTypes — GET (list + single) = Authenticated; POST/PUT/DELETE = Admin
                ("permittypes", "permittypesget")  => "Authenticated",
                ("permittypes", "permittypespost") => "Admin",
                ("permittypes", "permittypesput")  => "Admin",
                ("permittypes", "permittypesdelete") => "Admin",
                // Milestone 5 - Active permit types
                ("permittypes", "active") => "Citizen",            // GET /api/permit-types/active (getActivePermitTypes)

                // AuditLogs — all operations require Admin
                ("auditlogs", _) => "Admin",

                // Users: GET + Role = Admin; POST = Admin (updated spec)
                ("users", "usersget")  => "Admin",                

                // Documents — authenticated users
                ("documents", _) => "Authenticated",

                // Default: require authentication
                _ => "Authenticated"
            };
        }
    }
}
