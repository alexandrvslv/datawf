﻿using DataWF.Common;
using DataWF.Data;
using DataWF.Geometry;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Test.Data
{
    [TestFixture]
    public partial class TestORM
    {
        public const string SchemaName = "test";
        public const string EmployerTableName = "tb_employer";
        public const string PositionTableName = "tb_position";
        public const string FigureTableName = "tb_figure";
        private DBSchema schema;
        private IdCollection<IGroupIdentity> groups;

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DBService.Schems.Clear();
            DBTable.ClearAttributeCache();

            if (DBService.Connections.Count == 0)
                Serialization.Deserialize("connections.xml", DBService.Connections);

            AccessValue.Provider = new AccessProviderStub
            {
                Groups = new IdCollection<IGroupIdentity>
                {
                    new AccessGroupBung() { Id = 1, Name = "Group1"},
                    new AccessGroupBung() { Id = 2, Name = "Group2"},
                    new AccessGroupBung() { Id = 3, Name = "Group3"}
                }
            };
        }

        [Test]
        public Task GenerateSqlite()
        {
            return Generate(DBService.Connections["TestSqlLite"]);
        }

        [Test]
        public Task GeneratePostgres()
        {
            return Generate(DBService.Connections["TestPostgres"]);
        }

        [Test]
        public Task GenerateOracle()
        {
            return Generate(DBService.Connections["TestOracle"]);
        }

        [Test]
        public Task GenerateMySql()
        {
            return Generate(DBService.Connections["TestMySql"]);
        }

        [Test]
        public Task GenerateMsSql()
        {
            return Generate(DBService.Connections["TestMSSql"]);
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

            Assert.AreEqual(3, schem.Tables.Count);
            var table = schem.Tables[EmployerTableName];
            Assert.IsNotNull(table);
            Assert.IsInstanceOf<DBTable<Employer>>(table);
            var column = table.Columns["id"];
            Assert.IsNotNull(column);
            Assert.AreEqual(typeof(int), column.DataType);
        }

        public async Task Generate(DBConnection connection)
        {
            Assert.AreEqual(true, connection.CheckConnection(), $"Connection Fail!");
            schema = DBSchema.Generate(GetType().Assembly, SchemaName);

            Assert.IsNotNull(schema, "Attribute Generator Fail. On Schema");
            Assert.IsNotNull(Employer.DBTable, "Attribute Generator Fail. On Employer Table");
            Assert.IsNotNull(Position.DBTable, "Attribute Generator Fail. On Position Table");
            Assert.IsNotNull(Figure.DBTable, "Attribute Generator Fail. On Figure Table");

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
            result = schema.GetTablesInfo(connection.Schema, FigureTableName);
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
                    new AccessItem(groups.First(i => i.Id == 1), AccessType.Read),
                    new AccessItem(groups.First(i => i.Id == 2), AccessType.Admin),
                    new AccessItem(groups.First(i => i.Id == 3), AccessType.Create)
                })
            };
            Assert.AreEqual(employer.Type, EmployerType.Type2, "Default Value & Enum");

            employer.GenerateId();
            Assert.NotNull(employer.Id, "Id Generator Fail");

            await employer.Save();
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
            Assert.AreEqual(3, accessValue.Items.Count(), "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items.ElementAt(0).Read, "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items.ElementAt(1).Admin, "Insert sql Fail Byte Array");
            Assert.AreEqual(false, accessValue.Items.ElementAt(2).Delete, "Insert sql Fail Byte Array");

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

            await Position.DBTable.Save();
            Position.DBTable.Clear();
            var positions = await Position.DBTable.LoadAsync();
            Assert.AreEqual(7, positions.Count(), "Insert/Read several positions Fail");

            //Insert Geometry
            var polygon = new Polygon2D(new Point2D[] { new Point2D(10D, 10D), new Point2D(10D, 20D), new Point2D(20D, 20D), new Point2D(20D, 10D) });
            var bounds = polygon.Bounds;
            Assert.AreEqual(new Rectangle2D(10, 10, 20, 20), bounds, "Geometry Polygon get Bounds!");
            var location = bounds.BottomLeft;
            Assert.AreEqual(new Point2D(10, 10), location, "Geometry Rectangle get BottomLeft!");
            var matrix = Matrix2D.CreateIdentity();

            var polygon125 = new Polygon2D(new Point2D[] { new Point2D(-10D, -10D), new Point2D(-10D, 10D), new Point2D(10D, 10D), new Point2D(10D, -10D) });

            Figure.DBTable.Add(new Figure()
            {
                Matrix = matrix,
                Location = location,
                Box = bounds,
                Polygon = polygon
            });
            Figure.DBTable.Add(new Figure()
            {
                Id = 125,
                Matrix = Matrix2D.CreateIdentity(),
                Location = polygon125.Bounds.BottomLeft,
                Box = polygon125.Bounds,
                Polygon = polygon125
            });
            await Figure.DBTable.Save();
            Figure.DBTable.Clear();
            var figures = await Figure.DBTable.LoadAsync();
            Assert.AreEqual(2, figures.Count(), "Insert/Read several figures Fail");
            var figure = Figure.DBTable.LoadById(125);
            Assert.IsNotNull(figure, "Insert/Read figure Id 125 Fail");
            Assert.AreEqual(new Point2D(-10, -10), figure.Location, "Read/Write Geometry Point Fail!");
            Assert.AreEqual(new Rectangle2D(-10, -10, 10, 10), figure.Box, "Read/Write Geometry Rectangle Fail!");
            Assert.AreEqual(new Matrix2D(1, 0, 0, 0, 1, 0, 0, 0, 1), figure.Matrix, "Read/Write Geometry Matrix Fail!");
            Assert.AreEqual(polygon125, figure.Polygon, "Read/Write Geometry Polygon Fail!");

            //GetById
            employer = Employer.DBTable.LoadById(1);
            Assert.IsNotNull(employer, "GetById Fail");
            position = Position.DBTable.LoadById(4);
            Assert.IsNotNull(position, "GetById Fail");
            //Update
            employer.Position = position;
            await employer.Save();

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
            await table.Save();

            table.Clear();

            int c = 0;
            foreach (var item in table.Load(string.Empty))
            {
                if (++c == 5)
                    break;
            }
        }

        public class EmployerTable : DBTable<Employer>
        {
        }
    }
}
