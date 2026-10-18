using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DeadModer.Core.Enums;

namespace DeadModer.Core.Entities;

/// <summary>
/// Сущность глобальных настроек приложения (Singleton-запись в LiteDB).
/// </summary>
public partial class AppSettings : ObservableObject
{
    /// <summary>
    /// Фиксированный первичный ключ для хранения единственного экземпляра настроек в LiteDB.
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Путь к корневой папке установленной игры Deadlock (например, .../steamapps/common/Deadlock).
    /// </summary>
    [ObservableProperty]
    private string _gamePath = string.Empty;

    /// <summary>
    /// Пользовательский путь к папке хранения модов и кэша (по умолчанию в AppData).
    /// </summary>
    [ObservableProperty]
    private string _modsStoragePath = string.Empty;

    /// <summary>
    /// Путь к папке резервных копий оригинальных игровых файлов.
    /// </summary>
    [ObservableProperty]
    private string _backupsPath = string.Empty;

    /// <summary>
    /// Выбранная тема оформления (Dark, Light, System).
    /// </summary>
    [ObservableProperty]
    private AppTheme _theme = AppTheme.Dark;

    /// <summary>
    /// Язык интерфейса ("ru" или "en").
    /// </summary>
    [ObservableProperty]
    private string _language = "ru";

    /// <summary>
    /// Выбранный фоновый рисунок (Fone_1.png или Fone_2.png).
    /// </summary>
    [ObservableProperty]
    private string _selectedBackground = "Fone_1.png";

    /// <summary>
    /// Автоматически проверять наличие обновлений приложения и установленных модов.
    /// </summary>
    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    /// <summary>
    /// Запускать игру сразу с подключенными модами.
    /// </summary>
    [ObservableProperty]
    private bool _launchGameWithMods = true;

    /// <summary>
    /// Сворачивать приложение в системный трей при закрытии окна.
    /// </summary>
    [ObservableProperty]
    private bool _closeToTray = false;

    /// <summary>
    /// Дата последней успешной проверки обновлений.
    /// </summary>
    [ObservableProperty]
    private DateTime? _lastUpdateCheck;

    /// <summary>
    /// Дополнительные параметры командной строки при запуске Deadlock через менеджер.
    /// </summary>
    [ObservableProperty]
    private string _customLaunchArguments = string.Empty;

    /// <summary>
    /// Имя пользователя на платформе GameBanana.
    /// </summary>
    [ObservableProperty]
    private string _gameBananaUsername = string.Empty;

    /// <summary>
    /// Флаг успешной авторизации/симуляции аккаунта GameBanana.
    /// </summary>
    [ObservableProperty]
    private bool _isGameBananaAuthenticated = false;

    /// <summary>
    /// Токен авторизованной сессии GameBanana.
    /// </summary>
    [ObservableProperty]
    private string _gameBananaAuthToken = string.Empty;

    /// <summary>
    /// Email пользователя на GameBanana.
    /// </summary>
    [ObservableProperty]
    private string _gameBananaEmail = string.Empty;

    /// <summary>
    /// Имя пользователя на платформе Deadlocker.net.
    /// </summary>
    [ObservableProperty]
    private string _deadlockerUsername = string.Empty;

    /// <summary>
    /// Флаг успешной авторизации/симуляции аккаунта Deadlocker.
    /// </summary>
    [ObservableProperty]
    private bool _isDeadlockerAuthenticated = false;

    /// <summary>
    /// Токен авторизованной сессии Deadlocker.
    /// </summary>
    [ObservableProperty]
    private string _deadlockerAuthToken = string.Empty;

    /// <summary>
    /// Email пользователя на Deadlocker.
    /// </summary>
    [ObservableProperty]
    private string _deadlockerEmail = string.Empty;

    /// <summary>
    /// Флаг активности симуляции зарегистрированного участника сайтов.
    /// </summary>
    [ObservableProperty]
    private bool _isSimulatedMemberActive = false;

    /// <summary>
    /// Разблокировать все закрытые, 18+ и эксклюзивные моды в каталоге.
    /// </summary>
    [ObservableProperty]
    private bool _unlockRestrictedMods = true;

    /// <summary>
    /// URL аватарки профиля пользователя.
    /// </summary>
    [ObservableProperty]
    private string _userAvatarUrl = string.Empty;
}
