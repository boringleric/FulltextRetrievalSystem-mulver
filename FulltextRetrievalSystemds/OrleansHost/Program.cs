using Orleans;
using Orleans.Runtime.Configuration;
using System;

namespace OrleansHostServ
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // The Orleans silo environment is initialized in its own app domain in order to more
                // closely emulate the distributed situation, when the client and the server cannot
                // pass data via shared memory.
                AppDomain hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
                {
                    AppDomainInitializer = InitSilo,
                    AppDomainInitializerArguments = args,
                });
                Console.WriteLine("Host Fin!!!!!");
              
                GrainClient.Initialize("client.xml");
                //var config = ClientConfiguration.LocalhostSilo();
                //GrainClient.Initialize(config);
                // TODO: once the previous call returns, the silo is up and running.
                //       This is the place your custom logic, for example calling client logic
                //       or initializing an HTTP front end for accepting incoming requests.
                //Console.WriteLine("Orleans Silo is running.\n Cache Initializing...");
                //cs.GetCache();
                Console.WriteLine("Finished Loading.\nPress Enter to terminate Silo...");
                Console.ReadLine();

                hostDomain.DoCallBack(ShutdownSilo);

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Data);
                throw;
            }
            
        }
        static void InitSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
            {
                Console.Error.WriteLine("Failed to initialize Orleans silo");
            }
        }

        static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static OrleansHostWrapper hostWrapper;
    }
}
