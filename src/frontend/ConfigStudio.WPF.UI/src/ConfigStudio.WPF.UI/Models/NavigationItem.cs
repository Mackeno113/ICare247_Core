// File    : NavigationItem.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Mo ta item dieu huong cho sidebar tree.

using System.Collections.ObjectModel;
using MaterialDesignThemes.Wpf;
using Prism.Mvvm;
using Prism.Navigation;

namespace ConfigStudio.WPF.UI.Models;

public sealed class NavigationItem : BindableBase
{
    private bool _isExpanded;
    private bool _isSelected;

    public string Title { get; init; } = string.Empty;

    public PackIconKind Icon { get; init; }

    public string? NavigateTo { get; init; }

    public NavigationParameters? Parameters { get; init; }

    public ObservableCollection<NavigationItem> Children { get; init; } = [];

    public int Level { get; init; }

    public bool IsDivider { get; init; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
