using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    public class TransactFileStreamResult : FileStreamResult
    {
        public TransactFileStreamResult(Stream stream, string octet, DBTransaction transaction, string fileName)
            : base(stream, octet)
        {
            Transaction = transaction;
            FileDownloadName = fileName;
        }

        public bool DeleteFile { get; set; }

        public DBTransaction Transaction { get; }

        private void OnFileDelete()
        {
            if (DeleteFile && FileStream is FileStream fileStream)
            {
                try { File.Delete(fileStream.Name); }
                catch (Exception ex) { Helper.OnException(ex); }
            }
        }

        public override void ExecuteResult(ActionContext context)
        {
            try
            {

                base.ExecuteResult(context);
                base.FileStream.Close();
                Transaction?.Commit();
                OnFileDelete();
            }
            finally
            {
                Transaction?.Dispose();
            }
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            try
            {
                await base.ExecuteResultAsync(context);
                base.FileStream.Close();
                Transaction?.Commit();
                OnFileDelete();
            }
            finally
            {
                Transaction?.Dispose();
            }
        }

    }
}