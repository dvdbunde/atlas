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

                // PermitTypes — GET (list + single) = Authenticated; POST/PUT/DELETE = Admin
                ("permittypes", "permittypesget")  => "Authenticated",
                ("permittypes", "permittypespost") => "Admin",
                ("permittypes", "permittypesput")  => "Admin",
                ("permittypes", "permittypesdelete") => "Admin",

                // AuditLogs — all operations require Admin
                ("auditlogs", _) => "Admin",

                // Users: GET + Role = Admin; POST = Admin (updated spec)
                ("users", "usersget")  => "Admin",
                ("users", "role")      => "Admin",
                ("users", "userspost") => "Admin", 

                // Documents — authenticated users
                ("documents", _) => "Authenticated",

                // Default: require authentication
                _ => "Authenticated"
            };
        }
    }
}
