using System;
using System.Web;
using MAF.Common;
using MAF.Security.Core;

namespace MAF.Security.App
{
    /// <summary>
    /// Web认证员
    /// </summary>
    public class WebAuthenticationService : AuthenticationService
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        internal WebAuthenticationService(Repository repo) : base(repo) { }

        #region SignIn

        protected override void CollectClientInfo(LoginInfo info)
        {
            var ip = HttpContext.Current.Request.UserHostAddress;

            if (info.Ip != ip)
            {
                //TODO: 记录警告日志--IP发生变动
            }
            info.Ip = ip;
        }

        protected override void SignInClient(LoginInfo info)
        {
            // 创建Cookies
            var key = info.IsAnonymous ? SecurityConst.CID : SecurityConst.SID;
            var cookie = new HttpCookie(key, info.Token.ToString());
            if (info.IsRememberMe())
                cookie.Expires = info.ExpireTime;

            // 发送到客户端
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        #endregion

        #region SignOut

        /// <summary>
        /// 客户端注销登录上下文
        /// </summary>
        protected override void SignOutClient()
        {
            SignOutClient(SecurityConst.SID);
        }

        private static void SignOutClient(string key)
        {
            // 创建Cookies
            var cookie = new HttpCookie(key, string.Empty)
            {
                Expires = DateTime.Now.AddYears(-10)
            };

            // 发送到客户端
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        #endregion

        #region Authenticate

        /// <summary>
        /// 认证
        /// </summary>
        /// <param name="key"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <remarks>
        /// 在HttpApplication.AuthenticateRequest事件中调用此方法进行认证。
        /// </remarks>
        protected override bool Authenticate(SecurityKey key, Guid token)
        {
            if (token != Guid.Empty)
            {
                var prin = CreatePrincipal(token);
                if (prin != null)
                {
                    HttpContext.Current.User = prin;
                    return true;
                }
            }

            SignOutClient(key.Value);
            return false;
        }

        protected override Guid GetToken(SecurityKey key)
        {
            return TypeHelper.ConvertTo(WebHelper.GetCookie(key.Value), Guid.Empty);
        }

        #endregion
    }

    /*
    public class WinAuthenticationService : AuthenticationService
    {
        public WinAuthenticationService(Repository repo)
            : base(repo)
        {
        }

        protected override bool SignInClient(MafPrincipal principal)
        {
            // 在服务端保持登录
            Thread.CurrentPrincipal = principal;

            // 给程序集设置线程安全上下文
            //AppDomain.CurrentDomain.SetThreadPrincipal(context);  //理论上上来说此方法与上面给属性赋值应该是等价的，但是单元测试中却无效，何解？

            return true;
        }
    }
     * */
}