using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class DhikrPage : ContentPage
{
    private readonly DhikrViewModel _viewModel;

    public DhikrPage(DhikrViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
