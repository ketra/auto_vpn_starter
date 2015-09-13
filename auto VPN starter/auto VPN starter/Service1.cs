using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;

namespace auto_VPN_starter
{
    public partial class Service1 : ServiceBase
    {
        public System.Management.ManagementEventWatcher StartWtch;
        public System.Management.ManagementEventWatcher StopWtch;
        public Thread MainThread;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MainThread = new Thread(Processmonitor);
            MainThread.Start();
        }

        protected override void OnStop()
        {
            MainThread.Abort();
        }

        private void Processmonitor()
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            StartWtch = new ManagementEventWatcher("Select * From Win32_ProcessStartTrace");
            StopWtch = new ManagementEventWatcher("Select * From Win32_ProcessStopTrace");
            StartWtch.EventArrived += mgmtWtch_EventArrived;
            StopWtch.EventArrived += mgmtWtch_EventArrived;
            StopWtch.Start();
            StartWtch.Start();
        }

        void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            ServiceController service = new ServiceController("OpenVPNService", "Localhost");
            if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.StartPending)
            {
                service.Start();
            }
        }

        private bool CurrentlyRunning(string sProcessName)
        {
            Process[] Processes = Process.GetProcesses();
            foreach (Process SingleProcess in Processes)
            {
                if (SingleProcess.ProcessName.ToLower().Contains(sProcessName))
                { 
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Function that reports the users time logged in and returns this within a Timespan
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetUptimeUser()
        {
            ManagementScope ms = new ManagementScope("\\root\\cimv2");
            ObjectQuery oq = new ObjectQuery("Select * from Win32_Session");
            ManagementObjectSearcher query = new ManagementObjectSearcher(ms, oq);

            ManagementObjectCollection queryCollection = query.Get();
            DateTime lastBootUp = DateTime.Now.ToUniversalTime();
            foreach (ManagementObject mo in queryCollection)
            {
                if (mo["LogonType"].ToString().Equals("2")) //  2 - for logged on User
                {
                    lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["StartTime"].ToString());
                }
            }
            return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
        }

        /// <summary>
        /// Function for getting the current systems Uptime
        /// </summary>
        /// <returns>Timespan Uptime</returns>
        /// 
        public static TimeSpan GetUptime()
        {
            ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@");
            DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString());
            return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
        }

        void mgmtWtch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            TimeSpan minuptime = new TimeSpan(0,10,0);
            if (CurrentlyRunning("kodi") || GetUptime() < minuptime || GetUptimeUser() < minuptime)
            {
                ServiceController service = new ServiceController("OpenVPNService", "Localhost");
                if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.StartPending)
                {
                    service.Start();
                }
            }
            else
            {
                ServiceController service = new ServiceController("OpenVPNService", "Localhost");
                if (service.Status != ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.StopPending)
                {
                    service.Stop();
                }
            }

        }
    }
}
