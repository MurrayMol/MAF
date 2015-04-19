/*
 * Repositoy
 *      是DDD的资源库模式的一种实现；
 *      同时实现了“工作单元”模式；
 *      为内存存储和持久化存储提供一致的接口；
 *      以聚合根类型为最小存储单位，当然这只是个约定。
 * IRepoProvider
 *      数据存储与检索接口；
 *      它的实现为Repo提供数据存储能力；
 *      通过它的不同实现可以自由地切换ORM或数据库。
 * MemoryRepoProvider
 *      一个IRepoProvider的内存存储实现；
 *      使用它可以推迟考虑数据的持久化，适合于快速验证一些复杂的功能；
 *      还可以用来进行单元测试。
 *      
 * RepoTracker
 *      用来跟踪对象的新增/修改/删除；
 *      通过Repositoy.Commit把变更提交到存储。
 * Exp
 *      查询条件类，提供内存与持久化一致的查询接口
 * 
 * 用法：
 * var repo = new Repository(IRepoProvider);
 * 
 * C/U/D
 * var tracker = repo.NewTracker()；
 * tracker.Insert(object);
 * tracker.RegistUpdate(object);
 * tracker.Delete<Type>(id);
 * repo.Commit(tracker);
 * 
 * R
 * repo.GetById<Type>(id);          // Id查询
 * repo.Query<Type>(Exp,ListArgs);  // 条件查询，分页排序
 * repo.Query<Type>(FnQuery,Exp);   // 聚合函数查询
 */

using System.Collections.Specialized;
using System.Reflection;
using Fasterflect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MAF
{
    #region 存储库

    /// <summary>
    /// 存储库。注意：每个实例代表一个数据库连接，每个实例都应该管理起来。
    /// </summary>
    public sealed class Repository
    {
        private readonly IRepoProvider _provider;

        public Repository(IRepoProvider provider)
        {
            _provider = provider;
        }

        public RepoTracker NewTracker()
        {
            return new RepoTracker();
        }

        public bool Commit(RepoTracker changes, List<string> errList = null)
        {
            var succ = _provider.Commit(changes.GetSessionData(), errList);
            changes.Complete();
            return succ;
        }

#if DEBUG
        public void Clear<T>()
        {
            _provider.Clear<T>();
        }
#endif

        public T GetById<T>(object id)
        {
            return _provider.GetById<T>(id);
        }

        public IEnumerable<T> Query<T>(Exp exp = null, ListArgs arg = null)
        {
            return _provider.Query<T>(exp, arg);
        }

        public object Query<T>(FnQuery fn, Exp exp = null)
        {
            return _provider.Query<T>(fn, exp);
        }
    }

    /// <summary>
    /// 存储库扩展方法
    /// </summary>
    public static class RepositoryExtends
    {
        public static bool Insert(this Repository repo, object obj, List<string> errList = null)
        {
            var ss = repo.NewTracker();
            var succ = ss.Insert(obj, errList);
            return succ && repo.Commit(ss, errList);
        }

        private static bool Update(this Repository repo, object obj, List<string> errList = null)
        {
            var ss = repo.NewTracker();
            ss.RegistUpdate(obj, errList);
            return repo.Commit(ss, errList);
        }

        public static bool Delete<T>(this Repository repo, object id, List<string> errList = null)
        {
            var ss = repo.NewTracker();
            ss.Delete<T>(id);
            return repo.Commit(ss, errList);
        }
    }

    /// <summary>
    /// 资源库变更跟踪器。实现工作单元模式(Unit of work)。
    /// </summary>
    public class RepoTracker
    {
        private readonly EntityCollection _tranObjects = null;

        internal RepoTracker()
        {
            _tranObjects = new EntityCollection();
        }

        public bool Insert(object obj, List<string> errList = null)
        {
            if (obj == null)
            {
                if (errList != null) { errList.Add("参数不能为null"); }
                return false;
            }

            if (!CheckIsValid(obj, errList))
                return false;

            var entity = Entity.CreateInsertEntity(obj);
            _tranObjects.Add(entity);
            return true;
        }

        public void RegistUpdate(object obj, List<string> errList = null)
        {
            Update(obj, errList);
        }

        /// <summary>
        /// RegistUpdate方法会把被保存对象DeepClone一份记录下来，当被保存的对象与其他对象关联太深时，
        /// 不可能把一整块关联对象都保存起来，这就需要产生一个值对象(DTO)来进行保存，这时，业务对象与
        /// 最终被保存的对象不一致，所以需要一个RegistUpdate+(更改过程)+Update两个方法来记录变更过程
        /// （为了做到这更新部分字段）， 如果业务对象与最终被保存对象一致的情况，两个方法的效果一样的，
        /// 只需要在更新前执行Update方法。
        /// 例如:TreeNode对象，里面包含了Tree，如果直接保存TreeNode对象，会连整个Tree都被保存了。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="errList"></param>
        private void Update(object obj, List<string> errList = null)
        {
            if (obj == null) { return; }

            if (!CheckIsValid(obj, errList))
                return;

            var val = _tranObjects.GetById(RepoHelper.GetIdValue(obj), obj.GetType());
            if (val == null)
            {
                var entity = Entity.CreateUpdateEntity(obj);
                _tranObjects.Add(entity);
            }
            else
            {
                val.UpdateData(obj);
            }

        }

        private bool CheckIsValid(object obj, List<string> errList)
        {
            var valid = obj as IValidatable;
            return valid == null || valid.CheckIsValid(errList);
        }

        public void Delete<T>(object id)
        {
            _tranObjects.Add(Entity.CreateDeleteEntity(id, typeof(T)));
        }

        public void Complete()
        {
            _tranObjects.RemoveAll();
        }

        internal List<Entity> GetSessionData()
        {
            return _tranObjects.QueryAll();
        }
    }

    /// <summary>
    /// 存储库提供者
    /// </summary>
    public interface IRepoProvider
    {
        bool Commit(List<Entity> entities, List<string> errList = null);
        T GetById<T>(object id);
        IEnumerable<T> Query<T>(Exp exp, ListArgs arg);
        object Query<T>(FnQuery fn, Exp exp);
#if DEBUG
        void Clear<T>();
#endif
    }

    #endregion

    #region EntityCollection

    /// <summary>
    /// 内存数据集合：一个简单的内存数据库，实现以下功能
    /// 1. 可以像IList一样Add
    /// 2. 可以通过数据的Id删除（默认名字是Id）
    /// 3. 可以通过数据字段[或其子对象字段]作为条件批量删除？？？
    /// 4. 可以通过数据字段[或其子对象字段]作为条件批量查找
    /// 5. 不能够进行持久化，否则就会产生数据类型修改了，但是持久化的数据结构没有同步变化问题。
    /// </summary>
    public class EntityCollection
    {
        /// <summary>
        /// Key=类型
        /// </summary>
        private readonly Dictionary<Type, EntityTable> _data;

        /// <summary>
        /// 构造方法
        /// </summary>
        public EntityCollection()
        {
            _data = new Dictionary<Type, EntityTable>();
        }

        /// <summary>
        /// 添加，修改
        /// </summary>
        /// <param name="row"></param>
        public void Add(Entity row)
        {
            if (null == row)
                throw new ArgumentNullException("参数row不能为null");

            if (!_data.ContainsKey(row.DataType))
                _data[row.DataType] = new EntityTable();

            _data[row.DataType].Add(row);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public void Remove(object id, Type dataType)
        {
            if (_data.ContainsKey(dataType))
                _data[dataType].Remove(id);
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void RemoveAll()
        {
            _data.Clear();
        }

        /// <summary>
        /// 根据Id查找
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Entity GetById(object id, Type type)
        {
            return !_data.ContainsKey(type) ? null : _data[type].GetById(id);
        }

        /// <summary>
        /// 根据条件查找
        /// </summary>
        /// <param name="type"></param>
        /// <param name="exp"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public List<Entity> Query(Type type, Exp exp, ListArgs arg)
        {
            return !_data.ContainsKey(type) ? new List<Entity>() : _data[type].Query(exp, arg);
        }

        public List<Entity> QueryAll()
        {
            var list = new List<Entity>();
            foreach (var table in _data)
            {
                list.AddRange(table.Value.Query(null, null));
            }
            return list;
        }

        public int Count(Type type, Exp exp = null)
        {
            return Query(type, exp, null).Count;
        }

        /// <summary>
        /// EntityTable类
        /// </summary>
        public class EntityTable
        {
            /// <summary>
            /// Key=MemDataRow.Data.Id.ToString()
            /// </summary>
            private readonly Dictionary<object, Entity> _data;

            /// <summary>
            /// 构造方法
            /// </summary>
            public EntityTable()
            {
                _data = new Dictionary<object, Entity>();
            }

            /// <summary>
            /// 添加，修改
            /// </summary>
            /// <param name="row"></param>
            public void Add(Entity row)
            {
                if (null == row)
                    throw new ArgumentNullException("参数row不能为null");

                _data[row.Id] = row;
            }

            /// <summary>
            /// 删除
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public void Remove(object id)
            {
                _data.Remove(id);
            }

            /// <summary>
            /// 清除所有数据
            /// </summary>
            public void RemoveAll()
            {
                _data.Clear();
            }

            /// <summary>
            /// 根据Id查找
            /// </summary>
            /// <param name="id"></param>
            /// <param name="type"></param>
            /// <returns></returns>
            public Entity GetById(object id)
            {
                return _data.ContainsKey(id) ? _data[id] : null;
            }

            /// <summary>
            /// 根据条件查找
            /// </summary>
            /// <returns></returns>
            public List<Entity> Query(Exp exp, ListArgs arg)
            {
                var list = from row in _data
                           where exp == null || exp.IsMatch(row.Value.Data)
                           //// TODO: orderby 如何处理？
                           select row.Value;

                return ((arg != null) ? list.Skip(arg.Skip).Take(arg.Max) : list).ToList();
            }
        }
    }

    public class Entity
    {
        /// <summary>
        /// Data的Id值
        /// </summary>
        public object Id { get; private set; }

        /// <summary>
        /// 数据
        /// </summary>
        public object Data { get; private set; }

        /// <summary>
        /// Data的数据类型
        /// </summary>
        public Type DataType { get; private set; }

        /// <summary>
        /// Data的状态
        /// </summary>
        public EntityState State { get; private set; }

        /// <summary>
        /// 未经修改的Data，用于Update场合，其他情况为null
        /// </summary>
        public object DataOriginal { get; private set; }

        private Entity() { }

        private Entity(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data不能为null");
            }

            Id = RepoHelper.GetIdValue(data);
            if (Id == null)
            {
                throw new ArgumentNullException("实体必须有一个名为Id的主键属性，不区分大小写");
            }

            Data = data;
            DataType = data.GetType();
        }

        public static Entity CreateInsertEntity(object data)
        {
            return new Entity(data)
            {
                State = EntityState.Inserted
            };
        }

        public static Entity CreateUpdateEntity(object data)
        {
            return new Entity(data)
            {
                State = EntityState.Updated,
                DataOriginal = data.DeepClone()
            };
        }

        public static Entity CreateDeleteEntity(object id, Type type)
        {
            return new Entity()
            {
                Id = id,
                DataType = type,
                State = EntityState.Deleted
            };
        }

        public Entity DeepClone()
        {
            return new Entity()
            {
                Id = Id,
                Data = Data.DeepClone(),
                DataType = DataType,
                State = State,
                DataOriginal = DataOriginal
            };
        }

        public void UpdateData(object data)
        {
            if (data.GetType() != DataType)
                throw new Exception("data数据类型错误，必须与原DataType类型相同");

            Data = data;
        }
    }

    public enum EntityState
    {
        Inserted, Updated, Deleted
    }

    #endregion

    #region 内存存储库提供者

    /// <summary>
    /// 一个内存数据库，用于开发时快速验证功能和单元测试。
    /// </summary>
    public class MemoryRepoProvider : IRepoProvider
    {
        /// <summary>
        /// 数据库实例（允许有多个实例，使用静态实例为了防止被DeepClone）
        /// </summary>
        private static readonly Dictionary<TypeZone, EntityCollection> DataSet = new Dictionary<TypeZone, EntityCollection>();

        /// <summary>
        /// 数据库实例索引
        /// </summary>
        private readonly TypeZone _zone;

        // 这里没有直接使用下面的Data实例，而是使用了上面的索引，
        // 是为了防止对象被DeepClone时把数据库也Clone了。
        // private EntityCollection _data = DataSet[_zone];

        public MemoryRepoProvider(TypeZone zone = null)
        {
            _zone = zone ?? TypeZone.Empty;
            if (!DataSet.ContainsKey(_zone))
                DataSet.Add(_zone, new EntityCollection());
        }

        public bool Commit(List<Entity> entities, List<string> errList = null)
        {
            var data = DataSet[_zone];
            foreach (var entity in entities)
            {
                switch (entity.State)
                {
                    case EntityState.Deleted:
                        data.Remove(entity.Id, entity.DataType);
                        break;
                    case EntityState.Inserted:
                    case EntityState.Updated:
                        data.Add(entity.DeepClone());
                        break;
                }
            }
            return true;
        }

        public T GetById<T>(object id)
        {
            var row = DataSet[_zone].GetById(id, typeof(T));
            return row == null ? default(T) : (T)row.Data.DeepClone();
        }

        public virtual IEnumerable<T> Query<T>(Exp exp, ListArgs arg)
        {
            var list = DataSet[_zone].Query(typeof(T), exp, arg);
            return list.Select(o => (T)o.Data);
        }

        public object Query<T>(FnQuery fn, Exp exp)
        {
            if (fn == FnQuery.Count)
                return DataSet[_zone].Count(typeof(T), exp);

            throw new NotImplementedException("未实现此操作：" + fn);
        }

        public void Clear<T>()
        {
            DataSet[_zone].RemoveAll();
        }
    }

    #endregion

    #region 查询条件类

    /// <summary>
    /// 查询表达式， 例如有表达式：
    /// A>100 Or A=100 And (A=10 And A!=10)
    /// 变成代码是这样
    /// var exp = Exp.New("A",">",100).Or("A","<",100).AndGroup(Exp.New("A","=",100).And("A","!=",10))
    /// exp.IsMatch(object);   // 内存使用
    /// exp.ToSql();           // sql使用
    /// </summary>
    public sealed class Exp
    {
        #region 嵌套类

        /// <summary>
        /// 表达式项：操作数，操作符
        /// </summary>
        private interface IExpItem
        {
            string ToSql();
        }

        /// <summary>
        /// 操作数实现
        /// </summary>
        private class Operand : IExpItem
        {
            public Operand(string left, string op, object right)
            {
                Left = left;
                OP = op.Trim().ToLower();
                Right = right;
            }

            internal Operand(bool value)
            {
                Value = value;
            }

            public string Left { get; private set; }
            public string OP { get; private set; }
            public object Right { get; private set; }

            #region 内存实现使用

            public bool Value { get; private set; }

            public void Bind(object obj)
            {
                var leftVal = RepoHelper.TryGetValue(obj, Left);
                var op = OP.ToLower();
                if (op == "like")
                {
                    Value = leftVal.ToString().Contains(Right.ToString());
                    return;
                }

                if (leftVal == null)
                {
                    Value = Right == null;
                    return;
                }

                var result = ((IComparable)leftVal).CompareTo(Right);
                switch (op)
                {
                    case "=":
                        Value = result == 0;
                        break;
                    case ">":
                        Value = result > 0;
                        break;
                    case ">=":
                        Value = result > 0 || result == 0;
                        break;
                    case "<":
                        Value = result < 0;
                        break;
                    case "<=":
                        Value = result < 0 || result == 0;
                        break;
                    default:
                        throw new ArgumentException("不支持符号" + OP);
                }
            }

            #endregion

            #region Sql实现使用

            public string ToSql()
            {
                var rValue = Right;
                if (OP == "like")
                    rValue = "%" + rValue + "%";

                return Left + " " + OP + " " + WrapSqlValue(rValue);
            }

            private static string WrapSqlValue(object obj)
            {
                var sqlVal = obj.ToString();
                if (obj is string)  // 处理单|双引号
                {
                    sqlVal = sqlVal.Replace("'", "&#39;");
                    sqlVal = sqlVal.Replace("\"", "&#34;");
                }
                if (obj is string || obj is DateTime || obj is Guid)
                {
                    sqlVal = "'" + sqlVal + "'";
                }
                return sqlVal;
            }

            #endregion
        }

        /// <summary>
        /// 操作符实现
        /// </summary>
        private class Operator : IExpItem
        {
            /// <summary>
            /// 操作符
            /// </summary>
            private readonly string _operator;

            /// <summary>
            /// 栈内优先数
            /// </summary>
            public int Isp { get; private set; }

            /// <summary>
            /// 栈外优先数
            /// </summary>
            public int Icp { get; private set; }

            private Operator(string op, int isp, int icp)
            {
                _operator = op;
                Isp = isp;
                Icp = icp;
            }

            #region 操作符限定的实例

            public static Operator And { get; private set; }

            public static Operator Or { get; private set; }

            public static Operator LeftParentheses { get; private set; }

            public static Operator RightParentheses { get; private set; }

            public static Operator Sharp { get; private set; }

            #endregion

            static Operator()
            {
                And = new Operator("And", 4, 3);
                Or = new Operator("Or", 2, 1);
                LeftParentheses = new Operator("(", 1, 6);
                RightParentheses = new Operator(")", 6, 1);
                Sharp = new Operator("#", 0, 0);
            }

            public string ToSql()
            {
                return _operator;
            }
        }

        /// <summary>
        /// 表达式计算器
        /// </summary>
        private class ExpCalculator
        {
            private List<IExpItem> _postfixExp;
            private Stack<IExpItem> _s;

            private ExpCalculator(List<IExpItem> exp)
            {
                _s = new Stack<IExpItem>();
                _postfixExp = Postfix(exp);
            }

            private bool Get2Operands(out Operand left, out Operand right)
            {
                left = null;
                right = null;
                if (_s.Count == 0) return false;
                right = (Operand)_s.Pop();

                if (_s.Count == 0) return false;
                left = (Operand)_s.Pop();
                return true;
            }

            private bool DoOperator()
            {
                if (_postfixExp.Count == 2)  // 有个#号结尾，所以这里数量是2
                {
                    return (_postfixExp[0] as Operand).Value;
                }

                var result = false;
                foreach (var item in _postfixExp)
                {
                    if (item is Operand)
                    {
                        _s.Push(item);
                    }
                    else
                    {
                        var op = item as Operator;
                        Operand left, right;
                        var b = Get2Operands(out left, out right);
                        if (!b) { continue; }
                        if (op == Operator.And)
                        {
                            result = left.Value && right.Value;
                        }
                        else if (op == Operator.Or)
                        {
                            result = left.Value || right.Value;
                        }
                        _s.Push(new Operand(result));
                    }
                }
                return result;
            }

            /// <summary>
            /// 此段代码摘抄自《数据结构（用面向对象方法与C++描述）》一书的计算表达式章节，清华大学出版社。
            /// 将中缀表达式转换为后缀表达式。
            /// </summary>
            /// <returns></returns>
            private static List<IExpItem> Postfix(List<IExpItem> exp)
            {
                var list = new List<IExpItem>();
                var s = new Stack<IExpItem>();
                s.Push(Operator.Sharp);
                foreach (var e in exp)
                {
                    if (e is Operand)
                    {
                        list.Add(e);
                    }
                    else if (e == Operator.RightParentheses)
                    {
                        for (var y = s.Pop(); y != Operator.LeftParentheses; y = s.Pop())
                        {
                            list.Add(y);
                        }
                    }
                    else
                    {
                        var x = (Operator)e;
                        IExpItem y = null;
                        for (y = s.Pop(); ((Operator)y).Isp > x.Icp; y = s.Pop())
                        {
                            list.Add(y);
                        }
                        s.Push(y);
                        s.Push(e);
                    }
                }
                while (s.Count != 0)
                {
                    var y = s.Pop();
                    list.Add(y);
                }

                return list;
            }

            internal static bool Calculate(List<IExpItem> exp)
            {
                var calc = new ExpCalculator(exp);
                return calc.DoOperator();
            }
        }

        #endregion

        #region 构造方法与属性

        private readonly List<IExpItem> _expression;

        private Exp()
        {
            _expression = new List<IExpItem>();
        }

        #endregion

        #region 表达式输入（流畅）接口

        public static Exp New(string left, string op, object right)
        {
            var exp = new Exp();
            exp.AddOperand(left, op, right);

            return exp;
        }

        public static Exp NewGroup(Exp expr)
        {
            var exp = new Exp();
            exp.AddGroup(expr);

            return exp;
        }

        public Exp And(string left, string op, object right)
        {
            _expression.Add(Operator.And);
            AddOperand(left, op, right);

            return this;
        }

        public Exp Or(string left, string op, object right)
        {
            _expression.Add(Operator.Or);
            AddOperand(left, op, right);

            return this;
        }

        public Exp AndGroup(Exp expr)
        {
            _expression.Add(Operator.And);
            AddGroup(expr);

            return this;
        }

        public Exp OrGroup(Exp expr)
        {
            _expression.Add(Operator.Or);
            AddGroup(expr);

            return this;
        }

        private void AddOperand(string left, string op, object right)
        {
            _expression.Add(new Operand(left, op, right));
        }

        private void AddGroup(Exp expr)
        {
            _expression.Add(Operator.LeftParentheses);
            _expression.AddRange(expr._expression);
            _expression.Add(Operator.RightParentheses);
        }

        #endregion

        /// <summary>
        /// 内存比较
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsMatch(object obj)
        {
            foreach (var item in _expression)
            {
                if (item is Operand)
                {
                    ((Operand)item).Bind(obj);
                }
            }

            return ExpCalculator.Calculate(_expression);
        }

        /// <summary>
        /// 将条件转换成Sql语句
        /// </summary>
        /// <returns></returns>
        public string ToSql()
        {
            var list = new List<string>();
            foreach (var item in _expression)
            {
                list.Add(item.ToSql());
            }

            return string.Join(" ", list);
        }
    }

    /// <summary>
    /// 列表条件
    /// </summary>
    public class ListArgs
    {
        public ListArgs()
        {
            Max = int.MaxValue;
            Skip = 0;
            OrderBy = null;
        }

        public int Max { get; set; }
        public int Skip { get; set; }
        public OrderBy OrderBy { get; set; }
    }

    /// <summary>
    /// 聚合函数查询枚举
    /// </summary>
    public enum FnQuery
    {
        Count, Sum, Avg, Max, Min
    }

    /// <summary>
    /// 排序对象
    /// </summary>
    [Serializable]
    public class OrderBy
    {
        public static OrderBy New(string field, OrderType value)
        {
            var ob = new OrderBy();
            ob.And(field, value);
            return ob;
        }

        private readonly NameValueCollection _items;

        private OrderBy()
        {
            _items = new NameValueCollection();
        }

        public OrderBy And(string field, OrderType value)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentNullException("field不能为空");

            _items.Add(field, value.ToString());
            return this;
        }

        public OrderBy And(OrderBy orderBy)
        {
            _items.Add(orderBy._items);
            return this;
        }

        public string ToSql()
        {
            var ss = new List<string>();
            foreach (string name in _items)
            {
                ss.Add(name + " " + _items[name]);
            }
            return (ss.Count == 0) ? string.Empty : string.Join(",", ss);
        }
    }

    public enum OrderType { Asc, Desc }

    #endregion

    #region RepoHelper

    public class RepoHelper
    {
        /// <summary>
        /// 从对象中提取Id值
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object GetIdValue(object data)
        {
            // 注意：这样的取法比较方便，但是判断Id值为null这种情况不是很准确。
            //return  data.TryGetValue("Id") ?? data.TryGetValue("id") ?? data.TryGetValue("ID");

            var type = data.GetType();
            var prop = type.Property("Id") ?? type.Property("ID") ?? type.Property("id") ?? type.Property("Id");
            if (prop != null)
                return prop.Get(data);

            var field = type.Field("Id") ?? type.Field("ID") ?? type.Field("id");
            if (field != null)
                return field.Get(data);

            prop = GetIdPropFromAttr(type);
            if (prop != null)
                return prop.Get(data);

            throw new Exception("未能找到名为Id(不区分大写)或标有[EntityIdAttribute]特性的属性/字段");
        }

        public static PropertyInfo GetIdPropFromAttr(Type type)
        {
            var list = type.Properties();
            return list.FirstOrDefault(prop => prop.HasAttribute(typeof(EntityIdAttribute)));
        }

        /// <summary>
        /// 尝试读取复杂属性的值，例如 object.Property.Property
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        internal static object TryGetValue(object obj, string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName) || obj == null)
                return null;

            var ss = memberName.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            object result = obj;
            foreach (var s in ss)
            {
                result = result.TryGetValue(s);
                if (result == null)
                    return null;
            }
            return result;
        }
    }

    /// <summary>
    /// 当标识属性的名字不是“Id”时，用此特性标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EntityIdAttribute : Attribute
    {

    }

    #endregion

    #region 一些细粒度类

    /// <summary>
    /// 如果实体类实现了此接口，Repository会检查此接口。
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// 检查是否合格
        /// </summary>
        /// <returns>错误信息列表。</returns>
        bool CheckIsValid(List<string> errList = null);
    }

    /// <summary>
    /// 类型区域，相对于Repo而言，有了这个属性，则有机会把相同的类型存储于不同的表中。
    /// </summary>
    public class TypeZone
    {
        private TypeZone() { }

        public TypeZone(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        private static TypeZone _empty;
        private static TypeZone _test;

        public static TypeZone Empty
        {
            get { return _empty ?? (_empty = new TypeZone(string.Empty, string.Empty)); }
        }

        public static TypeZone Test
        {
            get { return _test ?? (_test = new TypeZone("Test", "测试区域")); }
        }
    }

    public class PageInfo
    {
        internal PageInfo(uint size, uint number = 1, OrderBy orderBy = null)
        {
            Size = size;
            Number = number;
            OrderBy = orderBy;
        }

        public uint Size { get; private set; }
        public uint Number { get; private set; }
        public OrderBy OrderBy { get; private set; }

        public void ChangeNumber(uint number)
        {
            Number = number;
        }

        public ListArgs ToListArgs()
        {
            return new ListArgs()
            {
                Max = (int)Size,
                Skip = (int)(Size * (Number - 1)),
                OrderBy = OrderBy
            };
        }
    }

    public class RecordInfo
    {
        internal RecordInfo(uint recordCount, uint pageSize)
        {
            Count = recordCount;
            PageCount = (uint)Math.Ceiling(Count / (double)pageSize);
        }

        public uint Count { get; private set; }

        public uint PageCount { get; private set; }
    }

    /// <summary>
    /// 分页信息类
    /// </summary>
    public class Pages
    {
        #region 构造&属性

        public Pages(uint pageSize, uint pageIndex, OrderBy orderBy = null)
        {
            CurrentPage = new PageInfo(Math.Max(pageSize, 1), Math.Max(pageIndex, 1), orderBy);
        }

        public PageInfo CurrentPage { get; private set; }

        public RecordInfo Record { get; private set; }

        #endregion

        #region Public Method

        public void SetRecord(int recordCount)
        {
            Record = new RecordInfo((uint)recordCount, CurrentPage.Size);
        }

        public string ToJson()
        {
            var s = "{{'RecordCount':{0},'PageSize':{1},'CurrentPage':{2},'PageCount':{3}}}";
            return string.Format(s, Record.Count, CurrentPage.Size, CurrentPage.Number, Record.PageCount);
        }

        #endregion Public Method
    }

    #endregion

    #region 客户使用帮助类

    /// <summary>
    /// 存储库提供者配置
    /// </summary>
    public class RepoProviderConf
    {
        public Type ProviderType { get; set; }
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// 存储库提供者选择器
    /// </summary>
    public abstract class RepoSelector
    {
        protected readonly RepoProviderConf Conf;

        protected RepoSelector(RepoProviderConf conf)
        {
            if (conf == null)
                throw new ArgumentNullException();
            if (conf.ProviderType == null)
                throw new ArgumentException("ProviderType不能为null");

            Conf = conf;
        }

        public Repository NewRepo(TypeZone zone = null)
        {
            var provider = SelectProvider(zone);
            if (provider == null)
                throw new NotImplementedException(string.Format("不支持此类型‘{0}’的Provider", Conf.ProviderType.Name));

            return new Repository(provider);
        }

        protected abstract IRepoProvider SelectProvider(TypeZone zone = null);
    }

    #endregion
}
