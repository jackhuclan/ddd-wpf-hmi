using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Commands;
using Hmi.Application.Messages;
using Hmi.Domain.Recipes;
using MediatR;

namespace Hmi.ViewModels;

/// <summary>配方管理页：配方列表展示、手动切换下发与配置刷新</summary>
public sealed partial class RecipeViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IRecipeStore _recipes;
    private readonly IMessenger _messenger;

    public override string PageTitle => "配方管理";

    public ObservableCollection<Recipe> Recipes { get; } = [];

    [ObservableProperty] private Recipe? _selectedRecipe;

    public string FooterText =>
        $"共 {Recipes.Count} 个配方（配置驱动，改 appsettings.Recipes 后刷新生效）";

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand SwitchCommand { get; }

    public RecipeViewModel(IMediator mediator, IRecipeStore recipes, IMessenger messenger)
    {
        _mediator = mediator;
        _recipes = recipes;
        _messenger = messenger;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SwitchCommand = new AsyncRelayCommand(SwitchAsync, () => SelectedRecipe is not null);

        messenger.Register<RecipeListChangedMessage, string>(this, HmiMessages.Token, (_, _) => RefreshList());
        RefreshList();
    }

    partial void OnSelectedRecipeChanged(Recipe? value) => SwitchCommand.NotifyCanExecuteChanged();

    private async Task RefreshAsync()
    {
        var tip = await _mediator.Send(new ReloadConfigCommand());
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        RefreshList();
    }

    private async Task SwitchAsync()
    {
        if (SelectedRecipe is null)
        {
            return;
        }
        var tip = await _mediator.Send(new SwitchRecipeCommand(SelectedRecipe.Code));
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        RefreshList();
    }

    private void RefreshList()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(RefreshList);
            return;
        }

        var selectedCode = SelectedRecipe?.Code;
        Recipes.Clear();
        foreach (var recipe in _recipes.GetAll()) Recipes.Add(recipe);
        SelectedRecipe = Recipes.FirstOrDefault(r => r.Code == selectedCode);
        OnPropertyChanged(nameof(FooterText));
        SwitchCommand.NotifyCanExecuteChanged();
    }
}
