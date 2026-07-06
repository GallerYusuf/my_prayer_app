using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class EsmaulHusnaPage : ContentPage
{
    public EsmaulHusnaPage(EsmaulHusnaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
