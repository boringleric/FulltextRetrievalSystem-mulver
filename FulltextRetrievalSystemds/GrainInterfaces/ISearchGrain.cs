using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    public interface ISearchGrain: IGrainWithGuidKey
    {
        // 发送检索信息
        Task<AnsInfo> SearchAns(SearchInfo Searchwords);
    }
}
