using System;
using System.Collections.Generic;
using System.Reflection;

namespace MAF
{
    public static class TypeHelper
    {
        /// <summary>
        /// 只应该用来转换基本类型
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type type, object defaultVal = null)
        {
            // 不传入类型，直接返回
            if (type == null) return value;

            // 不传入转换值，返回类型的默认值
            if (value == null) return GetDefaultValue(type);

            // 类型一致不需要转换
            if (value.GetType() == type) return value;

            // Nullable类型，把类型转换为实际类型
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            // 枚举类型处理
            if (type.IsEnum) return ConvertToEnum(value, type);

            // GUID类型处理
            if (value is string && type == typeof(Guid))
            {
                Guid guid;
                Guid.TryParse(value as string, out guid);
                return guid;
            }

            // bool类型增加与int类型的转换处理
            if (type == typeof(bool))
            {
                int i;
                if (TryConvertToInt(value, out i))
                {
                    return i != 0;
                }
            }

            // 其他的转换
            try
            {
                return System.Convert.ChangeType(value, type);
            }
            catch
            {
                return defaultVal ?? GetDefaultValue(type);
            }
        }

        public static T ConvertTo<T>(object obj, T defalutValue = default(T))
        {
            return (T)TypeHelper.ChangeType(obj, typeof(T), defalutValue);
        }

        private static bool TryConvertToInt(object value, out int result)
        {
            try
            {
                result = (int)System.Convert.ChangeType(value, typeof(int));
                return true;
            }
            catch
            {
                result = default(int);
                return false;
            }
        }

        private static object ConvertToEnum(object value, Type type)
        {
            if (value is string)
            {
                try
                {
                    return Enum.Parse(type, value as string);
                }
                catch
                {
                    return Activator.CreateInstance(type);
                }
            }
            else
            {
                return Enum.ToObject(type, value);
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (!type.IsValueType) return null;

            if (!_defaultValues.ContainsKey(type))
                _defaultValues[type] = Activator.CreateInstance(type);

            return _defaultValues[type];
        }

        private static readonly Dictionary<Type, object> _defaultValues = new Dictionary<Type, object>();

        #region 扩展方法，可以更方便地调用上面的接口

        public static Guid ToGuid(this string guid)
        {
            return ConvertTo(guid, Guid.Empty);
        }

        #endregion
    }

    /// <summary>
    /// 此类仅供参考，不要使用此类
    /// </summary>
    internal class _TypeHelper
    {
        /// <summary>
        /// 此方法仅供参考，来源：http://www.cnblogs.com/youring2/archive/2012/07/26/2610035.html
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object _ChangeType(object value, Type type)
        {
            if (value == null && type.IsGenericType) return Activator.CreateInstance(type);
            if (value == null) return null;
            if (type == value.GetType()) return value;
            if (type.IsEnum)
            {
                if (value is string)
                    return Enum.Parse(type, value as string);
                else
                    return Enum.ToObject(type, value);
            }
            if (!type.IsInterface && type.IsGenericType)
            {
                Type innerType = type.GetGenericArguments()[0];
                object innerValue = _ChangeType(value, innerType);
                return Activator.CreateInstance(type, new object[] { innerValue });
            }
            if (value is string && type == typeof(Guid)) return new Guid(value as string);
            if (value is string && type == typeof(Version)) return new Version(value as string);
            if (!(value is IConvertible)) return value;
            return System.Convert.ChangeType(value, type);
        }

        /// <summary>
        /// 又一种参考方法，利用反射的转换方法，来源：mcjiffy.cn
        /// </summary>
        /// <typeparam name="T">要转换的基础类型</typeparam>
        /// <param name="val">要转换的值</param>
        /// <returns></returns>
        public static T TryParse<T>(object val, T defaultVal = default(T))
        {
            if (val == null) return default(T);//返回类型的默认值
            Type tp = typeof(T);
            //泛型Nullable判断，取其中的类型
            if (tp.IsGenericType)
            {
                tp = tp.GetGenericArguments()[0];
            }
            if (tp.IsEnum)
            {
                if (!tp.IsEnumDefined(val))
                    return defaultVal;

                if (val is int)
                    return (T)val;

                if (val is string)
                    return (T)Enum.Parse(tp, val.ToString());
            }
            //string直接返回转换
            if (tp.Name.ToLower() == "string")
            {
                return (T)val;
            }
            //反射获取TryParse方法
            var TryParse = tp.GetMethod("TryParse"
                                        , BindingFlags.Public | BindingFlags.Static
                                        , Type.DefaultBinder
                                        , new Type[] { typeof(string), tp.MakeByRefType() }
                                        , new ParameterModifier[] { new ParameterModifier(2) });
            var parameters = new object[] { val, Activator.CreateInstance(tp) };
            bool success = (bool)TryParse.Invoke(null, parameters);
            //成功返回转换后的值，否则返回类型的默认值
            if (success)
            {
                return (T)parameters[1];
            }
            return defaultVal;
        }
    }
}
