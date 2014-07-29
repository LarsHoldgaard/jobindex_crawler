using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobCrawler
{
    class Program
    {
        //private static string Keywords = "jQuery; JavaScript; Angular; Nodejs; Node.js; AngularJS; front-end; frontend";
        private static string Keywords = "ASP.NET Web Forms; ASP.NET MVC; ASP.NET; C#; MVC 4+; KnockoutJS; Knockout; Web Forms; VB.NET; MSSQL; Entity Framework; LINQ; E-conomic; Razor";
        private static string FilePath = "jobs.txt";

        static void Main(string[] args)
        {
            List<ContactViewModel> contactsAdded = new List<ContactViewModel>();
            List<ICrawler> crawlers = new List<ICrawler>();
            crawlers.Add(new JobIndexCrawler());

            var keywords = Keywords.Split(';').Select(c=>c.Trim()).ToArray();

            foreach (var crawler in crawlers)
            {
                var contacts = crawler.Execute(keywords);
                contactsAdded.AddRange(contacts);
            }

            StoreContacts(contactsAdded);

            Console.WriteLine("DONE!");
        }

        private static void StoreContacts(List<ContactViewModel> contacts)
        {
            foreach (var contactViewModel in contacts)
            {
                var storePath = "jobs/" + contactViewModel.Email + ".txt";

                // if we already have a job post stored, no need to restore it
                if (File.Exists(storePath))
                {
                    continue;
                }

                using (StreamWriter wr = new StreamWriter(storePath))
                {
                    wr.WriteLine(contactViewModel.Email);
                    wr.WriteLine(contactViewModel.JobUrl);
                    wr.WriteLine(contactViewModel.Keywords);
                    wr.WriteLine("\n\n");
                    wr.WriteLine(contactViewModel.Html);    
                    wr.Close();
                }
            }
        }

    }
}
