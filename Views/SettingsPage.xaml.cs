using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class SettingsPage : ContentPage
{
    private bool _settingsLoaded;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load saved settings only on the first appearance. Reloading on every
        // OnAppearing would overwrite an unsaved city selection when returning
        // from the (modal) city picker.
        if (_settingsLoaded)
            return;

        if (BindingContext is SettingsViewModel vm)
        {
            vm.LoadSettingsCommand.Execute(null);
            _settingsLoaded = true;
        }
    }
}
