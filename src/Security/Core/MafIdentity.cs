using System;
using System.Collections.Generic;
using System.Security.Principal;
using MAF.Common;

namespace MAF.Security.Core
{
    /// <summary>
    /// 用户令牌，此类是个值对象
    /// 存储于客户端，用来标识一个MafPrincipal
    /// </summary>
    [Serializable]
    public class MafToken
    {
        public static MafToken New(string ip)
        {
            return new MafToken() { Ip = ip };
        }

        public static MafToken Get(string id, string ip)
        {
            return new MafToken()
            {
                Id = TypeHelper.ConvertTo(id, Guid.Empty),
                Ip = id
            };
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        private MafToken()
        {
            Id = CombGuid.NewGuid(); // 生成TokenId
        }

        /// <summary>
        /// 令牌(本对象的Id)
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// 登录IP地址
        /// </summary>
        public string Ip { get; private set; }
    }

    /// <summary>
    /// 用户标识
    /// </summary>
    public class MafIdentity : IIdentity
    {
        public MafUser User { get; protected set; }

        private MafIdentity()
        {
            AuthenticationType = "Web";
        }

        internal MafIdentity(MafUser user)
            : this()
        {
            IsAuthenticated = user.GetType() != typeof(MafAnonymousUser);

            Name = user.Name;
            User = user;
        }

        /// <summary>
        /// 用户Id
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 授权类型，此属性可以持久化到数据源，用来在登录时区分不同的认证类型
        /// </summary>
        public string AuthenticationType { get; private set; }

        /// <summary>
        /// 是否已认证
        /// </summary>
        public bool IsAuthenticated { get; private set; }
    }

    /// <summary>
    /// 用户主体
    /// </summary>
    public class MafPrincipal : IPrincipal
    {
        public MafPrincipal(Guid token, MafUser user)
        {
            Token = token;
            Identity = new MafIdentity(user);
        }

        public Guid Token { get; private set; }

        public IIdentity Identity { get; private set; }

        public bool IsInRole(string role)
        {
            var list = GetRoles();
            return list.Find(o => o.Name == role) != null;
        }

        public object GetUserInfo()
        {
            return null;
        }

        public List<MafRole> GetRoles()
        {
            return new List<MafRole>()
            {
                new MafRole("Admin","系统管理员"),
                new MafRole("Manager","管理员")
            };
        }
    }
}
