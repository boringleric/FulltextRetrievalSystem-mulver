using log4net;
using MathWorks.MATLAB.NET.Arrays;
using Orleans;
using GrainInterfaces;
using WebCommon;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace WebView.Controllers
{
    /// <summary>
    /// 总体查询的controller，允许匿名查询
    /// </summary>
    [AllowAnonymous]

    public class HomeController : Controller
    {
        //log4net日志记录启动
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 判断一个字符串是否为url
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsUrl(string str)
        {
            try
            {
                string Url = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                return Regex.IsMatch(str, Url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 
                return false;
            }
        }
        /// <summary>
        /// 返回的结果展示给view
        /// </summary>
        /// <param name="search">检索词</param>
        /// <param name="filter">停用词</param>
        /// <returns></returns>
       [ValidateInput(false)]
        private Guid GetGuid()
        {
            if (Request.Cookies["playerId"] != null)
            {
                return Guid.Parse(Request.Cookies["playerId"].Value);
            }
            var guid = Guid.NewGuid();
            Response.Cookies.Add(new HttpCookie("playerId", guid.ToString()));
            return guid;
        }
        public async Task<ActionResult> Result(string search, string filter, string page)
        {
            if (search == "我想玩旅行商问题的游戏上上下下左右左右BABA")
            {
                return View("Amazing");
            }
            TimeSpan ts;
            DateTime DateTimestart = DateTime.Now;
            DateTime DateTimeend;
            List<SearchResult> XapAns;
            bool urlflag = false;
            string searchbackup = search;
            //角色等级
            switch (filter)
            {
                case null:
                    if (User.IsInRole("Admin") || User.IsInRole("Sec"))
                    {
                        filter = "0";       //管理员和等级用户检索就是全部可见的
                    }
                    else
                    {
                        filter = "0";       //非管理员检索结果不可见等级
                    }
                    break;
                case "0":
                    if (User.IsInRole("Admin") || User.IsInRole("Sec"))
                    {
                        filter = "0";       //管理员和等级用户检索就是全部可见的
                    }
                    break;
                case "1":
                    break;
                case "2":
                    break;
                case "3":
                    break;
                case "4":
                case "5":
                    if (!User.IsInRole("Admin") && !User.IsInRole("Sec"))
                    {
                        
                        log.Warn("用户" + User.Identity.Name + "恶意访问！！");
                        filter = "0";
                    }
                    break;
                default:
                    filter = "0";
                    break;
            }
            //分页
            if (page == null)
            {
                page = "0";
            }
            ViewBag.Page = page;
            ViewBag.Filter = filter;

            if (string.IsNullOrEmpty(search))
            {
                //如果没有词检索就返回
                return RedirectToAction("Index");
            }
            else
            {
                if (IsUrl(search))
                {
                    urlflag = true;
                    search = Regex.Replace(search, @"/", " ");
                }
               
                //分词处理
                ViewBag.SearchWord = searchbackup;
                XapianLogic xl = new XapianLogic();
                uint num = 0;
                if (urlflag)
                {
                    var guid = GetGuid();
                    var searchgrain = GrainClient.GrainFactory.GetGrain<ISearchGrain>(guid);
                    SearchInfo si = new SearchInfo
                    {
                        SearchString = searchbackup,
                        filter = int.Parse(filter),
                        page = int.Parse(page),
                        filetype = "1980/01/01"
                    };
                    Console.WriteLine(guid + "\n");
                    AnsInfo ai = searchgrain.SearchAns(si).Result;
                    Console.WriteLine(guid + "ret" + "\n");
                    //xl.SearchReturn(searchbackup, int.Parse(page), int.Parse(filter),"1980/01/01", out num, out XapAns, out ts);
                    num = ai.totalnum;
                    XapAns = ai.retinfo;
                    DateTimeend = DateTime.Now;
                    ts = DateTimeend - DateTimestart;
                    ts.TotalMilliseconds.ToString();        //查询时间返回
                }
                else
                {
                    //如果是Ftp和共享文件夹的来源，支持用+fileextension：excel、word、ppt、pdf、txt、html，查找过滤
                    //if ((filter == "2" || filter == "3")&&search.Contains("+fileextension"))
                    var guid = GetGuid();
                    Console.WriteLine(guid + "\n");
                    //var searchgrain = GrainClient.GrainFactory.GetGrain<ISearchGrain>(guid);
                    Console.WriteLine(guid+"ret"+"\n");
                    SearchInfo si = new SearchInfo
                    {
                        SearchString = searchbackup,
                        filter = int.Parse(filter),
                        page = int.Parse(page)
                    };

                    if (search.Contains("+fileextension"))
                    {
                        int pos = search.IndexOf("+fileextension");     //若有使用扩展名检索，则筛选扩展名
                        string extension = search.Substring(pos + 15, search.Length - pos - 15);
                        string searchkeyword = search.Substring(0, pos);
                        si.filetype = extension;
                        //xl.SearchReturn(searchkeyword, int.Parse(page), int.Parse(filter), extension, out num, out XapAns, out ts); //带有扩展名检索
                        AnsInfo ai = new AnsInfo(); 
                        ai = await SearchAns(si, guid);
                        num = ai.totalnum;
                        XapAns = ai.retinfo;
                        DateTimeend = DateTime.Now;
                        ts = DateTimeend - DateTimestart;
                        ts.TotalMilliseconds.ToString();        //查询时间返回
                    }
                    else
                    {
                        si.filetype = "0";
                        AnsInfo ai = new AnsInfo();
                        ai = await SearchAns(si, guid);                        
                        //xl.SearchReturn(search, int.Parse(page), int.Parse(filter), "0", out num, out XapAns, out ts);  //无扩展名检索
                        num = ai.totalnum;
                        XapAns = ai.retinfo;
                        DateTimeend = DateTime.Now;
                        ts = DateTimeend - DateTimestart;
                        ts.TotalMilliseconds.ToString();        //查询时间返回
                    }
                }
                

                if (num == 0)
                {
                    //如果没有检索到结果
                    ViewBag.ZeroCheck = "0";
                    TempData["Zero"] = "内容未检索到！";
                    ViewBag.Ansnum = 0;
                    ViewBag.PageCount = 0;
                    ViewBag.time = ts;
                    return View();
                }
                else
                {
                    ViewBag.AnsNum = num;
                }
                //检索到则返回结果
                ViewBag.WebContent = XapAns;
                ViewBag.PageCount = (uint)Math.Ceiling(num / 10.0);
                //检索所用时间
                ViewBag.time = ts;
                return View();
            }
        }

        public async Task<AnsInfo> SearchAns(SearchInfo si, Guid guid)
        {
            var searchgrain = GrainClient.GrainFactory.GetGrain<ISearchGrain>(guid);
            AnsInfo ai = searchgrain.SearchAns(si).Result;
            
            return await Task.FromResult(ai);
        }
        public ActionResult Index()
        {
            //var guid = GetGuid();
            //var searchgrain = GrainClient.GrainFactory.GetGrain<ISearchGrain>(guid);
            //SearchInfo si = new SearchInfo();
            //si.SearchString = "test";
            //si.filter = 0;
            //si.page = 0;
            //si.filetype = "1980/01/01";
            //AnsInfo ai = searchgrain.SearchAns(si).Result;
            return View();
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Amazing()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        public ActionResult GetEmployer(string[] sendforcalc)
        {
            var pop = 5;
            var gem = 10;
            var Pa = 0.2;
            CalcTsp.Class1 calcTsp = new CalcTsp.Class1();
            double[] list = new double[20 * 3];
            int i = 0;
            foreach (var item in sendforcalc)
            {
                string[] arr = item.Split(' ');
                list[i] = i/3;
                list[i + 1] = double.Parse(arr[0]);
                list[i + 2] = double.Parse(arr[1]);
                i = i + 3;
            }
            MWNumericArray array = new MWNumericArray(20, 3, list);
            MWArray resultObj = calcTsp.CalcTsp(array, pop, Pa, gem);
            Array a = resultObj.ToArray();
            var numa = a.GetValue(0, 0);
            var aaaaa = numa.ToString();
            int num = int.Parse(aaaaa);
            return Json(num, JsonRequestBehavior.AllowGet);
        }
    }
}