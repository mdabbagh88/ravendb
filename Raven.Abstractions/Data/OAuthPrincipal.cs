using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Raven.Abstractions.Data
{
    public class OAuthPrincipal : IPrincipal, IIdentity
    {
        private readonly AccessTokenBody tokenBody;
        private readonly string tenantId;

        public OAuthPrincipal(AccessTokenBody tokenBody, string tenantId)
        {
            this.tokenBody = tokenBody;
            this.tenantId = tenantId;
        }

        public bool IsInRole(string role)
        {
            if ("Administrators".Equals(role, StringComparison.OrdinalIgnoreCase) == false)
                return false;

            var databaseAccess = tokenBody.AuthorizedDatabases
                .Where(x =>
                    string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) ||
                    x.TenantId == "*");

            return databaseAccess.Any(access => access.Admin);
        }

        public IIdentity Identity
        {
            get { return this; }
        }

        public string Name
        {
            get { return tokenBody.UserId; }
        }

        public string AuthenticationType
        {
            get { return "OAuth"; }
        }

        public bool IsAuthenticated
        {
            get { return true; }
        }

        public List<string> GetApprovedResources()
        {
            return tokenBody.AuthorizedDatabases.Select(access => access.TenantId).ToList();
        }

        public AccessTokenBody TokenBody
        {
            get { return tokenBody; }
        }

        public bool IsGlobalAdmin()
        {
            var databaseAccess = tokenBody.AuthorizedDatabases
                .Where(x => string.Equals(x.TenantId, Constants.SystemDatabase, StringComparison.OrdinalIgnoreCase));

            return databaseAccess.Any(access => access.Admin);
		
        }
    }
}