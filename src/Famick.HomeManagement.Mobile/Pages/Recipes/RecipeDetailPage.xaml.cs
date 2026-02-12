using Famick.HomeManagement.Mobile.Models;
using Famick.HomeManagement.Mobile.Services;

namespace Famick.HomeManagement.Mobile.Pages.Recipes;

[QueryProperty(nameof(RecipeId), "RecipeId")]
public partial class RecipeDetailPage : ContentPage
{
    private readonly ShoppingApiClient _apiClient;
    private RecipeDetail? _recipe;

    public string RecipeId { get; set; } = string.Empty;

    public RecipeDetailPage(ShoppingApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRecipeAsync().ConfigureAwait(false);
    }

    private async Task LoadRecipeAsync()
    {
        if (!Guid.TryParse(RecipeId, out var id))
        {
            MainThread.BeginInvokeOnMainThread(() => ShowError("Invalid recipe ID"));
            return;
        }

        MainThread.BeginInvokeOnMainThread(ShowLoading);

        try
        {
            var result = await _apiClient.GetRecipeAsync(id);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (result.Success && result.Data != null)
                {
                    _recipe = result.Data;
                    RenderRecipe();
                    ShowContentView();
                }
                else
                {
                    ShowError(result.ErrorMessage ?? "Failed to load recipe");
                }
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => ShowError($"Connection error: {ex.Message}"));
        }
    }

    private void RenderRecipe()
    {
        if (_recipe == null) return;

        TitleLabel.Text = _recipe.Name;
        RecipeNameLabel.Text = _recipe.Name;
        ServingsLabel.Text = _recipe.Servings.ToString();
        StepCountLabel.Text = _recipe.Steps.Count.ToString();

        // Primary image
        if (!string.IsNullOrEmpty(_recipe.PrimaryImageUrl))
        {
            PrimaryImage.Source = _recipe.PrimaryImageUrl;
            ImageContainer.IsVisible = true;
        }
        else
        {
            ImageContainer.IsVisible = false;
        }

        // Source
        if (!string.IsNullOrEmpty(_recipe.Source))
        {
            SourceLabel.Text = _recipe.Source;
            SourceContainer.IsVisible = true;
        }

        // Attribution
        if (!string.IsNullOrEmpty(_recipe.Attribution))
        {
            AttributionLabel.Text = _recipe.Attribution;
            AttributionContainer.IsVisible = true;
        }

        // Steps
        RenderSteps();

        // Nested recipes
        if (_recipe.NestedRecipes.Count > 0)
        {
            RenderNestedRecipes();
            NestedSection.IsVisible = true;
            NestedDivider.IsVisible = true;
        }

        // Notes
        if (!string.IsNullOrEmpty(_recipe.Notes))
        {
            NotesLabel.Text = _recipe.Notes;
            NotesSection.IsVisible = true;
            NotesDivider.IsVisible = true;
        }
    }

    private void RenderSteps()
    {
        StepsList.Children.Clear();

        if (_recipe == null || _recipe.Steps.Count == 0)
        {
            NoStepsLabel.IsVisible = true;
            return;
        }

        NoStepsLabel.IsVisible = false;
        var orderedSteps = _recipe.Steps.OrderBy(s => s.StepOrder).ToList();

        for (int i = 0; i < orderedSteps.Count; i++)
        {
            var step = orderedSteps[i];
            var stepCard = CreateStepCard(step, i + 1);
            StepsList.Children.Add(stepCard);
        }
    }

    private View CreateStepCard(RecipeStep step, int stepNumber)
    {
        var card = new Border
        {
            Padding = new Thickness(12),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Stroke = Colors.Transparent,
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#2A2A2A") : Color.FromArgb("#F5F5F5")
        };

        var layout = new VerticalStackLayout { Spacing = 6 };

        // Step header
        var headerText = !string.IsNullOrEmpty(step.Title)
            ? $"Step {stepNumber}: {step.Title}"
            : $"Step {stepNumber}";
        layout.Children.Add(new Label
        {
            Text = headerText,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold
        });

        // Step image
        if (!string.IsNullOrEmpty(step.DisplayImageUrl))
        {
            var imgBorder = new Border
            {
                HeightRequest = 150,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Stroke = Colors.Transparent,
                Content = new Image
                {
                    Source = step.DisplayImageUrl,
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 150
                }
            };
            layout.Children.Add(imgBorder);
        }

        // Instructions
        if (!string.IsNullOrEmpty(step.Instructions))
        {
            layout.Children.Add(new Label
            {
                Text = step.Instructions,
                FontSize = 14,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#BDBDBD") : Color.FromArgb("#424242")
            });
        }

        // Ingredients
        if (step.Ingredients.Count > 0)
        {
            layout.Children.Add(new Label
            {
                Text = "Ingredients:",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 4, 0, 0),
                TextColor = Colors.Gray
            });

            foreach (var ingredient in step.Ingredients.OrderBy(ing => ing.SortOrder))
            {
                layout.Children.Add(new Label
                {
                    Text = $"  \u2022 {ingredient.DisplayText}",
                    FontSize = 13,
                    TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#BDBDBD") : Color.FromArgb("#555555")
                });
            }
        }

        // Video URL
        if (!string.IsNullOrEmpty(step.VideoUrl))
        {
            var videoLabel = new Label
            {
                Text = "Watch Video",
                FontSize = 13,
                TextColor = Color.FromArgb("#1976D2"),
                TextDecorations = TextDecorations.Underline
            };
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (_, _) =>
            {
                try { await Launcher.OpenAsync(new Uri(step.VideoUrl)); }
                catch { /* ignore invalid URLs */ }
            };
            videoLabel.GestureRecognizers.Add(tapGesture);
            layout.Children.Add(videoLabel);
        }

        card.Content = layout;
        return card;
    }

    private void RenderNestedRecipes()
    {
        NestedList.Children.Clear();
        if (_recipe == null) return;

        foreach (var nested in _recipe.NestedRecipes)
        {
            var tapGesture = new TapGestureRecognizer();
            var nestedId = nested.RecipeId;
            tapGesture.Tapped += async (_, _) =>
            {
                await Shell.Current.GoToAsync(nameof(RecipeDetailPage), new Dictionary<string, object>
                {
                    { "RecipeId", nestedId.ToString() }
                });
            };

            var card = new Border
            {
                Padding = new Thickness(12, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Stroke = Colors.Transparent,
                BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#2A2A2A") : Color.FromArgb("#F5F5F5"),
                Content = CreateNestedRecipeGrid(nested.RecipeName)
            };
            card.GestureRecognizers.Add(tapGesture);
            NestedList.Children.Add(card);
        }
    }

    private static Grid CreateNestedRecipeGrid(string recipeName)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var nameLabel = new Label
        {
            Text = recipeName,
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center
        };
        grid.Children.Add(nameLabel);

        var arrowLabel = new Label
        {
            Text = "\u203A",
            FontSize = 20,
            TextColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(arrowLabel, 1);
        grid.Children.Add(arrowLabel);

        return grid;
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (_recipe == null) return;

        await Shell.Current.GoToAsync(nameof(RecipeEditPage), new Dictionary<string, object>
        {
            { "RecipeId", _recipe.Id.ToString() }
        });
    }

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        if (_recipe == null) return;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = _recipe.Name,
            Text = $"Check out this recipe: {_recipe.Name}",
            Uri = _recipe.Source
        });
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadRecipeAsync();
    }

    private void ShowLoading()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ContentScroll.IsVisible = false;
        ErrorFrame.IsVisible = false;
    }

    private void ShowContentView()
    {
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        ContentScroll.IsVisible = true;
        ErrorFrame.IsVisible = false;
    }

    private void ShowError(string message)
    {
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        ContentScroll.IsVisible = false;
        ErrorFrame.IsVisible = true;
        ErrorLabel.Text = message;
    }
}
