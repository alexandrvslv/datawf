using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace DataWF.Module.Flow
{
    public class SendEmail : IExecutable
    {
        public object Execute(ExecuteArgs arg)
        {
            var darg = (DocumentExecuteArgs)arg;
            var smtpServer = Book.DBTable.LoadByCode("smtpserver");
            if (smtpServer == null)
                throw new NullReferenceException("smtpserver book parameter not found!");

            using (var client = new SmtpClient(smtpServer.Value))
            {
                client.UseDefaultCredentials = true;
                //client.Credentials = new NetworkCredential("username", "password");

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(User.CurrentUser.EMail);
                    FillTo(message, darg);
                    message.Body = GetBody(darg);
                    message.Subject = "Documents Notify";
                    client.Send(message);
                }
            }
            return null;
        }

        public virtual void FillTo(MailMessage message, DocumentExecuteArgs arg)
        {
            if (arg.StageProcedure != null)
            {
                foreach (var department in arg.StageProcedure.GetDepartment(((Document)arg.Document).Template))
                {
                    foreach (var user in department.GetUsers())
                    {
                        message.To.Add(user.EMail);
                    }
                }
            }
        }

        public virtual string GetBody(DocumentExecuteArgs arg)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Informs you about document: {arg.Document}");
            if (arg.StageProcedure != null)
            {
                if (arg.StageProcedure.ProcedureType == StageParamProcudureType.Finish)
                {
                    builder.AppendLine($"Finish {arg.StageProcedure.Stage} stage!");
                }
                else if (arg.StageProcedure.ProcedureType == StageParamProcudureType.Start)
                {
                    builder.AppendLine($"Start {arg.StageProcedure.Stage} stage!");
                }
                else if (arg.StageProcedure.ProcedureType == StageParamProcudureType.Manual)
                {
                    builder.AppendLine($"Is on {arg.StageProcedure.Stage} stage!");
                }
            }
            builder.AppendLine($"Mailed you by {User.CurrentUser}!");
            return builder.ToString();
        }
    }
}
