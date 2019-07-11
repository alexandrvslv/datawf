using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public class TransactFileStreamResult : FileStreamResult
    {
        public TransactFileStreamResult(Stream stream, string octet, DBTransaction transaction, string fileName)
            : base(stream, octet)
        {
            Transaction = transaction;
            FileDownloadName = fileName;
        }

        public DBTransaction Transaction { get; }

        public override void ExecuteResult(ActionContext context)
        {
            try
            {

                base.ExecuteResult(context);
                Transaction.Commit();
            }
            finally
            {
                Transaction.Dispose();
            }
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            try
            {
                await base.ExecuteResultAsync(context);
                Transaction.Commit();
            }
            finally
            {
                Transaction.Dispose();
            }
        }

    }
}