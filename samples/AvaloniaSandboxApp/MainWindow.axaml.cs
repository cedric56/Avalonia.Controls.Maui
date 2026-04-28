using Avalonia.Controls;
using Microsoft.Maui.Media;
using System.Collections.ObjectModel;
using ListBox = Avalonia.Controls.ListBox;
using SelectionChangedEventArgs = Avalonia.Controls.SelectionChangedEventArgs;
using TextBox = Avalonia.Controls.TextBox;
using TextChangedEventArgs = Avalonia.Controls.TextChangedEventArgs;

namespace AvaloniaSandboxApp;

public partial class MainWindow : Window
{
    private List<SampleGroup> _allSamples = new List<SampleGroup>();

    private static readonly Dictionary<Type, Func<ContentPage>> PageFactory = new()
    {
        [typeof(HomePage)] = () => new HomePage(),
        // Essentials
        [typeof(PreferencesPage)] = () => new PreferencesPage(),
        [typeof(FilePickerPage)] = () => new FilePickerPage(),
        [typeof(SensorsPage)] = () => new SensorsPage(),

        // Settings
        [typeof(SettingsPage)] = () => new SettingsPage(),
    };

    public ObservableCollection<SampleGroup> FilteredSamples { get; private set; } = new ObservableCollection<SampleGroup>();


    public MainWindow()
    {
        InitializeComponent();
        InitializeSamples();
        UpdateMenu(string.Empty);
        NavigateToPage(typeof(HomePage));

        DataContext = this;
    }

    private void UpdateMenu(string searchText)
    {
        FilteredSamples.Clear();

        foreach (var group in _allSamples)
        {
            var filteredItems = group.Where(item =>
                string.IsNullOrWhiteSpace(searchText) ||
                item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                item.Detail.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filteredItems.Count > 0)
            {
                FilteredSamples.Add(new SampleGroup(group.Name, filteredItems));
            }
        }
    }


    private void InitializeSamples()
    {
        _allSamples = new List<SampleGroup>
        {
            new SampleGroup("Main", new List<SampleItem>
            {
                new("Home", "Home Page", typeof(HomePage))
            }),
            new SampleGroup("Essentials", new List<SampleItem>
            {
                new("File Picker", "Pick files from the device", typeof(FilePickerPage)),
                new("Preferences", "Key/value storage for app settings", typeof(PreferencesPage)),
                new("Sensors", "Access device sensors", typeof(SensorsPage)),
                
                
                
                //new("Screenshot", "Capture window screenshots", typeof(ScreenshotPage)),
            }),
            new SampleGroup("Settings", new List<SampleItem>
            {
                new("Theme", "Theme toggle and AppThemeBinding", typeof(SettingsPage))
            })
        };
    }

    public async void Button_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await MediaPicker.CaptureVideoAsync();
    }

    private async void NavigateToPage(Type pageType)
    {
        if (PageFactory.TryGetValue(pageType, out var factory))
        {
            //IsPresented = false;
            await Task.Yield();

            var page = factory();

            drawer.Content = page;

            //if (Detail is NavigationPage navPage)
            //{
            //    navPage.Navigation.InsertPageBefore(page, navPage.RootPage);
            //    await navPage.Navigation.PopToRootAsync(animated: false);
            //}
            //else
            //{
            //    Detail = new NavigationPage(page);
            //}
        }
    }

    private void MenuCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.AddedItems.OfType<SampleItem>().FirstOrDefault();

        if (selectedItem is not null)
        {
            NavigateToPage(selectedItem.PageType);

            // Clear selection so the same item can be tapped again
            if (sender is ListBox cv)
                cv.SelectedItem = null;
        }
    }

    private void OnSearchBarTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdateMenu(textBox.Text ?? string.Empty);
        }
    }
}

public class SampleItem
{
    public string Title { get; }
    public string Detail { get; }
    public Type PageType { get; }

    public SampleItem(string title, string detail, Type pageType)
    {
        Title = title;
        Detail = detail;
        PageType = pageType;
    }
}

public class SampleGroup : List<SampleItem>
{
    public string Name { get; }

    public List<SampleItem> Items => this;

    public SampleGroup(string name, List<SampleItem> items) : base(items)
    {
        Name = name;
    }
}

