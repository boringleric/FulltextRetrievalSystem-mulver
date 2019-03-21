using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using WebCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace WebView
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            const int InitializeAttemptsBeforeFailing = 5;
            var config = ClientConfiguration.LocalhostSilo();
            int attempt = 0;
            while (true)
            {
                try
                {
                    GrainClient.Initialize("client.xml");
                    Trace.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Trace.TraceWarning($"Attempt {attempt} of {InitializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > InitializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleTable.EnableOptimizations = false;
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //XapianLogic xl = new XapianLogic();
            //Thread thread = new Thread(new ThreadStart(XapianLogic.easyforthread));
            //thread.IsBackground = true;//这样能随主程序一起结束
            //thread.Start();

        }
    }
}
