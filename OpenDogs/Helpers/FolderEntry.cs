using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace OpenDogs.Helpers
{
    public class FolderEntry
    {
        public bool IsReal { get; set; }
        public StorageFolder? RealFolder { get; set; }
        public string? VirtualFolderPath { get; set; }
    }

}
