using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class DuasPage : ContentPage
{
    private readonly DuasViewModel _viewModel;

    public DuasPage(DuasViewModel viewModel)
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
