using System;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using System.Reflection;
using System.Web;

namespace MAF.Common
{
    /// <summary>
    /// 工具
    /// </summary>
    public class WebHelper
    {
        #region Cookie 方法

        /// <summary>
        ///  类型安全删除Cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        public static void RemoveCookie(string name, HttpContext context)
        {
            if (null == context) { context = HttpContext.Current; }
            if (null != context.Request.Cookies
              && null != context.Request.Cookies[name])
            {
                context.Response.Cookies[name].Value = "";                         // 删除Cookie
                context.Response.Cookies[name].Expires = DateTime.Now.AddDays(-10);
            }
        }

        /// <summary>
        ///  重载,类型安全删除Cookie
        /// </summary>
        /// <param name="name"></param>

        public static void RemoveCookie(string name)
        {
            RemoveCookie(name, null);
        }


        /// <summary>
        ///  类型安全获取Cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>

        public static string GetCookie(string name, HttpContext context)
        {
            if (null == context) { context = HttpContext.Current; }
            return GetCookie(name, context.Request.Cookies);
        }

        /// <summary>
        /// 类型安全获取Cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cookies"></param>
        /// <returns></returns>
        public static string GetCookie(string name, HttpCookieCollection cookies)
        {
            return (null != cookies && null != cookies[name]) ?
                cookies[name].Value :
                null;
        }

        /// <summary>
        ///  重载,类型安全获取Cookie
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        public static string GetCookie(string name)
        {
            return GetCookie(name, HttpContext.Current);
        }

        #endregion

        #region Cache 方法
        /// <summary>
        ///  类型安全删除Cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        public static void RemoveCache(string name, HttpContext context)
        {
            if (null == context) { context = HttpContext.Current; }
            if (!string.IsNullOrEmpty(name)) { context.Cache.Remove(name); }
        }
        /// <summary>
        ///  重载,类型安全删除Cache
        /// </summary>
        /// <param name="name"></param>

        public static void RemoveCache(string name)
        {
            RemoveCache(name, null);
        }

        /// <summary>
        ///  类型安全获取Cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static object GetCache(string name, HttpContext context)
        {
            if (null == context) { context = HttpContext.Current; }
            if (!string.IsNullOrEmpty(name)) { return context.Cache[name]; }
            return null;
        }
        /// <summary>
        ///  重载,类型安全获取Cache
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static object GetCache(string name)
        {
            return GetCache(name, null);
        }
        #endregion
    }
}