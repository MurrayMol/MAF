namespace MAF.Security.Core
{
    /// <summary>
    /// Interface for the authorization provider
    /// </summary>
    public interface IAuthorizationProvider
    {
        void Authorize(string[] allowedRoles, string[] deniedRoles);
    }
}
