using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;

namespace DDCKVMService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            this.InitializeComponent();

            // Automatically start after install
            this.AfterInstall += new InstallEventHandler(this.ServiceInstaller_AfterInstall);
        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            using (ServiceController sc = new ServiceController(this.serviceInstaller1.ServiceName))
            {
                sc.Start();

                // Start browser on install
                Process.Start("http://localhost:4280");
            }
        }
    }
}