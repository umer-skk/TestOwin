using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;

[assembly: OwinStartup(typeof(TestOwin.Startup))]

namespace TestOwin
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                string url = System.Configuration.ConfigurationManager.AppSettings["url"];
                var host = new ServerHost(url);
                host.Start();

                Console.WriteLine($"Server started. Listening on '{url}'.");
                Console.WriteLine("Press <Enter> to exit...");
                Console.ReadLine();

                host.Stop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new TemperatureService()
                };

                ServiceBase.Run(ServicesToRun);
            }
        }
    }

    class ServerHost
    {
        private readonly string url;
        public ServerHost(string url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            Uri uri;
            if (String.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri))
                throw new ArgumentException("Invalid url.", nameof(url));

            this.url = url;
        }

        private IDisposable webApp;
        public void Start()
        {
            webApp = WebApp.Start<Startup>(this.url);
        }

        public void Stop()
        {
            webApp?.Dispose();
        }
    }

    class Temperature
    {
        public const string In = "28-0415b12dd2ff";
        public const string Out = "28-011581f83aff";

        public static double? GetTemperature()
        {
            return GetTemperature(Out);
        }

        public static double? GetTemperature(string tempId)
        {
            string pattern = tempId;
            if (String.Compare(nameof(In), tempId, true) == 0)
                pattern = In;
            if (String.Compare(nameof(Out), tempId, true) == 0)
                pattern = Out;

            DirectoryInfo devicesDir = new DirectoryInfo("/sys/bus/w1/devices");
            if (devicesDir.Exists)
            {
                foreach (var deviceDir in devicesDir.GetDirectories(pattern))
                {
                    var file = deviceDir.GetFiles("w1_slave").FirstOrDefault();
                    if (file != null)
                    {
                        using (var sr = file.OpenText())
                        {
                            var w1slavetext = sr.ReadToEnd();
                            string temptext = w1slavetext.Split(new string[] { "t=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                            double temp = double.Parse(temptext) / 1000;

                            return temp;
                        }
                    }
                }
            }
            return null;
        }
    }

    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseWelcomePage("/");
            app.Use<TemperatureMiddleware>();
        }
    }

    class TemperatureMiddleware : OwinMiddleware
    {
        public TemperatureMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        private readonly PathString path = new PathString("/temp");
        public async override Task Invoke(IOwinContext context)
        {
            PathString remaining;
            if (context.Request.Path.StartsWithSegments(path, out remaining))
            {
                double? temp = null;
                if (!remaining.HasValue)
                    temp = Temperature.GetTemperature();
                else
                    temp = Temperature.GetTemperature(remaining.Value.TrimStart('/'));

                byte[] buffer = Encoding.UTF8.GetBytes(temp.HasValue ? temp.Value.ToString() : "<none>");
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}
