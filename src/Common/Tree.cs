using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Fasterflect;
using System;
using System.Collections.Generic;

namespace MAF
{
    // 实现说明：
    // 特点：实现了树的一般性操作，和便于持久化。
    // 1. 为了便于持久化，将树节点存储于线性结构（词典），并使每个节点的关联最弱（方法比属性弱）。
    // 2. 节点关系是由GetNodeById方法来维护，所有Id都指向词典的Key.
    // 3. 节点关系采用一个三叉链表的结构：ParentId、FirstChildId、NextSiblingId，
    //    这样无论移动那个节点，都只会影响相邻的一个节点（父节点或左兄弟节点）。

    /// <summary>
    /// 三叉链
    /// </summary>
    public class NodeLink : IEquatable<NodeLink>
    {
        public NodeLink()
        {
            ParentId = null;
            FirstChildId = null;
            NextSiblingId = null;
            DataId = null;
        }

        public object ParentId { get; internal set; }
        public object FirstChildId { get; set; }
        public object NextSiblingId { get; internal set; }

        [EntityId]
        public object DataId { get; internal set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((NodeLink)obj);
        }

        public bool Equals(NodeLink other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ParentId, other.ParentId) && Equals(FirstChildId, other.FirstChildId) && Equals(NextSiblingId, other.NextSiblingId) && Equals(DataId, other.DataId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (ParentId != null ? ParentId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FirstChildId != null ? FirstChildId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NextSiblingId != null ? NextSiblingId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DataId != null ? DataId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(NodeLink left, NodeLink right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NodeLink left, NodeLink right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// 树节点，通过三叉链来连接各个节点
    /// </summary>
    [Serializable]
    public class TreeNode<T> where T : IValidatable
    {
        #region 构造方法 & 属性

        private readonly Tree<T> _tree;

        internal TreeNode(Tree<T> tree, T data)
        {
            if (data == null)
                throw new ArgumentNullException();

            _tree = tree;

            Id = RepoHelper.GetIdValue(data);
            Link = new NodeLink() { DataId = Id };
            Data = data;
        }

        public object Id { get; private set; }

        public NodeLink Link { get; internal set; }

        /// <summary>
        /// 节点数据
        /// </summary>
        public T Data { get; internal set; }

        #endregion

        #region 方法

        /// <summary>
        /// 父节点
        /// </summary>
        public TreeNode<T> Parent()
        {
            return _tree.GetNodeById(Link.ParentId);
        }

        /// <summary>
        /// 第一个孩子节点
        /// </summary>
        public TreeNode<T> FirstChild()
        {
            return _tree.GetNodeById(Link.FirstChildId);
        }

        /// <summary>
        /// 是否第一个孩子节点
        /// </summary>
        /// <returns></returns>
        public bool IsFirstChild()
        {
            var p = Parent();
            return p == null || Id.Equals(p.Link.FirstChildId);
        }

        /// <summary>
        /// 最后一个孩子节点
        /// </summary>
        /// <returns></returns>
        public TreeNode<T> LastChild()
        {
            var p = FirstChild();
            var temp = p;
            while (p != null)
            {
                temp = p;
                p = p.NextSibling();
            }
            return temp;
        }

        /// <summary>
        /// 是否最后一个孩子节点
        /// </summary>
        /// <returns></returns>
        public bool IsLastChild()
        {
            var p = Parent();
            return p == null || Id.Equals(p.LastChild().Id);
        }

        /// <summary>
        /// 下一个兄弟节点
        /// </summary>
        public TreeNode<T> NextSibling()
        {
            return _tree.GetNodeById(Link.NextSiblingId);
        }

        /// <summary>
        /// 上一个兄弟节点
        /// </summary>
        /// <returns></returns>
        internal TreeNode<T> PrevSibling()
        {
            if (IsFirstChild()) return null;
            var p = Parent().FirstChild();
            while (p != null)
            {
                if (Equals(p.Link.NextSiblingId, Id))
                    break;
                p = p.NextSibling();
            }
            return p;
        }

        /// <summary>
        /// 孩子节点
        /// </summary>
        public List<TreeNode<T>> Children()
        {
            var list = new List<TreeNode<T>>();
            var p = FirstChild();
            while (p != null)
            {
                list.Add(p);
                p = p.NextSibling();
            }
            return list;
        }

        /// <summary>
        /// 祖先节点
        /// </summary>
        public IList<TreeNode<T>> Ancestry()
        {
            var list = new List<TreeNode<T>>();
            var p = Parent();

            while (p != null)
            {
                list.Add(p);
                p = p.Parent();
            }
            list.Reverse();
            return list;
        }

        /// <summary>
        /// 增加一个孩子节点
        /// </summary>
        public void AddChild(T data)
        {
            _tree.AppendNode(this, data);

            //// var sameName = position.Children().Find(o => node.Name == o.Name);
            //// if (sameName != null)
            ////     throw new Exception("AddChild错误，同一层级不能有重名节点");
        }

        /// <summary>
        /// 移除自己
        /// </summary>
        public void Remove()
        {
            _tree.RemoveNode(Id);
        }

        /// <summary>
        /// 移动自己
        /// </summary>
        /// <param name="toParentId"></param>
        public void Move(object toParentId)
        {
            _tree.MoveNode(Id, toParentId);
        }

        /// <summary>
        /// 访问所有孩子节点
        /// </summary>
        /// <param name="visit"></param>
        public void ForEach(Action<TreeNode<T>> visit)
        {
            _tree.ForEach(visit, Id);
        }

        /// <summary>
        /// 节点的深度，由于随着树层次的移动需要更新所有子节点，所以动态算出Depth可能会比较好
        /// </summary>
        public int Depth()
        {
            return Ancestry().Count;
        }

        /// <summary>
        /// 节点的宽度
        /// </summary>
        public int Width()
        {
            var parent = Parent();
            if (parent == null) { return 0; }

            var p = parent.FirstChild();
            var i = 0;
            while (p != null)
            {
                ++i;
                if (Equals(p.Id, Id)) break;

                p = p.NextSibling();
            }
            return i;
        }

        /// <summary>
        /// 节点属于的树
        /// </summary>
        public Tree<T> OwnerTree()
        {
            return _tree;
        }

        #endregion

        internal TreeNodeDto<T> ToDto()
        {
            return new TreeNodeDto<T>(Link, Data);
        }
    }

    /// <summary>
    /// 节点数据传输对象，
    /// 由于节点太复杂，内存模式下，如果要保存一个Node就会把整棵树都Clone了，
    /// 因此使用此对象来断开属性的递归调用
    /// </summary>
    public class TreeNodeDto<T> : IValidatable where T : IValidatable
    {
        private TreeNodeDto() { }

        public TreeNodeDto(NodeLink link, T data)
        {
            Link = link;
            Data = data;
            Id = Link.DataId;
        }

        public object Id { get; private set; }

        public NodeLink Link { get; internal set; }

        public T Data { get; internal set; }

        public bool CheckIsValid(List<string> errList = null)
        {
            return Data.CheckIsValid(errList);
        }
    }

    /// <summary>
    /// 树
    /// </summary>
    [Serializable]
    public class Tree<T> where T : IValidatable
    {
        /// <summary>
        /// 树的持久化跟踪
        /// </summary>
        private class TreePersistSession
        {
            private readonly RepoTracker _persistSession;

            public TreePersistSession(RepoTracker session)
            {
                _persistSession = session;
            }

            public void Insert(TreeNode<T> node)
            {
                if (_persistSession == null) return;
                if (node == null || node.Link == null || node.Data == null)
                    throw new ArgumentNullException();

                _persistSession.Insert(node.ToDto());
            }

            public void Update(TreeNode<T> node)
            {
                if (_persistSession == null) return;
                _persistSession.RegistUpdate(node.ToDto());
            }

            public void Delete(object id)
            {
                if (_persistSession == null) return;
                _persistSession.Delete<TreeNodeDto<T>>(id);
            }

            public RepoTracker GetRepoSession()
            {
                return _persistSession;
            }
        }

        #region 构造方法 & 属性

        /// <summary>
        /// 所有节点在这里集结
        /// </summary>
        private readonly Dictionary<object, TreeNode<T>> _nodes;

        /// <summary>
        /// 树从这里开始
        /// </summary>
        private object _rootId;

        /// <summary>
        /// 持久化跟踪，这个是可选属性
        /// </summary>
        private readonly TreePersistSession _session;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="session">此参数为null，表示树的变化不需要持久化</param>
        public Tree(RepoTracker session = null)
        {
            _nodes = new Dictionary<object, TreeNode<T>>();
            _session = new TreePersistSession(session);
        }

        /// <summary>
        /// 树的根节点
        /// </summary>
        public TreeNode<T> Root
        {
            get { return GetNodeById(_rootId); }
        }

        public bool IsEmpty
        {
            get { return !_nodes.Any(); }
        }

        #endregion

        #region 方法

        /// <summary>
        /// 创建孤儿节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal TreeNode<T> CreateNode(T data)
        {
            return new TreeNode<T>(this, data);
        }

        /// <summary>
        /// 建立树根节点，从持久化加载树不需要执行此操作
        /// </summary>
        /// <param name="data"></param>
        public void BuildRoot(T data)
        {
            if (Root != null)
                throw new Exception("Root已经存在。");

            var node = CreateNode(data);
            _rootId = node.Id;
            _nodes.Add(_rootId, node);
            _session.Insert(node);
        }

        /// <summary>
        /// 增加孩子
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public TreeNode<T> AppendNode(object parentId, T data)
        {
            var p = GetNodeById(parentId);
            if (p == null | data == null) { throw new ArgumentNullException(); }

            var node = CreateNode(data);
            if (GetNodeById(node.Id) != null)
                throw new Exception("当前节点Id和已存在的Id冲突，请输入新的Id");

            node.Link.ParentId = p.Id;
            _session.Insert(node);  // 持久化跟踪

            if (p.FirstChild() == null)
            {
                _session.Update(p);   // 持久化跟踪
                p.Link.FirstChildId = node.Id;
            }
            else
            {
                var lastChild = p.LastChild();
                _session.Update(lastChild);  // 持久化跟踪
                lastChild.Link.NextSiblingId = node.Id;
            }
            _nodes.Add(node.Id, node);
            return node;
        }

        /// <summary>
        /// 删除孩子
        /// </summary>
        /// <param name="id"></param>
        public void RemoveNode(object id)
        {
            var node = GetNodeById(id);
            if (node == null)
                throw new Exception("没有找到指定的节点");

            // 删除节点及其子孙节点
            node.ForEach(o =>
            {
                _nodes.Remove(o.Id);
                _session.Delete(o.Id);  // 持久化化跟踪
            });

            // 链接上后面的节点
            var nextSibling = node.NextSibling();
            if (nextSibling == null)
            {
                return;
            }

            var prevSibling = node.PrevSibling();
            if (prevSibling == null)
            {
                var p = node.Parent();
                _session.Update(p);  // 持久化跟踪
                p.Link.FirstChildId = node.NextSibling().Id;
            }
            else
            {
                _session.Update(prevSibling);  // 持久化跟踪
                prevSibling.Link.NextSiblingId = node.NextSibling().Id;
            }
        }

        /// <summary>
        /// 没有处理把节点移动到自己的孩子节点下的情况，调用时请注意，这样会丢失一颗子树
        /// </summary>
        /// <param name="id"></param>
        /// <param name="toParentId"></param>
        public void MoveNode(object id, object toParentId)
        {
            if (id.Equals(toParentId)) { return; }
            if (id.Equals(_rootId)) { return; }

            var node = GetNodeById(id);
            var toParent = GetNodeById(toParentId);
            var fromParent = node.Parent();
            if (node == null || fromParent == null || toParent == null)
                throw new ArgumentNullException();

            // 删除旧连接
            var nextSibling = node.NextSibling();
            if (node.IsFirstChild())
            {
                _session.Update(fromParent);
                fromParent.Link.FirstChildId = nextSibling == null ? null : nextSibling.Id;
            }
            else
            {
                var prevSibling = node.PrevSibling();
                _session.Update(prevSibling);
                prevSibling.Link.NextSiblingId = nextSibling == null ? null : nextSibling.Id;
            }

            // 建立新连接
            _session.Update(node);
            node.Link.ParentId = toParent.Id;
            node.Link.NextSiblingId = null;

            var lastChild = toParent.LastChild();
            if (lastChild != null)
            {
                _session.Update(lastChild);
                lastChild.Link.NextSiblingId = node.Id;
            }
            else
            {
                _session.Update(toParent);
                toParent.Link.FirstChildId = node.Id;
            }
        }

        /// <summary>
        /// 排序一个节点
        /// </summary>
        /// <param name="id">指定节点</param>
        /// <param name="movePos">顺序提升位数，正数往前、负数往后</param>
        public void SortNode(object id, int movePos)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 更新节点数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void UpdateNodeData(object id, T data)
        {
            var node = GetNodeById(id);
            node.Data = data;
            _session.Update(node);
        }

        /// <summary>
        /// 获取持久化Session，如果不是从持久化加载的树，返回null
        /// </summary>
        /// <returns></returns>
        public RepoTracker GetRepoSession()
        {
            return _session.GetRepoSession();
        }

        /// <summary>
        /// 历遍树
        /// </summary>
        /// <param name="visit"></param>
        /// <param name="currentId"></param>
        public void ForEach(Action<TreeNode<T>> visit, object currentId = null)
        {
            new PreOrder(this, currentId).ForEach(visit);
        }

        /// <summary>
        /// 查找节点
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TreeNode<T> GetNodeById(object id)
        {
            return (id != null && _nodes.ContainsKey(id)) ? _nodes[id] : null;
        }

        #endregion

        #region 历遍树

        public abstract class TreeIterator
        {
            protected TreeNode<T> _current;
            protected Tree<T> _tree;

            protected TreeIterator(Tree<T> tree, object currentId)
            {
                _tree = tree;
                _current = _tree.GetNodeById(currentId) ?? _tree.Root;
            }

            public abstract void ForEach(Action<TreeNode<T>> fn);
        }


        /// <summary>
        /// 前序历遍
        /// </summary>
        public class PreOrder : TreeIterator
        {
            public PreOrder(Tree<T> tree, object currentId) : base(tree, currentId) { }

            public override void ForEach(Action<TreeNode<T>> visit)
            {
                if (_tree.IsEmpty) return;

                if (visit != null) { visit(_current); }

                var p = _current;
                _current = _current.FirstChild();
                while (_current != null)
                {
                    ForEach(visit);
                    _current = _current.NextSibling();
                }
                _current = p;
            }
        }

        /// <summary>
        /// 中序历遍
        /// </summary>
        public class InOrder : TreeIterator
        {
            public InOrder(Tree<T> tree, object currentId) : base(tree, currentId) { }

            public override void ForEach(Action<TreeNode<T>> visit)
            {
                if (_tree.IsEmpty) return;

                visit(_current);

                var p = _current;
                var i = _current.FirstChild();
                while (i != null)
                {
                    ForEach(visit);
                    i = _current.NextSibling();
                }
                _current = p;
            }
        }

        /// <summary>
        /// 后序历遍
        /// </summary>
        public class PostOrder : TreeIterator
        {
            public PostOrder(Tree<T> tree, object currentId) : base(tree, currentId) { }

            public override void ForEach(Action<TreeNode<T>> visit)
            {
                if (_tree.IsEmpty) return;

                visit(_current);

                var p = _current;
                var i = _current.FirstChild();
                while (i != null)
                {
                    ForEach(visit);
                    i = _current.NextSibling();
                }
                _current = p;
            }
        }

        #endregion

        #region 加载树

        public class Loader
        {
            /// <summary>
            /// 从持久化加载树
            /// </summary>
            /// <param name="list">树节点DTO列表</param>
            /// <param name="persistSession">此参数不传，表示不需要把树的变更持久化</param>
            public static Tree<T> Load(IEnumerable<TreeNodeDto<T>> list, RepoTracker persistSession = null)
            {
                var tree = new Tree<T>(persistSession);
                list.ForEach(o =>
                {
                    var node = new TreeNode<T>(tree, o.Data) { Link = o.Link };
                    tree._nodes.Add(node.Id, node);
                    if (Equals(o.Link.ParentId, null))
                        tree._rootId = node.Id;
                });
                return tree;
            }

            /// <summary>
            /// 初始化树。如果不存在根节点，将会创建一个；其余行为与Load方法一样。
            /// </summary>
            /// <param name="repo">树的存储库，要保证有repo.Query[TreeNodeDto[T]]方法</param>
            /// <param name="rootData">如果当前树是空树，将会用此数据创建一个根节点</param>
            /// <returns></returns>
            public static Tree<T> Init(Repository repo, T rootData)
            {
                var nodes = repo.Query<TreeNodeDto<T>>();
                var session = repo.NewTracker();

                var tree = Load(nodes, session);

                if (tree.IsEmpty)   // 如果是空树，创建根节点
                {
                    tree.BuildRoot(rootData);
                    repo.Commit(tree.GetRepoSession());
                }

                return tree;
            }
        }

        #endregion
    }

    /// <summary>
    /// Html UI 帮助类
    /// </summary>
    public static class TreeHtmlHelper
    {
        public static string TreeInUL<T>(TreeNode<T> rootNode, string nodeTemplate) where T : IValidatable
        {
            var ss = new List<string>();
            var stack = new Stack<TreeNode<T>>();
            rootNode.ForEach(o =>
            {
                // 1. 打开层次，
                if (o.IsFirstChild()) { ss.Add("<ul>"); }

                // 2. 打开节点，并填充节点内容
                ss.Add("<li>");
                ss.Add(TemplateHelper.Replace(o.Data, nodeTemplate));

                // 3. 如果有子节点，那么继续访问子节点，并记录当前节点为“未关闭”
                if (o.FirstChild() != null)
                {
                    stack.Push(o);
                    return;
                }

                // 4. 关闭节点
                ss.Add("</li>");

                // 5. 如果没有兄弟节点，那么关闭本层次
                if (o.NextSibling() != null) { return; }
                ss.Add("</ul>");

                // 6. 检查未结束的节点
                while (true)
                {
                    if (stack.Count == 0) { break; }

                    var n = stack.Pop();
                    ss.Add("</li>");  // 关闭节点

                    if (n.NextSibling() != null) { break; }
                    ss.Add("</ul>");  // 关闭层次
                }
            });

            return string.Join("\r\n", ss);
        }

        public static string TreeInOption<T>(TreeNode<T> rootNode, string nodeTemplate, Guid selectedId) where T : IValidatable
        {
            var ss = new List<string>();
            rootNode.ForEach(node =>
            {
                var name = TemplateHelper.Replace(node.Data, nodeTemplate);
                name = "".PadLeft(node.Depth(), '—') + name;
                string selected = ((Guid)node.Id == selectedId) ? "selected='selected'" : "";
                ss.Add(string.Format(@"<option value='{0}' {2}>{1}</option>"
                    , node.Id
                    , name
                    , selected));
            });
            return string.Join("\n", ss.ToArray());
        }
    }
}
