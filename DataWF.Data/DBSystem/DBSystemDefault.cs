using System;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DataWF.Data
{
    public class DBSystemDefault : DBSystem
    {
        public override IDbConnection CreateConnection(DBConnection connection)
        {
            throw new NotImplementedException();
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            throw new NotImplementedException();
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            throw new NotImplementedException();
        }

        public override string GetConnectionString(DBConnection connection)
        {
            throw new NotImplementedException();
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            throw new NotImplementedException();
        }

        public override DbProviderFactory GetFactory()
        {
            throw new NotImplementedException();
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            throw new NotImplementedException();
        }

        public override string SequenceInline(DBSequence sequence)
        {
            throw new NotImplementedException();
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            throw new NotImplementedException();
        }
    }
}
