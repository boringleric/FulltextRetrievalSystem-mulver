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
    public class AnsInfo
    {
        public List<SearchResult> retinfo { get; set; }
        public uint totalnum { get; set; }
    }
    [Immutable]
    [Serializable]
    public class SearchResult
    {
        public string ahref;                        //原链接
        public string ahrefencode;                  //转码后原链接（Ftp用）
        public string link;                         //本地连接
        public string title;                        //标题
        public string snippet;                      //快照
        public string allcontent;                   //所有内容（等级库用）
    }
}
