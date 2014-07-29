using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using HtmlAgilityPack;

namespace JobCrawler
{
    public class JobIndexCrawler : ICrawler
    {
        private string jobIndexBasePath;
        private List<string> basePaths;
        private string pagination_css;
        private string jobpost_css;
        public JobIndexCrawler()
        {
            this.basePaths = new List<string>();
            basePaths.Add("http://it.jobindex.dk/job/it/internet/koebenhavn/");
            basePaths.Add("http://it.jobindex.dk/job/it/systemudvikling/koebenhavn/");
            basePaths.Add("http://it.jobindex.dk/job/it/systemudvikling/sjaelland/");
            basePaths.Add("http://it.jobindex.dk/job/it/internet/sjaelland/");
            basePaths.Add("http://it.jobindex.dk/job/it/itdrift/koebenhavn/");

        
            this.jobIndexBasePath = "http://it.jobindex.dk/";
            this.pagination_css = "jix_pagination_pages";
            this.jobpost_css = "PaidJob";
        }

        public List<ContactViewModel> Execute(string[] keywords)
        {
            if (!basePaths.Any())
            {
                throw new ArgumentException("No job pages to search");
            }

            // all urls with keywords in
            List<ContactViewModel> contacts = new List<ContactViewModel>();

            // all jobindex stores their posts externally, we have to crawl them
            List<string> jobCrawlPosts = new List<string>();

            // for each category we want to find jobs in
            foreach (var basePath in basePaths)
            {
                extractExternalJobPostLinks(basePath, jobCrawlPosts);
            }

            Console.WriteLine(string.Format("We found {0} possible job posts!", jobCrawlPosts.Count));

            jobCrawlPosts = jobCrawlPosts.Where(c => !c.Contains(".pdf")).ToList();
            Parallel.ForEach(jobCrawlPosts, post =>
            {
                visitExternalJobPost(post, contacts, keywords);
            });

            return contacts;
        }

        private void visitExternalJobPost(string jobCrawlPost, List<ContactViewModel> urls, string[] keywords)
        {
            Console.WriteLine("Visiting link :)");
            using (var client = new WebClient())
            {
                try
                {

                    var externalHtml = client.DownloadString(jobCrawlPost);

                    foreach (var keyword in keywords)
                    {
                        // yes this job matches keyword! woohoo
                        if (externalHtml.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine("Woohoo: we hit a " + keyword + " job!");

                            var email = getEmailFromHtml(externalHtml);

                            if (!string.IsNullOrEmpty(email))
                            {
                                urls.Add(new ContactViewModel()
                                {
                                    Html = externalHtml,
                                    Keywords = keyword,
                                    JobUrl = jobCrawlPost,
                                    Email = email
                                });

                            }
                            else
                            {
                                Console.WriteLine("But no email in job :(");
                            }

                            // if more than two keywords exist don't add twice
                            break;

                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving: " + jobCrawlPost);
                }

            }
        }

        private string getEmailFromHtml(string html)
        {
            Regex emailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*",
                RegexOptions.IgnoreCase);
            //find items that matches with our pattern
            MatchCollection emailMatches = emailRegex.Matches(html);

            StringBuilder sb = new StringBuilder();

            foreach (Match emailMatch in emailMatches)
            {
                return emailMatch.Value;
            }
            return string.Empty;
        }

        private void extractExternalJobPostLinks(string basePath, List<string> jobCrawlPosts)
        {
            List<string> paginationUrls = buildPagination(basePath);

            // iterate all jobs pages
            foreach (var paginationUrl in paginationUrls)
            {
                Console.WriteLine("Finding jobs on: " + paginationUrl);

                using (var client = new WebClient())
                {
                    var lp = client.DownloadString(paginationUrl);
                    var doc = CQ.Create(lp);

                    var jobPosts = doc.Select(string.Format(".{0}", jobpost_css)).Has("a");

                    // iterate all job posts
                    foreach (var ob in jobPosts.ToList())
                    {
                        // magic to find the link inside the job post
                        var text = ob.InnerHTML;
                        HtmlDocument agilityDoc = new HtmlDocument();
                        agilityDoc.LoadHtml(text);

                        var data =
                            agilityDoc.DocumentNode.Descendants()
                                .Where(c => c.Name == "a" && c.Attributes["href"] != null);

                        // don't ask, we need to ask second link
                        var linkNode = data.ToList()[1].Attributes["href"].Value;
                        var jobPath = jobIndexBasePath + linkNode;
                        jobCrawlPosts.Add(jobPath);
                    }
                }
            }
        }

        private List<string> buildPagination(string basePath)
        {
            List<string> paginationUrls = new List<string>();
            using (var client = new WebClient())
            {
                var html = client.DownloadString(basePath);
                var doc = CQ.Create(html);

                var paginationHtml = doc.Select(string.Format(".{0}", pagination_css)).Html();

                HtmlDocument agilityDoc = new HtmlDocument();
                agilityDoc.LoadHtml(paginationHtml);

                var data =
                    agilityDoc.DocumentNode.Descendants()
                        .Where(c => c.Name == "a" && c.Attributes["href"] != null).ToList();

                foreach (var htmlNode in data)
                {
                    var linkNode = htmlNode.Attributes["href"].Value;
                    var jobPath = jobIndexBasePath + linkNode;
                    paginationUrls.Add(jobPath);

                    int i = 0;
                }
            }

            return paginationUrls;
        }
    }
}
