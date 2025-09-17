using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using OpenDogs.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenDogs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WExplorer : Page
    {
        private ObservableCollection<FileItem> currentItems = new();
        private ObservableCollection<TreeItem> TreeRoot { get; set; }

        private Stack<FolderEntry> folderHistory = new Stack<FolderEntry>();

        private StorageFolder? currentFolder = null;
        private string? currentVirtualPath = null;
        private List<FileItem> allItems = new(); // Archivos completos (virtuales o reales)
        public ObservableCollection<FileItem> DisplayedItems { get; set; } = new();

        Stack<string> virtualNavigationStack = new Stack<string>();
        //string currentVirtualPath = ""; // ruta virtual actual

        private DdsPreviewHelper _ddsPreviewHelper = new();
        private string globalFullPathDat;
        private string globalDdsItem;
        private string? xbtRoot;
        private string globalFatDir;
        private string globalTools;
        private string globalView;
        private string globalPytool;
        private string global3DToolExport;
        private string globalGamerFolder;
        private string globalToolsPack;
        private string globalGameFolderP;
        private string globalGameBikPlayer;
        private string globalToolsWem;

        private Window? window;

        public WExplorer()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            window = e.Parameter as Window;
            var hwnd = WindowNative.GetWindowHandle(window); var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                folderHistory.Clear();
                currentFolder = folder;
                BackButton.IsEnabled = false;
                await LoadFolderContentsAsync(folder);
                LoadPropTool(folder.Name, folder.Path); // Cargar propiedades del juego
            }
            await LoadRealFilesRecursive(folder);
            allItems = new List<FileItem>(currentItems); // Copia los archivos cargados
            currentVirtualItems = currentItems.ToList(); // 🆕 guardar lo que se ve

            DisplayedItems.Clear();
            foreach (var item in currentVirtualItems)
                DisplayedItems.Add(item);

            FileGrid.ItemsSource = DisplayedItems;
        }

        private void LoadPropTool(string GameType, string GameFolder)
        {
            globalGamerFolder = GameFolder;
            globalGameFolderP = GameType;
            //
            if (GameType == "Watch Dogs Legion")
            {
                globalToolsPack = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "toolswdl", "PackLegion.exe");
                globalTools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "toolswdl", "UnpackLegion.exe");
            }
            else if (GameType == "WATCH_DOGS2")
            {
                globalToolsPack = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "WD2Pack.exe");
                globalTools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "WD2Extract.exe");
            }
            //
            globalView = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BsView", "viewt.html");
            globalGameBikPlayer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "radvideo64.exe");
            globalPytool = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "xbgtools", "convert_xbg_to_obj.py");
            global3DToolExport = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenBlenderTool", "blender.exe");
            globalToolsWem = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wemtool", "vgmstream-cli.exe");
        }

        private async Task LoadFolderContentsAsync(StorageFolder folder)
        {
            currentFolder = folder;
            currentVirtualPath = null;

            currentItems.Clear();

            var folders = await folder.GetFoldersAsync();
            var files = await folder.GetFilesAsync();

            foreach (var f in folders)
                currentItems.Add(new FileItem(f));

            foreach (var file in files)
                currentItems.Add(new FileItem(file));

            FileGrid.ItemsSource = currentItems;
            BackButton.IsEnabled = folderHistory.Count > 0;
        }

        private async void FileGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FileItem item)
            {
                string? newPath = item.Path;

                // Revisar si es diferente de la actual para agregar a historial
                bool isNewPathDifferent = false;
                if (item.Type == ItemType.Folder)
                {
                    if (IsRealPath(newPath))
                    {
                        isNewPathDifferent = currentFolder == null || !string.Equals(currentFolder.Path, newPath, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        isNewPathDifferent = currentVirtualPath == null || !string.Equals(currentVirtualPath, newPath, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    // Si es archivo, no afecta historial de carpetas
                    isNewPathDifferent = false;
                }

                if (isNewPathDifferent)
                {
                    // Guardar el estado actual en historial antes de cambiar
                    if (currentFolder != null)
                    {
                        // Carpeta real: usar pila folderHistory
                        folderHistory.Push(new FolderEntry
                        {
                            IsReal = true,
                            RealFolder = currentFolder,
                            VirtualFolderPath = null
                        });
                    }
                    else if (!string.IsNullOrEmpty(currentVirtualPath))
                    {
                        // Carpeta virtual: guardar en pila virtualNavigationStack
                        virtualNavigationStack.Push(currentVirtualPath);
                    }
                }

                if (item.Type == ItemType.Folder)
                {
                    if (IsRealPath(newPath))
                    {
                        var folder = await StorageFolder.GetFolderFromPathAsync(newPath);
                        currentVirtualPath = null; // estás en carpeta real
                        await LoadFolderContentsAsync(folder);
                    }
                    else
                    {
                        
                        currentVirtualPath = newPath; // actualiza ruta virtual
                        await LoadVirtualFolderContentsAsync(newPath);
                        
                    }
                }
                else
                {
                    if (item.IsVirtual)
                    {
                        string ext = Path.GetExtension(newPath).ToLowerInvariant();

                        if (ext == ".xbt")
                        {
                            //await ShowMessageAsync("Texture file", "This is a texture file");
                            await DialogHelper.ShowLoadingDialogAsync(window.Content.XamlRoot, async () =>
                            {
                                globalDdsItem = newPath;
                                //globalFullPathDat = newPath;
                                //globalFatDir = Path.GetDirectoryName(newPath);
                                //globalTools = globalToolsPack; // Asignar la herramienta correcta
                                xbtRoot = await DdsPreviewHelper.ExtraerTemporalXbtAsync(globalTools, globalFullPathDat, globalDdsItem, globalGameFolderP);
                            });
                            //CheckXBTFileExists(newPath);
                            if (xbtRoot != null)
                            {
                                string rutaBase = await FolderHelper.ObtenerRutaFinalAsync(window);

                                // Extraer el DDS como byte[]
                                byte[] ddsPreviewBytes = DdsPreviewHelper.Extract(xbtRoot, rutaBase);

                                // Convertir los bytes a imagen y asignar al ImagePreview
                                using var stream = new InMemoryRandomAccessStream();
                                using var writer = new DataWriter(stream.GetOutputStreamAt(0));
                                writer.WriteBytes(ddsPreviewBytes);
                                await writer.StoreAsync();
                                await writer.FlushAsync();
                                stream.Seek(0);

                                var bitmapImage = new BitmapImage();
                                try
                                {
                                    await bitmapImage.SetSourceAsync(stream);
                                }
                                catch (Exception)
                                {
                                    await ShowMessageAsync("Error", "Failed to extract the texture file or load xbt.");
                                    return;
                                }

                                //ImagePreview.Source = bitmapImage;

                                var resultDialog = new ContentDialog
                                {
                                    Title = "Preview",
                                    XamlRoot = window.Content.XamlRoot,
                                    PrimaryButtonText = "OK",
                                    Content = new StackPanel
                                    {
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Spacing = 10,
                                        Children =
                                        {
                                            new Microsoft.UI.Xaml.Controls.Image
                                            {
                                                Width = 500,
                                                Height = 500,
                                                Source = bitmapImage,
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            },
                                            new TextBlock
                                            {
                                                Text = $"{item.Name}",
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                TextAlignment = TextAlignment.Center,
                                                TextWrapping = TextWrapping.Wrap
                                            },
                                            new TextBlock
                                            {
                                                Text = $"{item.Path}",
                                                HorizontalAlignment = HorizontalAlignment.Center,
                                                TextAlignment = TextAlignment.Center,
                                                TextWrapping = TextWrapping.Wrap
                                            }
                                        }
                                    }
                                };

                                await resultDialog.ShowAsync();
                            }
                            else
                            {
                                await ShowMessageAsync("Error", "Failed to extract the texture file or load xbt.");
                            }

                        }
                        else if (ext == ".xbg")
                        {
                            //await ShowMessageAsync("3D Model file", "This is a virtual 3D file (.xbg).");
                            await DialogHelper.ShowLoadingDialogAsync(window.Content.XamlRoot, async () =>
                            {
                                xbgRoot = await DdsPreviewHelper.ExtraerConvertirYCargarXBG(
                                globalTools, globalFullPathDat, newPath, global3DToolExport, globalPytool);
                            });                                

                            if (xbgRoot is null)
                            {
                                await ShowMessageAsync("Error", "Failed to extract or convert the xbg file.");
                            }
                            else
                            {
                                //Debug.WriteLine("➡️ Llamando a LoadObj con: " + xbgRoot);
                                AbrirEnVisor3D(xbgRoot);
                            }
                        }
                        else if (ext == ".lib")
                        {
                            await ShowMessageAsync("Library file", "This is a virtual library file (.lib).");
                        }
                        else if (ext == ".cso")
                        {
                            await ShowMessageAsync("Shader file", "This is a virtual shader file (.cso).");
                        }
                        else if (ext == ".wem")
                        {
                            //await ShowMessageAsync("Audio file", "This is a virtual audio file (.wem).");
                            await DialogHelper.ShowLoadingDialogAsync(window.Content.XamlRoot, async () =>
                            {
                                wemRoot = await DdsPreviewHelper.ExtraerArchivoDesdeFatAsync(
                                    globalTools,
                                    globalFullPathDat,
                                    newPath,
                                    globalGameFolderP
                                );
                                await DdsPreviewHelper.PlayWem(globalToolsWem, wemRoot);
                            });
                            if (wemRoot != null)
                            {
                                await MostrarReproductorDesdeRutaAsync(window.Content.XamlRoot, $"{wemRoot}.wav", item.Path);
                            }
                        }
                        else if (ext == ".loc")
                        {
                            await ShowMessageAsync("Localization file", "This is a virtual localization file (.loc).");
                        }
                        else if (ext == ".bik")
                        {
                            await DialogHelper.ShowLoadingDialogAsync(window.Content.XamlRoot, async () =>
                            {
                                var root = await DdsPreviewHelper.ExtraerArchivoDesdeFatAsync(
                                    globalTools,
                                    globalFullPathDat,
                                    newPath,
                                    globalGameFolderP
                                );
                                await DdsPreviewHelper.PlayVideo(globalGameBikPlayer, root);
                            });
                            
                            //await ShowMessageAsync("3D Object file", "This is a virtual 3D object file (.obj).");
                        }
                        else if (ext == ".dds")
                        {
                            //// Aquí podrías abrir el archivo DDS con tu vista previa
                            //globalDdsItem = newPath;
                            //globalFullPathDat = newPath;
                            //globalFatDir = Path.GetDirectoryName(newPath);
                            //globalTools = globalToolsPack; // Asignar la herramienta correcta
                            //await _ddsPreviewHelper.ExtraerTemporalXbtAsync(globalTools, globalFatDir, globalDdsItem);
                        }
                        else
                        {
                            await ShowMessageAsync("Unknown file", "This virtual file cannot be opened.");
                        }
                    }
                    else if (newPath.EndsWith(".fat", StringComparison.OrdinalIgnoreCase))
                    {
                        await LoadFatContentsAsync(newPath);
                    }
                    else
                    {
                        // Abrir archivo normal u otra acción
                    }
                }

                // Ajustar habilitación del botón atrás
                BackButton.IsEnabled = folderHistory.Count > 0 || virtualNavigationStack.Count > 0;
            }
        }

        public static async Task MostrarReproductorDesdeRutaAsync(XamlRoot xamlRoot, string wavPath, string wemPath)
        {
            if (!File.Exists(wavPath))
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = $"File not found:\n{wemPath}",
                    XamlRoot = xamlRoot,
                    CloseButtonText = "OK"
                }.ShowAsync();
                return;
            }

            var file = await StorageFile.GetFileFromPathAsync(wavPath);
            var stream = await file.OpenAsync(FileAccessMode.Read);

            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromStream(stream, file.ContentType);

            // Controles
            var btnPlay = new Button { Content = "▶️", Width = 60 };
            var btnPause = new Button { Content = "⏸️", Width = 60 };
            var btnStop = new Button { Content = "⏹️", Width = 60 };

            btnPlay.Click += (s, e) => mediaPlayer.Play();
            btnPause.Click += (s, e) => mediaPlayer.Pause();
            btnStop.Click += (s, e) =>
            {
                mediaPlayer.Pause();
                mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            };

            var dialog = new ContentDialog
            {
                Title = "Wem player",
                XamlRoot = xamlRoot,
                PrimaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Children =
                {
                    new Microsoft.UI.Xaml.Controls.Image
                    {
                        Width = 50,
                        Height = 50,
                        Source = new BitmapImage(new Uri("ms-appx:///Assets/iconsex/wem.png")),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"File: {Path.GetFileName(wemPath)}",
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 8,
                        Children = { btnPlay, btnPause, btnStop }
                    }
                }
                }
            };

            await dialog.ShowAsync();

            mediaPlayer.Dispose();
        }

        public void AbrirEnVisor3D(string pathObj)
        {
            if (!File.Exists(pathObj))
            {
                Debug.WriteLine("❌ No se encontró el archivo OBJ: " + pathObj);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pathObj,
                    UseShellExecute = true // Importante para abrir con la app por defecto
                });
                Debug.WriteLine("📂 OBJ abierto con Visor 3D");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("🧨 Error al abrir el OBJ: " + ex.Message);
            }
        }

        private FileItem? rightClickedItem;

        private void FileItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FileItem item)
            {
                rightClickedItem = item;

                var flyout = FlyoutBase.GetAttachedFlyout(element);

                // Ocultar opciones específicas según extensión si es virtual
                if (item.IsVirtual)
                {
                    var ext = System.IO.Path.GetExtension(item.Path).ToLowerInvariant();

                    VirtualXbgOption.Visibility = ext == ".xbg" ? Visibility.Visible : Visibility.Collapsed;
                    //VirtualXbgView.Visibility = ext == ".xbg" ? Visibility.Visible : Visibility.Collapsed;

                    /////VirtualXbtOption.Visibility = ext == ".xbt" ? Visibility.Visible : Visibility.Collapsed;
                    //VirtualXbtView.Visibility = ext == ".xbt" ? Visibility.Visible : Visibility.Collapsed;
                    VirtualXbtMod.Visibility = ext == ".xbt" ? Visibility.Visible : Visibility.Collapsed;

                }
                else
                {
                    VirtualXbgOption.Visibility = Visibility.Collapsed;
                    //VirtualXbtOption.Visibility = Visibility.Collapsed;
                    //VirtualXbgView.Visibility = Visibility.Collapsed;
                    //VirtualXbtView.Visibility = Visibility.Collapsed;
                    VirtualXbtMod.Visibility = Visibility.Collapsed;
                }
                if (!item.IsVirtual)
                {
                    var ext = System.IO.Path.GetFileName(item.Name);

                    WatchPlay.Visibility = ext == "WatchDogs2.exe" || ext == "WatchDogsLegion.exe" ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    WatchPlay.Visibility = Visibility.Collapsed;

                }

                flyout?.ShowAt(element);
                e.Handled = true;
            }
        }
        private async void VirtualXbgOption_Click(object sender, RoutedEventArgs e)
        {
            if (rightClickedItem != null)
            {
                await ShowMessageAsync("Archivo 3D", $"Opción especial para archivo virtual .xbg:\n{rightClickedItem.Name}");
            }
        }

        private async void VirtualXbtOption_Click(object sender, RoutedEventArgs e)
        {
            if (rightClickedItem != null)
            {
                await ShowMessageAsync("Archivo de textura", $"Opción especial para archivo virtual .xbt:\n{rightClickedItem.Path}");
            }
        }


        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                XamlRoot = this.XamlRoot,
                CloseButtonText = "Ok"
            };
            await dialog.ShowAsync();
        }

        private async Task LoadVirtualFolderContentsAsync(string folderPath)
        {

            currentFolder = null;
            currentVirtualPath = folderPath;

            var parentNode = FindNodeByPath(TreeRoot, folderPath);
            if (parentNode == null)
            {
                currentItems.Clear();
                FileGrid.ItemsSource = currentItems;
                return;
            }

            var children = parentNode.Children;

            var itemsToShow = children.Select(ti =>
                ti.IsDirectory
                    ? new FileItem(new VirtualStorageFolder(ti.FullPath, ti.Name))
                    : new FileItem(new VirtualStorageFile(ti.FullPath, ti.Name))
            ).ToList();

            currentItems.Clear();
            foreach (var fi in itemsToShow)
                currentItems.Add(fi);

            // Actualiza currentVirtualItems con los items visibles actuales
            currentVirtualItems = new List<FileItem>(currentItems);

            // Actualiza DisplayedItems para que muestre estos elementos (sin filtro)
            DisplayedItems.Clear();
            foreach (var item in currentVirtualItems)
                DisplayedItems.Add(item);

            FileGrid.ItemsSource = DisplayedItems;

            BackButton.IsEnabled = folderHistory.Count > 0 || virtualNavigationStack.Count > 0;

            await LoadVirtualFilesRecursive(folderPath);
            allItems = new List<FileItem>(currentItems);
        }
        private TreeItem FindNodeByPath(IEnumerable<TreeItem> nodes, string path)
        {
            foreach (var node in nodes)
            {
                if (node.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return node;

                var found = FindNodeByPath(node.Children, path);
                if (found != null)
                    return found;
            }
            return null;
        }


        private bool IsRealPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        }

        private async Task LoadFatContentsAsync(string fatFilePath)
        {
            // Ejemplo: convertir ruta .fat a .filelist o leer el archivo para sacar contenido
            // Supondré que tienes alguna función para leer el contenido virtual dentro del .fat

            // Aquí usas lógica parecida a la que mostraste, adaptada a tu proyecto
            globalFullPathDat = fatFilePath;
            string rutaReducida = Path.ChangeExtension(fatFilePath, ".filelist");

            // Usa la carpeta base que usas en tu app
            string rutaBase = await FolderHelper.ObtenerRutaFinalAsync(window);
            string fixroot = Path.Combine(rutaBase, "WATCH_DOGS2");
            string rutaTentativa = Path.Combine(fixroot, rutaReducida);

            string rutaFinal = null;

            if (File.Exists(rutaTentativa))
            {
                rutaFinal = rutaTentativa;
            }
            else
            {
                string nombreBuscado = Path.GetFileName(rutaReducida);
                var archivos = Directory.EnumerateFiles(fixroot, "*", SearchOption.AllDirectories);

                rutaFinal = archivos.FirstOrDefault(f =>
                    Path.GetFileName(f).Equals(nombreBuscado, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(rutaFinal) && File.Exists(rutaFinal))
            {
                var rutas = File.ReadAllLines(rutaFinal);

                // Aquí construyes tu lista de FileItem con las rutas leídas (carpetas y archivos)
                var virtualItems = BuildVirtualTree(rutas);
                TreeRoot = BuildVirtualTree(rutas);

                // Convertir TreeItems (tu estructura) a ObservableCollection<FileItem>
                var itemsToShow = ConvertVirtualTreeToFileItems(virtualItems);

                currentItems.Clear();
                foreach (var fi in itemsToShow)
                {
                    currentItems.Add(fi);
                }
                FileGrid.ItemsSource = currentItems;
                ConstruirListaCompletaVirtual(); // 🔁 importante


            }
            else
            {
               // await MostrarDialogo("Error", $"No se encontró el archivo '{Path.GetFileName(rutaReducida)}' en la carpeta configurada.");
            }
        }

        private ObservableCollection<FileItem> ConvertVirtualTreeToFileItems(ObservableCollection<TreeItem> treeItems)
        {
            var list = new ObservableCollection<FileItem>();

            foreach (var ti in treeItems)
            {
                if (ti.IsDirectory)
                {
                    // Crear FileItem folder con la ruta y nombre virtual
                    list.Add(new FileItem(new VirtualStorageFolder(ti.FullPath, ti.Name)));
                }
                else
                {
                    // Crear FileItem file con ruta y nombre virtual
                    list.Add(new FileItem(new VirtualStorageFile(ti.FullPath, ti.Name)));
                }
            }

            return list;
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            await DialogHelper.ShowLoadingDialogAsync(window.Content.XamlRoot, async () =>
            {
                if (virtualNavigationStack.Count > 0)
                {
                    currentVirtualPath = virtualNavigationStack.Pop();
                    await LoadVirtualFolderContentsAsync(currentVirtualPath);
                    BackButton.IsEnabled = virtualNavigationStack.Count > 0 || folderHistory.Count > 0;
                }
                else if (folderHistory.Count > 0)
                {
                    var last = folderHistory.Pop();

                    if (last.IsReal && last.RealFolder != null)
                    {
                        currentVirtualPath = null;
                        await LoadFolderContentsAsync(last.RealFolder);
                    }
                    else if (!last.IsReal && !string.IsNullOrEmpty(last.VirtualFolderPath))
                    {
                        currentVirtualPath = last.VirtualFolderPath;
                        await LoadVirtualFolderContentsAsync(currentVirtualPath);
                    }

                    BackButton.IsEnabled = virtualNavigationStack.Count > 0 || folderHistory.Count > 0;
                }
                else
                {
                    BackButton.IsEnabled = false;
                }
            });
        }




        public ObservableCollection<TreeItem> BuildVirtualTree(IEnumerable<string> paths)
        {
            var rootItems = new ObservableCollection<TreeItem>();
            var lookup = new Dictionary<string, TreeItem>();

            foreach (var fullPath in paths)
            {
                var parts = fullPath.Split('\\', '/');
                string currentPath = "";
                ObservableCollection<TreeItem> currentLevel = rootItems;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    currentPath = Path.Combine(currentPath, part); // Construye la ruta relativa

                    if (!lookup.ContainsKey(currentPath))
                    {
                        bool isDir = i < parts.Length - 1;

                        var newItem = new TreeItem
                        {
                            Name = part,
                            FullPath = currentPath,     // 👈 Guarda la ruta completa aquí
                            IsDirectory = isDir
                        };

                        currentLevel.Add(newItem);
                        lookup[currentPath] = newItem;
                    }

                    currentLevel = lookup[currentPath].Children;
                }
            }

            return rootItems;
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FileContextMenu_Opening(object sender, object e)
        {

        }

        private async Task LoadRealFilesRecursive(StorageFolder folder)
        {
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
                allItems.Add(new FileItem(file));

            var subfolders = await folder.GetFoldersAsync();
            foreach (var sub in subfolders)
                await LoadRealFilesRecursive(sub); // recursivamente
        }
        private Task LoadVirtualFilesRecursive(string path)
        {
            allItems.Clear();

            void Traverse(TreeItem node)
            {
                if (!node.IsDirectory)
                {
                    allItems.Add(new FileItem(new VirtualStorageFile(node.FullPath, node.Name)));
                }

                foreach (var child in node.Children)
                {
                    if (child.IsDirectory)
                        allItems.Add(new FileItem(new VirtualStorageFolder(child.FullPath, child.Name)));

                    Traverse(child);
                }
            }

            var rootNode = FindNodeByPath(TreeRoot, path);
            if (rootNode != null)
            {
                Traverse(rootNode);
            }

            return Task.CompletedTask; // solo esto
        }


        void AgregarVirtualItemsDesdeArbol(TreeItem nodo)
        {
            if (nodo.IsDirectory)
                allVirtualItems.Add(new FileItem(new VirtualStorageFolder(nodo.FullPath, nodo.Name)));
            else
                allVirtualItems.Add(new FileItem(new VirtualStorageFile(nodo.FullPath, nodo.Name)));

            foreach (var hijo in nodo.Children)
                AgregarVirtualItemsDesdeArbol(hijo);
        }
        void ConstruirListaCompletaVirtual()
        {
            allVirtualItems.Clear();

            if (TreeRoot == null) return;

            foreach (var root in TreeRoot)
                AgregarVirtualItemsDesdeArbol(root);
        }



        // Lista con todos los archivos y carpetas virtuales del FAT (no se borra nunca)
        private List<FileItem> allVirtualItems = new List<FileItem>();

        // Lista de lo que hay en la carpeta virtual actual (cambia al navegar)
        private List<FileItem> currentVirtualItems = new List<FileItem>();
        private string? wemRoot;
        private string? xbgRoot;

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";

            DisplayedItems.Clear();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                // Sin filtro, mostrar solo los elementos de la carpeta actual
                foreach (var item in currentVirtualItems)
                    DisplayedItems.Add(item);
            }
            else
            {
                foreach (var item in allVirtualItems)
                {
                    string nombre = item.Name.ToLowerInvariant();

                    if (nombre.Contains(filtro) ||
                        nombre.StartsWith(filtro) ||
                        nombre.EndsWith(filtro))
                    {
                        DisplayedItems.Add(item);
                    }
                }
            }
        }

        private void PlayGame(object sender, RoutedEventArgs e)
        {

        }

        private async void ExtractDDS(object sender, RoutedEventArgs e)
        {
            object? dds = null;
            await DialogHelper.ShowLoadingDialogAsync(window.Content.XamlRoot, async () =>
            {
                globalDdsItem = rightClickedItem?.Path;
                dds = await DdsPreviewHelper.GenDDS(globalTools, globalFullPathDat, globalDdsItem);
            });
            if (dds != null)
            {
                await ShowMessageAsync("DDS Extraction", $"DDS file extracted successfully:\n{dds}");
            }
            else
            {
                await ShowMessageAsync("Error", "Failed to extract the DDS file.");
            }
        }
    }
}