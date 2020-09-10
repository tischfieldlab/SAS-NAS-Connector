using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SAS_NAS_Connector
{
    class ConnectorActor : ObservableObject
    {
        private ConnectionViewModel cinfo;
        private Window owner;

        private bool isBusy;
        private string status;

        public ConnectorActor(ConnectionViewModel ConnectionInfo, Window Owner)
        {
            this.cinfo = ConnectionInfo;
            this.owner = Owner;
        }

        public StepResult Connect(PasswordBox password)
        {
            this.Status = string.Empty;
            this.IsBusy = true;

            // First check if data looks valid!
            this.Status = "Validating connection information...";
            var res1 = this.AttemptValidateConnectionInfo();
            if (res1 is StepErrorResult)
            {
                this.IsBusy = false;
                return res1;
            }

            // Second we need to SSH into the box to register the credentials
            this.Status = "Attempting to establish SSH connection...";
            var res2 = this.AttemptSSHConnection(password);
            if (res2 is StepErrorResult)
            {
                this.IsBusy = false;
                return res2;
            }

            // Third actually try to mount the drive
            this.Status = "Attempting to mount share...";
            var res3 = this.AttemptShareMount(password);
            if (res3 is StepErrorResult)
            {
                this.IsBusy = false;
                return res3;
            }

            this.Status = string.Empty;
            this.IsBusy = false;
            return new StepSuccessResult();
        }

        public bool IsBusy
        {
            get => this.isBusy;
            set => this.SetField(ref this.isBusy, value);
        }
        public string Status
        {
            get => this.status;
            set => this.SetField(ref this.status, value);
        }


        protected StepResult AttemptValidateConnectionInfo()
        {
            if (!this.cinfo.IsValid)
            {
                return new StepErrorResult()
                {
                    Title = "Please complete all fields properly!",
                    Message = this.cinfo.Error,
                };
            }
            return new StepSuccessResult();
        }

        protected StepResult AttemptSSHConnection(PasswordBox password)
        {
            using (SshClient client = new SshClient(this.cinfo.Host, this.cinfo.Username, password.Password))
            {
                try
                {
                    client.Connect();
                    client.Disconnect();
                }
                catch (Exception except)
                {
                    return new StepErrorResult()
                    {
                        Title = "Error establishing SSH connection!",
                        Message = except.Message,
                    };
                }
            }
            return new StepSuccessResult();
        }

        protected StepResult AttemptShareMount(PasswordBox password)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = "net";
                p.StartInfo.Arguments = $@" use {this.cinfo.MountLocation} ""{this.cinfo.Share}"" {password.Password} /USER:RAD\{this.cinfo.Username} /PERSISTENT:YES";
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                if (!output.Trim().Equals("The command completed successfully."))
                {
                    Console.Write(output);
                    throw new Exception(output);
                }
                p.Dispose();
            }
            catch (Exception except)
            {
                return new StepErrorResult()
                {
                    Title = "Error mounting share!",
                    Message = except.Message,
                };
            }
            return new StepSuccessResult();
        }
    }


    public abstract class StepResult { }
    public class StepSuccessResult : StepResult { }
    public class StepErrorResult : StepResult
    {
        public string Title { get; set; }
        public string Message { get; set; }
    }
}
