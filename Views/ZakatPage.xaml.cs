using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class ZakatPage : ContentPage
{
    public ZakatPage(ZakatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
