using System;
using System.Collections.Generic;
using MAF.Security.Core;

namespace MAF.Security.App
{
    /// <summary>
    ///  认证服务
    /// </summary>
    public abstract class AuthenticationService : SignInService
    {
        private readonly Repository _repo;

        internal AuthenticationService(Repository repo)
        {
            _repo = repo;
#if DEBUG
            _repo.Insert(new MafUser("Admin", "123", "123"));
#endif
        }

        #region SignIn

        public bool SignIn(string userName, string psw, bool isRememberMe, List<string> errList = null)
        {
            // 获取用户信息
            var user = _repo.GetById<MafUser>(userName);

            // 登录
            return SignIn(user, psw, isRememberMe, errList) != null;
        }

        protected sealed override void SignInServer(LoginInfo info)
        {
            _repo.Insert(info, null);
        }

        #endregion

        #region SignOut

        public bool SignOut(List<string> errList = null)
        {
            var token = GetToken(SecurityKey.SID);
            var info = _repo.GetById<LoginInfo>(token);
            if (info == null) return false;

            var ch = _repo.NewTracker();   // 注册修改
            ch.RegistUpdate(info, errList);

            info.ForgetMe();
            info.ChangeLastLogOn(DateTime.Now);  // 提交修改

            SignOutClient();
            return _repo.Commit(ch);

        }

        protected abstract void SignOutClient();

        #endregion

        #region Authenticate

        protected MafPrincipal CreatePrincipal(Guid token)
        {
            var info = _repo.GetById<LoginInfo>(token);
            if (info == null)
                return null;

            var user = info.IsAnonymous
                ? new MafAnonymousUser()
                : _repo.GetById<MafUser>(info.UserName);

            return new MafPrincipal(token, user);
        }

        #endregion
    }

    public class AuthorizationService
    {

    }
}