using System.Collections.Generic;
using System.Security;
using System.Threading;
using MAF.Security.Core;

namespace MAF.Security.App
{

    /// <summary>
    /// 模式：观察者。单例。
    /// 职责：观察者目标，收集观察者提供的数据，并进行权限判断。
    /// </summary>
    public class MafSecurityContext
    {
        private static MafSecurityContext _current = null;
        private static Dictionary<MafPermission, string> _map = null;
        private static List<IPermRoleMapProvider> _providers = null;

        public static MafSecurityContext Current
        {
            get
            {
                if (_current == null)
                {
                    _map = new Dictionary<MafPermission, string>();
                    foreach (var provider in _providers)
                    {
                        foreach (var perm in provider.GetMap())
                        {
                            _map.Add(perm.Key, perm.Value);
                        }
                    }
                }
                return _current ?? (_current = new MafSecurityContext());
            }
        }

        public static void Append(IPermRoleMapProvider provider)
        {
            if (_providers == null)
                _providers = new List<IPermRoleMapProvider>();

            _providers.Add(provider);
        }

        private static string GetRole(MafPermission perm)
        {
            return _map[perm];
        }

        /// <summary>
        /// 检查是否已经登录
        /// </summary>
        /// <param name="isThrowException"></param>
        /// <returns></returns>
        /// <remarks>
        /// 注意: 在Web系统中不要在HttpApplication.AuthenticateRequest事件中调用此方法，
        ///       要在HttpApplication.AuthorizeRequest中调用。否则将一直返回false。
        ///       因为Thread.CurrentPrincipal必须要在AuthenticateRequest调用完成后才能得到正确的值。
        /// </remarks>
        public bool CheckIsLogin(bool isThrowException = true)
        {
            var isLogin = Thread.CurrentPrincipal.Identity.IsAuthenticated;
            if (isThrowException && !isLogin)
            {
                throw new SecurityException("只有认证用户才能执行此操作");
            }

            return isLogin;
        }

        public bool CheckPermission(MafPermission perm, bool isThrowException = true)
        {
            var role = GetRole(perm);
            var isInRole = Thread.CurrentPrincipal.IsInRole(role);
            if (isThrowException && !isInRole)
            {
                throw new SecurityException("权限不足");
            }
            return isInRole;
        }
    }

    /// <summary>
    /// 权限-角色映射提供者
    /// 模式：观察者
    /// 职责：观察者，自动注册到目标，并返回数据供目标使用
    /// </summary>
    public interface IPermRoleMapProvider
    {
        Dictionary<MafPermission, string> GetMap();
    }

    public class SecurityPermRoleMapProvider : IPermRoleMapProvider
    {
        public Dictionary<MafPermission, string> GetMap()
        {
            return new Dictionary<MafPermission, string>
            {
                {SecurityPermissions.AddUser, "Manager"},
                {SecurityPermissions.AddRole, "Manager"}
            };
        }
    }

    public static class SecurityPermissions
    {
        public static MafPermission AddUser;
        public static MafPermission AddRole;

        static SecurityPermissions()
        {
            AddUser = new MafPermission("Add", "User");
            AddRole = new MafPermission("Add", "Role");
        }
    }
}
