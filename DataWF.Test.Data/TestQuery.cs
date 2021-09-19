using DataWF.Common;
using DataWF.Data;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Test.Data
{
    [TestFixture]
    public class TestQuery
    {
        private TestProvider provider;
        private TestSchema schema;
        private EmployerTable<Employer> employers;
        private PositionTable positions;

        [SetUp]
        public async Task Setup()
        {
            provider = new TestProvider();
            await provider.CreateNew();

            schema = provider.Schema;
            employers = schema.Employer;
            positions = schema.Position;
        }

        [Test]
        public void TestColumnIndex()
        {
            var resultOne = employers.IdKey.SelectOne<Employer>(1);
            Assert.IsNotNull(resultOne, "Select by Index Equal Fail");

            var result = employers.IdKey.SelectIndex<Employer>(CompareType.NotEqual, 1);
            Assert.AreEqual(employers.Count - 1, result.Count(), "Select by Index NotEqual Fail");

            result = employers.IdKey.SelectIndex<Employer>(CompareType.Greater, 1);
            Assert.AreEqual(employers.Count - 1, result.Count(), "Select by Index Field.Greater Fail");

            result = employers.IdKey.SelectIndex<Employer>(CompareType.GreaterOrEqual, 1);
            Assert.AreEqual(employers.Count, result.Count(), "Select by Index Field.GreaterOrEqual Fail");

            result = employers.IdKey.SelectIndex<Employer>(CompareType.Less, 2);
            Assert.AreEqual(1, result.Count(), "Select by Index Field.Less Fail");

            result = employers.IdKey.SelectIndex<Employer>(CompareType.LessOrEqual, 2);
            Assert.AreEqual(2, result.Count(), "Select by Index Field.LessOrEqual Fail");

            result = employers.IdKey.SelectIndex<Employer>(CompareType.In, new[] { 2, 5 });
            Assert.AreEqual(2, result.Count(), "Select by Index Field.In Fail");

            result = employers.IdKey.SelectIndex<Employer>(CompareType.NotIn, new[] { 2, 5 });
            Assert.AreEqual(employers.Count - 2, result.Count(), "Select by Index Field.NotIn Fail");
        }

        [Test]
        public void TestColumnSelect()
        {
            var result = positions.NameENKey.Select<Position>(CompareType.Equal, "First Position");
            Assert.AreEqual(1, result.Count(), "Select by Name.Equal Fail");

            result = positions.NameENKey.Select<Position>(CompareType.NotEqual, "First Position");
            Assert.AreEqual(4, result.Count(), "Select by Name.NotEqual Fail");

            result = positions.NameENKey.Select<Position>(CompareType.Like, "%Position");
            Assert.AreEqual(5, result.Count(), "Select by Name.Like Fail");

            result = positions.NameENKey.Select<Position>(CompareType.NotLike, "Sub%");
            Assert.AreEqual(3, result.Count(), "Select by Name.NotLike Fail");

            result = positions.NameENKey.Select<Position>(CompareType.Greater, "First Position");
            Assert.AreEqual(4, result.Count(), "Select by Name.Greater Fail");

            result = positions.NameENKey.Select<Position>(CompareType.GreaterOrEqual, "First Position");
            Assert.AreEqual(5, result.Count(), "Select by Name.GreaterOrEqual Fail");

            result = positions.NameENKey.Select<Position>(CompareType.Less, "Second Position");
            Assert.AreEqual(1, result.Count(), "Select by Name.Less Fail");

            result = positions.NameENKey.Select<Position>(CompareType.LessOrEqual, "Second Position");
            Assert.AreEqual(2, result.Count(), "Select by Name.LessOrEqual Fail");

            result = positions.NameENKey.Select<Position>(CompareType.In, new[] { "First Position", "Third Position" });
            Assert.AreEqual(2, result.Count(), "Select by Name.In Fail");

            result = positions.NameENKey.Select<Position>(CompareType.NotIn, new[] { "First Position", "Third Position" });
            Assert.AreEqual(3, result.Count(), "Select by Name.NotIn Fail");
        }
        [Test]
        public void TestParseQuery()
        {
            string queryText = $@"
select emp.Id as emplyer_id
    , emp.DateCreate as emplyer_date    
    , (select subPos.NameEN
        from Position subPos 
        where subPos.Id = emp.PositionId ) as sub_query_position_name
    , concat(pos.NameEN, 'bla bla') as join_position_name
from Employer emp
    join Position pos on pos.Id = emp.PositionId
where (emp.Id != 1 or emp.Id = 1)
    and emp.DateCreate is not null
    and emp.DateCreate '2000-01-01' and '3000-01-01'
    and emp.PositionId in (select subPos2.Id
                           from Position subPos2 
                           where subPos2.Code in ('2','3'))";
            var query = employers.Query(queryText);

            Assert.AreEqual(4, query.Columns.Count, "Parse select columns Fail");

            Assert.IsAssignableFrom<QColumn>(query.Columns[0]);
            var idColumn = (QColumn)query.Columns[0];
            Assert.AreEqual(employers.IdKey, idColumn.Column);
            Assert.AreEqual("emp", idColumn.TableAlias);
            Assert.AreEqual("emplyer_id", idColumn.ColumnAlias);

            Assert.IsAssignableFrom<QColumn>(query.Columns[1]);
            var dateColumn = (QColumn)query.Columns[1];
            Assert.AreEqual(employers.DateCreateKey, dateColumn.Column);
            Assert.AreEqual("emp", dateColumn.TableAlias);
            Assert.AreEqual("emplyer_date", dateColumn.ColumnAlias);

            Assert.IsAssignableFrom<QQuery<DBItem>>(query.Columns[2]);
            var posSubQuery = (QQuery<DBItem>)query.Columns[2];
            Assert.AreEqual("sub_query_position_name", posSubQuery.ColumnAlias);
            Assert.AreEqual(positions.NameENKey, ((QColumn)posSubQuery.Columns[0]).Column);
            Assert.AreEqual(positions, posSubQuery.Tables[0].Table);
            Assert.AreEqual(positions.IdKey, posSubQuery.Parameters[0].LeftColumn);
            Assert.AreEqual(employers.PositionIdKey, posSubQuery.Parameters[0].RightColumn);

            Assert.IsAssignableFrom<QFunction>(query.Columns[3]);
            var posFunction = (QFunction)query.Columns[3];
            Assert.AreEqual(QFunctionType.concat, posFunction.Type);
            Assert.AreEqual(2, posFunction.Items.Count);
            Assert.IsAssignableFrom<QColumn>(posFunction.Items[0]);
            var posArgColumn = (QColumn)posFunction.Items[0];
            Assert.AreEqual(positions.NameENKey, posArgColumn.Column);
            Assert.IsAssignableFrom<QValue>(posFunction.Items[1]);
            var posArgString = (QValue)posFunction.Items[1];
            Assert.AreEqual("bla bla", posArgString.Value);


            Assert.AreEqual(2, query.Tables.Count);
            var baseTable = query.Tables[0];
            Assert.AreEqual(employers, baseTable.Table);
            Assert.AreEqual("emp", baseTable.TableAlias);
            var joinTable = query.Tables[1];
            Assert.AreEqual(positions, joinTable.Table);
            Assert.AreEqual("pos", joinTable.TableAlias);
            Assert.AreEqual(JoinType.Join, joinTable.Join);
        }

        [Test]
        public void TestBuildQuery()
        {
            var query = employers.Query();

            query.Column(employers.IdKey)
                .Column(employers.DateCreateKey)
                .Column(positions.Query(query)
                                .Column(positions.NameENKey)
                                .Where(positions.IdKey, CompareType.Equal, employers.PositionIdKey))
                .Join(employers.PositionIdKey)
                .Column(QFunctionType.concat, positions.NameENKey, "bla bla")
                .Where(pGoup => pGoup.And(employers.IdKey, CompareType.NotEqual, 1)
                                    .Or(employers.IdKey, CompareType.Equal, 1))
                .And(employers.DateCreateKey, CompareType.IsNot, null)
                .And(employers.DateCreateKey, CompareType.Between, new DateInterval(new DateTime(2000, 01, 01), new DateTime(3000, 01, 01)))
                .And(employers.PositionIdKey, CompareType.In, positions.Query(query)
                                                                    .Column(positions.IdKey)
                                                                    .Where(positions.CodeKey, CompareType.In, new[] { "2", "3" }));

            Console.WriteLine(query.FormatAll());

        }
    }
}
