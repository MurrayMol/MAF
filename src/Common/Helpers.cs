using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Fasterflect;

namespace MAF
{
    public sealed class TemplateHelper
    {
        private const string PropHolder = @"\{.*?\}";

        /// <summary>
        /// 目的：减少循环的编写。
        /// 通过高效反射替换模板中的{var}与属性名匹配字获取属性值。
        /// 如果模板中有需要循环的Select元素怎么办？遇到这种情况，只能手工处理了。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="listContentTemplate"></param>
        /// <returns></returns>
        public static string ForEach<T>(IEnumerable<T> list, string listContentTemplate)
        {
            if (list == null || !list.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var obj in list)
            {
                sb.Append(Replace(obj, listContentTemplate));
            }
            return sb.ToString();
        }

        public static string Replace(object obj, string template)
        {
            var matches = Regex.Matches(template, PropHolder);
            foreach (Match match in matches)
            {
                var replayFlag = match.Value;
                var propName = replayFlag.Trim("{} ".ToCharArray());
                var propVal = RepoHelper.TryGetValue(obj, propName);
                var value = propVal == null ? string.Empty : propVal.ToString();
                template = template.Replace(replayFlag, value);
            }
            return template;
        }
    }

    public sealed class ExceptionHelper
    {
        public static void ThrowBizException(List<string> errList, Action handle = null)
        {
            if (errList == null || errList.Count == 0)
                return;

            if (handle != null) { handle(); }
            throw new Exception(string.Join("/r/n", errList));
        }
    }

    public sealed class Cmd
    {
        private readonly Action _checkPermission;

        public Cmd(Action checkPermission)
        {
            _checkPermission = checkPermission;
        }

        public void Execute(Action<List<string>> action)
        {
            if (_checkPermission != null)
                _checkPermission();

            if (action == null)
                throw new NotImplementedException();

            var errList = new List<string>();
            action(errList);
            ExceptionHelper.ThrowBizException(errList);
        }

        public static bool CheckIsValid(List<string> errList, Action<List<string>> action)
        {
            if (action == null)
                throw new ArgumentNullException();

            var list = new List<string>();
            action(list);

            if (errList != null)
                errList.AddRange(list);

            return !list.Any();
        }
    }

}
