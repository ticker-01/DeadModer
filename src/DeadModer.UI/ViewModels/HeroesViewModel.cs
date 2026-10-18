using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.UI.Services;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// Официальные векторные пути Google Material Symbols (https://fonts.google.com/icons).
/// </summary>
public static class MaterialIcons
{
    public const string Fire = "M12 23c-4.97 0-9-3.58-9-8 0-3.17 2.11-6.73 6.27-10.59.88-.82 2.29-.21 2.29.99 0 2.15 1.5 3.82 3.44 3.82 2.21 0 4-1.79 4-4 0-.75-.3-1.43-.79-1.93-.56-.58-.33-1.57.45-1.84C20.66 2.05 23 5.48 23 9c0 7.73-4.93 14-11 14z";
    public const string Bolt = "M11 21h-1l1-7H7.5c-.88 0-1.33-1.07-.71-1.7l7-7c.63-.63 1.71-.18 1.71.7v5h3.5c.88 0 1.33 1.07.71 1.7l-7 7c-.63.63-1.71.18-1.71-.7v-3z";
    public const string Shield = "M12 2L4 5v6.09c0 5.05 3.41 9.76 8 10.91 4.59-1.15 8-5.86 8-10.91V5l-8-3z";
    public const string Heart = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z";
    public const string Eye = "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";
    public const string Target = "M12 2C6.49 2 2 6.49 2 12s4.49 10 10 10 10-4.49 10-10S17.51 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3-8c0 1.66-1.34 3-3 3s-3-1.34-3-3 1.34-3 3-3 3 1.34 3 3z";
    public const string Speed = "M20.38 8.57l-1.23 1.85a8 8 0 0 1-.22 7.58H5.07A8 8 0 0 1 15.58 6.85l1.85-1.23A10 10 0 0 0 3.35 19a2 2 0 0 0 1.72 1h13.85a2 2 0 0 0 1.74-1 10 10 0 0 0-.28-10.43z M10.59 15.41a2 2 0 0 0 2.83 0l5.66-8.49-8.49 5.66a2 2 0 0 0 0 2.83z";
    public const string Star = "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z";
    public const string Rotate = "M12 6v3l4-4-4-4v3c-4.42 0-8 3.58-8 8 0 1.57.46 3.03 1.24 4.26L6.7 14.8c-.45-.83-.7-1.79-.7-2.8 0-3.31 2.69-6 6-6zm6.76 1.74L17.3 9.2c.44.84.7 1.79.7 2.8 0 3.31-2.69 6-6 6v-3l-4 4 4 4v-3c4.42 0 8-3.58 8-8 0-1.57-.46-3.03-1.24-4.26z";
    public const string Sparkle = "M12 2l2.4 7.6 7.6 2.4-7.6 2.4-2.4 7.6-2.4-7.6-7.6-2.4 7.6-2.4z";
}

/// <summary>
/// Способность героя Deadlock (Пассивная, 1, 2, 3, Ульта) с чистой векторной иконкой Google Material Symbols.
/// </summary>
public class HeroAbilityViewModel
{
    public string Slot { get; init; } = "1";
    public string Name { get; init; } = string.Empty;
    public string IconEmoji { get; init; } = "🔥";
    public string Description { get; init; } = string.Empty;

    private string? _iconPathData;
    public string IconPathData
    {
        get => _iconPathData ?? GetDefaultPathData();
        init => _iconPathData = value;
    }

    private string? _accentColor;
    public string AccentColor
    {
        get => _accentColor ?? GetDefaultColor();
        init => _accentColor = value;
    }

    private string GetDefaultPathData()
    {
        if (Slot == "Ульта") return MaterialIcons.Star;
        return IconEmoji switch
        {
            "🔥" or "🌋" or "💥" => MaterialIcons.Fire,
            "⚡" => MaterialIcons.Bolt,
            "🛡" => MaterialIcons.Shield,
            "🎯" => MaterialIcons.Target,
            "👁" => MaterialIcons.Eye,
            "🔄" or "🌀" => MaterialIcons.Rotate,
            "🦅" or "🪶" => MaterialIcons.Speed,
            "💣" or "✨" => MaterialIcons.Sparkle,
            "❤️" or "➕" => MaterialIcons.Heart,
            _ => MaterialIcons.Bolt
        };
    }

    private string GetDefaultColor()
    {
        return "#FFFFFF";
    }
}

/// <summary>
/// Модель персонажа Deadlock для сетки и правого инспектора (по образцу Heroes Screen).
/// </summary>
public partial class HeroItemViewModel : ViewModelBase
{
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Epithet { get; init; } = string.Empty;
    public string HeroCode { get; init; } = string.Empty;
    public string RoleType { get; init; } = "Assault"; // Assault, Tank, Support, Control, Defense
    public string RoleRu { get; init; } = "Атакующий";
    public string RoleEn { get; init; } = "Assault";
    public string RoleIcon { get; init; } = "🔥";
    public string BadgeColor { get; init; } = "#EF4444";
    public string LoreSummaryRu { get; init; } = string.Empty;
    public string LoreSummaryEn { get; init; } = string.Empty;
    public string CardPortraitUrl { get; init; } = string.Empty;
    public string FullPortraitUrl { get; init; } = string.Empty;
    public bool IsLocked { get; init; } = false;

    public int Hp { get; init; } = 550;
    public double HpPercent => Math.Clamp(Hp / 10.0, 10, 100);
    public double Stamina { get; init; } = 3.2;
    public double StaminaPercent => Math.Clamp(Stamina * 20.0, 10, 100);
    public string Speed { get; init; } = "7.0 м/с";
    public double SpeedPercent { get; init; } = 70.0;

    public ObservableCollection<string> Features { get; } = [];
    public ObservableCollection<HeroAbilityViewModel> Abilities { get; } = [];

    [ObservableProperty]
    private bool _isSelected = false;

    [ObservableProperty]
    private int _modCount = 0;

    [ObservableProperty]
    private Bitmap? _cardBitmap;

    [ObservableProperty]
    private Bitmap? _fullBitmap;

    public string Role => Loc.IsEnglish ? RoleEn : RoleRu;
    public string RoleBadgeText => Role;
    public string RoleIconPathData => RoleType switch
    {
        "Assault" => MaterialIcons.Fire,
        "Defense" or "Tank" => MaterialIcons.Shield,
        "Support" => MaterialIcons.Heart,
        "Control" => MaterialIcons.Eye,
        _ => MaterialIcons.Target
    };
    public string LoreSummary => Loc.IsEnglish ? (string.IsNullOrEmpty(LoreSummaryEn) ? LoreSummaryRu : LoreSummaryEn) : LoreSummaryRu;

    private readonly Action<HeroItemViewModel>? _onSelect;

    public HeroItemViewModel() { }

    public HeroItemViewModel(Action<HeroItemViewModel>? onSelect = null)
    {
        _onSelect = onSelect;
        Loc.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(RoleBadgeText));
            OnPropertyChanged(nameof(RoleIconPathData));
            OnPropertyChanged(nameof(LoreSummary));
        };
    }

    public void LoadPortraits()
    {
        if (CardBitmap == null && !string.IsNullOrEmpty(CardPortraitUrl))
        {
            _ = Task.Run(async () =>
            {
                var bmp = await ImageCacheService.LoadBitmapAsync(CardPortraitUrl);
                if (bmp != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => CardBitmap = bmp);
                }
            });
        }

        if (FullBitmap == null && !string.IsNullOrEmpty(FullPortraitUrl))
        {
            _ = Task.Run(async () =>
            {
                var bmp = await ImageCacheService.LoadBitmapAsync(FullPortraitUrl);
                if (bmp != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => FullBitmap = bmp);
                }
            });
        }
    }

    [RelayCommand]
    public void Select()
    {
        _onSelect?.Invoke(this);
    }
}

/// <summary>
/// ViewModel экрана "Герои" в точном соответствии с дизайн-макетом Deadlock.
/// 4x4 сетка с пагинацией, фильтрами и подробным правым инспектором.
/// </summary>
public partial class HeroesViewModel : ViewModelBase
{
    private const int PageSize = 16;
    private readonly Action<string>? _onHeroFilterRequested;
    private readonly ICachedModRepository? _cachedModRepository;
    private readonly ISteamLocator? _steamLocator;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedRoleFilter = "All";

    [ObservableProperty]
    private HeroItemViewModel? _selectedHero;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 2;

    public ObservableCollection<HeroItemViewModel> AllHeroes { get; } = [];
    public ObservableCollection<HeroItemViewModel> FilteredHeroes { get; } = [];
    public ObservableCollection<HeroItemViewModel> DisplayedHeroes { get; } = [];

    public HeroesViewModel() : this(null, null, null) { }

    public HeroesViewModel(
        Action<string>? onHeroFilterRequested = null,
        ICachedModRepository? cachedModRepository = null,
        ISteamLocator? steamLocator = null)
    {
        _onHeroFilterRequested = onHeroFilterRequested;
        _cachedModRepository = cachedModRepository;
        _steamLocator = steamLocator;

        InitializeRoster();
        ApplyFilter();

        if (AllHeroes.Count > 0)
        {
            SelectHero(AllHeroes[0]);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplyFilter();
    }

    [RelayCommand]
    public void FilterRole(string role)
    {
        SelectedRoleFilter = role;
        CurrentPage = 1;
        ApplyFilter();
    }

    [RelayCommand]
    public void SelectHero(HeroItemViewModel hero)
    {
        if (hero.IsLocked) return;

        foreach (var h in AllHeroes)
        {
            h.IsSelected = (h == hero);
        }

        SelectedHero = hero;
        hero.LoadPortraits();
    }


    [RelayCommand]
    public void ViewHeroMods()
    {
        if (SelectedHero != null && !SelectedHero.IsLocked)
        {
            _onHeroFilterRequested?.Invoke(SelectedHero.Name);
        }
    }

    [RelayCommand]
    public void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdatePageDisplay();
        }
    }

    [RelayCommand]
    public void PrevPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePageDisplay();
        }
    }

    [RelayCommand]
    public void GoToPage(object? pageObj)
    {
        if (pageObj == null) return;
        int page = pageObj is int i ? i : (int.TryParse(pageObj.ToString(), out var p) ? p : 1);
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            UpdatePageDisplay();
        }
    }

    private void ApplyFilter()
    {
        FilteredHeroes.Clear();

        var query = SearchQuery.Trim();
        var role = SelectedRoleFilter;

        var matches = AllHeroes.Where(h =>
        {
            if (h.IsLocked) return true;

            var matchSearch = string.IsNullOrWhiteSpace(query) ||
                              h.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              h.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              h.Epithet.Contains(query, StringComparison.OrdinalIgnoreCase);

            var matchRole = role == "All" ||
                            (role == "Assault" && h.RoleType == "Assault") ||
                            (role == "Defense" && (h.RoleType == "Defense" || h.RoleType == "Tank")) ||
                            (role == "Tank" && h.RoleType == "Tank") ||
                            (role == "Support" && h.RoleType == "Support") ||
                            (role == "Control" && h.RoleType == "Control");

            return matchSearch && matchRole;
        }).ToList();

        foreach (var h in matches)
        {
            FilteredHeroes.Add(h);
        }

        TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredHeroes.Count / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = 1;

        UpdatePageDisplay();
    }

    private void UpdatePageDisplay()
    {
        DisplayedHeroes.Clear();
        var paged = FilteredHeroes.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        foreach (var h in paged)
        {
            DisplayedHeroes.Add(h);
            h.LoadPortraits();
        }
    }

    private void InitializeRoster()
    {
        AllHeroes.Clear();

        // 1. INFERNUS (Selected)
        var infernus = new HeroItemViewModel(SelectHero)
        {
            Name = "Infernus",
            Title = "INFERNUS",
            Epithet = "The Burning Soul",
            HeroCode = "inferno",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 550,
            Stamina = 3.2,
            Speed = "7.0 м/с",
            SpeedPercent = 70.0,
            LoreSummaryRu = "Инфернус — мастер огня и хаоса. Его пламя не только сжигает врагов, но и оставляет след в их памяти.",
            LoreSummaryEn = "Infernus is a master of fire and mayhem, leaving smoldering devastation in his wake.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/inferno_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/inferno_full.png",
            ModCount = 42
        };
        infernus.Features.Add("Высокий урон с близкой и средней дистанции");
        infernus.Features.Add("Обладает мощными способностями контроля");
        infernus.Features.Add("Требует правильного позиционирования");
        infernus.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Катализатор", IconEmoji = "🔥", Description = "Наносит периодический урон огнем." });
        infernus.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Огненный рывок", IconEmoji = "⚡", Description = "Быстрый рывок с огненным следом." });
        infernus.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Воспламенение", IconEmoji = "💥", Description = "Поджигает область перед собой." });
        infernus.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Огненная волна", IconEmoji = "🌋", Description = "Создает стену всепожирающего пламени." });
        infernus.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Инфернальный вихрь", IconEmoji = "🌪", Description = "Колоссальный взрыв огненной стихии." });
        AllHeroes.Add(infernus);

        // 2. VINDICTA
        var vindicta = new HeroItemViewModel(SelectHero)
        {
            Name = "Vindicta",
            Title = "VINDICTA",
            Epithet = "The Vengeful Sniper",
            HeroCode = "hornet",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 500,
            Stamina = 2.8,
            Speed = "6.8 м/с",
            SpeedPercent = 68.0,
            LoreSummaryRu = "Призрак казненной ведьмы, наносящая смертоносные снайперские выстрелы с высоты птичьего полета.",
            LoreSummaryEn = "Vengeful spirit soaring above the battlefield to snipe targets from afar.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/hornet_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/hornet_full.png",
            ModCount = 38
        };
        vindicta.Features.Add("Смертоносный урон на сверхдальних дистанциях");
        vindicta.Features.Add("Способность парить над полем боя");
        vindicta.Features.Add("Капканы для обездвиживания противников");
        vindicta.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Ворон", IconEmoji = "🦅", Description = "Призывает призрачного ворона." });
        vindicta.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Капкан", IconEmoji = "⛓", Description = "Приковывает цель к земле." });
        vindicta.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Полет", IconEmoji = "🪶", Description = "Взмывает в воздух с ускорением." });
        vindicta.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Метка ведьмы", IconEmoji = "🎯", Description = "Увеличивает урон по цели." });
        vindicta.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Смертельный выстрел", IconEmoji = "⚡", Description = "Выстрел снайперской винтовки со сверхвысоким уроном." });
        AllHeroes.Add(vindicta);

        // 3. PARADOX
        var paradox = new HeroItemViewModel(SelectHero)
        {
            Name = "Paradox",
            Title = "PARADOX",
            Epithet = "The Time Weaver",
            HeroCode = "chrono",
            RoleType = "Control",
            RoleRu = "Контроль",
            RoleEn = "Control",
            RoleIcon = "👁",
            BadgeColor = "#8B5CF6",
            Hp = 550,
            Stamina = 3.0,
            Speed = "7.0 м/с",
            SpeedPercent = 70.0,
            LoreSummaryRu = "Манипулятор временем и пространством, меняющийся местами с врагами и замораживающий пули.",
            LoreSummaryEn = "Master of temporal mechanics, swapping positions with enemies and warping space.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/chrono_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/chrono_full.png",
            ModCount = 29
        };
        paradox.Features.Add("Смена позиций с вражескими героями");
        paradox.Features.Add("Пространственная стена остановки снарядов");
        paradox.Features.Add("Взрывные импульсы хроно-энергии");
        paradox.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Хроно-щит", IconEmoji = "🛡", Description = "Поглощает кинетическую энергию." });
        paradox.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Импульсная граната", IconEmoji = "💣", Description = "Замедляет и наносит урон." });
        paradox.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Искажение времени", IconEmoji = "🌀", Description = "Стена, останавливающая пули." });
        paradox.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Кинетический карабин", IconEmoji = "🔫", Description = "Заряженный выстрел с оглушением." });
        paradox.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Парадокс-смена", IconEmoji = "🔄", Description = "Меняется местами с выбранным героем." });
        AllHeroes.Add(paradox);

        // 4. MO & KRILL
        var moKrill = new HeroItemViewModel(SelectHero)
        {
            Name = "Mo & Krill",
            Title = "MO & KRILL",
            Epithet = "The Dynamic Underground Duo",
            HeroCode = "digger",
            RoleType = "Tank",
            RoleRu = "Танк",
            RoleEn = "Tank",
            RoleIcon = "🛡",
            BadgeColor = "#3B82F6",
            Hp = 650,
            Stamina = 3.5,
            Speed = "6.5 м/с",
            SpeedPercent = 65.0,
            LoreSummaryRu = "Неразлучный дуэт крота и обезьяны, закапывающийся под землю и сокрушающий ряды противников.",
            LoreSummaryEn = "Inseparable burrowing team ambushing from below and dishing out massive disruption.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/digger_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/digger_full.png",
            ModCount = 22
        };
        moKrill.Features.Add("Подземное перемещение и внезапные засады");
        moKrill.Features.Add("Обезоруживание и замедление врагов");
        moKrill.Features.Add("Долгое удержание цели в захвате");
        moKrill.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Толстая шкура", IconEmoji = "🦔", Description = "Увеличивает броню в бою." });
        moKrill.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Вгрызание", IconEmoji = "⛏", Description = "Подземный туннель с вылетом наружу." });
        moKrill.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Песчаная струя", IconEmoji = "💨", Description = "Ослепляет и обезоруживает." });
        moKrill.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Растерзание", IconEmoji = "🐾", Description = "Вгрызается в плоть противника." });
        moKrill.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Комбо-захват", IconEmoji = "💢", Description = "Прижимает врага к земле и молотит его." });
        AllHeroes.Add(moKrill);

        // 5. WRAITH
        var wraith = new HeroItemViewModel(SelectHero)
        {
            Name = "Wraith",
            Title = "WRAITH",
            Epithet = "The High Roller",
            HeroCode = "wraith",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 500,
            Stamina = 3.0,
            Speed = "7.2 м/с",
            SpeedPercent = 72.0,
            LoreSummaryRu = "Азартный гангстер, стреляющий зачарованными картами и телепортирующийся по полю боя.",
            LoreSummaryEn = "High-rolling mobster showering opponents with enchanted cards and rapid teleports.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/wraith_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/wraith_full.png",
            ModCount = 35
        };
        wraith.Features.Add("Колоссальный темп стрельбы и урон карт");
        wraith.Features.Add("Мгновенная телепортация для спасения или пуша");
        wraith.Features.Add("Телекинетический подъем ключевой цели");
        wraith.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Туз в рукаве", IconEmoji = "🃏", Description = "Карты наносят бонусный урон." });
        wraith.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Карточный фокус", IconEmoji = "🎴", Description = "Шквал взрывных карт." });
        wraith.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Телепорт", IconEmoji = "🚪", Description = "Мгновенное перемещение вперед." });
        wraith.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Полный вперед", IconEmoji = "⚡", Description = "Увеличивает скорострельность команды." });
        wraith.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Телекинез", IconEmoji = "🌀", Description = "Поднимает врага в воздух, обездвиживая его." });
        AllHeroes.Add(wraith);

        // 6. SHIV
        var shiv = new HeroItemViewModel(SelectHero)
        {
            Name = "Shiv",
            Title = "SHIV",
            Epithet = "The Lethal Blade",
            HeroCode = "shiv",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 550,
            Stamina = 3.2,
            Speed = "7.4 м/с",
            SpeedPercent = 74.0,
            LoreSummaryRu = "Безжалостный наемник с метательными клинками, накапливающий ярость и добивающий врагов казнью.",
            LoreSummaryEn = "Ruthless assassin building feral rage and executing wounded targets in a flash.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/shiv_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/shiv_full.png",
            ModCount = 26
        };
        shiv.Features.Add("Метательные зазубренные ножи с кровотечением");
        shiv.Features.Add("Откладывание получаемого урона через ярость");
        shiv.Features.Add("Казнящий прыжок на раненых противников");
        shiv.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Кровавая ярость", IconEmoji = "🩸", Description = "Накапливает ярость от нанесенного урона." });
        shiv.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Зазубренный нож", IconEmoji = "🗡", Description = "Метает клинок, замедляя врага." });
        shiv.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Удар с разгона", IconEmoji = "💨", Description = "Рывок сквозь врагов с уроном." });
        shiv.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Кровопускание", IconEmoji = "💉", Description = "Очищает отложенный урон." });
        shiv.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Убийственный рывок", IconEmoji = "⚡", Description = "Мгновенно казнит врагов с низким здоровьем." });
        AllHeroes.Add(shiv);

        // 7. LADY GEIST
        var geist = new HeroItemViewModel(SelectHero)
        {
            Name = "Lady Geist",
            Title = "LADY GEIST",
            Epithet = "The Life Drinker",
            HeroCode = "spectre",
            RoleType = "Support",
            RoleRu = "Поддержка",
            RoleEn = "Support",
            RoleIcon = "➕",
            BadgeColor = "#10B981",
            Hp = 550,
            Stamina = 3.0,
            Speed = "6.9 м/с",
            SpeedPercent = 69.0,
            LoreSummaryRu = "Аристократка, жертвующая собственным здоровьем ради разрушительной силы и обменивающаяся душами.",
            LoreSummaryEn = "Aristocrat sacrificing life essence for immense blood magic and health swapping.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/spectre_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/spectre_full.png",
            ModCount = 31
        };
        geist.Features.Add("Жертва здоровья ради колоссального урона бомб");
        geist.Features.Add("Вытягивание жизненной силы через осколки души");
        geist.Features.Add("Мгновенный обмен здоровьем с врагом");
        geist.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Кровавый пакт", IconEmoji = "🍷", Description = "Усиливает магию при низком HP." });
        geist.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Эссенция бомбы", IconEmoji = "🧪", Description = "Взрывная сфера крови." });
        geist.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Вытягивание жизни", IconEmoji = "🩸", Description = "Похищает здоровье у противника." });
        geist.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Осколки вины", IconEmoji = "💔", Description = "Шквал самонаводящихся зарядов." });
        geist.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Обмен душами", IconEmoji = "🔄", Description = "Обменивает свой процент HP с врагом." });
        AllHeroes.Add(geist);

        // 8. IVY
        var ivy = new HeroItemViewModel(SelectHero)
        {
            Name = "Ivy",
            Title = "IVY",
            Epithet = "The Living Gargoyle",
            HeroCode = "tengu",
            RoleType = "Support",
            RoleRu = "Поддержка",
            RoleEn = "Support",
            RoleIcon = "➕",
            BadgeColor = "#10B981",
            Hp = 550,
            Stamina = 3.0,
            Speed = "7.1 м/с",
            SpeedPercent = 71.0,
            LoreSummaryRu = "Ожившая горгулья, связывающая души союзников для лечения и переносящая их по воздуху.",
            LoreSummaryEn = "Gargoyle companion binding life links to heal allies and airlifting them into action.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/tengu_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/tengu_full.png",
            ModCount = 27
        };
        ivy.Features.Add("Связка душ: совместное лечение и боевые бонусы");
        ivy.Features.Add("Оглушающая каменная форма");
        ivy.Features.Add("Воздушный перенос союзника с бомбардировкой");
        ivy.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Каменная кожа", IconEmoji = "🗿", Description = "Защита от физического урона." });
        ivy.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Колючая лоза", IconEmoji = "🌿", Description = "Замедляет и обездвиживает врагов." });
        ivy.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Связующая нить", IconEmoji = "🔗", Description = "Дает союзнику вампиризм и скорострельность." });
        ivy.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Каменная форма", IconEmoji = "🪨", Description = "Падает на землю, оглушая всех вокруг." });
        ivy.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Воздушный подъем", IconEmoji = "🦅", Description = "Поднимает себя или союзника в полет и сбрасывает бомбу." });
        AllHeroes.Add(ivy);

        // 9. ABRAMS
        var abrams = new HeroItemViewModel(SelectHero)
        {
            Name = "Abrams",
            Title = "ABRAMS",
            Epithet = "The Heavyweight Brawler",
            HeroCode = "bull",
            RoleType = "Tank",
            RoleRu = "Танк",
            RoleEn = "Tank",
            RoleIcon = "🛡",
            BadgeColor = "#3B82F6",
            Hp = 700,
            Stamina = 3.5,
            Speed = "6.6 м/с",
            SpeedPercent = 66.0,
            LoreSummaryRu = "Детектив с мистической книгой и демонической силой, доминирующий в ближнем бою.",
            LoreSummaryEn = "Tough-as-nails detective fueled by hellish power, dominating up-close boxing.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/bull_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/bull_full.png",
            ModCount = 33
        };
        abrams.Features.Add("Огромный запас здоровья и регенерация");
        abrams.Features.Add("Таранный рывок с припечатыванием к стене");
        abrams.Features.Add("Мощные сокрушительные удары кулаком");
        abrams.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Несокрушимость", IconEmoji = "🛡", Description = "Восстанавливает полученный урон." });
        abrams.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Поглощение жизни", IconEmoji = "💀", Description = "Крадет здоровье у окружающих врагов." });
        abrams.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Таранный рывок", IconEmoji = "🐂", Description = "Несется вперед и припечатывает врага к стене." });
        abrams.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Внутренняя сила", IconEmoji = "📖", Description = "Пассивно восстанавливает часть потерянного здоровья." });
        abrams.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Сейсмический прыжок", IconEmoji = "💥", Description = "Взмывает ввысь и обрушивается на землю, оглушая область." });
        AllHeroes.Add(abrams);

        // 10. HAZE
        var haze = new HeroItemViewModel(SelectHero)
        {
            Name = "Haze",
            Title = "HAZE",
            Epithet = "The Silent Assassin",
            HeroCode = "haze",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 500,
            Stamina = 3.0,
            Speed = "7.3 м/с",
            SpeedPercent = 73.0,
            LoreSummaryRu = "Скрытный спецагент, усыпляющая врагов кинжалом и устраивающая смертоносный танец пуль.",
            LoreSummaryEn = "Covert operative eliminating high-priority targets from pure stealth.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/haze_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/haze_full.png",
            ModCount = 45
        };
        haze.Features.Add("Невидимость и засады со спины");
        haze.Features.Add("Усыпляющий кинжал для изоляции цели");
        haze.Features.Add("Колоссальный урон в ультимативном танце пуль");
        haze.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Фиксация цели", IconEmoji = "🎯", Description = "Каждая пуля увеличивает урон последующих." });
        haze.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Снотворный кинжал", IconEmoji = "🗡", Description = "Усыпляет врага при попадании." });
        haze.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Дымка невидимости", IconEmoji = "🌫", Description = "Становится невидимой и получает ускорение." });
        haze.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Шквал пуль", IconEmoji = "🔫", Description = "Пассивно повышает урон выстрелов." });
        haze.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Танец пуль", IconEmoji = "🌪", Description = "Круговой шквал выстрелов с уклонением." });
        AllHeroes.Add(haze);

        // 11. KELVIN
        var kelvin = new HeroItemViewModel(SelectHero)
        {
            Name = "Kelvin",
            Title = "KELVIN",
            Epithet = "The Cryo Specialist",
            HeroCode = "kelvin",
            RoleType = "Defense",
            RoleRu = "Защита",
            RoleEn = "Defense",
            RoleIcon = "🛡",
            BadgeColor = "#3B82F6",
            Hp = 600,
            Stamina = 3.2,
            Speed = "6.8 м/с",
            SpeedPercent = 68.0,
            LoreSummaryRu = "Полярный исследователь, замораживающий противников, строящий ледяные мосты и купол абсолютного нуля.",
            LoreSummaryEn = "Arctic explorer manipulating sub-zero frost to freeze foes and build ice highways.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/kelvin_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/kelvin_full.png",
            ModCount = 25
        };
        kelvin.Features.Add("Ледяной луч с нарастающим замедлением");
        kelvin.Features.Add("Создание ледяных мостов в воздухе");
        kelvin.Features.Add("Непробиваемый ледяной купол с лечением");
        kelvin.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Глубокая заморозка", IconEmoji = "❄", Description = "Замедляет скорость передвижения врагов." });
        kelvin.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Морозная граната", IconEmoji = "🧊", Description = "Взрывается, нанося урон и замедляя." });
        kelvin.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Ледяной путь", IconEmoji = "⛸", Description = "Создает в воздухе ледяную дорожку для скольжения." });
        kelvin.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Замораживающий луч", IconEmoji = "🥶", Description = "Потоковый луч абсолютного холода." });
        kelvin.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Купол холода", IconEmoji = "🌐", Description = "Непробиваемый ледяной купол, лечащий союзников внутри." });
        AllHeroes.Add(kelvin);

        // 12. DYNAMO
        var dynamo = new HeroItemViewModel(SelectHero)
        {
            Name = "Dynamo",
            Title = "DYNAMO",
            Epithet = "The Quantum Scholar",
            HeroCode = "sumo",
            RoleType = "Support",
            RoleRu = "Поддержка",
            RoleEn = "Support",
            RoleIcon = "➕",
            BadgeColor = "#10B981",
            Hp = 550,
            Stamina = 3.0,
            Speed = "6.7 м/с",
            SpeedPercent = 67.0,
            LoreSummaryRu = "Ученый профессор, чье тело стало живой сингулярностью, способной искривлять само пространство.",
            LoreSummaryEn = "Gentleman scholar transformed into a living celestial phenomenon.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/sumo_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/sumo_full.png",
            ModCount = 20
        };
        dynamo.Features.Add("Мощная аура лечения для всей команды");
        dynamo.Features.Add("Подбрасывание врагов кинетическим импульсом");
        dynamo.Features.Add("Черная дыра (Сингулярность), переворачивающая тимфайты");
        dynamo.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Квантовая связь", IconEmoji = "⚛", Description = "Усиливает союзников при лечении." });
        dynamo.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Кинетический импульс", IconEmoji = "💥", Description = "Подбрасывает врагов в воздух." });
        dynamo.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Квантовое перемещение", IconEmoji = "✨", Description = "Телепортирует себя и союзников вперед." });
        dynamo.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Омолаживающее сияние", IconEmoji = "💚", Description = "Лечебная аура вокруг себя." });
        dynamo.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Сингулярность", IconEmoji = "🕳", Description = "Черная дыра, засасывающая и обездвиживающая врагов." });
        AllHeroes.Add(dynamo);

        // 13. SEVEN
        var seven = new HeroItemViewModel(SelectHero)
        {
            Name = "Seven",
            Title = "SEVEN",
            Epithet = "The Stormbringer",
            HeroCode = "gigawatt",
            RoleType = "Control",
            RoleRu = "Контроль",
            RoleEn = "Control",
            RoleIcon = "👁",
            BadgeColor = "#8B5CF6",
            Hp = 550,
            Stamina = 3.0,
            Speed = "7.0 м/с",
            SpeedPercent = 70.0,
            LoreSummaryRu = "Осужденный маг молний, способный вызывать катастрофический электрический шторм над всем полем боя.",
            LoreSummaryEn = "Electrified convict channeling world-shattering lightning storms.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/gigawatt_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/gigawatt_full.png",
            ModCount = 37
        };
        seven.Features.Add("Цепная молния, зачищающая волны крипов");
        seven.Features.Add("Оглушающий шар статического заряда");
        seven.Features.Add("Глобальный шторм с гигантским радиусом поражения");
        seven.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Перегрузка", IconEmoji = "⚡", Description = "Электрический урон распространяется по цепи." });
        seven.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Шар молнии", IconEmoji = "🔮", Description = "Энергетическая сфера с уроном по области." });
        seven.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Статический заряд", IconEmoji = "⚡", Description = "Оглушает цель после задержки." });
        seven.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Ударные пули", IconEmoji = "🔋", Description = "Выстрелы бьют током несколько врагов." });
        seven.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Грозовой шторм", IconEmoji = "🌩", Description = "Вызывает яростную бурю молний в огромном радиусе." });
        AllHeroes.Add(seven);

        // 14. HOLLIDAY (TALON in some concept arts)
        var holliday = new HeroItemViewModel(SelectHero)
        {
            Name = "Holliday",
            Title = "TALON",
            Epithet = "The Quickdraw Sheriff",
            HeroCode = "astro",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 550,
            Stamina = 3.0,
            Speed = "7.1 м/с",
            SpeedPercent = 71.0,
            LoreSummaryRu = "Шериф Дикого Запада с прыжковыми бочками, лассо и молниеносной стрельбой из револьвера.",
            LoreSummaryEn = "Western lawman leaping into gunfights with trampolines and ricochet shots.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/astro_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/astro_full.png",
            ModCount = 19
        };
        holliday.Features.Add("Отскоки от бочек для вертикальной мобильности");
        holliday.Features.Add("Рикошетящие пули из крупнокалиберного револьвера");
        holliday.Features.Add("Лассо для захвата и подтягивания противников");
        holliday.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Порох", IconEmoji = "💥", Description = "Каждый выстрел накапливает кинетическую мощь." });
        holliday.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Пороховая бочка", IconEmoji = "🛢", Description = "Устанавливает батут для прыжков." });
        holliday.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Лассо", IconEmoji = "🪢", Description = "Связывает и притягивает врага." });
        holliday.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Рикошет", IconEmoji = "🎯", Description = "Выстрел отскакивает во вторичные цели." });
        holliday.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Огненный шквал", IconEmoji = "🤠", Description = "Скоростная разрядка барабана по нескольким целям." });
        AllHeroes.Add(holliday);

        // 15. GREY TALON
        var greyTalon = new HeroItemViewModel(SelectHero)
        {
            Name = "Grey Talon",
            Title = "GREY TALON",
            Epithet = "The Spirit Archer",
            HeroCode = "archer",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 500,
            Stamina = 2.8,
            Speed = "7.0 м/с",
            SpeedPercent = 70.0,
            LoreSummaryRu = "Старый следопыт и шаман, пускающий заряженные стрелы и управляющий духом гигантской хищной совы.",
            LoreSummaryEn = "Venerable hunter launching spiritual arrows and piloting a lethal spirit owl.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/archer_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/archer_full.png",
            ModCount = 28
        };
        greyTalon.Features.Add("Заряжаемые бронебойные стрелы сквозь укрытия");
        greyTalon.Features.Add("Капканы, ловящие врагов в ловушку");
        greyTalon.Features.Add("Управляемая сова со взрывом на ультра-дистанции");
        greyTalon.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Орлиный глаз", IconEmoji = "👁", Description = "Увеличивает дальность обзора и точность." });
        greyTalon.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Заряженная стрела", IconEmoji = "🏹", Description = "Мощная стрела, прошивающая препятствия." });
        greyTalon.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Прыжок в воздух", IconEmoji = "🪶", Description = "Взмывает вверх и парит, стреляя сверху." });
        greyTalon.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Ловушка обездвиживания", IconEmoji = "🪤", Description = "Привязывает наступившего врага." });
        greyTalon.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Дух Совы", IconEmoji = "🦉", Description = "Управляет духом совы, взрывающимся во врага." });
        AllHeroes.Add(greyTalon);

        // 16. THE CURIOSITY SHOP (Locked card matching mockup)
        var mysteryCard = new HeroItemViewModel
        {
            Name = "The Curiosity Shop",
            Title = "THE CURIOSITY SHOP",
            Epithet = "Secret Vault",
            HeroCode = "shop",
            RoleType = "Other",
            RoleRu = "Секрет",
            RoleEn = "Secret",
            RoleIcon = "🔒",
            BadgeColor = "#475569",
            Hp = 0,
            Stamina = 0,
            Speed = "—",
            SpeedPercent = 0,
            LoreSummaryRu = "Таинственная лавка древностей откроется в следующем обновлении.",
            LoreSummaryEn = "The mysterious curiosity shop will open in a future update.",
            IsLocked = true
        };
        AllHeroes.Add(mysteryCard);

        // --- ДОПОЛНИТЕЛЬНЫЕ ГЕРОИ (СТРАНИЦА 2) ---
        // Bebop
        var bebop = new HeroItemViewModel(SelectHero)
        {
            Name = "Bebop",
            Title = "BEBOP",
            Epithet = "The Scrapyard Juggernaut",
            HeroCode = "bebop",
            RoleType = "Tank",
            RoleRu = "Танк",
            RoleEn = "Tank",
            RoleIcon = "🛡",
            BadgeColor = "#3B82F6",
            Hp = 650,
            Stamina = 3.2,
            Speed = "6.5 м/с",
            SpeedPercent = 65.0,
            LoreSummaryRu = "Тяжелый робот с гарпуном, притягивающий врагов и взрывающий гипер-лазером.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/bebop_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/bebop_full.png",
            ModCount = 30
        };
        bebop.Features.Add("Крюк для притягивания врагов на колоссальной дистанции");
        bebop.Features.Add("Липкие бомбы с таймером детонации");
        bebop.Features.Add("Гиперлазер, плавящий целые команды");
        bebop.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Броня свалки", IconEmoji = "⚙", Description = "Повышает устойчивость к взрывам." });
        bebop.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Апперкот", IconEmoji = "🥊", Description = "Отправляет врага в полет." });
        bebop.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Липкая бомба", IconEmoji = "💣", Description = "Прикрепляет бомбу к цели." });
        bebop.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Крюк-гарпун", IconEmoji = "🪝", Description = "Притягивает врага или союзника." });
        bebop.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Гипер-луч", IconEmoji = "⚡", Description = "Мощнейший потоковый лазерный луч." });
        AllHeroes.Add(bebop);

        // McGinnis
        var mcginnis = new HeroItemViewModel(SelectHero)
        {
            Name = "McGinnis",
            Title = "MCGINNIS",
            Epithet = "The Industrial Engineer",
            HeroCode = "engineer",
            RoleType = "Defense",
            RoleRu = "Защита",
            RoleEn = "Defense",
            RoleIcon = "🛡",
            BadgeColor = "#3B82F6",
            Hp = 600,
            Stamina = 3.0,
            Speed = "6.7 м/с",
            SpeedPercent = 67.0,
            LoreSummaryRu = "Инженер-механик, возводящая защитные турели, лечебные станции и стены.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/engineer_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/engineer_full.png",
            ModCount = 24
        };
        mcginnis.Features.Add("Установка огневых турелей");
        mcginnis.Features.Add("Лечебные станции для союзников");
        mcginnis.Features.Add("Ракетный залп по площадям");
        mcginnis.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Перегрев", IconEmoji = "🔧", Description = "Скорострельность растет при стрельбе." });
        mcginnis.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Огневая турель", IconEmoji = "🤖", Description = "Устанавливает автоматическую турель." });
        mcginnis.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Лечебная станция", IconEmoji = "💉", Description = "Лечит союзников в радиусе." });
        mcginnis.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Спектральная стена", IconEmoji = "🧱", Description = "Блокирует проход врагам." });
        mcginnis.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Ракетный шквал", IconEmoji = "🚀", Description = "Залп десятков тяжелых ракет." });
        AllHeroes.Add(mcginnis);

        // Yamato
        var yamato = new HeroItemViewModel(SelectHero)
        {
            Name = "Yamato",
            Title = "YAMATO",
            Epithet = "The Crimson Shadow",
            HeroCode = "yamato",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 550,
            Stamina = 3.2,
            Speed = "7.3 м/с",
            SpeedPercent = 73.0,
            LoreSummaryRu = "Мастер тени и катаны, совершающая рывки сквозь врагов и обретающая временное бессмертие.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/yamato_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/yamato_full.png",
            ModCount = 29
        };
        yamato.Features.Add("Молниеносный крюк к противнику");
        yamato.Features.Add("Теневые рассечения катаной");
        yamato.Features.Add("Временная неуязвимость при трансформации");
        yamato.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Клинок тени", IconEmoji = "🗡", Description = "Усиливает удары катаной." });
        yamato.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Удар в полете", IconEmoji = "💨", Description = "Рывок к врагу с рассечением." });
        yamato.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Крюк к цели", IconEmoji = "🪢", Description = "Притягивается к выбранному врагу." });
        yamato.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Теневой вихрь", IconEmoji = "🌀", Description = "Круговой удар с замедлением." });
        yamato.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Трансформация Тени", IconEmoji = "👹", Description = "Дает полную неуязвимость и сбрасывает кулдауны." });
        AllHeroes.Add(yamato);

        // Warden
        var warden = new HeroItemViewModel(SelectHero)
        {
            Name = "Warden",
            Title = "WARDEN",
            Epithet = "The Law Enforcer",
            HeroCode = "warden",
            RoleType = "Defense",
            RoleRu = "Защита",
            RoleEn = "Defense",
            RoleIcon = "🛡",
            BadgeColor = "#3B82F6",
            Hp = 600,
            Stamina = 3.0,
            Speed = "6.9 м/с",
            SpeedPercent = 69.0,
            LoreSummaryRu = "Страж порядка с алхимическими склянками, сковывающий противников и поглощающий их силу.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/warden_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/warden_full.png",
            ModCount = 21
        };
        warden.Features.Add("Алхимическая склянка с замедлением");
        warden.Features.Add("Оковы закона, оглушающие цель");
        warden.Features.Add("Импульс вытягивания жизненной силы");
        warden.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Стойкость закона", IconEmoji = "⚖", Description = "Дает щит при ударе по врагам." });
        warden.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Алхимическая склянка", IconEmoji = "🧪", Description = "Замедляет и снижает урон врагов." });
        warden.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Щит воли", IconEmoji = "🛡", Description = "Поглощает урон и дает ускорение." });
        warden.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Оковы", IconEmoji = "⛓", Description = "Оглушает врага, если он не сбежал." });
        warden.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Карающий импульс", IconEmoji = "💥", Description = "Высасывает здоровье у всех врагов вокруг." });
        AllHeroes.Add(warden);

        // Mirage
        var mirage = new HeroItemViewModel(SelectHero)
        {
            Name = "Mirage",
            Title = "MIRAGE",
            Epithet = "The Desert Wanderer",
            HeroCode = "mirage",
            RoleType = "Assault",
            RoleRu = "Атакующий",
            RoleEn = "Assault",
            RoleIcon = "🔥",
            BadgeColor = "#EF4444",
            Hp = 550,
            Stamina = 3.0,
            Speed = "7.2 м/с",
            SpeedPercent = 72.0,
            LoreSummaryRu = "Странник пустыни, призывающий торнадо, песчаных скарабеев и телепортирующийся к союзникам.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/mirage_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/mirage_full.png",
            ModCount = 18
        };
        mirage.Features.Add("Торнадо, поднимающее врагов в воздух");
        mirage.Features.Add("Песчаные скарабеи с кражей здоровья");
        mirage.Features.Add("Глобальная телепортация к союзнику");
        mirage.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Джинный шаг", IconEmoji = "🏜", Description = "Увеличивает мобильность в бою." });
        mirage.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Торнадо", IconEmoji = "🌪", Description = "Поднимает и оглушает врагов." });
        mirage.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Огненные скарабеи", IconEmoji = "🪲", Description = "Крадут здоровье и замедляют." });
        mirage.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Песчаный вихрь", IconEmoji = "💨", Description = "Ослепляет область выстрелов." });
        mirage.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Путешественник", IconEmoji = "✨", Description = "Телепортируется к любому союзнику на карте." });
        AllHeroes.Add(mirage);

        // Pocket
        var pocket = new HeroItemViewModel(SelectHero)
        {
            Name = "Pocket",
            Title = "POCKET",
            Epithet = "The Briefcase Prodigy",
            HeroCode = "synth",
            RoleType = "Control",
            RoleRu = "Контроль",
            RoleEn = "Control",
            RoleIcon = "👁",
            BadgeColor = "#8B5CF6",
            Hp = 500,
            Stamina = 3.2,
            Speed = "7.2 м/с",
            SpeedPercent = 72.0,
            LoreSummaryRu = "Неуловимый дуэлянт с волшебным чемоданом, скрывающийся от любого урона и заражающий аффликцией.",
            CardPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/synth_card.png",
            FullPortraitUrl = "avares://DeadModer.UI/Assets/Heroes/synth_full.png",
            ModCount = 25
        };
        pocket.Features.Add("Прячется в чемодан, избегая любого урона");
        pocket.Features.Add("Телепортация через летящий плащ");
        pocket.Features.Add("Аффликция с нарастающим периодическим уроном");
        pocket.Abilities.Add(new HeroAbilityViewModel { Slot = "Пассивная", Name = "Карманное измерение", IconEmoji = "💼", Description = "Временный иммунитет к урону." });
        pocket.Abilities.Add(new HeroAbilityViewModel { Slot = "1", Name = "Шквал барража", IconEmoji = "💥", Description = "Залп кинетических снарядов." });
        pocket.Abilities.Add(new HeroAbilityViewModel { Slot = "2", Name = "Летящий плащ", IconEmoji = "🧥", Description = "Телепортируется к точке полета плаща." });
        pocket.Abilities.Add(new HeroAbilityViewModel { Slot = "3", Name = "Укрытие в чемодане", IconEmoji = "🧳", Description = "Прячется в чемодан, взрываясь при выходе." });
        pocket.Abilities.Add(new HeroAbilityViewModel { Slot = "Ульта", Name = "Аффликция", IconEmoji = "🧪", Description = "Заражает врагов ядом с уроном в % от HP." });
        AllHeroes.Add(pocket);
    }
}
