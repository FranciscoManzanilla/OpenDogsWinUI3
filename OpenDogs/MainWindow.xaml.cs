using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OpenDogs.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenDogs
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.SetTitleBar(AppTitleBar);
        }

        private void nvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag?.ToString();

                switch (tag)
                {
                    case "WatchEx":
                        // En tu MainWindow.xaml.cs
                        contentFrame.Navigate(typeof(WExplorer), this); // pasar la MainWindow como parámetro
                        break;
                    case "WatchText":
                        //contentFrame.Navigate(typeof(SamplePage2));
                        break;
                    case "WatchMod":
                        //contentFrame.Navigate(typeof(SamplePage3));
                        break;
                    case "WatchManager":
                        //contentFrame.Navigate(typeof(SamplePage4));
                        break;
                }
            }
        }
    }
}
