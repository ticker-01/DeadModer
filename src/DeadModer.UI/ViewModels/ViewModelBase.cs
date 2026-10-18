using CommunityToolkit.Mvvm.ComponentModel;
using DeadModer.UI.Services;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// Базовая ViewModel для всех моделей представления с встроенной поддержкой локализации.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    public LocalizationService Loc => LocalizationService.Instance;
}
