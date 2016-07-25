using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace PerfmonCollectorSvc
{
    class PerfmonCounter
    {
        private string category;
        private string counter;
        private string instance;
        private System.Diagnostics.PerformanceCounter perfmonCounterObj = null;
        private ConcurrentQueue<string> resultQueue = new ConcurrentQueue<string>();
        private string hostname = string.Format("\\{0}", System.Environment.MachineName.ToUpper());
        private string counterName;
        public string CounterName
        {
            get
            {
                return this.counterName;
            }

        }
        public string[] GetResults()
        {
            List<string> results = new List<string>();
            string result = "";
            while (resultQueue.TryDequeue(out result))
            {
                results.Add(result);
            }
            return results.ToArray();
        }

        public PerfmonCounter(string category, string counter, string instance)
        {
            this.category = category.Trim();
            this.counter = counter.Trim();
            this.instance = instance.Trim();
            if (PerformanceCounterCategory.CounterExists(counter, category))
            {
                if (instance != "")
                {
                    this.counterName = string.Format("\"{0}\\{1}({3})\\{2}\"", hostname, category, counter, instance);
                }
                else
                {
                    this.counterName = string.Format("\"{0}\\{1}\\{2}\"", hostname, category, counter);
                }
            }
        }

        private string GetDateTime()
        {
            return System.DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff");
        }

         public void CollectCounterValue(object obj)
        {
            string result = " ";
            string sampleDateTime = GetDateTime();
            if (perfmonCounterObj != null)
            {
                try
                {
                    //result = perfmonCounterObj.NextValue().ToString();
                    result = perfmonCounterObj.NextValue().ToString("F");
                    sampleDateTime = GetDateTime();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception occurred: " + ex.Message);
                    perfmonCounterObj = null;
                }
            }
            else if (PerformanceCounterCategory.CounterExists(counter, category))
            {
                if (instance != "" && PerformanceCounterCategory.InstanceExists(instance, category))
                {
                    perfmonCounterObj = new PerformanceCounter(category, counter, instance, true);
                    counterName = string.Format("\"{0}\\{1}({3})\\{2}\"", hostname, category, counter, instance);
                }
                else if (instance == "")
                {
                    perfmonCounterObj = new PerformanceCounter(category, counter, true);
                    counterName = string.Format("\"{0}\\{1}\\{2}\"", hostname, category, counter);
                }
                try
                {
                    if (perfmonCounterObj != null)
                    {
                        //perfmonCounterObj.NextValue();
                        result = perfmonCounterObj.NextValue().ToString("F");
                        sampleDateTime = GetDateTime();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception occurred: " + ex.Message);
                    perfmonCounterObj = null;
                }
            }
            //Console.WriteLine(System.DateTime.Now.ToString() + " - " + category + "_" + counter + "_" + instance + "    -  " + result);
            resultQueue.Enqueue(string.Format("\"{0}\",\"{1}\"", sampleDateTime, result));
        }
    }
}
