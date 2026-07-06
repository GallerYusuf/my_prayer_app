using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class QuranPage : ContentPage
{
    private readonly QuranViewModel _viewModel;

    public QuranPage(QuranViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // One list is visible at a time: search results, favorites, juz list or surahs.
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(QuranViewModel.IsSearchMode)
                or nameof(QuranViewModel.ShowFavorites)
                or nameof(QuranViewModel.ShowJuzList))
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

        JuzView.IsVisible = !ResultsView.IsVisible && _viewModel.ShowJuzList;
        SurahsView.IsVisible = !ResultsView.IsVisible && !JuzView.IsVisible;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
