using System.Collections.ObjectModel;

namespace ControlGallery.Pages;

public partial class AppActionsPage : ContentPage
{
    private ObservableCollection<AppAction> _actions = new();

    public AppActionsPage()
    {
        InitializeComponent();

        if (!AppActions.Current.IsSupported)
        {
            AddActionbutton.IsEnabled = false;
            RemoveSelectedButton.IsEnabled = false;
        }

        ActionsList.ItemsSource = _actions;
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ActionTitleEntry.Text) ||
            string.IsNullOrWhiteSpace(ActionIdEntry.Text))
            return;

        _actions.Add(new AppAction(ActionIdEntry.Text, ActionTitleEntry.Text));

        try
        {
            await AppActions.Current.SetAsync(_actions);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }

        ActionTitleEntry.Text = string.Empty;
        ActionIdEntry.Text = string.Empty;
    }

    private async void OnRemoveClicked(object sender, EventArgs e)
    {
        if (ActionsList.SelectedItem is AppAction selected)
        {
            _actions.Remove(selected);

            try
            {
                await AppActions.Current.SetAsync(_actions);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    private void OnAppActionActivated(object? sender, AppActionEventArgs e)
    {
        Dispatcher.Dispatch(async () =>
        {
            LastActionLabel.Text = $"Last triggered: {e.AppAction.Title} ({e.AppAction.Id})";

            await Task.Delay(5000);

            LastActionLabel.Text = "Last triggered: None";
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        AppActions.Current.AppActionActivated += OnAppActionActivated;        
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Avoid memory leaks
        AppActions.Current.AppActionActivated -= OnAppActionActivated;
    }
}