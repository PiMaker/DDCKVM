using System;
using System.Management;

namespace DDCKVMService
{
    // Code adapted from: https://stackoverflow.com/a/19435744
    public class USBHandler
    {
        public event EventHandler<string> DeviceInserted, DeviceRemoved;

        private readonly ManagementEventWatcher insertWatcher, removeWatcher;

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== USB EVENT (inserted) ===");
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                foreach (var property in instance.Properties)
                {
                    Console.WriteLine(property.Name + " = " + property.Value);
                }

                DeviceInserted?.Invoke(this, instance.Properties["DeviceID"].Value.ToString());
            }
            catch
            {
                // ignore exceptions in favor of not crashing
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== USB EVENT (unplugged) ===");
                ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                foreach (var property in instance.Properties)
                {
                    Console.WriteLine(property.Name + " = " + property.Value);
                }

                DeviceRemoved?.Invoke(this, instance.Properties["DeviceID"].Value.ToString());
            }
            catch
            {
                // ignore exceptions in favor of not crashing
            }
        }

        public void Deregister()
        {
            this.insertWatcher.Stop();
            this.removeWatcher.Stop();
        }

        public USBHandler()
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            this.insertWatcher = new ManagementEventWatcher(insertQuery);
            this.insertWatcher.EventArrived += new EventArrivedEventHandler(this.DeviceInsertedEvent);
            this.insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            this.removeWatcher = new ManagementEventWatcher(removeQuery);
            this.removeWatcher.EventArrived += new EventArrivedEventHandler(this.DeviceRemovedEvent);
            this.removeWatcher.Start();
        }
    }
}