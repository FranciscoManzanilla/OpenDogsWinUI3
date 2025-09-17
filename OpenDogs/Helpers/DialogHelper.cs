using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDogs.Helpers
{
    public class DialogHelper
    {
        public static async Task ShowLoadingDialogAsync(XamlRoot xamlRoot, Func<Task> taskToRun)
        {
            var dialog = new ContentDialog
            {
                Title = "Process file system...",
                XamlRoot = xamlRoot,
                DefaultButton = ContentDialogButton.None,
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 10,
                    Children =
                {
                    new ProgressRing { Width = 60, Height = 60, IsActive = true },
                    new TextBlock { Text = "Please wait to exploit the dogs....", HorizontalAlignment = HorizontalAlignment.Center }
                }
                }
            };

            var showTask = dialog.ShowAsync();

            await taskToRun();

            dialog.Hide();
        }

    }
}
