using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class ExplorePage : ContentPage
{
    private readonly ExploreViewModel _viewModel;

    public ExplorePage(ExploreViewModel viewModel)
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
