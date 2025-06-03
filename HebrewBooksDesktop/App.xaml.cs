using System.Windows;

namespace HebrewBooksDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            WpfLib.Helpers.UpdateHelper.Update("KleiKodesh", "HebrewBooksLib", "v0.4");
        }
    }
}
