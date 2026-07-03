using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class SurahPage : ContentPage
{
    private readonly SurahViewModel _viewModel;

    public SurahPage(SurahViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();

        if (_viewModel.ScrollToVerse > 0 && _viewModel.Verses.Count >= _viewModel.ScrollToVerse)
            VersesView.ScrollTo(_viewModel.ScrollToVerse - 1, position: ScrollToPosition.Start, animate: false);
    }
}
