// File    : NavigationHistoryService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Impl breadcrumb + back/forward history cho shell.

using System;
using System.Collections.Generic;
using System.Linq;
using ConfigStudio.WPF.UI.Core.Constants;
using Prism.Navigation.Regions;

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Quan ly 2 stack — <c>_back</c> (lich su da xem) va <c>_forward</c> (sau khi GoBack).
/// Crumbs duoc xay tu <c>_back + current</c> theo thu tu cu -> moi.
/// </summary>
public sealed class NavigationHistoryService : INavigationHistoryService
{
    private readonly IRegionManager _regionManager;
    private readonly Stack<NavigationCrumb> _back    = new();
    private readonly Stack<NavigationCrumb> _forward = new();
    private NavigationCrumb? _current;
    private bool _isNavigatingProgrammatically;

    public NavigationHistoryService(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public IReadOnlyList<NavigationCrumb> Crumbs
    {
        get
        {
            var list = new List<NavigationCrumb>(_back.Count + 1);
            // _back la stack — peek dau la moi nhat. Reverse de duyet theo thu tu cu -> moi.
            foreach (var c in _back.Reverse()) list.Add(c);
            if (_current is not null) list.Add(_current);
            return list;
        }
    }

    public bool CanGoBack    => _back.Count > 0;
    public bool CanGoForward => _forward.Count > 0;

    public event EventHandler? Changed;

    public void RegisterNavigation(NavigationCrumb crumb, bool isHierarchical)
    {
        if (_isNavigatingProgrammatically)
        {
            // GoBack/GoForward/Jump da tu set _current — chi raise event.
            RaiseChanged();
            return;
        }

        if (_current is not null)
        {
            // Neu entry hien tai trung viewName voi crumb moi va khong phai hierarchical
            // -> coi nhu reload, khong push len back.
            var sameView = string.Equals(_current.ViewName, crumb.ViewName, StringComparison.Ordinal);
            if (!sameView)
            {
                if (isHierarchical) _back.Push(_current);
                else { _back.Clear(); _back.Push(_current); }
            }
        }

        if (!isHierarchical)
        {
            // Root navigation moi -> clear ca back va forward de tao chain moi.
            _back.Clear();
        }

        _current = crumb;
        _forward.Clear();
        RaiseChanged();
    }

    public void GoBack()
    {
        if (!CanGoBack || _current is null) return;
        var target = _back.Pop();
        _forward.Push(_current);
        NavigateTo(target);
    }

    public void GoForward()
    {
        if (!CanGoForward || _current is null) return;
        var target = _forward.Pop();
        _back.Push(_current);
        NavigateTo(target);
    }

    public void JumpToCrumb(NavigationCrumb target)
    {
        if (_current is null) return;
        // Tim target trong _back. Pop tat ca crumb sau target sang forward.
        var stack = new List<NavigationCrumb>(_back);
        var idx = stack.FindIndex(c => ReferenceEquals(c, target));
        if (idx < 0) return;

        // Pop cac entry tu top stack toi idx (exclude) ve forward, theo thu tu nguoc.
        _forward.Push(_current);
        for (var i = 0; i < idx; i++) _forward.Push(_back.Pop());
        var popped = _back.Pop(); // chinh la target
        NavigateTo(popped);
    }

    private void NavigateTo(NavigationCrumb crumb)
    {
        _isNavigatingProgrammatically = true;
        _current = crumb;
        try
        {
            if (crumb.Parameters is Prism.Navigation.NavigationParameters np)
                _regionManager.RequestNavigate(RegionNames.Content, crumb.ViewName, np);
            else if (crumb.Parameters is not null)
            {
                // Copy sang NavigationParameters concrete cho overload cua RequestNavigate.
                var copy = new Prism.Navigation.NavigationParameters();
                foreach (var kv in crumb.Parameters) copy.Add(kv.Key, kv.Value);
                _regionManager.RequestNavigate(RegionNames.Content, crumb.ViewName, copy);
            }
            else
                _regionManager.RequestNavigate(RegionNames.Content, crumb.ViewName);
        }
        finally
        {
            _isNavigatingProgrammatically = false;
        }
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
