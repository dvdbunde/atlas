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
                ("applications", "getapplications")        => "Authenticated",
                ("applications", "getapplicationbyid")     => "Authenticated",
                ("applications", "approveapplication")     => "OfficerOrAdmin",
                ("applications", "rejectapplication")       => "OfficerOrAdmin",
                ("applications", "requestinfo")             => "OfficerOrAdmin",
                ("applications", "assigntoofficer")         => "OfficerOrAdmin",

                // Milestone 5 - Draft workflow
                ("applications", "createdraft")             => "Citizen",  // POST /api/applications/draft
                ("applications", "updatedraft")             => "Citizen",  // PUT /api/applications/{id}
                ("applications", "submitdraft")             => "Citizen",  // POST /api/applications/{id}/submit
                ("applications", "resubmitdraft")           => "Citizen",  // POST /api/applications/{id}/resubmit
                ("applications", "getcitizendashboard")     => "Citizen",  // GET /api/applications/citizen/dashboard
                ("applications", "getofficerdashboard")     => "OfficerOrAdmin", // GET /api/applications/officer/dashboard

                // PermitTypes — GET (list + single) = Authenticated; POST/PUT/DELETE = Admin
                ("permittypes", "getpermittypes")           => "Authenticated",
                ("permittypes", "createpermittype")         => "Admin",
                ("permittypes", "getpermittypebyid")        => "Authenticated",
                ("permittypes", "updatepermittype")         => "Admin",
                ("permittypes", "deactivatepermittype")     => "Admin",
                // Milestone 5 - Active permit types
                ("permittypes", "getactivepermittypes")     => "Citizen",   // GET /api/permit-types/active

                // AuditLogs — all operations require Admin
                ("auditlogs", _) => "Admin",

                // Users: ALL -> Admin
                ("users", "getusers")  => "Admin",
                ("users", "getuserbyid") => "Admin",

                // Documents — authenticated users
                ("documents", _) => "Authenticated",

                // Default: require authentication
                _ => "Authenticated"
            };
        }
    }
}
