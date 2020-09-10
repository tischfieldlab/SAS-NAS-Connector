using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAS_NAS_Connector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConnectionViewModel cinfo;
        private ConnectorActor actor;


        public MainWindow()
        {
            InitializeComponent();
            this.reset();
        }

        protected void reset()
        {
            this.DataContext = this.cinfo = new ConnectionViewModel();
            this.statusPanel.DataContext = this.actor = new ConnectorActor(this.cinfo, this);
            this.password.Clear();
        }

        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            this.reset();
        }

        private async void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            StepResult result = null;
            await Task.Run(() =>
            {
                result = this.actor.Connect(this.password);
            });

            if (result is StepErrorResult)
            {
                // Looks like we got an error somewhere in the chain of operations, report it to the user!
                MessageBox.Show(this, (result as StepErrorResult).Message, (result as StepErrorResult).Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                // If we got here, looks like thing succeeded
                MessageBox.Show(this, "Successfully mapped the share!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            this.cinfo.RequeryDrives();
        }
    }
}
