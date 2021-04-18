using DataWF.Common;
using DataWF.Data;
using DataWF.Geometry;
using DocumentFormat.OpenXml.Bibliography;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Test.Data
{

    [TestFixture]
    public partial class TestORM
    {
        public const string SchemaName = "test";
        public const string EmployerTableName = "tb_employer";
        public const string TestColumnsTableName = "tb_test_column";
        public const string PositionTableName = "tb_position";
        public const string FigureTableName = "tb_figure";
        public const string FileTableName = "tb_file";
        private TestSchema schema;

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            schema = new TestSchema();

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
        public async Task GenerateSqlite()
        {
            await Generate(DBService.Connections["TestSqlLite"]);
        }

        [Test]
        public async Task GeneratePostgresWithLOB()
        {
            await Generate(DBService.Connections["TestPostgresLOB"]);
        }

        [Test]
        public async Task GeneratePostgresWithBLOB()
        {
            await Generate(DBService.Connections["TestPostgresBLOB"]);
        }

        [Test]
        public async Task GeneratePostgres()
        {
            await Generate(DBService.Connections["TestPostgres"]);
        }

        [Test]
        public async Task GenerateOracle()
        {
            await Generate(DBService.Connections["TestOracle"]);
        }

        [Test]
        public async Task GenerateMySql()
        {
            await Generate(DBService.Connections["TestMySql"]);
        }

        [Test]
        public async Task GenerateMsSql()
        {
            await Generate(DBService.Connections["TestMSSql"]);
        }

        [Test]
        public void SchemaReplicate()
        {
            var schema1 = new TestSchema()
            {
                Name = "test_schema1",
                Connection = new DBConnection
                {
                    System = DBSystem.SQLite,
                    DataBase = "replicant1",
                    DataBaseId = 1
                }

            };
            schema1.Generate("test_schema1");

            var rService1 = new ReplicationService(new ReplicationSettings
            {
                Instance = new SRInstance
                {
                    Host = "localhost",
                    Port = 51001
                },
                Schems = new List<SRSchema>(new[] { new SRSchema { SchemaName = schema1.Name } })
            });

            var schema2 = new TestSchema()
            {
                Name = "test_schema2",
                Connection = new DBConnection
                {
                    System = DBSystem.SQLite,
                    DataBase = "replicant2",
                    DataBaseId = 2
                }
            };
            schema2.Generate("test_schema2");


            var rService2 = new ReplicationService(new ReplicationSettings
            {
                Instance = new SRInstance
                {
                    Host = "localhost",
                    Port = 51002
                },
                Schems = new List<SRSchema>(new[] { new SRSchema { SchemaName = schema2.Name } })
            });

        }

        [Test]
        public void SchemaSerialization()
        {
            DBService.Schems.Clear();
            DBService.Schems.Add(schema);
            DBService.Schems.Add(schema.LogSchema);

            var buffer = Serialization.Instance.Serialize(DBService.Schems);
            PrintBuffer(buffer);

            DBService.Schems.Clear();
            Serialization.Instance.Deserialize(buffer, DBService.Schems);

            Assert.AreEqual(2, DBService.Schems.Count);
            Assert.AreEqual(6, schema.Tables.Count);

            var table = schema.Tables[EmployerTableName];

            Assert.IsNotNull(table);
            Assert.IsInstanceOf<DBTable<Employer>>(table);

            var column = table.Columns["id"];

            Assert.IsNotNull(column);
            Assert.AreEqual(typeof(int?), column.DataType);

            void PrintBuffer(ArraySegment<byte> buffer)
            {
                var text = System.Text.Encoding.UTF8.GetString(buffer);
                using (var reader = new StringReader(text))
                {
                    for (; ; )
                    {
                        var line = reader.ReadLine();
                        if (line != null)
                            Debug.WriteLine(line);
                        else
                            break;
                    }
                }
            }
        }

        public async Task Generate(DBConnection connection)
        {
            Assert.AreEqual(true, connection.CheckConnection(true), $"Connection Fail!");
            schema.Generate(null);

            var employerTable = schema.Employer;
            var positionTable = schema.Position;
            var figureTable = schema.Figure;
            var fileDataTable = schema.FileData;
            var fileStoreTable = schema.FileStore;
            var testColumnsTable = schema.TestColumns;
            Assert.IsNotNull(employerTable, "Attribute Generator Fail. On Employer Table");
            Assert.IsNotNull(positionTable, "Attribute Generator Fail. On Position Table");
            Assert.IsNotNull(figureTable, "Attribute Generator Fail. On Figure Table");
            Assert.IsNotNull(fileDataTable, "Attribute Generator Fail. On FileData Table");
            Assert.IsNotNull(fileStoreTable, "Attribute Generator Fail. On FileStore Table");
            Assert.IsNotNull(testColumnsTable, "Attribute Generator Fail. On FileStore Table");

            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.IntId)) is DBColumn<int>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.IntUnsigned)) is DBColumn<uint>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.IntNullable)) is DBColumn<int?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Short)) is DBColumn<short>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.ShortUnsigned)) is DBColumn<ushort>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.ShortNullable)) is DBColumn<short?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Long)) is DBColumn<long>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.LongNullable)) is DBColumn<long?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Byte)) is DBColumn<byte>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.ByteSigned)) is DBColumn<sbyte>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.ByteNullable)) is DBColumn<byte?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Float)) is DBColumn<float>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.FloatNullable)) is DBColumn<float?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Double)) is DBColumn<double>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.DoubleNullable)) is DBColumn<double?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Decimal)) is DBColumn<decimal>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.DecimalNullable)) is DBColumn<decimal?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.Bool)) is DBColumn<bool>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.BoolNullable)) is DBColumn<bool?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.DateTime)) is DBColumn<DateTime>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.DateTimeNullable)) is DBColumn<DateTime?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.TimeSpan)) is DBColumn<TimeSpan>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.TimeSpanNullable)) is DBColumn<TimeSpan?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumByte)) is DBColumn<UInt8Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumByteSigned)) is DBColumn<Int8Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumByteNullable)) is DBColumn<UInt8Enum?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumShort)) is DBColumn<Int16Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumShortUnsigned)) is DBColumn<UInt16Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumShortNullable)) is DBColumn<Int16Enum?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumInt)) is DBColumn<Int32Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumIntUnsigned)) is DBColumn<UInt32Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumIntNullable)) is DBColumn<Int32Enum?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumLong)) is DBColumn<Int64Enum>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.EnumLongNullable)) is DBColumn<Int64Enum?>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.ByteArray)) is DBColumn<byte[]>);
            Assert.IsTrue(testColumnsTable.ParseProperty(nameof(TestColumns.String)) is DBColumn<string>);

            var idColumn = employerTable.Columns["id"];
            Assert.IsNotNull(idColumn, "Attribute Generator Fail. On Column Employer Id");
            var positionColumn = employerTable.Columns["positionid"];
            Assert.IsNotNull(positionColumn, "Attribute Generator Fail. On Column Employer Position");
            Assert.IsNotNull(positionColumn.ReferenceTable, "Attribute Generator Fail. On Column Employer Position Reference");

            if (Directory.Exists(connection.GetFilesPath()))
                Directory.Delete(connection.GetFilesPath(), true);
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
            var tcDefault = new TestColumns(testColumnsTable)
            {
                IntId = 1,
                String = "Default Values"
                //DateTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value//MSSQL
            };
            var tcFillNull = new TestColumns(testColumnsTable)
            {
                IntId = 2,
                String = "Nullable Default Values",
                BoolNullable = false,
                //DateTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value,//MSSQL
                DateTimeNullable = new DateTime(2000, 01, 01),
                TimeSpanNullable = TimeSpan.Zero,
                ByteNullable = 0,
                IntNullable = 0,
                ShortNullable = 0,
                LongNullable = 0,
                DecimalNullable = 0M,
                DoubleNullable = 0D,
                FloatNullable = 0F,
                EnumByteNullable = UInt8Enum.Min,
                EnumIntNullable = Int32Enum.Default,
                EnumLongNullable = Int64Enum.Default,
                EnumShortNullable = Int16Enum.Default,
            };
            var tcFillMax = new TestColumns(testColumnsTable)
            {
                IntId = 3,
                String = "Max Values",
                Bool = true,
                BoolNullable = true,
                DateTimeNullable = DateTime.Now,
                TimeSpanNullable = new TimeSpan(1, 1, 1),
                Byte = byte.MaxValue,
                ByteSigned = sbyte.MaxValue,
                ByteNullable = byte.MaxValue,
                IntUnsigned = uint.MaxValue,
                IntNullable = int.MaxValue,
                Short = short.MaxValue,
                ShortUnsigned = ushort.MaxValue,
                ShortNullable = short.MaxValue,
                Long = long.MaxValue,
                LongNullable = long.MaxValue,
                Decimal = decimal.MaxValue,
                DecimalNullable = decimal.MaxValue,
                Double = double.MaxValue,
                DoubleNullable = double.MaxValue,
                Float = float.MaxValue,
                FloatNullable = float.MaxValue,
                EnumLong = Int64Enum.Max,
                EnumLongNullable = Int64Enum.Max,
                EnumInt = Int32Enum.Max,
                EnumIntUnsigned = UInt32Enum.Max,
                EnumIntNullable = Int32Enum.Max,
                EnumShort = Int16Enum.Max,
                EnumShortUnsigned = UInt16Enum.Max,
                EnumShortNullable = Int16Enum.Max,
                EnumByte = UInt8Enum.Max,
                EnumByteSigned = Int8Enum.Max,
                EnumByteNullable = UInt8Enum.Max,
            };
            testColumnsTable.Add(tcDefault);
            testColumnsTable.Add(tcFillNull);
            testColumnsTable.Add(tcFillMax);

            await testColumnsTable.Save();

            var tcresult = schema.Connection.ExecuteQResult(testColumnsTable.BuildQuery("", "a") + " order by int_value");
            Assert.AreEqual(testColumnsTable.Count, tcresult.Values.Count, "Insert sql Fail");
            for (int r = 0; r < tcresult.Values.Count; r++)
            {
                for (int i = 0; i < tcresult.Columns.Count; i++)
                {
                    var column = testColumnsTable.Columns[i];
                    var cvalue = testColumnsTable[r].GetValue(column);
                    var rvalue = column.ParseValue(tcresult.Values[r][i]);
                    if (column.DBDataType == DBDataType.DateTime
                        && cvalue is DateTime cdate && rvalue is DateTime rdate)
                    {
                        cvalue = cdate.AddTicks(-(cdate.Ticks % TimeSpan.TicksPerSecond));
                        rvalue = rdate.AddTicks(-(rdate.Ticks % TimeSpan.TicksPerSecond));
                    }
                    Assert.AreEqual(cvalue, rvalue, $"Insert sql Fail {column}");
                }
            }

            var employer = new Employer(employerTable)
            {
                Identifier = $"{1:8}",
                IsActive = true,
                Age = 40,
                LongId = 120321312321L,
                Salary = 231323.32M,
                Name = "Ivan",
                Access = new AccessValue(new[]
                {
                    new AccessItem(AccessValue.Provider.GetAccessIdentity(1, IdentityType.Group), AccessType.Read),
                    new AccessItem(AccessValue.Provider.GetAccessIdentity(2, IdentityType.Group), AccessType.Admin),
                    new AccessItem(AccessValue.Provider.GetAccessIdentity(3, IdentityType.Group), AccessType.Create)
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
            Assert.AreEqual(employer.LongId, qresult.Get(0, "longid"), "Insert sql Fail Long");
            Assert.AreEqual(employer.Salary, employerTable.ParseProperty(nameof(Employer.Salary)).ParseValue(qresult.Get(0, "salary")), "Insert sql Fail Decimal");

            var lodar = qresult.Get(0, "is_active").ToString();
            Assert.IsTrue(lodar == "1" || lodar == "True", "Insert sql Fail Bool");
            Assert.IsInstanceOf<byte[]>(qresult.Get(0, "group_access"), "Insert sql Fail Byte Array");

            var accessValue = new AccessValue((byte[])qresult.Get(0, "group_access"));
            Assert.AreEqual(3, accessValue.Items.Count(), "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items.ElementAt(0).Read, "Insert sql Fail Byte Array");
            Assert.AreEqual(true, accessValue.Items.ElementAt(1).Admin, "Insert sql Fail Byte Array");
            Assert.AreEqual(false, accessValue.Items.ElementAt(2).Delete, "Insert sql Fail Byte Array");

            employerTable.Clear();
            Assert.AreEqual(0, employerTable.Count, "Clear table Fail");

            //Insert Several
            Position position = GeneratePositions(positionTable);
            var nullIds = positionTable.Select(positionTable.PrimaryKey, CompareType.Is, (object)null).ToList();
            Assert.AreEqual(6, nullIds.Count, "Select by null Fail");

            await positionTable.Save();
            positionTable.Clear();
            var positions = await positionTable.LoadAsync();
            Assert.AreEqual(7, positions.Count(), "Insert/Read several positions Fail");

            //GetById
            employer = employerTable.LoadById(1);
            Assert.IsNotNull(employer, "GetById Fail");
            position = positionTable.LoadById(4);
            Assert.IsNotNull(position, "GetById Fail");
            //Update
            employer.Position = position;
            await employer.Save();

            qresult = schema.Connection.ExecuteQResult($"select * from {EmployerTableName}");
            Assert.AreEqual(4, qresult.Get(0, "positionid"), "Update sql Fail");

            //Insert Geometry
            var polygon = new Polygon2D(new Point2D[] { new Point2D(10D, 10D), new Point2D(10D, 20D), new Point2D(20D, 20D), new Point2D(20D, 10D) });
            var bounds = polygon.Bounds;
            Assert.AreEqual(new Rectangle2D(10, 10, 20, 20), bounds, "Geometry Polygon get Bounds!");
            var location = bounds.BottomLeft;
            Assert.AreEqual(new Point2D(10, 10), location, "Geometry Rectangle get BottomLeft!");
            var matrix = Matrix2D.CreateIdentity();

            var polygon125 = new Polygon2D(new Point2D[] { new Point2D(-10D, -10D), new Point2D(-10D, 10D), new Point2D(10D, 10D), new Point2D(10D, -10D) });

            figureTable.Add(new Figure(figureTable)
            {
                Matrix = matrix,
                Location = location,
                Box = bounds,
                Polygon = polygon
            });
            figureTable.Add(new Figure(figureTable)
            {
                Id = 125,
                Matrix = Matrix2D.CreateIdentity(),
                Location = polygon125.Bounds.BottomLeft,
                Box = polygon125.Bounds,
                Polygon = polygon125
            });
            await figureTable.Save();
            figureTable.Clear();
            var figures = await figureTable.LoadAsync();
            Assert.AreEqual(2, figures.Count(), "Insert/Read several figures Fail");
            var figure = figureTable.LoadById(125);
            Assert.IsNotNull(figure, "Insert/Read figure Id 125 Fail");
            Assert.AreEqual(new Point2D(-10, -10), figure.Location, "Read/Write Geometry Point Fail!");
            Assert.AreEqual(new Rectangle2D(-10, -10, 10, 10), figure.Box, "Read/Write Geometry Rectangle Fail!");
            Assert.AreEqual(new Matrix2D(1, 0, 0, 0, 1, 0, 0, 0, 1), figure.Matrix, "Read/Write Geometry Matrix Fail!");
            Assert.AreEqual(polygon125, figure.Polygon, "Read/Write Geometry Polygon Fail!");

            //Files
            var file = new FileStore(fileStoreTable) { Id = 1, FileName = "test.pdf", FileLastWrite = DateTime.UtcNow };
            ArraySegment<byte> buffer;
            using (var transaction = new DBTransaction(fileStoreTable))
            {
                using (var stream = new FileStream("test.pdf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    buffer = Helper.GetBytes(stream);
                    stream.Position = 0;
                    await file.SetBlob(stream, transaction);
                }
                transaction.Commit();
            }
            using (var transaction = new DBTransaction(fileStoreTable))
            {
                using (var stream = await file.GetBlob(transaction))
                {
                    var newBuffer = Helper.GetBytes(stream);
                    Assert.IsTrue(Helper.CompareByte(newBuffer, buffer), "Get/Set BLOB Fail!");
                }
            }

            result = schema.GetTablesInfo(connection.Schema, TestColumnsTableName);

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
                row["id"] = i + 1;
                row["test_date"] = DateTime.Now.AddDays(-i);
                row["test_varchar"] = "string value " + i;
                row["test_numeric"] = i / 1000M;
                table.Add(row);
            }
            await table.Save();

            table.Clear();

            table.Load(string.Empty);
            Assert.AreEqual(1000, table.Count, "Read/Write Geometry Rectangle Fail!");
        }

        private static Position GeneratePositions(PositionTable<Position> positionTable)
        {
            positionTable.Add(new Position(positionTable) { Code = "1", Name = "First Position" });
            positionTable.Add(new Position(positionTable) { Code = "2", Name = "Second Position" });
            var position = new Position(positionTable) { Id = 0, Code = "3", Name = "Group Position" };
            position.Attach();
            var sposition = new Position(positionTable) { Code = "4", Parent = position, Name = "Sub Group Position" };
            sposition.Attach();

            //Select from internal Index
            positionTable.Add(new Position(positionTable) { Code = "t1", Name = "Null Index" });
            positionTable.Add(new Position(positionTable) { Code = "t2", Name = "Null Index" });
            positionTable.Add(new Position(positionTable) { Code = "t3", Name = "Null Index" });
            return position;
        }
    }
}
