// File    : INavigationHistoryService.cs
// Module  : Core
// Layer   : Shared
// Purpose : Service quan ly history navigation + breadcrumb chain.

using System.Collections.Generic;

namespace ConfigStudio.WPF.UI.Core.Services;

/// <summary>
/// Quan ly lich su navigation cho shell + cung cap breadcrumb data cho UI.
/// VM goi RegisterNavigation tu OnNavigatedTo de update current entry.
/// Shell goi GoBack/GoForward tu nut tren breadcrumb bar.
/// </summary>
public interface INavigationHistoryService
{
    /// <summary>Breadcrumb chain hien tai (path tu root den view dang xem).</summary>
    IReadOnlyList<NavigationCrumb> Crumbs { get; }

    /// <summary>True khi co the GoBack (history > 1 entry).</summary>
    bool CanGoBack { get; }

    /// <summary>True khi co the GoForward (sau khi GoBack, chua re-navigate).</summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Dang ky 1 buoc navigation vao history. Goi tu OnNavigatedTo cua VM dich.
    /// Truyen isHierarchical=true khi entry la "con" cua entry truoc (vd FormEditor -> FieldConfig).
    /// Truyen isHierarchical=false khi la "root" moi (vd click sidebar -> FormManager).
    /// </summary>
    void RegisterNavigation(NavigationCrumb crumb, bool isHierarchical);

    /// <summary>Lui 1 buoc trong history. Khong lam gi khi !CanGoBack.</summary>
    void GoBack();

    /// <summary>Tien 1 buoc trong forward stack. Khong lam gi khi !CanGoForward.</summary>
    void GoForward();

    /// <summary>Jump den 1 crumb cu the (click vao breadcrumb). Bo qua khi entry khong ton tai.</summary>
    void JumpToCrumb(NavigationCrumb target);

    /// <summary>Fire moi khi Crumbs / CanGoBack / CanGoForward thay doi.</summary>
    event System.EventHandler? Changed;
}
