using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobCrawler
{
    public interface ICrawler
    {
        List<ContactViewModel> Execute(string[] keywords);
    }
}
