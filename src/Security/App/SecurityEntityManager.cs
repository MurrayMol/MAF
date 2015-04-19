using System;
using System.Collections.Generic;
using MAF.Security.Core;

namespace MAF.Security.App
{

    /// <summary>
    /// 职责：持久化，事务，安全
    /// 原则：接口隔离
    /// </summary>
    public class SecurityEntityManager : IUserManager, IRoleManager, IPermManager, ILoginInfoManager
    {
        private readonly Repository _repo;

        public SecurityEntityManager(Repository repo)
        {
            _repo = repo;
        }

        #region User

        public void AddUser(MafUser user)
        {
            _repo.Insert(user);
        }

        public void ModifyUser(MafUser user)
        {
            var oldUser = _repo.GetById<MafUser>(user.Name);
            var ch = _repo.NewTracker();
            ch.RegistUpdate(oldUser);
            oldUser = user;
            _repo.Commit(ch);
        }

        public void RemoveUser(string id)
        {
            _repo.Delete<MafUser>(id);
        }

        public IEnumerable<MafUser> QueryUser(Pages page)
        {
            var arg = (page == null) ? null : page.CurrentPage.ToListArgs();
            return _repo.Query<MafUser>(null, arg);
        }

        #endregion

        #region Role

        public void AddRole(MafRole role)
        {
            _repo.Insert(role);
        }

        public void ModifyRole(MafRole role)
        {
            var oldRole = _repo.GetById<MafRole>(role.Name);
            var ch = _repo.NewTracker();
            ch.RegistUpdate(oldRole);
            oldRole = role;
            _repo.Commit(ch);
        }

        public void RemoveRole(string id)
        {
            _repo.Delete<MafRole>(id);
        }

        #endregion

        #region Perm

        public void AddPerm()
        {

        }

        public void ModifyPerm()
        {

        }

        public void RemovePerm()
        {

        }

        #endregion

        #region LoginInfo

        public void DeleteLoginInfo()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LoginInfo> QueryLoginInfo(Pages page)
        {
            var arg = (page == null) ? null : page.CurrentPage.ToListArgs();
            return _repo.Query<LoginInfo>(null, arg);
        }

        #endregion
    }
}
