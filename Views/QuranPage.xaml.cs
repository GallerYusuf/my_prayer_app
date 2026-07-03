using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class QuranPage : ContentPage
{
    private readonly QuranViewModel _viewModel;

    public QuranPage(QuranViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // ResultsView shows either search results or favorites; swap its source in one place.
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(QuranViewModel.IsSearchMode) or nameof(QuranViewModel.ShowFavorites))
                UpdateResultsView();
        };
    }

    private void UpdateResultsView()
    {
        if (_viewModel.IsSearchMode)
        {
            ResultsView.ItemsSource = _viewModel.SearchResults;
            ResultsView.IsVisible = true;
        }
        else if (_viewModel.ShowFavorites)
        {
            ResultsView.ItemsSource = _viewModel.Favorites;
            ResultsView.IsVisible = true;
        }
        else
        {
            ResultsView.IsVisible = false;
        }

        SurahsView.IsVisible = !ResultsView.IsVisible;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
