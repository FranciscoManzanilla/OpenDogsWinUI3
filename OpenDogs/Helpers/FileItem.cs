using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace OpenDogs.Helpers
{
    public enum ItemType
    {
        Folder,
        File
    }

    public class FileItem : INotifyPropertyChanged
    {
        private BitmapImage _thumbnail;

        public string Name { get; }
        public string Path { get; }
        public bool IsVirtual { get; }
        // 🔽 Estas dos propiedades son clave para búsqueda recursiva:
        public bool IsDirectory => Type == ItemType.Folder;
        public ObservableCollection<FileItem> Children { get; set; } = new ObservableCollection<FileItem>();


        public ItemType Type { get; }
        //public BitmapImage Thumbnail { get; set; }

        // Constructor para StorageFile real
        public FileItem(StorageFile file)
        {
            Type = ItemType.File;
            Name = file.Name;
            Path = file.Path;
            IsVirtual = false;

            LoadThumbnailAsync(file);
        }

        // Constructor para StorageFolder real
        public FileItem(StorageFolder folder)
        {
            Type = ItemType.Folder;
            Name = folder.Name;
            Path = folder.Path;
            IsVirtual = false;

            LoadFolderIcon();
        }

        // Constructor para VirtualStorageFolder
        // Constructor para carpetas virtuales
        public FileItem(VirtualStorageFolder folder)
        {
            Type = ItemType.Folder;
            Name = folder.Name;
            Path = folder.Path;
            IsVirtual = true;
            LoadFolderIcon();
        }

        // Constructor para archivos virtuales
        public FileItem(VirtualStorageFile file)
        {
            Type = ItemType.File;
            Name = file.Name;
            Path = file.Path;
            IsVirtual = true;

            string ext = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
            string iconPath = "ms-appx:///Assets/iconsex/level.png";

            switch (ext)
            {
                case ".xbt": iconPath = "ms-appx:///Assets/iconsex/xbt.png"; break;
                case ".xbg": iconPath = "ms-appx:///Assets/iconsex/xbg.png"; break;
                case ".lib": iconPath = "ms-appx:///Assets/iconsex/lib.png"; break;
                case ".cso": iconPath = "ms-appx:///Assets/iconsex/shdr.png"; break;
                case ".wem": iconPath = "ms-appx:///Assets/iconsex/wem.png"; break;
                case ".loc": iconPath = "ms-appx:///Assets/iconsex/loc.png"; break;
                case ".obj": iconPath = "ms-appx:///Assets/iconsex/obj.png"; break;
                case ".bik": iconPath = "ms-appx:///Assets/iconsex/bik.png"; break;

            }

            Thumbnail = new BitmapImage(new Uri(iconPath));
        }


        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        //public FileItem(StorageFile file)
        //{
        //    Type = ItemType.File;
        //    Name = file.Name;
        //    Path = file.Path;
        //    LoadThumbnailAsync(file);
        //}

        //public FileItem(StorageFolder folder)
        //{
        //    Type = ItemType.Folder;
        //    Name = folder.Name;
        //    Path = folder.Path;
        //    LoadFolderIcon();
        //}

        private async void LoadThumbnailAsync(StorageFile file)
        {
            const uint size = 48;
            var thumb = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, size);
            if (thumb != null && thumb.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
            {
                var bmp = new BitmapImage();
                bmp.SetSource(thumb);
                Thumbnail = bmp;
            }
            else
            {
                string ext = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
                string iconPath = "ms-appx:///Assets/iconsex/fileunk.png";

                switch (ext)
                {
                    case ".dat":
                        iconPath = "ms-appx:///Assets/iconsex/dat.png";
                        break;
                    case ".exe":
                        iconPath = "ms-appx:///Assets/iconsex/exe.png";
                        break;
                    case ".fat":
                        iconPath = "ms-appx:///Assets/iconsex/fat.png";
                        break;
                    case ".xbt":
                        iconPath = "ms-appx:///Assets/iconsex/xbt.png";
                        break;
                    case ".xbg":
                        iconPath = "ms-appx:///Assets/iconsex/xbg.png";
                        break;
                        // Agrega más extensiones según necesites
                }

                Thumbnail = new BitmapImage(new Uri(iconPath));
            }
        }


        private void LoadFolderIcon()
        {
            Thumbnail = new BitmapImage(new Uri("ms-appx:///Assets/iconsex/folder.png"));
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class TreeItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }

        public ObservableCollection<TreeItem> Children { get; set; } = new();
    }

    public class FolderNode
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public override string ToString() => Name; // Esto se usa para mostrarlo en el TreeView
    }

    public class VirtualStorageFolder
    {
        public string Name { get; }
        public string Path { get; }

        public VirtualStorageFolder(string path, string name)
        {
            Path = path;
            Name = name;
        }
    }

    public class VirtualStorageFile
    {
        public string Name { get; }
        public string Path { get; }

        public VirtualStorageFile(string path, string name)
        {
            Path = path;
            Name = name;
        }
    }


}
