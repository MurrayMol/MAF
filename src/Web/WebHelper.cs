using System;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using System.Reflection;
using System.Web;

namespace MAF.Common
{
    /// <summary>
    /// ����
    /// </summary>
    public class WebHelper
    {
        #region Cookie ����

        /// <summary>
        ///  ���Ͱ�ȫɾ��Cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        public static void RemoveCookie(string name, HttpContext context)
        {
            if (null == context) { context = HttpContext.Current; }
            if (null != context.Request.Cookies
              && null != context.Request.Cookies[name])
            {
                context.Response.Cookies[name].Value = "";                         // ɾ��Cookie
                context.Response.Cookies[name].Expires = DateTime.Now.AddDays(-10);
            }
        }

        /// <summary>
        ///  ����,���Ͱ�ȫɾ��Cookie
        /// </summary>
        /// <param name="name"></param>

        public static void RemoveCookie(string name)
        {
            RemoveCookie(name, null);
        }


        /// <summary>
        ///  ���Ͱ�ȫ��ȡCookie
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
        /// ���Ͱ�ȫ��ȡCookie
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
        ///  ����,���Ͱ�ȫ��ȡCookie
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        public static string GetCookie(string name)
        {
            return GetCookie(name, HttpContext.Current);
        }

        #endregion

        #region Cache ����
        /// <summary>
        ///  ���Ͱ�ȫɾ��Cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        public static void RemoveCache(string name, HttpContext context)
        {
            if (null == context) { context = HttpContext.Current; }
            if (!string.IsNullOrEmpty(name)) { context.Cache.Remove(name); }
        }
        /// <summary>
        ///  ����,���Ͱ�ȫɾ��Cache
        /// </summary>
        /// <param name="name"></param>

        public static void RemoveCache(string name)
        {
            RemoveCache(name, null);
        }

        /// <summary>
        ///  ���Ͱ�ȫ��ȡCache
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
        ///  ����,���Ͱ�ȫ��ȡCache
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