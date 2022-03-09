namespace DupTerminator.WindowsSpecific
{
    using DupTerminator.BusinessLogic;
    using System.Management;

    public class WindowsUtil : IWindowsUtil
    {
        public List<String> GetPhisicalDrives()
        {
            var query = new WqlObjectQuery("SELECT * FROM Win32_DiskDrive");
            using (var searcher = new ManagementObjectSearcher(query))
            {
                var ymp = searcher.Get()
                                 .OfType<ManagementObject>();
                return searcher.Get()
                                 .OfType<ManagementObject>()
                                 .Select(o => o.Properties["DeviceID"].Value.ToString())
                                 .ToList();
            }
        }

        public string GetModelFromDrive(string driveLetter)
        {
            // Must be 2 characters long.
            // Function expects "C:" or "D:" etc...
            if (driveLetter.Length != 2)
                throw new ArgumentException("Function expects \"C: \" or \"D: \" etc...");

            try
            {
                using (var partitions = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_LogicalDisk.DeviceID='" + driveLetter +
                                                 "'} WHERE ResultClass=Win32_DiskPartition"))
                {
                    foreach (var partition in partitions.Get())
                    {
                        using (var drives = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" +
                                                             partition["DeviceID"] +
                                                             "'} WHERE ResultClass=Win32_DiskDrive"))
                        {
                            foreach (var drive in drives.Get())
                            {
                                return (string)drive["Model"];
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            // Not Found
            return null;
        }

        public Dictionary<string, List<string>> GetDrives()
        {
            var drives = GetLogicalDrives();
            var disks = new Dictionary<string, List<string>>();
            foreach (var drive in drives)
            {
                var model = GetModelFromDrive(drive.Substring(0, 2));
                if (!disks.ContainsKey(model))
                    disks.Add(model, new List<string>());
                disks[model].Add(drive);
            }
            return disks;
        }

        private IEnumerable<string> GetLogicalDrives()
        {
            DriveInfo[] myDrives = DriveInfo.GetDrives();

            return myDrives.Select(d => d.RootDirectory.FullName);
        }
    }
}