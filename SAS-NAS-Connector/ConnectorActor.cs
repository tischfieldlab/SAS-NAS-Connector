using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private bool isBusy;
        private string status;

        public ConnectorActor(ConnectionViewModel ConnectionInfo)
        {
            this.cinfo = ConnectionInfo;
        }

        public StepResult Connect(PasswordBox password)
        {
            this.Status = string.Empty;
            this.IsBusy = true;

            var steps = new List<PipelineStep>()
            {
                new PipelineStep("Validating connection information...")
                {
                    Implementation = () => this.AttemptValidateConnectionInfo()
                },
                new PipelineStep("Attempting to establish SSH connection...")
                {
                    Condition = () => this.cinfo.NeedsSshLogin,
                    Implementation = () => this.AttemptSSHConnection(password)
                },
                new PipelineStep("Attempting to store credentials...")
                {
                    Implementation = () => this.AttemptStoreCredentials(password)
                },
                new PipelineStep("Attempting to mount share...")
                {
                    Implementation = () => this.AttemptShareMount(password)
                },
                new PipelineStep("Verifying share mount...")
                {
                    Implementation = () => this.AttemptTestShareMount()
                },
            };

            // run the pipeline steps
            foreach(var step in steps)
            {
                if (step.Condition == null || step.Condition.Invoke())
                {
                    this.Status = step.Description;
                    var result = step.Implementation.Invoke();
                    if (result is StepErrorResult)
                    {
                        this.IsBusy = false;
                        return result;
                    }
                }
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

        protected StepResult AttemptStoreCredentials(PasswordBox password)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;

                p.StartInfo.FileName = "cmdkey";
                p.StartInfo.Arguments = $@" /add:{this.cinfo.ShareHostname} /user:{this.cinfo.Domain}\{this.cinfo.Username} /pass:{password.Password}";

                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                if (!output.Trim().Equals("CMDKEY: Credential added successfully."))
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
                    Title = "Saving connection credentials!",
                    Message = except.Message,
                };
            }
            return new StepSuccessResult();
        }

        protected StepResult AttemptShareMount(PasswordBox password)
        {
            try
            {
                string persist = this.cinfo.IsPersistent ? "YES" : "NO";
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;

                p.StartInfo.FileName = "net";
                p.StartInfo.Arguments = $@" use {this.cinfo.MountLocation} ""{this.cinfo.Share}"" /PERSISTENT:{persist}";

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

        protected StepResult AttemptTestShareMount()
        {
            try
            {
                var results = Directory.GetFileSystemEntries(this.cinfo.MountLocation);
            }
            catch (Exception except)
            {
                return new StepErrorResult()
                {
                    Title = "Error testing share mount!",
                    Message = except.Message,
                };
            }
            return new StepSuccessResult();
        }
    }

    public class PipelineStep
    {
        public PipelineStep() { }
        public PipelineStep(string Description)
        {
            this.Description = Description;
        }
        public string Description { get; set; }
        public Func<StepResult> Implementation { get; set; }
        public Func<bool> Condition { get; set; }
    }

    public abstract class StepResult { }
    public class StepSuccessResult : StepResult { }
    public class StepErrorResult : StepResult
    {
        public string Title { get; set; }
        public string Message { get; set; }
    }
}
