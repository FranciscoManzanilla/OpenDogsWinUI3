using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace OpenDogs.Helpers
{
    public static class FolderHelper
    {
        public static async Task<string> PedirCarpetaAsync(Window window)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                FileTypeFilter = { "*" },
                CommitButtonText = "Select Folder",

            };

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }

        public static async Task<string> ObtenerRutaFinalAsync(Window window)
        {
            string rutaGuardada = ConfigHelper.CargarRutaConfig();

            if (!string.IsNullOrEmpty(rutaGuardada) && Directory.Exists(rutaGuardada))
                return rutaGuardada;

            string nuevaRuta = await PedirCarpetaAsync(window);

            if (!string.IsNullOrEmpty(nuevaRuta))
            {
                ConfigHelper.GuardarRutaConfig(nuevaRuta);
                return nuevaRuta;
            }

            return null;
        }
    }

}
