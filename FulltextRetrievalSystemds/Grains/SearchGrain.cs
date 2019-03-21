using Orleans;
using Orleans.Concurrency;
using GrainInterfaces;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class SearchGrain : Grain, ISearchGrain
    {
        public override Task OnActivateAsync()
        {
            Guid primaryKey = this.GetPrimaryKey();
            return base.OnActivateAsync();
        }
        public async Task<AnsInfo> SearchAns(SearchInfo Searchwords)
        {
            try
            {
                AnsInfo ai = new AnsInfo();
                string search = Searchwords.SearchString + "&" + Searchwords.page + "&" + Searchwords.filter + "&" + Searchwords.filetype;
                if (Cache.CacheServices.cache.Exists(search))
                {
                    var cat = Cache.CacheServices.cache.Get(search);
                    ai = (AnsInfo)cat;
                    DeactivateOnIdle();
                    return await Task.FromResult(ai);

                }
                else
                {
                    Cache.CacheServices cs = new Cache.CacheServices();
                    ai = cs.SearchforCache(Searchwords);
                    DeactivateOnIdle();
                    return await Task.FromResult(ai);
                }
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Data); 
                throw;
            }
            
        }
    }
}
