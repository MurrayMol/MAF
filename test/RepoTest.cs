
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Fasterflect;
using MAF.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MAF.Tests
{
    /// <summary>
    ///包含所有 Repository 单元测试
    ///</summary>
    [TestClass()]
    public class RepoTest
    {
        private Repository _repo;
        private Guid _testId;

        #region 附加测试特性

        /// <summary>
        ///获取或设置测试上下文，上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext { get; set; }

        // 
        //编写测试时，还可使用以下特性:
        //
        //使用 ClassInitialize 在运行类中的第一个测试前先运行代码
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //使用 ClassCleanup 在运行完类中的所有测试后再运行代码
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //使用 TestInitialize 在运行每个测试前先运行代码
        [TestInitialize()]
        public void MyTestInitialize()
        {
            _repo = new Repository(new MemoryRepoProvider());
            _testId = Guid.NewGuid();

            // 初始化数据
            var tracker = _repo.NewTracker();

            tracker.Insert(new TestEntity(_testId, "A"));
            tracker.Insert(new TestEntity());

            _repo.Commit(tracker);
        }

        //使用 TestCleanup 在运行完每个测试后运行代码
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        #region CRUD 测试

        [TestMethod]
        public void CanInsert()
        {
            // 初始化了两条数据
            Assert.AreEqual(2, _repo.Query<TestEntity>(FnQuery.Count));

            // 当Id相同时，只会记录一条数据，至于记录了那条数据依赖于RepoProvider实现。
            var enty1 = new TestEntity("A");
            var enty2 = new TestEntity("B");
            enty2.Id = enty1.Id;

            var track = _repo.NewTracker();
            track.Insert(enty1);
            track.Insert(enty2);
            _repo.Commit(track);
            Assert.AreEqual(2 + 1, _repo.Query<TestEntity>(FnQuery.Count));

            // 如果实现了IValidatable接口，添加时会进行检查
            var errList = new List<string>();
            track.Insert(new TestEntity1(), errList);
            Assert.AreEqual("未实现", errList[0]);
        }

        [TestMethod]
        public void CanUpdate()
        {
            var obj = _repo.GetById<TestEntity>(_testId);
            Assert.AreEqual("A", obj.Name);

            // 注册修改
            var track = _repo.NewTracker();
            track.RegistUpdate(obj);
            obj.Name = "B";

            // Commit前值没有改变
            var obj1 = _repo.GetById<TestEntity>(_testId);
            Assert.AreEqual("A", obj1.Name);

            // Commit后值改变了
            _repo.Commit(track);
            var obj2 = _repo.GetById<TestEntity>(_testId);
            Assert.AreEqual("B", obj2.Name);
        }

        [TestMethod]
        public void CanDelete()
        {
            var track = _repo.NewTracker();
            track.Delete<TestEntity>(_testId);

            // Commit前数据有没有被删除
            var obj = _repo.GetById<TestEntity>(_testId);
            Assert.IsNotNull(obj);

            // Commit后数据被删除了
            _repo.Commit(track);
            var obj1 = _repo.GetById<TestEntity>(_testId);
            Assert.IsNull(obj1);
        }

        [TestMethod]
        public void CanQuery()
        {
            // 查询单个对象
            var obj = _repo.GetById<TestEntity>(_testId);
            Assert.IsNotNull(obj);
            Assert.AreEqual("A", obj.Name);

            // 查询多个对象
            var list1 = _repo.Query<TestEntity>(Exp.New("Name", "=", "A"));
            Assert.AreEqual(1, list1.Count());

            // 分页查询多个对象
            var list2 = _repo.Query<TestEntity>(null, new ListArgs() { Max = 1, Skip = 1, OrderBy = null });
            Assert.AreEqual(1, list2.Count());
            Assert.IsNull(list2.ToList()[0].Name);
        }

        #endregion
    }

    public class TestEntity
    {
        public TestEntity()
        {
            Id = CombGuid.NewGuid();
        }

        public TestEntity(string name)
            : this()
        {
            Name = name;
        }

        public TestEntity(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    public class TestEntity1 : TestEntity, IValidatable
    {

        public bool CheckIsValid(List<string> errList = null)
        {
            var list = new List<string>();
            list.Add("未实现");
            if (errList != null)
                errList.AddRange(list);

            return list.Count == 0;
        }
    }
}
