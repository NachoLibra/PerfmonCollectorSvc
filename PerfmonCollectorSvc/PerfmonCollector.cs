using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PerfmonCollectorSvc
{
    class PerfmonCollector
    {
        private Dictionary<string, PerfmonCounter> perfmonCounterList = null;
        private int refreshInterval;
        private int loggingInterval;
        private string logFolder;
        private int logRetention;
        private System.Threading.Timer dailyArchiveTimer;
        private System.Timers.Timer collectorTimer;
        private System.Timers.Timer loggerTimer;

        public void Add(string key, PerfmonCounter value)
        {
            if (!perfmonCounterList.ContainsKey(key))
            {
                perfmonCounterList.Add(key, value);
            }
        }

        public int RefreshInterval
        {
            get
            {
                return this.refreshInterval;
            }
            set
            {
                if (value <= 300) this.refreshInterval = value ;
            }
        }

        public int LoggingInterval
        {
            get
            {
                return this.loggingInterval;
            }
            set
            {
                if (value > 0 && value <= 60) this.loggingInterval = value;
            }
        }

        public int LogRetention
        {
            get
            {
                return this.logRetention;
            }
            set
            {
                if (value >= 5 && value <= 30) this.logRetention = value;
            }
        }
        public string LogFolder
        {
            get
            {
                return this.logFolder;
            }
            set
            {
                if (value != null && Directory.Exists(@value)) this.logFolder = @value;
                CreateFolder(this.logFolder);
            }
        }

        public int Count
        {
            get
            {
                return this.perfmonCounterList.Count;
            }
        }

        #region Constructor

        public PerfmonCollector()
        {
            this.perfmonCounterList = new Dictionary<string, PerfmonCounter>();
            this.refreshInterval = 5;
            this.loggingInterval = 10;
            this.logRetention = 10;  //days
            this.logFolder = @"c:\rbfg\log\perfmon";
        }
        #endregion


        #region Public Methods
        public void Start()
        {
            if (perfmonCounterList.Count > 0)
            {
                DateTime tomorrowMorning = DateTime.Today.AddDays(1.0).AddHours(4.0);
                TimeSpan dueTime = tomorrowMorning - DateTime.Now;
                dailyArchiveTimer = new System.Threading.Timer(new System.Threading.TimerCallback(DailyArchiveTimerCallback), null, dueTime, new TimeSpan(24, 0, 0));
                //Perfmon Collector timer
                collectorTimer = new System.Timers.Timer(refreshInterval * 1000);
                collectorTimer.Elapsed +=new ElapsedEventHandler(collectorTimer_Elapsed);
                collectorTimer.AutoReset = true;
                collectorTimer.Start();
                //Perfmon logger timer
                loggerTimer = new System.Timers.Timer(LoggingInterval * 1000);
                loggerTimer.Elapsed += new ElapsedEventHandler(loggerTimer_Elapsed);
                loggerTimer.AutoReset = true;
                loggerTimer.Start();
             }
        }

        public void Stop()
        {
            if (collectorTimer != null)
            {
                collectorTimer.Stop();
                collectorTimer = null;
            }
            if (loggerTimer != null)
            {
                loggerTimer.Stop();
                loggerTimer = null;
            }
            if (dailyArchiveTimer != null)
            {
                dailyArchiveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                dailyArchiveTimer = null;
            }
            if (perfmonCounterList != null)
            {
                perfmonCounterList.Clear();
                perfmonCounterList = null;
            }
        }

        public void ReadConfig(string configFile)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            if (File.Exists(configFile))
            {
                String[] countersList = File.ReadAllLines(@configFile);
                if (countersList.Length > 0)
                {
                    string[] stringSeparators = new string[] { "," };
                    foreach (string item in countersList)
                    {
                        item.Trim();
                        if (!item.StartsWith("#"))
                        {
                            //Camel case perfmon counter string
                            string perfmonCounterText = textInfo.ToTitleCase(item);
                            //create text without special characters to use as key for Counterlist
                            string perfmonCounterKey = Regex.Replace(perfmonCounterText, @"\W", "", RegexOptions.IgnoreCase);
                            //slice perfmon rule and trim each element  
                            string[] counter = perfmonCounterText.Split(stringSeparators, StringSplitOptions.None).Select(d => d.Trim()).ToArray();
                            if (counter.Length >= 2 && counter.Length <= 3)
                            {
                                PerfmonCounter pc = null;
                                if (PerformanceCounterCategory.CounterExists(counter[1], counter[0]))
                                {
                                    PerformanceCounterCategory pcc = new PerformanceCounterCategory(counter[0]);
                                    string[] instances = pcc.GetInstanceNames();
                                    if (instances.Length > 0 && counter.Length == 3 && counter[2] != "")
                                    {
                                        pc = new PerfmonCounter(counter[0], counter[1], counter[2]);
                                    }
                                    else if (instances.Length == 0 && counter.Length == 2)
                                    {
                                        pc = new PerfmonCounter(counter[0], counter[1], "");
                                    }
                                    instances = null;
                                    pcc = null;
                                    if (!this.perfmonCounterList.ContainsKey(perfmonCounterKey))
                                    {
                                        this.perfmonCounterList.Add(perfmonCounterKey, pc);
                                    }
                                }

                            }

                        }

                    }
                }
            }
        }
        #endregion

        #region EventHandlers

        private void DailyArchiveTimerCallback(object state)
        {
            if (!Directory.Exists(@logFolder))
            {
                string[] directories = Directory.GetDirectories(@logFolder);
                if (directories.Length > 0)
                {
                    foreach (string dir in directories)
                    {
                        if (Regex.IsMatch(dir,@"\d{8}"))
                        {
                            if ((System.DateTime.Now - Directory.GetCreationTime(dir)).TotalDays >= logRetention)
                            {
                                //delete directory and its subdirectories
                                Directory.Delete(dir, true);
                            }
                        }
                    }
                }
            }
        }


        private void loggerTimer_Elapsed(object sender,EventArgs e)
        {
            //throw new NotImplementedException();

            string logdir = @logFolder + @"\" + System.DateTime.Now.ToString("yyyyMMdd");
            if (!Directory.Exists(logdir)) CreateFolder(logdir);
            foreach (string key in perfmonCounterList.Keys)
            {
                PerfmonCounter perfmonCounter = perfmonCounterList[key];
                string[] results = perfmonCounter.GetResults();
                string logfile = @logdir + @"\" + key + ".csv";
                if (results.Length > 0)
                {
                    try
                    {
                        //write header if new file
                        if (!File.Exists(@logfile)) File.AppendAllText(@logfile, String.Format("DateTime,{0}\r\n", perfmonCounter.CounterName));
                        File.AppendAllLines(@logfile, results);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }


        private void collectorTimer_Elapsed(object sender, EventArgs e)
        {
            foreach (PerfmonCounter perfmonCounter in perfmonCounterList.Values)
            {
                //Task.Factory.StartNew(() => perfmonCounter.GetCounterValue());
                //perfmonCounter.CollectCounterValue();
                ThreadPool.QueueUserWorkItem(perfmonCounter.CollectCounterValue);
            }
        }
        #endregion

        #region Misc methods
        private void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    GrantEveryoneAccess(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void GrantEveryoneAccess(string fullPath)
        {
            DirectoryInfo dInfo = new DirectoryInfo(fullPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
        }


        #endregion




    }
}
