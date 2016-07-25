using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PerfmonCollectorSvc
{
    public partial class PerfmonCollectorSvc : ServiceBase
    {
        private PerfmonCollector perfmonCollector;

        public PerfmonCollectorSvc()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            perfmonCollector = new PerfmonCollector();
            ReadConfig();
            if (perfmonCollector.Count > 0)
            {
                perfmonCollector.Start();
            }
            else
            {
                OnStop();
            }
        }

        protected override void OnStop()
        {
            if (perfmonCollector != null) perfmonCollector.Stop();

        }

        public void Start()
        {
            OnStart(null);
        }

        private void ReadConfig()
        {
            if (ConfigurationManager.AppSettings["configfilepath"] != null)
            {
                string configFile = ConfigurationManager.AppSettings["configfilepath"];
                string refreshIntv = ConfigurationManager.AppSettings["sampleinterval"];
                string loggingIntv = ConfigurationManager.AppSettings["logginginterval"];
                string logFolder = ConfigurationManager.AppSettings["LogFolder"];
                string logRetention = ConfigurationManager.AppSettings["LogRetention"];
                int interval = 0;
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                if (File.Exists(@configFile))
                {
                    if (refreshIntv != null && refreshIntv != "")
                    {
                        if (Int32.TryParse(refreshIntv, out interval))
                        {
                            perfmonCollector.RefreshInterval = interval;
                        }
                    }
                    if (loggingIntv != null && loggingIntv != "")
                    {
                        if (Int32.TryParse(loggingIntv, out interval))
                        {
                            perfmonCollector.LoggingInterval = interval;
                        }
                    }
                    if (logRetention != null && logRetention != "")
                    {
                        if (Int32.TryParse(logRetention, out interval))
                        {
                            perfmonCollector.LogRetention = interval;
                        }
                    }
                    if (logFolder != null && logFolder != "" && Directory.Exists(@logFolder))
                    {
                        perfmonCollector.LogFolder = @logFolder;
                    }
                    if (configFile != "")
                    {
                        perfmonCollector.ReadConfig(@configFile);
                    }
                }
            }
        }
    }
}
