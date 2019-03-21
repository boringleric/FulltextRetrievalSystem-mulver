using CacheManager.Core;
using JiebaNet.Segmenter;
using GrainInterfaces;
using WebCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cache
{
    public class CacheServices
    {
        public static ICacheManager<object> cache = cache = CacheFactory.Build("getStartedCache", settings =>
        {
            settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithDictionaryHandle("inProcessCache")
            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(5))
            .EnableStatistics()
            .And
            .WithRedisConfiguration("redis", config =>//Redis缓存配置
            {
                config.WithAllowAdmin()
                    .WithDatabase(0)
                    .WithEndpoint("localhost", 6379);
            })
            .WithMaxRetries(1000)//尝试次数
            .WithRetryTimeout(100)//尝试超时时间
            .WithRedisBackplane("redis")//redis使用Back Plane
            .WithRedisCacheHandle("redis", true);//redis缓存handle            
        });


        public AnsInfo SearchforCache(SearchInfo Searchwords)
        {
            AnsInfo ai = new AnsInfo();
            Xapian.MSet xm = new Xapian.MSet();
            XapianLogic xl = new XapianLogic();

            if (Searchwords.filter == 5)    //sec, not cache
            {
                List<SearchResult> ls = new List<SearchResult>();
                uint num = 0;
                xl.SearchforsecReturn(Searchwords.SearchString, Searchwords.page, Searchwords.filter, Searchwords.filetype, out num, out ls);
            }
            else
            {
                xl.SearchReturnforSilo(Searchwords.SearchString, Searchwords.filter, Searchwords.filetype, out xm);
                SearchReturn(xm, Searchwords, out ai);
                string search = Searchwords.SearchString + "&" + Searchwords.page + "&" + Searchwords.filter + "&" + Searchwords.filetype;
                cache.Put(search, ai);
            }
            return ai;
        }

        private void SearchReturn(Xapian.MSet xm, SearchInfo Searchwords, out AnsInfo ai)
        {
            ai = new AnsInfo();
            List<SearchResult> XapResList = new List<SearchResult>();
            string query = Searchwords.SearchString;
            query = query.Replace("\\", "");
            query = query.Replace("/", "");
            int page = Searchwords.page;
            var segmenter = new JiebaSegmenter();
            var segments = segmenter.Cut(query);
            string querystr = string.Join(" ", segments);    //分词
            //若返回不为空
            if (xm != null)
            {
                ai.totalnum = xm.Size();    //结果数目
                int pagecount = 0;
                for (Xapian.MSetIterator iter = xm.Begin(); iter != xm.End(); ++iter)
                {
                    SearchResult sr = new SearchResult();
                    ++pagecount;
                    if (pagecount <= ((page - 1) * 10))     //获得分页
                    {
                        continue;
                    }
                    else
                    {
                        if (XapResList.Count >= 10)         //每页10个结果
                        {
                            break;
                        }

                        Xapian.Document iterdoc = iter.GetDocument();
                        bool ftpflag = false;                             //ftp标记，转码用
                        bool emflag = false;
                        string strcontent = iterdoc.GetData();           //取出正文
                        string strtitle = iterdoc.GetValue(3);           //取出标题 ValueTitle
                        string strahref = iterdoc.GetValue(1);            //取出链接
                        string source = iterdoc.GetValue(0);
                        string strcut = "";
                        int contentlen = strcontent.Length;              //判断正文长度，为下面筛选含有关键词片段做准备
                        uint docid = iter.GetDocId();

                        if (source == "4")
                        {
                            sr.allcontent = strcontent;
                        }
                        if (source == "2")
                        {
                            ftpflag = true;
                            strahref = UrlEncode(strahref);             //若为ftp链接，需要转码
                        }
                        string[] strquerycut = querystr.Split(' ');
                        string emlink = "";
                        List<string> tmp = new List<string>();
                        foreach (var item in strquerycut)
                        {
                            if (item == "e" || item == "E" || item == "m" || item == "M" ||
                                item == "em" || item == "Em" || item == "Em" || item == "EM" ||
                                item == "<" || item == ">")
                            {
                                emflag = true;
                                if (emlink != "")
                                {
                                    emlink = emlink + "|" + item;
                                }
                                else
                                {
                                    emlink = item;
                                }
                            }
                            else
                            {
                                tmp.Add(item);
                            }
                        }
                        HashSet<string> hs = new HashSet<string>(tmp); //此时已经去掉重复的数据保存在hashset中
                        String[] strunique = new String[hs.Count];
                        hs.CopyTo(strunique);

                        int cutlen = strunique.Length;
                        int count = 0;

                        if (emlink != "" && cutlen == 0)
                        {
                            foreach (var item in strquerycut)
                            {
                                //消掉*问号空格
                                if (item == " " || item == "")
                                {
                                    continue;
                                }
                                CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
                                int conpos = Compare.IndexOf(strcontent, item, CompareOptions.IgnoreCase);      //根据位置标红
                                                                                                                //int conpos = strcontent.IndexOf(item);      //根据位置标红
                                if (conpos != -1)
                                {
                                    if (contentlen - conpos > 150 && conpos > 50)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, 200);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (conpos > 50)
                                    {
                                        ////截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, contentlen - conpos + 50);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }

                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (contentlen - conpos > 150)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(0, conpos + 150);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }

                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else
                                    {
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        //不够150的全拿出
                                        strcut = strcontent;
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }

                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                }
                                else
                                {
                                    CompareInfo Comparetitle = CultureInfo.InvariantCulture.CompareInfo;
                                    int conpostitle = Comparetitle.IndexOf(strtitle, item, CompareOptions.IgnoreCase);      //根据位置标红
                                    if (conpostitle != -1)
                                    {
                                        if (contentlen > 200)
                                        {
                                            strcut = strcontent.Substring(0, 200);
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }

                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                        else
                                        {
                                            strcut = strcontent;
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }

                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                    }
                                    else
                                    {
                                        ++count;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //每一个词都查一遍
                            foreach (var item in strunique)
                            {
                                //消掉*问号空格
                                if (item == " " || item == "")
                                {
                                    continue;
                                }
                                CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;
                                int conpos = Compare.IndexOf(strcontent, item, CompareOptions.IgnoreCase);      //根据位置标红
                                                                                                                //int conpos = strcontent.IndexOf(item);      //根据位置标红
                                if (conpos != -1)
                                {
                                    if (contentlen - conpos > 150 && conpos > 50)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, 200);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红，大小写不敏感，regex替换法，replace大小写敏感
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (conpos > 50)
                                    {
                                        ////截取150字作为cache
                                        strcut = strcontent.Substring(conpos - 50, contentlen - conpos + 50);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else if (contentlen - conpos > 150)
                                    {
                                        //截取150字作为cache
                                        strcut = strcontent.Substring(0, conpos + 150);
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                    else
                                    {
                                        //strcut = HttpUtility.HtmlEncode(strcut);
                                        //不够150的全拿出
                                        strcut = strcontent;
                                        if (emflag)
                                        {
                                            strtitle = ReplaceCntent(emlink, strtitle);
                                            strcut = ReplaceCntent(emlink, strcut);
                                        }
                                        for (; count < cutlen; count++)
                                        {
                                            if (strunique[count] == " " || strunique[count] == "")
                                            {
                                                continue;
                                            }
                                            //标红
                                            strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                            strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                        }
                                        strcut = "..." + strcut + "...";
                                        goto Finally;
                                    }
                                }
                                else
                                {
                                    CompareInfo Comparetitle = CultureInfo.InvariantCulture.CompareInfo;
                                    int conpostitle = Comparetitle.IndexOf(strtitle, item, CompareOptions.IgnoreCase);      //根据位置标红
                                    if (conpostitle != -1)
                                    {
                                        if (contentlen > 200)
                                        {
                                            strcut = strcontent.Substring(0, 200);
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }
                                            //strcut = HttpUtility.HtmlEncode(strcut);
                                            for (; count < cutlen; count++)
                                            {
                                                if (strunique[count] == " " || strunique[count] == "")
                                                {
                                                    continue;
                                                }
                                                //标红
                                                strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                                //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            }
                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                        else
                                        {
                                            strcut = strcontent;
                                            if (emflag)
                                            {
                                                strtitle = ReplaceCntent(emlink, strtitle);
                                                strcut = ReplaceCntent(emlink, strcut);
                                            }
                                            //strcut = HttpUtility.HtmlEncode(strcut);
                                            for (; count < cutlen; count++)
                                            {
                                                if (strunique[count] == " " || strunique[count] == "")
                                                {
                                                    continue;
                                                }
                                                //标红
                                                strtitle = Regex.Replace(strtitle, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                                //strtitle = strtitle.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                //strcut = strcut.Replace(strquerycut[count], "<font color = red>" + strquerycut[count] + "</font>");
                                                strcut = Regex.Replace(strcut, Regex.Escape(strunique[count]), "<em>" + strunique[count] + "</em>", RegexOptions.IgnoreCase);
                                            }
                                            strcut = "..." + strcut + "...";
                                            goto Finally;
                                        }
                                    }
                                    else
                                    {
                                        ++count;
                                    }
                                }
                            }
                        }


                        //找到合适的内容之后返回结果
                        Finally:
                        sr.ahref = iterdoc.GetValue(1);
                        if (ftpflag)                    //判断是否需要转码链接
                        {
                            sr.ahrefencode = strahref; //ftp则使用转码链接
                        }
                        else
                        {
                            sr.ahrefencode = sr.ahref;
                        }
                        sr.link = iterdoc.GetValue(2);
                        sr.title = strtitle;
                        sr.snippet = strcut;
                        XapResList.Add(sr);
                    }
                }
                ai.retinfo = XapResList;
            }
            else
            {
                ai.totalnum = 0;
                ai.retinfo = null;
            }
        }

        #region replace
        private string ReplaceCntent(string pattern, string Content)
        {
            // string pattern = "e|E|m|M|em|EM|Em|eM|<|>";
            Regex Reg = new Regex(pattern);
            MatchEvaluator evaluator = new MatchEvaluator(ConvertToEM);
            return Reg.Replace(Content, evaluator);
        }
        private string ConvertToEM(Match m)
        {
            string Letter = string.Empty;
            switch (m.Value)
            {
                case "m":
                    Letter = @"<em>m</em>";
                    break;
                case "e":
                    Letter = @"<em>e</em>";
                    break;
                case "em":
                    Letter = @"<em>em</em>";
                    break;
                case "M":
                    Letter = @"<em>M</em>";
                    break;
                case "E":
                    Letter = @"<em>E</em>";
                    break;
                case "Em":
                    Letter = @"<em>Em</em>";
                    break;
                case "EM":
                    Letter = @"<em>EM</em>";
                    break;
                case "eM":
                    Letter = @"<em>eM</em>";
                    break;
                case "<":
                    Letter = @"<em><</em>";
                    break;
                case ">":
                    Letter = @"<em>></em>";
                    break;
                default:
                    Letter = "";
                    break;
            }
            return Letter;
        }

        protected string UrlEncode(string url)
        {
            byte[] bs = Encoding.GetEncoding("GB2312").GetBytes(url);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bs.Length; i++)
            {
                if (bs[i] < 128)
                    sb.Append((char)bs[i]);
                else
                {
                    sb.Append("%" + bs[i++].ToString("x").PadLeft(2, '0'));
                    sb.Append("%" + bs[i].ToString("x").PadLeft(2, '0'));
                }
            }
            return sb.ToString();
        }
        #endregion




    }
}
