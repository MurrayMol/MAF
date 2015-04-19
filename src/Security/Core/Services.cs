using System;
using System.Collections.Generic;

namespace MAF.Security.Core
{
    public abstract class SignInService
    {
        #region SignIn

        protected LoginInfo SignIn(MafUser user, string psw, bool isRememberMe, List<string> errList)
        {
            LoginInfo info;

            try
            {
                // 建立登录信息
                info = new LoginInfo(user, psw, isRememberMe);
                CollectClientInfo(info);
            }
            catch (Exception ex)
            {
                if (errList != null) { errList.Add(ex.Message); }
                return null;
            }

            // 记录在服务端
            SignInServer(info);

            // 记录在客户端
            SignInClient(info);

            return info;
        }

        protected abstract void CollectClientInfo(LoginInfo info);

        protected abstract void SignInClient(LoginInfo info);

        protected abstract void SignInServer(LoginInfo info);

        protected LoginInfo SignInAnonymous()
        {
            var user = new MafAnonymousUser();
            return SignIn(user, user.Password, true, null);
        }

        #endregion

        #region Authenticate

        public void Authenticate()
        {
            if (Authenticate(SecurityKey.SID, GetToken(SecurityKey.SID)))
                return;

            if (Authenticate(SecurityKey.CID, GetToken(SecurityKey.CID)))
                return;

            var info = SignInAnonymous();
            Authenticate(SecurityKey.CID, info.Token);
        }

        protected abstract bool Authenticate(SecurityKey key, Guid token);

        protected abstract Guid GetToken(SecurityKey key);

        #endregion
    }
}
