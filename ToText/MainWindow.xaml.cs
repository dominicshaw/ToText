using ToText.ViewModels;

namespace ToText
{
    public partial class MainWindow
    {

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
