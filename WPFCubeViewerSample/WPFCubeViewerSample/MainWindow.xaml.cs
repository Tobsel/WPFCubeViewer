using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFCubeViewerSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Setting up the ViewModel
            DataContext = ViewModel =  new MainWindowViewModel();

            // Easy Sample Code
            ViewModel.Code = @"// Add your C# code here returning the color number for each voxel
// You have access to x,y,z,t and extent variables

return 7;";

            Brush1.Background = Viewer.Palette(1);
            Brush2.Background = Viewer.Palette(2);
            Brush3.Background = Viewer.Palette(3);
            Brush4.Background = Viewer.Palette(4);
            Brush5.Background = Viewer.Palette(5);
            Brush6.Background = Viewer.Palette(6);
            Brush7.Background = Viewer.Palette(7);
            Brush8.Background = Viewer.Palette(8);
            Brush9.Background = Viewer.Palette(9);
            Brush10.Background = Viewer.Palette(10);
            Brush11.Background = Viewer.Palette(11);
            Brush12.Background = Viewer.Palette(12);
            Brush13.Background = Viewer.Palette(13);
            Brush14.Background = Viewer.Palette(14);
            Brush15.Background = Viewer.Palette(15);
            Brush16.Background = Viewer.Palette(16);
        }
    }
}