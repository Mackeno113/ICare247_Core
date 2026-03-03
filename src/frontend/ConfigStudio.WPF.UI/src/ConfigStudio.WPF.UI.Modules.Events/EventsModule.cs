// File    : EventsModule.cs
// Module  : Events
// Layer   : Presentation
// Purpose : Dang ky view Event Editor.

using ConfigStudio.WPF.UI.Core.Constants;
using ConfigStudio.WPF.UI.Modules.Events.ViewModels;
using ConfigStudio.WPF.UI.Modules.Events.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace ConfigStudio.WPF.UI.Modules.Events;

public sealed class EventsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<EventEditorView, EventEditorViewModel>(ViewNames.EventEditor);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
