// File    : IThemeService.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Interface doi theme runtime cho shell.

namespace ConfigStudio.WPF.UI.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void ApplyTheme(AppTheme theme);
}
