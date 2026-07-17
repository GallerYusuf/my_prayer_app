using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class QuranPage : ContentPage
{
    private readonly QuranViewModel _viewModel;

    public QuranPage(QuranViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // One list is visible at a time: search results, favorites, topics, topic verses, juz list or surahs.
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(QuranViewModel.IsSearchMode)
                or nameof(QuranViewModel.ShowFavorites)
                or nameof(QuranViewModel.ShowJuzList)
                or nameof(QuranViewModel.ShowTopics)
                or nameof(QuranViewModel.ShowTopicVerses))
                UpdateResultsView();
        };
    }

    private void UpdateResultsView()
    {
        if (_viewModel.IsSearchMode)
        {
            ResultsView.ItemsSource = _viewModel.SearchResults;
            ResultsView.IsVisible = true;
            TopicsView.IsVisible = false;
            TopicVersesView.IsVisible = false;
        }
        else if (_viewModel.ShowFavorites)
        {
            ResultsView.ItemsSource = _viewModel.Favorites;
            ResultsView.IsVisible = true;
            TopicsView.IsVisible = false;
            TopicVersesView.IsVisible = false;
        }
        else if (_viewModel.ShowTopicVerses)
        {
            ResultsView.IsVisible = false;
            TopicsView.IsVisible = false;
            TopicVersesView.IsVisible = true;
        }
        else if (_viewModel.ShowTopics)
        {
            ResultsView.IsVisible = false;
            TopicsView.IsVisible = true;
            TopicVersesView.IsVisible = false;
        }
        else
        {
            ResultsView.IsVisible = false;
            TopicsView.IsVisible = false;
            TopicVersesView.IsVisible = false;
        }

        JuzView.IsVisible = !ResultsView.IsVisible && !TopicsView.IsVisible && !TopicVersesView.IsVisible && _viewModel.ShowJuzList;
        SurahsView.IsVisible = !ResultsView.IsVisible && !TopicsView.IsVisible && !TopicVersesView.IsVisible && !JuzView.IsVisible;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
