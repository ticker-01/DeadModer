using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeadModer.UI.Services;

/// <summary>
/// Сервис локализации интерфейса приложения (Русский / Английский).
/// Поддерживает реактивную смену языка на лету через привязку к индексатору XAML.
/// </summary>
public sealed partial class LocalizationService : ObservableObject
{
    private static readonly Lazy<LocalizationService> _lazyInstance = new(() => new LocalizationService());
    public static LocalizationService Instance => _lazyInstance.Value;

    [ObservableProperty]
    private string _currentLanguage = "ru";

    public bool IsRussian => CurrentLanguage == "ru";
    public bool IsEnglish => CurrentLanguage == "en";

    public event Action? LanguageChanged;

    private readonly Dictionary<string, string> _ru = new(StringComparer.OrdinalIgnoreCase)
    {
        // Приложение
        ["App_Title"] = "DEADMODER",
        ["App_Subtitle"] = "Моды для Deadlock",
        ["App_Version"] = "Deadmoder v0.0.1 (Alpha)",
        ["App_Quote"] = "Модификации Source 2",

        // Навигация
        ["Nav_Home"] = "Главная",
        ["Nav_Mods"] = "Каталог",
        ["Nav_Heroes"] = "Герои",
        ["Nav_MyMods"] = "Мои моды",
        ["Nav_Settings"] = "Настройки",

        // Статус игры и запуск
        ["Status_GameRunning"] = "В игре (Deadlock)",
        ["Status_GameNotRunning"] = "Игра не запущена",
        ["Status_ReadyToLaunch"] = "Готов к запуску",
        ["Status_Launching"] = "Запуск игры...",
        ["Btn_LaunchGame"] = "Запустить Deadlock",

        // Баннер
        ["Banner_Title"] = "Модифицируй\nсвой Deadlock",
        ["Banner_Subtitle"] = "Устанавливай, настраивай и играй с модами, которые делают игру ещё лучше.",

        // Поиск и фильтры
        ["Search_Placeholder"] = "Поиск модов по названию или автору...",
        ["Cat_All"] = "Все",
        ["Cat_Gameplay"] = "Геймплей",
        ["Cat_UI"] = "Интерфейс",
        ["Cat_Visual"] = "Графика",
        ["Cat_Sounds"] = "Звуки",
        ["Cat_Other"] = "Разное",

        // Секции каталога
        ["Section_Popular"] = "Популярные моды",
        ["Section_Recent"] = "Последние обновления",
        ["Link_ViewAll"] = "Смотреть все",

        // Таблица модов
        ["Col_Author"] = "Автор",
        ["Col_Date"] = "Дата",
        ["Col_Version"] = "Версия",
        ["Col_Size"] = "Размер",
        ["Col_Downloads"] = "Загрузок",
        ["Btn_Install"] = "Установить",
        ["Btn_Installed"] = "Установлен",
        ["Btn_Enabled"] = "Включен",
        ["Btn_Disabled"] = "Отключен",
        ["Btn_Downloading"] = "Загрузка...",
        ["Btn_Installing"] = "Установка...",
        ["Btn_Cancel"] = "Отмена",
        ["Btn_Error"] = "Ошибка",

        // Мои моды
        ["MyMods_Title"] = "Мои моды Deadlock",
        ["MyMods_Subtitle"] = "Управление VPK файлами в папке game/citadel/addons",
        ["Stat_Total"] = "Всего модов",
        ["Stat_Active"] = "Активно",
        ["Stat_Storage"] = "Занято места",
        ["Btn_OpenAddons"] = "Открыть addons",
        ["Btn_Sync"] = "Синхронизировать",
        ["Btn_EnableAll"] = "Включить все",
        ["Btn_DisableAll"] = "Отключить все",
        ["Btn_DeleteMod"] = "Удалить мод",
        ["MyMods_Empty"] = "У вас пока нет установленных модов. Выберите моды в Каталоге.",
        ["Local_Detected"] = "Локальный файл",

        // Герои
        ["Heroes_Title"] = "Герои Deadlock",
        ["Heroes_Subtitle"] = "Выберите персонажа для быстрого поиска скинов, анимаций и звуковых модов",
        ["Heroes_Search"] = "Поиск героя (например: Infernus, Seven, Vindicta)...",
        ["Heroes_ModsCount"] = "модов",
        ["Btn_ViewMods"] = "Смотреть моды",

        // Инспектор
        ["Inspector_Title"] = "Инспектор мода",
        ["Inspector_NoMod"] = "Выберите мод из списка или каталога для подробного просмотра",
        ["Inspector_Size"] = "Размер",
        ["Inspector_Compatibility"] = "Совместимость",
        ["Inspector_LatestVersion"] = "Последняя версия",
        ["Inspector_Date"] = "Дата установки",
        ["Btn_OpenFolder"] = "Открыть папку addons",
        ["Btn_Delete"] = "Удалить с диска",
        ["Inspector_QuickLinks"] = "Быстрые переходы",
        ["Inspector_QuickSettings"] = "Настройки",
        ["Inspector_QuickSettingsSub"] = "Параметры Deadlock и Steam",
        ["Inspector_QuickMyMods"] = "Мои моды",
        ["Inspector_QuickMyModsSub"] = "Управление установленными модами",
        ["Inspector_QuickHeroes"] = "Герои",
        ["Inspector_QuickHeroesSub"] = "Выбор персонажа и его скинов",
        ["Inspector_CreateMod"] = "Создай свой мод",
        ["Inspector_CreateModSub"] = "Делись своими идеями с комьюнити Deadlock.",
        ["Inspector_SelectedMod"] = "Выбранный мод",
        ["Inspector_OpenFolderBtn"] = "Открыть папку мода",
        ["Inspector_DeleteBtn"] = "Удалить",
        ["Inspector_OpenEditorBtn"] = "Открыть редактор",

        // 3D Модели и инспекция персонажей
        ["Btn_3DModel"] = "3D Модель",
        ["Btn_Close3D"] = "Закрыть",
        ["Viewer3D_Title"] = "Source 2 Осмотр Модели",
        ["Viewer3D_Subtitle"] = "Инспекция полигональной сетки и костей героя",
        ["Viewer3D_LaunchTools"] = "Запустить в Source 2 ModelDoc",
        ["Viewer3D_OpenFolder"] = "Папка моделей",
        ["Viewer3D_Export"] = "Экспорт модели",
        ["Viewer3D_Rotation"] = "Вращение 360°",
        ["Viewer3D_Wireframe"] = "Каркас (Wireframe)",
        ["Viewer3D_Shaded"] = "Освещение",
        ["Viewer3D_Format"] = "Формат",
        ["Viewer3D_Bones"] = "Кости скелета",
        ["Viewer3D_Polys"] = "Полигоны",
        ["Viewer3D_Materials"] = "Материалы",
        ["Heroes_RoleAll"] = "Все герои",
        ["Heroes_RoleAssault"] = "Атакующие",
        ["Heroes_RoleTank"] = "Танк",
        ["Heroes_RoleSupport"] = "Поддержка",
        ["Heroes_RoleDefense"] = "Защита",
        ["Heroes_RoleControl"] = "Контроль",
        ["Heroes_RoleMage"] = "Маг",
        ["Heroes_Difficulty"] = "Сложность",
        ["Heroes_StatHp"] = "Здоровье",
        ["Heroes_StatStamina"] = "Выносливость",
        ["Heroes_StatSpeed"] = "Скорость",
        ["Heroes_Features"] = "Особенности",
        ["Heroes_ActionDetails"] = "Подробнее >",
        ["Heroes_ActionInGame"] = "Смотреть в игре",

        // Модальная страница мода (Mockup 2)
        ["Mod_Download"] = "Скачать",
        ["Mod_Favorite"] = "В избранное",
        ["Mod_TabDescription"] = "Описание",
        ["Mod_TabScreenshots"] = "Скриншоты",
        ["Mod_Tab3DModel"] = "3D Модель",
        ["Mod_TabReviews"] = "Отзывы",
        ["Mod_TabVersions"] = "Версии",
        ["Catalog_LoadMore"] = "Загрузить больше модов",
        ["Catalog_LoadingMore"] = "Загрузка модов...",
        ["Catalog_TotalLoaded"] = "Показано модов:",
        ["Mod_WhatChanges"] = "Что изменяет:",
        ["Mod_Compatibility"] = "Совместимость",
        ["Mod_Installation"] = "Установка",
        ["Mod_InstallStep1"] = "Скачайте мод",
        ["Mod_InstallStep2"] = "Распакуйте архив в папку addons",
        ["Mod_InstallStep3"] = "Запустите игру",
        ["Mod_OtherByAuthor"] = "Другие моды от этого автора",
        ["Mod_GameVersion"] = "Версия игры: 1.0.x+",
        ["Mod_Platform"] = "Платформа: Windows",
        ["Mod_Dependencies"] = "Зависимости: Нет",
        ["Mod_BackToCatalog"] = "← Все моды",
        ["GameInfo_StatusActive"] = "Поддержка модов Source 2: Активна (gameinfo.gi)",
        ["GameInfo_StatusInactive"] = "Требуется настройка gameinfo.gi",
        ["GameInfo_BtnEnable"] = "Активировать моды в 1 клик",

        // Настройки
        ["Settings_Title"] = "Настройки приложения",
        ["Settings_Subtitle"] = "Конфигурация интеграции со Steam, путей к файлам игры и оформления",
        ["Settings_SteamStatus"] = "Статус Steam",
        ["Settings_AutoDetect"] = "Определить через Steam",
        ["Settings_Autosearch"] = "Автопоиск",
        ["Settings_DetectedSuccess"] = "Deadlock успешно найден в Steam",
        ["Settings_PathVerified"] = "Путь к игре подтвержден",
        ["Settings_PathNotFound"] = "Папка игры не найдена. Нажмите 'Определить через Steam'",
        ["Settings_PathsSection"] = "Пути к директориям",
        ["Settings_GamePath"] = "Путь к папке игры Deadlock",
        ["Settings_GamePathHint"] = "Обычно: .../steamapps/common/Deadlock",
        ["Settings_AddonsPath"] = "Папка модов addons (Source 2)",
        ["Settings_AddonsPathHint"] = "Папка .../game/citadel/addons, куда монтируются .vpk моды",
        ["Settings_BackupsPath"] = "Папка резервных копий",
        ["Settings_BehaviorSection"] = "Автоматизация и запуск",
        ["Settings_AutoCheckUpdates"] = "Автоматическая проверка обновлений модов",
        ["Settings_AutoCheckUpdatesHint"] = "Периодически сверять версии с GameBanana и уведомлять об апдейтах",
        ["Settings_LaunchWithMods"] = "Запускать игру с модами",
        ["Settings_LaunchWithModsHint"] = "Автоматически синхронизировать включенные .vpk перед запуском Deadlock",
        ["Settings_CloseToTray"] = "Сворачивать в системный трей",
        ["Settings_CloseToTrayHint"] = "Не закрывать менеджер при клике на крестик, а прятать в трей",
        ["Settings_AppearanceSection"] = "Язык и фон",
        ["Settings_Language"] = "Язык интерфейса",
        ["Settings_Background"] = "Фоновое изображение",
        ["Settings_Background1"] = "Фон 1 (Атмосферный)",
        ["Settings_Background2"] = "Фон 2 (Цитадель)",
        ["Btn_SaveSettings"] = "Сохранить изменения",
        ["Settings_SavedSuccess"] = "Настройки успешно сохранены"
    };

    private readonly Dictionary<string, string> _en = new(StringComparer.OrdinalIgnoreCase)
    {
        // App
        ["App_Title"] = "DEADMODER",
        ["App_Subtitle"] = "Deadlock Mod Manager",
        ["App_Version"] = "Deadmoder v0.0.1 (Alpha)",
        ["App_Quote"] = "Source 2 Modifications",

        // Navigation
        ["Nav_Home"] = "Home",
        ["Nav_Mods"] = "Catalog",
        ["Nav_Heroes"] = "Heroes",
        ["Nav_MyMods"] = "My Mods",
        ["Nav_Settings"] = "Settings",

        // Game status & launch
        ["Status_GameRunning"] = "In Game (Deadlock)",
        ["Status_GameNotRunning"] = "Game not running",
        ["Status_ReadyToLaunch"] = "Ready to launch",
        ["Status_Launching"] = "Launching game...",
        ["Btn_LaunchGame"] = "Launch Deadlock",

        // Banner
        ["Banner_Title"] = "Customize\nyour Deadlock",
        ["Banner_Subtitle"] = "Install, configure, and play with next-generation mods.",

        // Search & filters
        ["Search_Placeholder"] = "Search mods by title or author...",
        ["Cat_All"] = "All",
        ["Cat_Gameplay"] = "Gameplay",
        ["Cat_UI"] = "Interface",
        ["Cat_Visual"] = "Visuals",
        ["Cat_Sounds"] = "Audio",
        ["Cat_Other"] = "Other",

        // Catalog sections
        ["Section_Popular"] = "Popular Mods",
        ["Section_Recent"] = "Recent Updates",
        ["Link_ViewAll"] = "View All",

        // Mod table
        ["Col_Author"] = "Author",
        ["Col_Date"] = "Date",
        ["Col_Version"] = "Version",
        ["Col_Size"] = "Size",
        ["Col_Downloads"] = "Downloads",
        ["Btn_Install"] = "Install",
        ["Btn_Installed"] = "Installed",
        ["Btn_Enabled"] = "Enabled",
        ["Btn_Disabled"] = "Disabled",
        ["Btn_Downloading"] = "Downloading...",
        ["Btn_Installing"] = "Installing...",
        ["Btn_Cancel"] = "Cancel",
        ["Btn_Error"] = "Error",

        // My mods
        ["MyMods_Title"] = "My Deadlock Mods",
        ["MyMods_Subtitle"] = "Manage physical VPK files in game/citadel/addons",
        ["Stat_Total"] = "Total Mods",
        ["Stat_Active"] = "Active",
        ["Stat_Storage"] = "Storage Used",
        ["Btn_OpenAddons"] = "Open addons",
        ["Btn_Sync"] = "Sync addons",
        ["Btn_EnableAll"] = "Enable all",
        ["Btn_DisableAll"] = "Disable all",
        ["Btn_DeleteMod"] = "Delete mod",
        ["MyMods_Empty"] = "No mods installed yet. Browse the Catalog to get started.",
        ["Local_Detected"] = "Local file",

        // Heroes
        ["Heroes_Title"] = "Deadlock Heroes",
        ["Heroes_Subtitle"] = "Select a character to quickly find skins, voice lines, and cosmetic mods",
        ["Heroes_Search"] = "Search hero (e.g. Infernus, Seven, Vindicta)...",
        ["Heroes_ModsCount"] = "mods",
        ["Btn_ViewMods"] = "View Mods",

        // Inspector
        ["Inspector_Title"] = "Mod Inspector",
        ["Inspector_NoMod"] = "Select a mod from the list or catalog to view details",
        ["Inspector_Size"] = "Size",
        ["Inspector_Compatibility"] = "Compatibility",
        ["Inspector_LatestVersion"] = "Latest Version",
        ["Inspector_Date"] = "Install Date",
        ["Btn_OpenFolder"] = "Open addons folder",
        ["Btn_Delete"] = "Delete from disk",
        ["Inspector_QuickLinks"] = "Quick Links",
        ["Inspector_QuickSettings"] = "Settings",
        ["Inspector_QuickSettingsSub"] = "Deadlock and Steam configuration",
        ["Inspector_QuickMyMods"] = "My Mods",
        ["Inspector_QuickMyModsSub"] = "Manage installed modifications",
        ["Inspector_QuickHeroes"] = "Heroes",
        ["Inspector_QuickHeroesSub"] = "Hero roster and custom models",
        ["Inspector_CreateMod"] = "Create your own mod",
        ["Inspector_CreateModSub"] = "Share your creations with the Deadlock community",
        ["Inspector_SelectedMod"] = "Selected mod",
        ["Inspector_OpenFolderBtn"] = "Open mod folder",
        ["Inspector_DeleteBtn"] = "Delete",
        ["Inspector_OpenEditorBtn"] = "Open editor",

        // 3D Models & Hero inspection
        ["Btn_3DModel"] = "3D Model",
        ["Btn_Close3D"] = "Close",
        ["Viewer3D_Title"] = "Source 2 Model Inspector",
        ["Viewer3D_Subtitle"] = "Hero polygonal mesh and skeleton inspection",
        ["Viewer3D_LaunchTools"] = "Launch in Source 2 ModelDoc",
        ["Viewer3D_OpenFolder"] = "Model Directory",
        ["Viewer3D_Export"] = "Export Model",
        ["Viewer3D_Rotation"] = "360° Rotation",
        ["Viewer3D_Wireframe"] = "Wireframe",
        ["Viewer3D_Shaded"] = "Lighting",
        ["Viewer3D_Format"] = "Format",
        ["Viewer3D_Bones"] = "Skeleton Bones",
        ["Viewer3D_Polys"] = "Polygons",
        ["Viewer3D_Materials"] = "Materials",
        ["Heroes_RoleAll"] = "All Heroes",
        ["Heroes_RoleAssault"] = "Assault",
        ["Heroes_RoleTank"] = "Tank",
        ["Heroes_RoleSupport"] = "Support",
        ["Heroes_RoleDefense"] = "Defense",
        ["Heroes_RoleControl"] = "Control",
        ["Heroes_RoleMage"] = "Mage",
        ["Heroes_Difficulty"] = "Difficulty",
        ["Heroes_StatHp"] = "Health",
        ["Heroes_StatStamina"] = "Stamina",
        ["Heroes_StatSpeed"] = "Speed",
        ["Heroes_Features"] = "Features",
        ["Heroes_ActionDetails"] = "Details >",
        ["Heroes_ActionInGame"] = "View In-Game",

        // Mod detail modal (Mockup 2)
        ["Mod_Download"] = "Download",
        ["Mod_Favorite"] = "Favorite",
        ["Mod_TabDescription"] = "Description",
        ["Mod_TabScreenshots"] = "Screenshots",
        ["Mod_Tab3DModel"] = "3D Model",
        ["Mod_TabReviews"] = "Reviews",
        ["Mod_TabVersions"] = "Versions",
        ["Catalog_LoadMore"] = "Load More Mods",
        ["Catalog_LoadingMore"] = "Loading mods...",
        ["Catalog_TotalLoaded"] = "Mods shown:",
        ["Mod_WhatChanges"] = "What changes:",
        ["Mod_Compatibility"] = "Compatibility",
        ["Mod_Installation"] = "Installation",
        ["Mod_InstallStep1"] = "Download the mod",
        ["Mod_InstallStep2"] = "Extract archive into addons folder",
        ["Mod_InstallStep3"] = "Launch Deadlock",
        ["Mod_OtherByAuthor"] = "Other mods by this author",
        ["Mod_GameVersion"] = "Game version: 1.0.x+",
        ["Mod_Platform"] = "Platform: Windows",
        ["Mod_Dependencies"] = "Dependencies: None",
        ["Mod_BackToCatalog"] = "← All Mods",
        ["GameInfo_StatusActive"] = "Source 2 Mod Support: Active (gameinfo.gi)",
        ["GameInfo_StatusInactive"] = "gameinfo.gi setup required",
        ["GameInfo_BtnEnable"] = "Enable mods in 1 click",

        // Settings
        ["Settings_Title"] = "Application Settings",
        ["Settings_Subtitle"] = "Configure Steam integration, game paths, and appearance",
        ["Settings_SteamStatus"] = "Steam Status",
        ["Settings_AutoDetect"] = "Detect via Steam",
        ["Settings_Autosearch"] = "Autodetect",
        ["Settings_DetectedSuccess"] = "Deadlock successfully detected in Steam",
        ["Settings_PathVerified"] = "Game path verified",
        ["Settings_PathNotFound"] = "Game folder not found. Click 'Detect via Steam'",
        ["Settings_PathsSection"] = "Directories",
        ["Settings_GamePath"] = "Deadlock Game Path",
        ["Settings_GamePathHint"] = "Usually: .../steamapps/common/Deadlock",
        ["Settings_AddonsPath"] = "Addons Folder (Source 2)",
        ["Settings_AddonsPathHint"] = "Path to .../game/citadel/addons for VPK files",
        ["Settings_BackupsPath"] = "Backups Folder",
        ["Settings_BehaviorSection"] = "Automation and Launch",
        ["Settings_AutoCheckUpdates"] = "Automatically check for mod updates",
        ["Settings_AutoCheckUpdatesHint"] = "Periodically check GameBanana for newer versions",
        ["Settings_LaunchWithMods"] = "Launch game with mods",
        ["Settings_LaunchWithModsHint"] = "Automatically synchronize active VPK files before launch",
        ["Settings_CloseToTray"] = "Minimize to system tray",
        ["Settings_CloseToTrayHint"] = "Do not exit when clicking the close button",
        ["Settings_AppearanceSection"] = "Language & Background",
        ["Settings_Language"] = "Interface Language",
        ["Settings_Background"] = "Background Image",
        ["Settings_Background1"] = "Background 1 (Atmospheric)",
        ["Settings_Background2"] = "Background 2 (Citadel)",
        ["Btn_SaveSettings"] = "Save Changes",
        ["Settings_SavedSuccess"] = "Settings successfully saved"
    };

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        var dict = CurrentLanguage == "en" ? _en : _ru;
        if (dict.TryGetValue(key, out var val))
        {
            return val;
        }

        // Запасной поиск в противоположном словаре
        var fallbackDict = CurrentLanguage == "en" ? _ru : _en;
        if (fallbackDict.TryGetValue(key, out var fallbackVal))
        {
            return fallbackVal;
        }

        return key;
    }

    public void SetLanguage(string language)
    {
        var normalized = language.ToLowerInvariant().StartsWith("en") ? "en" : "ru";
        if (CurrentLanguage == normalized) return;

        CurrentLanguage = normalized;
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(IsRussian));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged("Item[]");

        LanguageChanged?.Invoke();
    }
}
