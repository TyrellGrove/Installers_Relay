using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Installer_Relay_Windows_Service
{
    public partial class Service1 : ServiceBase
    {
        #region Public Variables

        public RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Traffic Technology Services\APhA", true);
        public string destination = @"\Installers_Relay";
        public string log = @"\Installer_Relay_Window_Service_Log.txt";
        public string Host = @"\PSA Host Setup.exe";
        public string Hub = @"\PSA Hub Setup.exe";
        public string Satellite = @"\PSA Satellite Setup.exe";
        public string Observer = @"\PSA Observer Setup.exe";
        public string Utilities = @"\PSA Utilities Setup.exe";

        public string HubIPAddress = string.Empty;
        public string HostTo = string.Empty;
        public string HubTo = string.Empty;
        public string SatelliteTo = string.Empty;
        public string ObserverTo = string.Empty;
        public string UtilitiesTo = string.Empty;

        public Task New;
        public FileInfo myLogFile;

        public System.Timers.Timer myTimer = new System.Timers.Timer();
        #endregion

        public Service1()
        {
            InitializeComponent();
            Initialization();
        }

        public void Initialization()
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged += this.SystemEvents_PowerModeChanged;

            try
            {
                if (!System.IO.Directory.Exists(destination)) System.IO.Directory.CreateDirectory(destination);

                if (!System.IO.File.Exists(destination + log)) System.IO.File.Create(destination + log);

                HubIPAddress = @"\\" + key.GetValue("HubAddress").ToString();
            }

            catch (Exception e) {LogWritter(myLogFile, destination + log, " The following exception was captured when trying to make the file directory or log file: ", e.ToString());}

            HostTo = @"C:" + destination + Host;
            HubTo = @"C:" + destination + Hub;
            SatelliteTo = @"C:" + destination + Satellite;
            ObserverTo = @"C:" + destination + Observer;
            UtilitiesTo = @"C:" + destination + Utilities;

            Host = HubIPAddress + destination + Host;
            Hub = HubIPAddress + destination + Hub;
            Satellite = HubIPAddress + destination + Satellite;
            Observer = HubIPAddress + destination + Observer;
            Utilities = HubIPAddress + destination + Utilities;

            destination = @"C:" + destination;
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            //Uncomment the below line to help debug this service when it is running.
            //System.Diagnostics.Debugger.Launch();
            //Has much to do

            myTimer.Interval = 20000; //20 seconds
            myTimer.Elapsed += new System.Timers.ElapsedEventHandler(Checking);
            myTimer.AutoReset = true;
            myTimer.Enabled = true;
        }

        protected override void OnStop()
        {
            myTimer.Enabled = false;
        }

        private void Checking(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (System.IO.File.Exists(Host) & !IsFileLocked(new FileInfo(Host)))
                {
                    Task myHost = new Task(() => TransferFile(Host, HostTo));
                    myHost.Start();
                }

                if (System.IO.File.Exists(Hub) & !IsFileLocked(new FileInfo(Hub)))
                {
                    Task myHub = new Task(() => TransferFile(Hub, HubTo));
                    myHub.Start();
                }

                if (System.IO.File.Exists(Observer) & !IsFileLocked(new FileInfo(Observer)))
                {
                    Task myObserver = new Task(() => TransferFile(Observer, ObserverTo));
                    myObserver.Start();
                }

                if (System.IO.File.Exists(Satellite) & !IsFileLocked(new FileInfo(Satellite)))
                {
                    Task mySatellite = new Task(() => TransferFile(Satellite, SatelliteTo));
                    mySatellite.Start();
                }

                if (System.IO.File.Exists(Utilities) & !IsFileLocked(new FileInfo(Utilities)))
                {
                    Task myUtilities = new Task(() => TransferFile(Utilities, UtilitiesTo));
                    myUtilities.Start();
                }
            }

            catch (Exception ee) { LogWritter(myLogFile, destination + log, " There was an error in trying to start a task: ", ee.ToString()); }    
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                New.Dispose();
                New = new Task(() => OnStart(null));
                New.Start();
            }
        }

        private void TransferFile(string FilePath, string FilePathTo)
        {
            try
            {
                string FilePathFileVersion = string.Empty;
                string FilePathToFileVersion = string.Empty;
                
                try { FilePathFileVersion = FileVersionInfo.GetVersionInfo(FilePath).FileVersion.ToString(); }
                catch { FilePathFileVersion = "3.2.0.0"; }

                try { FilePathToFileVersion = FileVersionInfo.GetVersionInfo(FilePathTo).FileVersion.ToString(); }
                catch { FilePathToFileVersion = "3.2.0.0"; }

                if (System.IO.File.Exists(FilePath) & FilePathFileVersion != FilePathToFileVersion)
                {
                        System.IO.File.Copy(FilePath, FilePathTo, true);
                        myLogFile = new FileInfo(destination + log);
                        LogWritter(myLogFile, destination + log, FileVersionInfo.GetVersionInfo(FilePathTo).FileName.ToString() + " File version: " + FilePathFileVersion + " was added to this machine.", "");
                }
            }
            catch (Exception e)
            {
                if (System.IO.File.Exists(destination + log))
                {
                    LogWritter(myLogFile, destination + log, " The following exception was captured when trying to move the requested files: ", e.ToString());
                }
            }
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private void LogWritter(FileInfo myLogFile, string FilePath, string Message, string e) {
            while (IsFileLocked(myLogFile)) { System.Threading.Thread.Sleep(50); }

            if (System.IO.File.Exists(FilePath))
            {
                using (StreamWriter sw = System.IO.File.AppendText(destination + log))
                {
                    sw.WriteLine(DateTime.Now + Message + e);
                }
            }
        }
    }
}
