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
        _viewModel.SurahNavigated += OnSurahNavigated;
        await _viewModel.OnAppearingAsync();

        if (_viewModel.ScrollToVerse > 1 && _viewModel.Verses.Count >= _viewModel.ScrollToVerse)
        {
            await Task.Delay(60);
            VersesView.ScrollTo(_viewModel.ScrollToVerse - 1, position: ScrollToPosition.Start, animate: false);
        }
    }

    protected override void OnDisappearing()
    {
        _viewModel.SurahNavigated -= OnSurahNavigated;
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private void OnSurahNavigated(object? sender, EventArgs e) =>
        VersesView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);

    private void OnVersesScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        var index = e.FirstVisibleItemIndex >= 0 ? e.FirstVisibleItemIndex : e.CenterItemIndex;
        _viewModel.OnVisibleVerseChanged(index);
    }
}
