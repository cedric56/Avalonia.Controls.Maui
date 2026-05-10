namespace ControlGallery.Pages;

public partial class FlashLightPage : ContentPage
{
    private bool _isActived;
	public FlashLightPage()
	{
		InitializeComponent();		
    }

    protected override async void OnAppearing()
    {
        var isSupported = await Flashlight.Default.IsSupportedAsync();
        IsSupportedLabel.Text = isSupported ? "Flashlight is supported on this device." : "Flashlight is not supported on this device.";
        ToggleButton.IsEnabled = isSupported;
    }

    private async void OnToggleFlashlightClicked(object sender, EventArgs e)
	{
        if (_isActived)
        {
            await Flashlight.Default.TurnOffAsync();
            _isActived = false;
        }
        else
        {
            await Flashlight.Default.TurnOnAsync();
            _isActived= true;
        }
    }
}