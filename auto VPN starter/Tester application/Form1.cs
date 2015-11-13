using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Tester_application
{

    public partial class Form1 : Form
    {
        public System.Management.ManagementEventWatcher StartWtch;
        public System.Management.ManagementEventWatcher StopWtch;
        DateTime Resumed;
        public Thread MainThread;
        
        public Form1()
        {
            InitializeComponent();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Resumed = DateTime.Now;
                    ServiceController service = new ServiceController("OpenVPNService", "Localhost");
                    if (service.Status != ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.StopPending)
                    {
                        service.Stop();
                    }
                    if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.StartPending)
                    {
                        service.Start();
                    }
                    break;
                default:
                    break;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            MainThread = new Thread(Processmonitor);
            MainThread.Start();
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
            //get a list of all running processes on current system
            Process[] Processes = Process.GetProcesses();

            //Iterate to every process to check if it is out required process
            foreach (Process SingleProcess in Processes)
            {

                if (SingleProcess.ProcessName.ToLower().Contains(sProcessName))
                {
                    //process found 
                    return true;
                }
            }

            //Process not found
            return false;
        }
        public static TimeSpan GetUptime()
        {
            ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@");
            DateTime lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString());
            return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
        }

        public static TimeSpan GetUptimeUser()
        {
            ManagementScope ms = new ManagementScope("\\root\\cimv2");

            ObjectQuery oq = new ObjectQuery("Select * from Win32_Session");

            ManagementObjectSearcher query = new ManagementObjectSearcher(ms, oq);

            ManagementObjectCollection queryCollection = query.Get();

            DateTime lastBootUp = DateTime.Now.ToUniversalTime();
            foreach (ManagementObject mo in queryCollection)
            {

                if (mo["LogonType"].ToString().Equals("2"))
                //  2 - for logged on User
                {
                   lastBootUp = ManagementDateTimeConverter.ToDateTime(mo["StartTime"].ToString());
                }

            }
            return DateTime.Now.ToUniversalTime() - lastBootUp.ToUniversalTime();
        }

        public TimeSpan GetResumeTime()
        {
            return DateTime.Now.ToUniversalTime() - Resumed.ToUniversalTime();
        }

        void mgmtWtch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            TimeSpan minuptime = new TimeSpan(0, 10, 0);
            if (CurrentlyRunning("kodi") || CurrentlyRunning("outlook") || GetUptime() < minuptime || GetUptimeUser() < minuptime || GetResumeTime() < minuptime)
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
