using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAS_NAS_Connector
{
    public class DriveHelper
    {
        public static IEnumerable<string> GetAvailableDriveLetters()
        {
            return Enumerable.Range('A', 'Z' - 'A' + 1)
                .Select(i => (Char)i + ":")
                .Except(DriveInfo.GetDrives().Select(s => s.Name.Replace("\\", "")));
        }

        public static bool IsDriveAvailable(string drive)
        {
            return GetAvailableDriveLetters().Contains(drive);
        }
    }
}
