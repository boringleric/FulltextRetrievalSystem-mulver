using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    [Immutable]
    [Serializable]
    public class SearchInfo
    {
        public string SearchString { get; set; }
        public int page { get; set; }
        public int filter { get; set; }
        public string filetype { get; set; }
    }
}
