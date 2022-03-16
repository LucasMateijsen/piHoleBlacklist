using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlocklistMaker
{
    public class BlockListFileList
    {
        public string Url { get; set; }
        public int RawDomains { get; set; }
        public int FilteredDomains { get; set; }

        public BlockListFileList()
        {

        }

        public BlockListFileList(string url)
        {
            Url = url;
            RawDomains = 0;
            FilteredDomains = 0;
        }

        public BlockListFileList(string url, int rawDomain)
        {
            Url = url;
            RawDomains = rawDomain;
            FilteredDomains = 0;
        }

        public BlockListFileList(string url, int rawDomain, int filteredDomains)
        {
            Url = url;
            RawDomains = rawDomain;
            FilteredDomains = filteredDomains;
        }
    }
}
