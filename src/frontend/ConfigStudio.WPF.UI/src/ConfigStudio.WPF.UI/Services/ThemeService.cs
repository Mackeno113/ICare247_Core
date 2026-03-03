// File    : ThemeService.cs
// Module  : Shell
// Layer   : Presentation
// Purpose : Ap dung va chuyen doi theme bang cach thay ResourceDictionary.

using System.Windows;
using System.Linq;

namespace ConfigStudio.WPF.UI.Services;

public sealed class ThemeService : IThemeService
{
    private const string ThemePathLightOcean = "Themes/Shell.xaml";
    private const string ThemePathSlateProfessional = "Themes/Shell.SlateProfessional.xaml";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.LightOcean;

    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            CurrentTheme = theme;
            return;
        }

        var targetSource = new Uri(GetThemePath(theme), UriKind.Relative);
        var dictionaries = app.Resources.MergedDictionaries;

        var existingThemeDictionary = dictionaries.FirstOrDefault(static d =>
            d.Source is not null &&
            (string.Equals(d.Source.OriginalString, ThemePathLightOcean, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(d.Source.OriginalString, ThemePathSlateProfessional, StringComparison.OrdinalIgnoreCase)));

        if (existingThemeDictionary is not null)
        {
            var existingPath = existingThemeDictionary.Source?.OriginalString;
            if (string.Equals(existingPath, targetSource.OriginalString, StringComparison.OrdinalIgnoreCase))
            {
                CurrentTheme = theme;
                return;
            }

            var index = dictionaries.IndexOf(existingThemeDictionary);
            dictionaries.RemoveAt(index);
            dictionaries.Insert(index, new ResourceDictionary { Source = targetSource });
            CurrentTheme = theme;
            return;
        }

        dictionaries.Insert(1, new ResourceDictionary { Source = targetSource });
        CurrentTheme = theme;
    }

    private static string GetThemePath(AppTheme theme)
    {
        return theme == AppTheme.SlateProfessional
            ? ThemePathSlateProfessional
            : ThemePathLightOcean;
    }
}
