using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class ReligiousDaysPage : ContentPage
{
    private readonly ReligiousDaysViewModel _viewModel;

    public ReligiousDaysPage(ReligiousDaysViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
