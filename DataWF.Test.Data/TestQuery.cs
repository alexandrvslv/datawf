﻿using DataWF.Common;
using DataWF.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        public void ColumnIndex()
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
        public void ColumnSelect()
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
        public void ParseQueryAutoJoin()
        {
            string queryText = $@"
select *
from Employer    
where Position.Code in ('2','3')";
            var query = employers.Query(queryText);

            Assert.AreEqual(2, query.Tables.Count);
            var baseTable = query.Tables[0];
            Assert.AreEqual(employers, baseTable.Table);
            var joinTable = query.Tables[1];
            Assert.AreEqual(positions, joinTable.Table);
            Assert.AreEqual(JoinType.Left, joinTable.Join);

            Assert.AreEqual(1, query.Parameters.Count);

            Assert.AreEqual(positions.CodeKey, query.Parameters[0].LeftColumn);
        }

        [Test]
        public void ParseQueryNoPrefix()
        {
            string queryText = $@"
select Id
    , DateCreate
    , (select NameEN
        from Position
        where Id = Employer.PositionId )    
from Employer    
where (emp.Id != 1 or emp.Id = 1)
    and DateCreate is not null
    and DateCreate between '2000-01-01' and '3000-01-01'
    and PositionId in (select Id
                           from Position
                           where Code in ('2','3'))";
            var query = employers.Query(queryText);

            Assert.AreEqual(3, query.Columns.Count, "Parse select columns Fail");

            Assert.IsAssignableFrom<QColumn>(query.Columns[0]);
            var idColumn = (QColumn)query.Columns[0];
            Assert.AreEqual(employers.IdKey, idColumn.Column);

            Assert.IsAssignableFrom<QColumn>(query.Columns[1]);
            var dateColumn = (QColumn)query.Columns[1];
            Assert.AreEqual(employers.DateCreateKey, dateColumn.Column);

            Assert.IsAssignableFrom<QQuery<DBItem>>(query.Columns[2]);
            var posSubQuery = (QQuery<DBItem>)query.Columns[2];
            Assert.AreEqual(positions.NameENKey, ((QColumn)posSubQuery.Columns[0]).Column);
            Assert.AreEqual(positions, posSubQuery.Tables[0].Table);
            Assert.AreEqual(positions.IdKey, posSubQuery.Parameters[0].LeftColumn);
            Assert.AreEqual(employers.PositionIdKey, posSubQuery.Parameters[0].RightColumn);

            Assert.AreEqual(1, query.Tables.Count);
            var baseTable = query.Tables[0];
            Assert.AreEqual(employers, baseTable.Table);

        }

        [Test]
        public void ParseQuery()
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
where ((Id != 1 or Id = 1) and (Id <= 5 or Id >= 5))
    and emp.DateCreate is not null
    and emp.DateCreate between '2000-01-01' and '3000-01-01'
    and emp.PositionId in (select subPos2.Id
                           from Position subPos2 
                           where subPos2.Code in ('2','3'))";
            var query = employers.Query(queryText);

            Assert.AreEqual(4, query.Columns.Count);

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
            Assert.IsAssignableFrom<QParam>(joinTable.On);

            var onParam = joinTable.On;
            Assert.IsAssignableFrom<QColumn>(onParam.LeftItem);
            Assert.AreEqual("pos", onParam.LeftItem.TableAlias);
            var onLeftColumn = onParam.LeftQColumn.Column;
            Assert.AreEqual(positions.IdKey, onLeftColumn);

            Assert.AreEqual(CompareType.Equal, onParam.Comparer);

            Assert.IsAssignableFrom<QColumn>(onParam.RightItem);
            Assert.AreEqual("emp", onParam.RightItem.TableAlias);
            var onRightColumn = onParam.RightQColumn.Column;
            Assert.AreEqual(employers.PositionIdKey, onRightColumn);

            Assert.AreEqual(4, query.Parameters.Count);

            var groupParam = query.Parameters[0];
            Assert.AreEqual(true, groupParam.IsCompaund);
            Assert.AreEqual(2, groupParam.Parameters.OfType<QParam>().Count());

            var subGroup = (QParam)groupParam.Parameters[0];
            Assert.AreEqual(true, subGroup.IsCompaund);
            Assert.AreEqual(2, subGroup.Parameters.OfType<QParam>().Count());

            var groupParam1 = (QParam)subGroup.Parameters[0];
            Assert.AreEqual("emp", groupParam1.LeftItem.TableAlias);
            Assert.AreEqual(employers.IdKey, groupParam1.LeftColumn);
            Assert.AreEqual(CompareType.NotEqual, groupParam1.Comparer);
            Assert.IsAssignableFrom<QValue>(groupParam1.RightItem);
            Assert.AreEqual(1, groupParam1.RightItem.GetValue<Employer>());

            var groupParam2 = (QParam)subGroup.Parameters[1];
            Assert.AreEqual(LogicType.Or, groupParam2.Logic);
            Assert.AreEqual("emp", groupParam2.LeftItem.TableAlias);
            Assert.AreEqual(employers.IdKey, groupParam2.LeftColumn);
            Assert.AreEqual(CompareType.Equal, groupParam2.Comparer);
            Assert.IsAssignableFrom<QValue>(groupParam2.RightItem);
            Assert.AreEqual(1, groupParam2.RightItem.GetValue<Employer>());

            var param2 = query.Parameters[1];
            Assert.AreEqual(false, param2.IsCompaund);
            Assert.AreEqual(LogicType.And, param2.Logic);
            Assert.AreEqual("emp", param2.LeftItem.TableAlias);
            Assert.AreEqual(employers.DateCreateKey, param2.LeftColumn);
            Assert.AreEqual(CompareType.IsNot, param2.Comparer);

            var param3 = query.Parameters[2];
            Assert.AreEqual(false, param3.IsCompaund);
            Assert.AreEqual(LogicType.And, param3.Logic);
            Assert.AreEqual("emp", param3.LeftItem.TableAlias);
            Assert.AreEqual(employers.DateCreateKey, param3.LeftColumn);
            Assert.AreEqual(CompareType.Between, param3.Comparer);
            Assert.IsAssignableFrom<QBetween>(param3.RightItem);
            var between = (QBetween)param3.RightItem;
            Assert.AreEqual(new DateTime(2000, 01, 01), between.MinValue());
            Assert.AreEqual(new DateTime(3000, 01, 01), between.MaxValue());

            var param4 = query.Parameters[3];
            Assert.AreEqual(false, param4.IsCompaund);
            Assert.AreEqual(LogicType.And, param4.Logic);
            Assert.AreEqual("emp", param4.LeftItem.TableAlias);
            Assert.AreEqual(employers.PositionIdKey, param4.LeftColumn);
            Assert.AreEqual(CompareType.In, param4.Comparer);
            Assert.IsAssignableFrom<QQuery<DBItem>>(param4.RightItem);

            var paramQuery = (QQuery<DBItem>)param4.RightItem;
            Assert.AreEqual(positions.IdKey, ((QColumn)paramQuery.Columns[0]).Column);
            Assert.AreEqual("subPos2", paramQuery.Columns[0].TableAlias);
            Assert.AreEqual(positions, paramQuery.Tables[0].Table);
            Assert.AreEqual("subPos2", paramQuery.Tables[0].TableAlias);
            Assert.AreEqual(positions.CodeKey, paramQuery.Parameters[0].LeftColumn);
            Assert.AreEqual(CompareType.In, paramQuery.Parameters[0].Comparer);
            Assert.AreEqual("subPos2", paramQuery.Parameters[0].LeftItem.TableAlias);
            Assert.IsAssignableFrom<QArray>(paramQuery.Parameters[0].RightItem);
            Assert.AreEqual(2, ((QArray)paramQuery.Parameters[0].RightItem).Items.OfType<QValue>().Count());
            Assert.AreEqual("2", ((QArray)paramQuery.Parameters[0].RightItem).Items[0].GetValue<Position>());
            Assert.AreEqual("3", ((QArray)paramQuery.Parameters[0].RightItem).Items[1].GetValue<Position>());

        }

        [Test]
        public void TestBuildQuery()
        {
            var query = employers.Query();
            var posCodes = new[] { "2", "3" };
            query.Column(employers.IdKey)
                .Column(employers.DateCreateKey)
                .Column(positions.Query(query)
                                .Column(positions.NameENKey)
                                .Where(positions.IdKey, CompareType.Equal, employers.PositionIdKey))
                .Join(employers.PositionIdKey)
                .Column(QFunctionType.trim, positions.NameENKey)
                .Where(pTopGoup => pTopGoup.And(pGoup => pGoup.And(employers.IdKey, CompareType.NotEqual, 1)
                                                                .Or(employers.IdKey, CompareType.Equal, 1))
                                           .And(pGoup => pGoup.And(employers.IdKey, CompareType.LessOrEqual, 5)
                                                                .Or(employers.IdKey, CompareType.GreaterOrEqual, 5)))
                .And(employers.DateCreateKey, CompareType.IsNot, null)
                .And(employers.DateCreateKey, CompareType.Between, new DateInterval(new DateTime(2000, 01, 01), new DateTime(3000, 01, 01)))
                .And(employers.PositionIdKey, CompareType.In, positions.Query(query)
                                                                    .Column(positions.IdKey)
                                                                    .Where(positions.CodeKey, CompareType.In, posCodes));

            Console.WriteLine(query.FormatAll());


            var posSubQuery = ((IEnumerable<Position>)positions).Where(p => posCodes.Contains(p.Code)).Select(p => p.Id);
            var linqQuery = from emp in ((IEnumerable<Employer>)employers)
                            join pos in ((IEnumerable<Position>)positions) on emp.PositionId equals pos.Id
                            where ((emp.Id != 1 || emp.Id == 1) && (emp.Id <= 5 || emp.Id >= 5))
                                && emp.DateCreate != default(DateTime)
                                && (emp.DateCreate >= new DateTime(2000, 01, 01) && emp.DateCreate <= new DateTime(3000, 01, 01)
                                && posSubQuery.Contains(emp.PositionId))
                            select new
                            {
                                ID = emp.Id,
                                DateCerate = emp.DateCreate,
                                SubPosName = ((IEnumerable<Position>)positions).FirstOrDefault(p => p.Id == emp.Id)?.Name,
                                PosName = pos.Name.Trim()
                            };


            var command = query.ToCommand();
            var list = schema.Connection.ExecuteQResult(command);

            var linqCount = linqQuery.Count();
            var queryExecuteCount = list.Values.Count;
            var qqueryCount = query.Select().Count();
            Assert.AreEqual(linqCount, queryExecuteCount);
            Assert.AreEqual(linqQuery.Count(), qqueryCount);

        }
    }
}
