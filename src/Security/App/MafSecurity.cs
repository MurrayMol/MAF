using System;
using MAF.Security.Core;

namespace MAF.Security.App
{
    internal class SecurityRepoContext : RepoSelector
    {
        public SecurityRepoContext(RepoProviderConf conf) : base(conf) { }

        protected override IRepoProvider SelectProvider(TypeZone zone = null)
        {
            if (typeof(MemoryRepoProvider) == Conf.ProviderType)
                return new MemoryRepoProvider();

            // TODO: 其他的Provider
            return null;
        }
    }

    public class MafSecurity
    {
        private static SecurityRepoContext _repoContext;
        private static SecurityEntityManager _manager;

        /// <summary>
        /// 建立
        /// </summary>
        public static void SetUp(RepoProviderConf repoConf)
        {
            _repoContext = new SecurityRepoContext(repoConf);
            MafSecurityContext.Append(new SecurityPermRoleMapProvider());
        }

        public static IUserManager GetUserManager()
        {
            return GetManager();
        }

        public static IRoleManager GetRoleManager()
        {
            return GetManager();
        }

        public static IPermManager GetPermManager()
        {
            return GetManager();
        }

        public static ILoginInfoManager GetLoginInfoManager()
        {
            return GetManager();
        }

        public static AuthenticationService GetWebAuthenticationService()
        {
            CheckIsRepoPrepare();
            return new WebAuthenticationService(_repoContext.NewRepo());
        }

        private static SecurityEntityManager GetManager()
        {
            CheckIsRepoPrepare();
            return _manager ?? (_manager = new SecurityEntityManager(_repoContext.NewRepo()));
        }

        private static void CheckIsRepoPrepare()
        {
            if (_repoContext == null)
                throw new Exception("调用其他方法之前请先调用SetUp方法");
        }
    }
}
