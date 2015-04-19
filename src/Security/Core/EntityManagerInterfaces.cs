using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAF.Security.Core
{
    public interface IUserManager
    {
        void AddUser(MafUser user);
        void ModifyUser(MafUser user);
        void RemoveUser(string id);

        IEnumerable<MafUser> QueryUser(Pages page);
    }

    public interface IRoleManager
    {
        void AddRole(MafRole role);
        void ModifyRole(MafRole role);
        void RemoveRole(string id);
    }

    public interface IPermManager
    {
        void AddPerm();
        void ModifyPerm();
        void RemovePerm();
    }

    public interface ILoginInfoManager
    {
        void DeleteLoginInfo();

        IEnumerable<LoginInfo> QueryLoginInfo(Pages page);
    }

}
