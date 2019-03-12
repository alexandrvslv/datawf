using DataWF.Common;
using DataWF.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Test.Data
{
    [TestFixture]
    public class TestORM
    {
        private const string SchemaName = "test";
        private const string EmployerTableName = "tb_employer";
        private const string PositionTableName = "tb_position";
        private DBSchema schema;

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DBService.Schems.Clear();
            DBTable.ClearAttributeCache();

            if (DBService.Connections.Count == 0)
                Serialization.Deserialize("connections.xml", DBService.Connections);

            AccessValue.Groups = new List<IAccessGroup> {
                new AccessGroupBung() { Id = 1, Name = "Group1"},
                new AccessGroupBung() { Id = 2, Name = "Group2"},
                new AccessGroupBung() { Id = 3, Name = "Group3"}
            };
        }

        [Test]
        public void GenerateSqlite()
        {
            Generate(DBService.Connections["TestSqlLite"]);
        }

        [Test]
        public void GeneratePostgres()
        {
            Generate(DBService.Connections["TestPostgres"]);
        }

        [Test]
        public void GenerateOracle()
        {
            Generate(DBService.Connections["TestOracle"]);
        }

        [Test]
        public void GenerateMySql()
        {
            Generate(DBService.Connections["TestMySql"]);
        }

        [Test]
        public void GenerateMsSql()
        {
            Generate(DBService.Connections["TestMSSql"]);
        }

        [Test]
        public void SchemaSerialization()
        {
            var schem = DBSchema.Generate(GetType().Assembly, SchemaName);
            var file = "data.xml";
            Serialization.Serialize(DBService.Schems, file);
            DBService.Schems.Clear();
            Serialization.Deserialize(file, DBService.Schems);
            Assert.AreEqual(2, DBService.Schems.Count);

            Assert.AreEqual(2, schem.Tables.Count);
            var table = schem.Tables[EmployerTableName];
            Assert.IsNotNull(table);
            Assert.IsInstanceOf<DBTable<Employer>>(table);
            var column = table.Columns["id"];
            Assert.IsNotNull(column);
            Assert.AreEqual(typeof(int), column.DataType);
        }

        public void Generate(DBConnection connection)
        {
            connection.CheckConnection();
            schema = DBSchema.Generate(GetType().Assembly, SchemaName);

            Assert.IsNotNull(schema, "Attribute Generator Fail. On Schema");
            Assert.IsNotNull(Employer.DBTable, "Attribute Generator Fail. On Employer Table");
            Assert.IsNotNull(Position.DBTable, "Attribute Generator Fail. On Position Table");

            var idColumn = Employer.DBTable.Columns["id"];
            Assert.IsNotNull(idColumn, "Attribute Generator Fail. On Column Employer Id");
            var positionColumn = Employer.DBTable.Columns["positionid"];
            Assert.IsNotNull(positionColumn, "Attribute Generator Fail. On Column Employer Position");
            Assert.IsNotNull(positionColumn.ReferenceTable, "Attribute Generator Fail. On Column Employer Position Reference");
            schema.Connection = connection;

            schema.DropDatabase();
            schema.CreateDatabase();

            var result = schema.GetTablesInfo(connection.Schema, EmployerTableName);
            Assert.IsTrue(result.Count() == 1, "Generate Sql Table / Get Information Fail.");
            result = schema.GetTablesInfo(connection.Schema, PositionTableName);
            Assert.IsTrue(result.Count() == 1, "Generate Sql Table / Get Information Fail.");
            //Insert
            var employer = new Employer()
            {
                Identifier = $"{1:8}",
                Lodar = true,
                Age = 40,
                Height = 180,
                LongId = 120321312321L,
                Weight = 123.12333F,
                DWeight = 123.1233433424434D,
                Salary = 231323.32M,
                Name = "Ivan",
                Access = new AccessValue(new[]
                {
                    new AccessItem(AccessValue.Groups.First(i => i.Id == 1), AccessType.View),
                    new AccessItem(AccessValue.Groups.First(i => i.Id == 2), AccessType.Admin),
                    new AccessItem(AccessValue.Groups.First(i => i.Id == 3), AccessType.Create)
                })
            };
            Assert.AreEqual(employer.Type, EmployerType.Type2, "Default Value & Enum");

            employer.GenerateId();
            Assert.NotNull(employer.Id, "Id Generator Fail");

            employer.Save();
            var qresult = schema.Connection.ExecuteQResult($"select * from {EmployerTableName}");
            Assert.AreEqual(1, qresult.Values.Count, "Insert sql Fail");
            Assert.AreEqual(employer.Id, qresult.Get(0, "id"), "Insert sql Fail Int");
            Assert.AreEqual(employer.Identifier, qresult.Get(0, "identifier"), "Insert sql Fail String");
            Assert.AreEqual((int?)employer.Type, qresult.Get(0, "typeid"), "Insert sql Fail Enum");
            Assert.AreEqual(employer.Age, qresult.Get(0, "age"), "Insert sql Fail Byte");
            Assert.AreEqual(employer.Height, qresult.Get(0, "height"), "Insert sql Fail Short");
            Assert.AreEqual(employer.LongId, qresult.Get(0, "longid"), "Insert sql Fail Long");
            Assert.AreEqual(employer.Weight, qresult.Get(0, "weight"), "Insert sql Fail Float");
            Assert.AreEqual(employer.DWeight, qresult.Get(0, "dweight"), "Insert sql Fail Double");
            Assert.AreEqual(employer.Salary, qresult.Get(0, "salary"), "Insert sql Fail Decimal");
            var lodar = qresult.Get(0, "lodar").ToString();
            Assert.IsTrue(lodar == "1" || lodar == "True", "Insert sql Fail Bool");
            Assert.IsInstanceOf<byte[]>(qresult.Get(0, "group_access"), "Insert sql Fail Byte Array");
            var accessValue = new AccessValue((byte[])qresult.Get(0, "group_access"));
            Assert.AreEqual(3, accessValue.Items.Count, "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items[0].View, "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items[1].Admin, "Insert sql Fail Byte Array");
            Assert.AreEqual(false, accessValue.Items[2].Delete, "Insert sql Fail Byte Array");

            Employer.DBTable.Clear();
            Assert.AreEqual(0, Employer.DBTable.Count, "Clear table Fail");

            //Insert Several
            Position.DBTable.Add(new Position() { Code = "1", Name = "First Position" });
            Position.DBTable.Add(new Position() { Code = "2", Name = "Second Position" });
            var position = new Position() { Id = 0, Code = "3", Name = "Group Position" };
            position.Attach();
            var sposition = new Position() { Code = "4", Parent = position, Name = "Sub Group Position" };
            sposition.Attach();

            //Select from internal Index
            Position.DBTable.Add(new Position() { Code = "t1", Name = "Null Index" });
            Position.DBTable.Add(new Position() { Code = "t2", Name = "Null Index" });
            Position.DBTable.Add(new Position() { Code = "t3", Name = "Null Index" });
            var nullIds = Position.DBTable.Select(Position.DBTable.PrimaryKey, CompareType.Is, null).ToList();
            Assert.AreEqual(6, nullIds.Count, "Select by null Fail");

            Position.DBTable.Save();
            Position.DBTable.Clear();
            var positions = Position.DBTable.Load();
            Assert.AreEqual(7, positions.Count(), "Insert/Read several Fail");

            //GetById
            employer = Employer.DBTable.LoadById(1);
            Assert.IsNotNull(employer, "GetById Fail");
            position = Position.DBTable.LoadById(4);
            Assert.IsNotNull(position, "GetById Fail");
            //Update
            employer.Position = position;
            employer.Save();

            qresult = schema.Connection.ExecuteQResult($"select * from {EmployerTableName}");
            Assert.AreEqual(4, qresult.Get(0, "positionid"), "Update sql Fail");


            connection.ExecuteQuery(@"create table test_table(
      id int primary key, 
      test_date date, 
      test_varchar varchar(512),
      test_numeric numeric(20,10))");

            result = schema.GetTablesInfo(connection.Schema, "test_table");
            schema.GenerateTablesInfo(result);
            var table = schema.Tables["test_table"] as DBTable<DBItem>;
            Assert.IsNotNull(table, "DBInformation Load Fail");

            table.Load().LastOrDefault();
            for (int i = 0; i < 1000; i++)
            {
                var row = table.NewItem();
                row["id"] = i;
                row["test_date"] = DateTime.Now.AddDays(-i);
                row["test_varchar"] = "string value " + i;
                row["test_numeric"] = i / 1000M;
                table.Add(row);
            }
            table.Save();

            table.Clear();

            int c = 0;
            foreach (var item in table.Load(string.Empty))
            {
                if (++c == 5)
                    break;
            }
        }

        public enum EmployerType
        {
            Type1,
            Type2,
            Type3,
        }

        [Table(PositionTableName, "Default")]
        public class Position : DBItem
        {
            private Position parent;

            public static DBTable<Position> DBTable
            {
                get { return GetTable<Position>(); }
            }

            public Position()
            {
            }

            [Column("id", Keys = DBColumnKeys.Primary)]
            public int? Id
            {
                get { return GetProperty<int?>(); }
                set { SetProperty(value); }
            }

            [Column("code", 20, Keys = DBColumnKeys.Code | DBColumnKeys.Unique | DBColumnKeys.Indexing)]
            [Index("positioncode", true)]
            public string Code
            {
                get { return GetProperty<string>(); }
                set { SetProperty(value); }
            }

            [Column("parentid", Keys = DBColumnKeys.Group)]
            public int? ParentId
            {
                get { return GetProperty<int?>(); }
                set { SetProperty(value); }
            }

            [Reference(nameof(ParentId))]
            public Position Parent
            {
                get { return GetPropertyReference<Position>(ref parent); }
                set { parent = SetPropertyReference(value); }
            }

            [Column("name", 200, Keys = DBColumnKeys.Culture)]
            public string Name
            {
                get { return GetName(); }
                set { SetName(value); }
            }

            [Column("description")]
            public string Description
            {
                get { return GetProperty<string>(); }
                set { SetProperty(value); }
            }
        }

        public class EmployerTable : DBTable<Employer>
        {

        }

        [Table(EmployerTableName, "Default")]
        public class Employer : DBItem
        {
            private Position position;

            public static DBTable<Employer> DBTable
            {
                get { return GetTable<Employer>(); }
            }

            public Employer()
            {
            }

            [Column("id", Keys = DBColumnKeys.Primary)]
            public int? Id
            {
                get { return GetProperty<int?>(); }
                set { SetProperty(value); }
            }

            [Column("identifier", 20, Keys = DBColumnKeys.Code), Index("employeridentifier", true)]
            public string Identifier
            {
                get { return GetProperty<string>(); }
                set { SetProperty(value); }
            }

            [Column("positionid")]
            public int? PositionId
            {
                get { return GetProperty<int?>(); }
                set { SetProperty(value); }
            }

            [Reference(nameof(PositionId))]
            public Position Position
            {
                get { return GetPropertyReference<Position>(ref position); }
                set { position = SetPropertyReference(value); }
            }

            [Column("typeid", Keys = DBColumnKeys.ElementType), DefaultValue(EmployerType.Type2)]
            public EmployerType? Type
            {
                get { return GetProperty<EmployerType?>(); }
                set { SetProperty(value); }
            }

            [Column("longid")]
            public long? LongId
            {
                get { return GetProperty<long?>(); }
                set { SetProperty(value); }
            }

            [Column("height")]
            public short? Height
            {
                get { return GetProperty<short?>(); }
                set { SetProperty(value); }
            }

            [Column("weight")]
            public float? Weight
            {
                get { return GetProperty<float?>(); }
                set { SetProperty(value); }
            }

            [Column("dweight")]
            public double? DWeight
            {
                get { return GetProperty<double?>(); }
                set { SetProperty(value); }
            }

            [Column("salary", 23, 3)]
            public decimal? Salary
            {
                get { return GetProperty<decimal?>(); }
                set { SetProperty(value); }
            }

            [Column("age")]
            public byte? Age
            {
                get { return GetProperty<byte?>(); }
                set { SetProperty(value); }
            }

            [Column("lodar")]
            public bool? Lodar
            {
                get { return GetProperty<bool?>(); }
                set { SetProperty(value); }
            }

            [Column("name", 20, Keys = DBColumnKeys.Culture)]
            public string Name
            {
                get { return GetName(); }
                set { SetName(value); }
            }
        }
    }

    public class AccessGroupBung : IAccessGroup
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsCurrentUser(IUserIdentity user) { return true; }
    }
}
