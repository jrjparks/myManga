using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows;

namespace myManga_App.Controls.ScreenBrightness
{
    public sealed class MonitorBrightness : DependencyObject, IDisposable
    {
        #region Instance
        private static MonitorBrightness _Instance;
        private static Object SyncObj = new Object();
        public static MonitorBrightness Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new MonitorBrightness(); }
                    }
                }
                return _Instance;
            }
        }
        #endregion

        #region Events
        public delegate void MonitorBrightnessChange(Object sender, GenericEventArgs<Byte> e);
        public event MonitorBrightnessChange MonitorBrightnessChanged;
        private void OnMonitorBrightnessChange(Byte e)
        { OnMonitorBrightnessChange(this, e); }
        private void OnMonitorBrightnessChange(Object sender, Byte e)
        {
            if (MonitorBrightnessChanged != null)
                MonitorBrightnessChanged(sender, new GenericEventArgs<Byte>(e));
        }
        #endregion

        #region Variables

        #region Public
        public static readonly DependencyProperty BrightnessLevelsProperty = DependencyProperty.Register(
            "BrightnessLevels",
            typeof(Byte[]),
            typeof(MonitorBrightness));
        /// <summary>
        /// Get a byte array of monitor brightness levels.
        /// </summary>
        public Byte[] BrightnessLevels
        {
            get { return (Byte[])GetValue(BrightnessLevelsProperty); }
            private set { SetValue(BrightnessLevelsProperty, value); }
        }

        public static readonly DependencyProperty BrightnessLevelCountProperty = DependencyProperty.Register(
            "BrightnessLevelCount",
            typeof(Int32),
            typeof(MonitorBrightness));
        /// <summary>
        /// Get a byte array of monitor brightness levels.
        /// </summary>
        public Int32 BrightnessLevelCount
        {
            get { return (Int32)GetValue(BrightnessLevelCountProperty); }
            private set { SetValue(BrightnessLevelCountProperty, value); }
        }

        public static readonly DependencyProperty CurrentBrightnessProperty = DependencyProperty.Register(
            "CurrentBrightness",
            typeof(Byte),
            typeof(MonitorBrightness));

        /// <summary>
        /// Get the byte value of the current monitor brightness.
        /// </summary>
        public Byte CurrentBrightness
        {
            get { SetValue(CurrentBrightnessProperty, GetCurrentBrightness()); return (Byte)GetValue(CurrentBrightnessProperty); }
            set { SetValue(CurrentBrightnessProperty, value); SetBrightness(value); }
        }

        public static readonly DependencyProperty WatchingMonitorBrightnessProperty = DependencyProperty.Register(
            "WatchingMonitorBrightness",
            typeof(Boolean),
            typeof(MonitorBrightness));
        /// <summary>
        /// Get a boolean value of whether the brightness is being mointored.
        /// </summary>
        public Boolean WatchingMonitorBrightness
        {
            get { return (Boolean)GetValue(WatchingMonitorBrightnessProperty); }
            private set { SetValue(WatchingMonitorBrightnessProperty, value); }
        }

        /// <summary>
        /// Get a boolean value of whether the monitor supports brightness control.
        /// </summary>
        public Boolean SupportsControl
        { get { return (BrightnessLevels.Length > 0); } }
        #endregion

        #region Private
        private ManagementEventWatcher MonitorWatcher;
        private readonly ManagementScope MonitorScope;
        private readonly WqlEventQuery MonitorEventQuery;
        private readonly Dictionary<String, SelectQuery> MonitorQuerys;
        #endregion

        #endregion

        #region Constructor Methods
        private MonitorBrightness()
        {
            MonitorScope = new ManagementScope("\\root\\WMI");
            MonitorEventQuery = new WqlEventQuery("WmiMonitorBrightnessEvent");
            MonitorQuerys = new Dictionary<string, SelectQuery>(2);
            MonitorQuerys.Add("Get", new SelectQuery("WmiMonitorBrightness"));
            MonitorQuerys.Add("Set", new SelectQuery("WmiMonitorBrightnessMethods"));

            BrightnessLevels = GetBrightnessLevels();
            CurrentBrightness = GetCurrentBrightness();

            WatchingMonitorBrightness = false;
        }

        public Int32 IndexOfByte(Byte Byte)
        {
            for (Int32 Index = 0; Index < BrightnessLevels.Length; ++Index)
                if (BrightnessLevels[Index] == Byte)
                    return Index;
            return -1;
        }
        #endregion

        #region Brightness Methods
        private Byte GetCurrentBrightness()
        {
            //Store result
            Byte curBrightness = 0;

            try
            {
                using (ManagementObjectSearcher ManageObjSearch = new ManagementObjectSearcher(MonitorScope, MonitorQuerys["Get"]))
                {
                    foreach (ManagementObject Obj in ManageObjSearch.Get())
                    {
                        curBrightness = (Byte)Obj["CurrentBrightness"];
                        break; //Only work on the first object
                    }
                }
            }
            catch { }
            return curBrightness;
        }

        private Byte[] GetBrightnessLevels()
        {
            //Store result
            Byte[] BrightnessLevels = new Byte[0];

            try
            {
                using (ManagementObjectSearcher ManageObjSearch = new ManagementObjectSearcher(MonitorScope, MonitorQuerys["Get"]))
                {
                    foreach (ManagementObject Obj in ManageObjSearch.Get())
                    {
                        BrightnessLevels = (Byte[])Obj["Level"];
                        break; //Only work on the first object
                    }
                }
            }
            catch { }
            return BrightnessLevels;
        }

        private void SetBrightness(Byte targetBrightness)
        {
            if (WatchingMonitorBrightness)
                MonitorWatcher.Stop();
            try
            {
                using (ManagementObjectSearcher MangObjSear = new ManagementObjectSearcher(MonitorScope, MonitorQuerys["Set"]))
                {
                    foreach (ManagementObject Obj in MangObjSear.Get())
                    {
                        Obj.InvokeMethod(
                            "WmiSetBrightness",
                            new Object[]
                            {   //Note the reversed order - won't work otherwise!
                                UInt32.MaxValue,
                                targetBrightness
                            }
                        );
                        break; //Only work on the first object
                    }
                }
            }
            catch { }
            if (WatchingMonitorBrightness)
                MonitorWatcher.Start();
        }

        public override string ToString()
        {
            return String.Format("{0}%", CurrentBrightness);
        }
        #endregion

        #region Brightness Event Watcher
        private void SetupBrightnessWatch()
        {
            MonitorWatcher = new ManagementEventWatcher(MonitorScope, MonitorEventQuery);
            //StartMonitorWatch();
        }

        /// <summary>
        /// Start monitoring the brightness.
        /// </summary>
        public void StartMonitorWatch()
        {
            if (MonitorWatcher == null)
                SetupBrightnessWatch();
            if (MonitorWatcher != null && !WatchingMonitorBrightness)
            {
                MonitorWatcher.EventArrived += MonitorWatcher_EventArrived;
                WatchingMonitorBrightness = true;
                MonitorWatcher.Start();
            }
        }

        /// <summary>
        /// Stop monitoring the brightness.
        /// </summary>
        public void StopMonitorWatch()
        {
            if (MonitorWatcher != null && WatchingMonitorBrightness)
            {
                MonitorWatcher.Stop();
                MonitorWatcher.EventArrived -= MonitorWatcher_EventArrived;
                WatchingMonitorBrightness = false;
            }
        }

        private void MonitorWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            Byte BrightnessValue = (Byte)e.NewEvent.Properties["Brightness"].Value;
            OnMonitorBrightnessChange(BrightnessValue);
        }
        #endregion

        #region IDisposable Methods
        public void Dispose()
        {
            if (MonitorWatcher != null)
            {
                StopMonitorWatch();
                MonitorWatcher.Dispose();
            }
        }
        #endregion
    }
}
