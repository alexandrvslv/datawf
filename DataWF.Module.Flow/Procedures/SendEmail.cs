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
                    message.To.Add("receiver@me.com");
                    message.Body = "body";
                    message.Subject = "subject";
                    client.Send(message);
                }
            }
            return null;
        }
    }
}
