using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class WorshipPage : ContentPage
{
    private readonly WorshipViewModel _viewModel;

    public WorshipPage(WorshipViewModel viewModel)
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
