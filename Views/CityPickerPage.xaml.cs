using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class CityPickerPage : ContentPage
{
    public CityPickerPage(CityPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CityPickerViewModel vm)
            vm.SetNavigation(Navigation);
    }

    private async void OnItemSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BindingContext is not CityPickerViewModel vm)
            return;

        if (e.CurrentSelection.FirstOrDefault() is not PickerItem item)
            return;

        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        await vm.HandleItemAsync(item);
    }
}
