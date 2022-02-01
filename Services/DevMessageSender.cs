using Petaframework.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework.Services
{
    public class DevMessageSender : IEmailSender, ISmsSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            Console.WriteLine($"\nEMAIL SENT: \n" +
                $"TO: {email} \n" +
                $"SUBJECT: {subject} \n" +
                $"BODY: {message} \n");

            return Task.FromResult(0);
        }

        public Task SendSmsAsync(string number, string message)
        {
            Console.WriteLine($"\nSMS SENT: \n" +
                $"TO: {number} \n" +
                $"BODY: {message} \n");

            return Task.FromResult(0);
        }
    }
}
