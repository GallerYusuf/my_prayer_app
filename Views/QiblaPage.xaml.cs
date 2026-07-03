using PrayerApp.ViewModels;

namespace PrayerApp.Views;

public partial class QiblaPage : ContentPage
{
    public QiblaPage(QiblaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is QiblaViewModel vm)
            vm.StartCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is QiblaViewModel vm)
            vm.StopCommand.Execute(null);
    }
}
