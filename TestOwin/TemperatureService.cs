using System;
using System.ServiceProcess;

namespace TestOwin
{
    partial class TemperatureService : ServiceBase
    {
        private readonly ServerHost host;
        public TemperatureService()
        {
            string url = System.Configuration.ConfigurationManager.AppSettings["url"];
            this.host = new TestOwin.ServerHost(url);

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            host.Start();
        }

        protected override void OnStop()
        {
            host.Stop();
        }
    }
}
