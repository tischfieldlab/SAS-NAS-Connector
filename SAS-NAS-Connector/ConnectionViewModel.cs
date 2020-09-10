using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAS_NAS_Connector
{
    class ConnectionViewModel : ObservableObject, IDataErrorInfo
    {
        private ObservableCollection<string> _availableDrives = new ObservableCollection<string>();

        private string username;
        private string host = Properties.Settings.Default.DefaultHost;
        private string share = Properties.Settings.Default.DefaultShare;
        private string mountTo;
        private bool persist = Properties.Settings.Default.DefaultPersistance;

        public ConnectionViewModel()
        {
            this.AvailableDrives = new ReadOnlyObservableCollection<string>(this._availableDrives);
            this.RequeryDrives();
        }
        
        public ReadOnlyObservableCollection<string> AvailableDrives { get; protected set; }
        public string Username
        {
            get => this.username;
            set {
                this.SetField(ref this.username, value);
                this.NotifyPropertyChanged(nameof(this.IsValid));
            }
        }

        public string Host
        {
            get => this.host;
            set {
                this.SetField(ref this.host, value);
                this.NotifyPropertyChanged(nameof(this.IsValid));
            }
        }

        public string Share
        {
            get => this.share;
            set {
                this.SetField(ref this.share, value);
                this.NotifyPropertyChanged(nameof(this.IsValid));
            }
        }

        public string MountLocation
        {
            get => this.mountTo;
            set {
                this.SetField(ref this.mountTo, value);
                this.NotifyPropertyChanged(nameof(this.IsValid));
            }
        }

        public bool IsPersistent
        {
            get => this.persist;
            set {
                this.SetField(ref this.persist, value);
                this.NotifyPropertyChanged(nameof(this.IsValid));
            }
        }

        
        public void RequeryDrives()
        {
            this._availableDrives.Clear();
            foreach(var d in DriveHelper.GetAvailableDriveLetters())
            {
                this._availableDrives.Add(d);
            }

            if (DriveHelper.IsDriveAvailable(Properties.Settings.Default.DefaultDrive))
            {
                this.MountLocation = Properties.Settings.Default.DefaultDrive;
            }
        }


        #region IDataErrorInfo Members

        public bool IsValid
        {
            get
            {
                var ValidatedProperties = this.GetType().GetProperties().Select(pi => pi.Name);
                foreach (string property in ValidatedProperties)
                {
                    if (this[property] != null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public string Error
        {
            get {
                return this.GetType()
                        .GetProperties()
                        .Select(pi => this[pi.Name])
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Aggregate(string.Empty, (a, e) => a + "\n " + e);
            }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(this.Username))
                {
                    // Validate property and return a string if there is an error
                    if (string.IsNullOrEmpty(this.Username))
                        return "Username is Required";
                }

                if (columnName == nameof(this.Host))
                {
                    if (string.IsNullOrEmpty(this.Host))
                        return "Hostname is Required";

                    if (Uri.CheckHostName(this.Host) == UriHostNameType.Unknown)
                        return "Hostname must be a valid hostname";
                }


                if (columnName == nameof(this.Share))
                {
                    if (string.IsNullOrEmpty(this.Share))
                        return "Share Location is Required";

                    if (!Uri.TryCreate(this.Share, UriKind.Absolute, out Uri uri) || !uri.IsUnc)
                        return @"Share Location must be a valid UNC path (i.e.  \\server\share)";
                }

                if (columnName == nameof(this.MountLocation))
                {
                    if (string.IsNullOrEmpty(this.MountLocation))
                        return "Mount To is Required";
                }

                // If there's no error, null gets returned
                return null;
            }
        }
        #endregion
    }
}
