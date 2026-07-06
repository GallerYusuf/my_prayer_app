using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class KazaPage : ContentPage
{
    public KazaPage(KazaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
