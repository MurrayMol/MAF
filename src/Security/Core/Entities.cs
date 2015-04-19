using System;
using System.Security;
using MAF.Common;

namespace MAF.Security.Core
{
    public class LoginInfo
    {
        private LoginInfo()
        {
            Token = CombGuid.NewGuid();

            var now = DateTime.Now;

            CreateOn = now;
            LastLogOn = CreateOn;
            ExpireTime = now;
        }

        public LoginInfo(MafUser user, string psw, bool isRememberMe)
            : this()
        {
            if (user == null)
                throw new SecurityException("用户名不存在");

            if (user.Password != psw)
                throw new SecurityException("密码错误");

            UserName = user.Name;
            IsAnonymous = typeof(MafAnonymousUser) == user.GetType();
            ExpireTime = isRememberMe ? ExpireTime.AddYears(100) : ExpireTime.AddDays(1);
        }

        [EntityId]
        public Guid Token { get; private set; }
        public string UserName { get; private set; }
        public string Ip { get; set; }
        public bool IsAnonymous { get; private set; }
        public DateTime ExpireTime { get; private set; }
        public DateTime CreateOn { get; private set; }
        public DateTime LastLogOn { get; private set; }

        public void ChangeLastLogOn(DateTime time)
        {
            LastLogOn = time;
        }

        public void RememberMe()
        {
            ExpireTime = DateTime.Now.AddMonths(3);
        }

        public void ForgetMe()
        {
            ExpireTime = DateTime.Now;
        }

        public bool IsExpired()
        {
            return ExpireTime > DateTime.Now;
        }

        public bool IsRememberMe()
        {
            return ExpireTime > DateTime.Now.AddDays(1);
        }
    }

    public class MafUser
    {
        private MafUser() { }

        public MafUser(string name, string psw1, string psw2)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException();
            if (string.IsNullOrWhiteSpace(psw1))
                throw new ArgumentNullException();
            if (psw1 != psw2)
                throw new Exception("两次输入密码不一致");

            Name = name;
            Password = psw1;
        }

        [EntityId]
        public string Name { get; protected set; }

        public string Password { get; protected set; }

        public void ChangePassword(string oldPsw, string newPsw)
        {
            if (oldPsw != Password)
                throw new Exception("旧密码匹配");

            Password = newPsw;
        }
    }

    /// <summary>
    /// 匿名用户
    /// </summary>
    public class MafAnonymousUser : MafUser
    {
        public MafAnonymousUser() : base("Anonymous", "123456", "123456") { }
    }

    /// <summary>
    /// 钥匙用户，系统中没有管理员用户时，此用户激活，反正则禁用。
    /// </summary>
    public class ManagerKeyUser : MafUser
    {

        public ManagerKeyUser()
            : base("ManagerKey", "123456", "123456") { }
    }

    public class MafRole
    {
        private MafRole() { }

        public MafRole(string name, string description)
        {
            Name = name;
            Description = description;
        }

        [EntityId]
        public string Name { get; set; }

        public string Description { get; set; }
    }

    public class MafPermission
    {
        public MafPermission(string action, string resource)
        {
            Action = action;
            Resource = resource;
        }

        public string Action { get; private set; }

        public string Resource { get; private set; }
    }
}

/*
分析模式--关于面向对象建模的思考

对象是什么？属性+方法。面向对象建模的主要工作就是确定对象拥有什么属性，属性应该是什么样子的。
曾经我以为属性是个很简单的东西，例如“人”这个对象，有一个头，双手，双腿，躯干。Over。



刚从一份餐饮软件开发工作中脱身出来，该软件采用“传统三层结构[注1]”开发，即所谓的“失血领域模型[MF]”。

注1：这个名字是一个同事告诉我的，我才知道原来三层结构还成了传统。
 */
