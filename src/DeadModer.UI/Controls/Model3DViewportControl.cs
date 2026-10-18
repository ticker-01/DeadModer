using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace DeadModer.UI.Controls;

/// <summary>
/// Профессиональный трехмерный интерактивный вьюпорт моделей персонажей Deadlock в стиле Source 2 ModelDoc.
/// Обеспечивает:
/// - Свободное вращение 360° мышью (по горизонтали и вертикали)
/// - Автоматический режим вращения тумбы (Turntable Auto-rotate)
/// - Масштабирование колесиком мыши (Zoom)
/// - Процедурные детализированные меши героев Source 2 с динамическим вычислением нормалей
/// - Двустороннее студийное освещение (Key + Rim Light) в стиле ModelDoc Studio
/// - Переключение режимов: Каркас (Wireframe), Студийный рендер (Shading), Освещение
/// - Круглая платформа-пьедестал Source 2 с радиальной разметкой
/// </summary>
public class Model3DViewportControl : Control
{
    public static readonly StyledProperty<double> RotationYProperty =
        AvaloniaProperty.Register<Model3DViewportControl, double>(nameof(RotationY), 35.0);

    public static readonly StyledProperty<double> RotationXProperty =
        AvaloniaProperty.Register<Model3DViewportControl, double>(nameof(RotationX), 12.0);

    public static readonly StyledProperty<bool> IsWireframeProperty =
        AvaloniaProperty.Register<Model3DViewportControl, bool>(nameof(IsWireframe), false);

    public static readonly StyledProperty<bool> IsLightingProperty =
        AvaloniaProperty.Register<Model3DViewportControl, bool>(nameof(IsLighting), true);

    public static readonly StyledProperty<string> AccentColorHexProperty =
        AvaloniaProperty.Register<Model3DViewportControl, string>(nameof(AccentColorHex), "#38BDF8");

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<Model3DViewportControl, double>(nameof(Zoom), 1.0);

    public static readonly StyledProperty<string> HeroNameProperty =
        AvaloniaProperty.Register<Model3DViewportControl, string>(nameof(HeroName), string.Empty);

    public static readonly StyledProperty<bool> IsAutoRotatingProperty =
        AvaloniaProperty.Register<Model3DViewportControl, bool>(nameof(IsAutoRotating), false);

    public double RotationY
    {
        get => GetValue(RotationYProperty);
        set => SetValue(RotationYProperty, value);
    }

    public double RotationX
    {
        get => GetValue(RotationXProperty);
        set => SetValue(RotationXProperty, value);
    }

    public bool IsWireframe
    {
        get => GetValue(IsWireframeProperty);
        set => SetValue(IsWireframeProperty, value);
    }

    public bool IsLighting
    {
        get => GetValue(IsLightingProperty);
        set => SetValue(IsLightingProperty, value);
    }

    public string AccentColorHex
    {
        get => GetValue(AccentColorHexProperty);
        set => SetValue(AccentColorHexProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public string HeroName
    {
        get => GetValue(HeroNameProperty);
        set => SetValue(HeroNameProperty, value);
    }

    public bool IsAutoRotating
    {
        get => GetValue(IsAutoRotatingProperty);
        set => SetValue(IsAutoRotatingProperty, value);
    }

    private bool _isDragging;
    private Point _lastPointerPosition;
    private readonly DispatcherTimer _autoRotateTimer;

    static Model3DViewportControl()
    {
        AffectsRender<Model3DViewportControl>(
            RotationYProperty,
            RotationXProperty,
            IsWireframeProperty,
            IsLightingProperty,
            AccentColorHexProperty,
            ZoomProperty,
            HeroNameProperty,
            IsAutoRotatingProperty);
    }

    public Model3DViewportControl()
    {
        ClipToBounds = true;

        _autoRotateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS для плавного вращения
        };
        _autoRotateTimer.Tick += (s, e) =>
        {
            if (IsAutoRotating && !_isDragging)
            {
                RotationY = (RotationY + 0.8) % 360.0;
                InvalidateVisual();
            }
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsAutoRotating && !_autoRotateTimer.IsEnabled)
        {
            _autoRotateTimer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _autoRotateTimer.Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsAutoRotatingProperty)
        {
            if (IsAutoRotating)
            {
                if (VisualRoot != null && !_autoRotateTimer.IsEnabled)
                    _autoRotateTimer.Start();
            }
            else
            {
                _autoRotateTimer.Stop();
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0 ? 300 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) || availableSize.Height <= 0 ? 280 : availableSize.Height;
        return new Size(width, height);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var newZoom = Math.Clamp(Zoom + e.Delta.Y * 0.12, 0.45, 2.8);
        Zoom = newZoom;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var currentPoint = e.GetCurrentPoint(this);
        if (currentPoint.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastPointerPosition = currentPoint.Position;
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            var pos = e.GetPosition(this);
            var deltaX = pos.X - _lastPointerPosition.X;
            var deltaY = pos.Y - _lastPointerPosition.Y;

            RotationY = (RotationY + deltaX * 0.7) % 360.0;
            if (RotationY < 0) RotationY += 360.0;

            RotationX = Math.Clamp(RotationX - deltaY * 0.45, -60.0, 60.0);

            _lastPointerPosition = pos;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
        }
    }

    public struct Vertex3D
    {
        public double X, Y, Z;
        public Vertex3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    public enum FaceMaterialType
    {
        ArmorPrimary,     // Основная броня (темный графит)
        ArmorSecondary,   // Вторичные пластины (сталь)
        VisorFace,        // Визор / лицо
        AccentGlow,       // Энергетические элементы (подсветка героя)
        ClothCape,        // Плащ / крылья
        WeaponMetal       // Оружие
    }

    public class PolygonFace
    {
        public int[] Indices { get; }
        public FaceMaterialType Material { get; }

        public PolygonFace(int[] indices, FaceMaterialType material = FaceMaterialType.ArmorPrimary)
        {
            Indices = indices;
            Material = material;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 10 || bounds.Height <= 10) return;

        var cx = bounds.Width / 2.0;
        var cy = bounds.Height * 0.54; // Смещение чуть ниже центра для идеального позиционирования персонажа
        var scale = Math.Min(bounds.Width, bounds.Height) * 0.32 * Math.Clamp(Zoom, 0.45, 2.8);

        // 1. Градиентный фон Source 2 ModelDoc студии
        var bgBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0x0C, 0x10, 0x1C), 0.0),
                new GradientStop(Color.FromRgb(0x07, 0x09, 0x12), 1.0)
            }
        };
        context.DrawRectangle(bgBrush, null, new Rect(0, 0, bounds.Width, bounds.Height));

        // Вычисляем углы в радианах (модель обращена лицом к зрителю при исходном угле)
        var radY = (RotationY + 180.0) * Math.PI / 180.0;
        var radX = RotationX * Math.PI / 180.0;

        var cosY = Math.Cos(radY);
        var sinY = Math.Sin(radY);
        var cosX = Math.Cos(radX);
        var sinX = Math.Sin(radX);

        // Парсинг акцентного цвета героя
        Color accentColor;
        try { accentColor = Color.Parse(AccentColorHex); }
        catch { accentColor = Color.FromRgb(0x38, 0xBD, 0xF8); }

        var wirePen = new Pen(new SolidColorBrush(Color.FromArgb(210, 0xFF, 0xFF, 0xFF)), 1.1);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 0x94, 0xA3, 0xB8)), 0.8);
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 0x38, 0xBD, 0xF8)), 1.1);

        // 2. Отрисовка поворотной платформы Source 2 ModelDoc
        DrawModelDocTurntable(context, cx, cy, scale, cosY, sinY, cosX, sinX, gridPen, axisPen, accentColor);

        // 3. Получение меша героя с учетом анатомии и экипировки
        var (vertices, faces) = GenerateDeadlockHeroModel(HeroName);

        // 4. Трансформация 3D-вершин в экранные координаты с перспективной проекцией
        var transformedVertices = new Point[vertices.Length];
        var cameraSpaceVertices = new Vertex3D[vertices.Length];
        var depths = new double[vertices.Length];
        const double cameraDistance = 5.2;

        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];

            // Вращение вокруг оси Y
            var x1 = v.X * cosY + v.Z * sinY;
            var z1 = -v.X * sinY + v.Z * cosY;

            // Вращение вокруг оси X
            var y2 = v.Y * cosX - z1 * sinX;
            var z2 = v.Y * sinX + z1 * cosX;

            cameraSpaceVertices[i] = new Vertex3D(x1, y2, z2);
            depths[i] = z2;

            // Перспективная проекция
            var proj = cameraDistance / (cameraDistance + z2);
            transformedVertices[i] = new Point(cx + x1 * scale * proj, cy - y2 * scale * proj);
        }

        // 5. Отрисовка геометрии
        if (IsWireframe)
        {
            // РЕЖИМ СЕТКИ (Source 2 Wireframe Mode)
            var drawnEdges = new HashSet<long>();
            foreach (var face in faces)
            {
                for (var j = 0; j < face.Indices.Length; j++)
                {
                    var a = face.Indices[j];
                    var b = face.Indices[(j + 1) % face.Indices.Length];
                    var edgeKey = ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);

                    if (drawnEdges.Add(edgeKey))
                    {
                        var edgeColor = face.Material == FaceMaterialType.AccentGlow
                            ? accentColor
                            : Color.FromArgb(190, 0x94, 0xA3, 0xB8);
                        var pen = new Pen(new SolidColorBrush(edgeColor), 0.9);
                        context.DrawLine(pen, transformedVertices[a], transformedVertices[b]);
                    }
                }
            }

            // Узловые точки сетки (Vertices)
            var vertexBrush = new SolidColorBrush(Color.FromArgb(230, 0xFF, 0xFF, 0xFF));
            for (var i = 0; i < transformedVertices.Length; i += 2)
            {
                context.DrawEllipse(vertexBrush, null, transformedVertices[i], 1.2, 1.2);
            }
        }
        else
        {
            // РЕЖИМ ТВЕРДОТЕЛЬНОГО РЕНДЕРИНГА СО СТУДИЙНЫМ ШЕЙДИНГОМ
            var sortedFaces = new List<(PolygonFace Face, double AvgDepth, Vertex3D Normal, bool IsFrontFacing)>();

            foreach (var face in faces)
            {
                if (face.Indices.Length < 3) continue;

                double sumDepth = 0;
                foreach (var idx in face.Indices)
                {
                    sumDepth += depths[idx];
                }
                var avgDepth = sumDepth / face.Indices.Length;

                // Вычисляем истинную геометрическую нормаль грани
                var p0 = cameraSpaceVertices[face.Indices[0]];
                var p1 = cameraSpaceVertices[face.Indices[1]];
                var p2 = cameraSpaceVertices[face.Indices[2]];

                var ax = p1.X - p0.X; var ay = p1.Y - p0.Y; var az = p1.Z - p0.Z;
                var bx = p2.X - p0.X; var by = p2.Y - p0.Y; var bz = p2.Z - p0.Z;
                var nx = ay * bz - az * by;
                var ny = az * bx - ax * bz;
                var nz = ax * by - ay * bx;
                var len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (len > 1e-6) { nx /= len; ny /= len; nz /= len; }
                else { nx = 0; ny = 0; nz = 1; }

                var normal = new Vertex3D(nx, ny, nz);
                var isFrontFacing = nz >= -0.05;

                sortedFaces.Add((face, avgDepth, normal, isFrontFacing));
            }

            // Сортировка по удаленности от камеры (Painter's Algorithm)
            sortedFaces.Sort((a, b) => b.AvgDepth.CompareTo(a.AvgDepth));

            // Источники света студии Source 2
            var keyLight = new Vertex3D(0.577, 0.707, 0.408);   // Основной студийный свет (сверху-справа)
            var rimLight = new Vertex3D(-0.577, 0.300, -0.745);  // Контурный контрсвет (сзади-слева)

            foreach (var (face, _, normal, isFrontFacing) in sortedFaces)
            {
                // Расчет диффузного освещения студии (двустороннее затенение для предотвращения "дыр")
                var dotKey = Math.Abs(normal.X * keyLight.X + normal.Y * keyLight.Y + normal.Z * keyLight.Z);
                var dotRim = Math.Abs(normal.X * rimLight.X + normal.Y * rimLight.Y + normal.Z * rimLight.Z);

                var totalLight = IsLighting ? Math.Clamp(0.32 + dotKey * 0.65 + dotRim * 0.22, 0.22, 1.15) : 0.85;
                if (!isFrontFacing) totalLight *= 0.72; // Мягкое контурное затемнение обратных граней

                // Определение цвета материала
                Color baseColor;
                switch (face.Material)
                {
                    case FaceMaterialType.ArmorPrimary:
                        baseColor = Color.FromRgb(0x23, 0x2C, 0x3D); // Темный графит брони
                        break;
                    case FaceMaterialType.ArmorSecondary:
                        baseColor = Color.FromRgb(0x3E, 0x4B, 0x62); // Стальные пластины
                        break;
                    case FaceMaterialType.VisorFace:
                        baseColor = Color.FromRgb(0xD0, 0xD7, 0xE2); // Лицо / полированный визор
                        break;
                    case FaceMaterialType.AccentGlow:
                        baseColor = accentColor;                     // Неоновая энергия героя
                        totalLight = Math.Max(totalLight, 0.75);
                        break;
                    case FaceMaterialType.ClothCape:
                        baseColor = Color.FromRgb(0x1B, 0x22, 0x30); // Плащ / крылья
                        break;
                    case FaceMaterialType.WeaponMetal:
                        baseColor = Color.FromRgb(0x47, 0x55, 0x69); // Оружейная сталь
                        break;
                    default:
                        baseColor = Color.FromRgb(0x28, 0x33, 0x47);
                        break;
                }

                byte r = (byte)Math.Clamp(baseColor.R * totalLight, 10, 255);
                byte g = (byte)Math.Clamp(baseColor.G * totalLight, 10, 255);
                byte b = (byte)Math.Clamp(baseColor.B * totalLight, 10, 255);

                var fillBrush = new SolidColorBrush(Color.FromRgb(r, g, b));
                var strokePen = new Pen(new SolidColorBrush(Color.FromArgb(40, 0x0F, 0x17, 0x2A)), 0.6);

                var streamGeometry = new StreamGeometry();
                using (var gc = streamGeometry.Open())
                {
                    gc.BeginFigure(transformedVertices[face.Indices[0]], true);
                    for (var k = 1; k < face.Indices.Length; k++)
                    {
                        gc.LineTo(transformedVertices[face.Indices[k]]);
                    }
                    gc.EndFigure(true);
                }

                context.DrawGeometry(fillBrush, strokePen, streamGeometry);
            }
        }

        // 6. HUD видоискатель и угловые засечки в стиле Source 2 ModelDoc
        DrawHUDViewportDetails(context, bounds, RotationY, RotationX, HeroName);
    }

    private void DrawModelDocTurntable(
        DrawingContext context,
        double cx, double cy, double scale,
        double cosY, double sinY, double cosX, double sinX,
        Pen gridPen, Pen axisPen, Color accentColor)
    {
        const double rOuter = 1.35;
        const double rInner = 0.90;
        const int segments = 32;
        const double baseY = -1.18;

        // Внешний и внутренний круги платформы
        var outerPoints = new Point[segments];
        var innerPoints = new Point[segments];

        for (var i = 0; i < segments; i++)
        {
            var angle = i * 2.0 * Math.PI / segments;
            var px = rOuter * Math.Cos(angle);
            var pz = rOuter * Math.Sin(angle);
            outerPoints[i] = ProjectPoint(px, baseY, pz, cx, cy, scale, cosY, sinY, cosX, sinX);

            var ipx = rInner * Math.Cos(angle);
            var ipz = rInner * Math.Sin(angle);
            innerPoints[i] = ProjectPoint(ipx, baseY, ipz, cx, cy, scale, cosY, sinY, cosX, sinX);
        }

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            context.DrawLine(gridPen, outerPoints[i], outerPoints[next]);
            context.DrawLine(gridPen, innerPoints[i], innerPoints[next]);
        }

        // Осевые направляющие платформы (X и Z)
        var pX1 = ProjectPoint(-rOuter, baseY, 0, cx, cy, scale, cosY, sinY, cosX, sinX);
        var pX2 = ProjectPoint(rOuter, baseY, 0, cx, cy, scale, cosY, sinY, cosX, sinX);
        var pZ1 = ProjectPoint(0, baseY, -rOuter, cx, cy, scale, cosY, sinY, cosX, sinX);
        var pZ2 = ProjectPoint(0, baseY, rOuter, cx, cy, scale, cosY, sinY, cosX, sinX);

        context.DrawLine(axisPen, pX1, pX2);
        context.DrawLine(axisPen, pZ1, pZ2);

        // Мягкая тень от персонажа на платформе
        var shadowCenter = ProjectPoint(0, baseY, 0, cx, cy, scale, cosY, sinY, cosX, sinX);
        var shadowBrush = new RadialGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.FromArgb(90, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
            }
        };
        context.DrawEllipse(shadowBrush, null, shadowCenter, scale * 0.75, scale * 0.35);
    }

    private static Point ProjectPoint(
        double px, double py, double pz,
        double cx, double cy, double scale,
        double cosY, double sinY, double cosX, double sinX)
    {
        var x1 = px * cosY + pz * sinY;
        var z1 = -px * sinY + pz * cosY;
        var y2 = py * cosX - z1 * sinX;
        var z2 = py * sinX + z1 * cosX;
        var proj = 5.2 / (5.2 + z2);
        return new Point(cx + x1 * scale * proj, cy - y2 * scale * proj);
    }

    private static void DrawHUDViewportDetails(DrawingContext context, Rect bounds, double rotY, double rotX, string heroName)
    {
        // Угловые скобки видоискателя
        var cornerPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 0xFF, 0xFF, 0xFF)), 1.2);
        const double m = 12.0;
        const double l = 12.0;

        // Верхний левый
        context.DrawLine(cornerPen, new Point(m, m), new Point(m + l, m));
        context.DrawLine(cornerPen, new Point(m, m), new Point(m, m + l));

        // Верхний правый
        context.DrawLine(cornerPen, new Point(bounds.Width - m, m), new Point(bounds.Width - m - l, m));
        context.DrawLine(cornerPen, new Point(bounds.Width - m, m), new Point(bounds.Width - m, m + l));

        // Нижний левый
        context.DrawLine(cornerPen, new Point(m, bounds.Height - m), new Point(m + l, bounds.Height - m));
        context.DrawLine(cornerPen, new Point(m, bounds.Height - m), new Point(m, bounds.Height - m - l));

        // Нижний правый
        context.DrawLine(cornerPen, new Point(bounds.Width - m, bounds.Height - m), new Point(bounds.Width - m - l, bounds.Height - m));
        context.DrawLine(cornerPen, new Point(bounds.Width - m, bounds.Height - m), new Point(bounds.Width - m, bounds.Height - m - l));
    }

    /// <summary>
    /// Генерирует анатомический меш персонажа Deadlock с учетом конкретного героя:
    /// Голова, головной убор, торс, наплечники, крылья/плащ, руки с оружием, пояс, бедра, колени и сапоги.
    /// </summary>
    private static (Vertex3D[] Vertices, PolygonFace[] Faces) GenerateDeadlockHeroModel(string? heroName = null)
    {
        var v = new List<Vertex3D>();
        var f = new List<PolygonFace>();

        var name = (heroName ?? string.Empty).ToLowerInvariant();

        // -------------------------------------------------------------
        // 1. ГОЛОВА И ШЛЕМ / ШЛЯПА
        // -------------------------------------------------------------
        var headBase = v.Count;
        v.Add(new Vertex3D(0.0, 1.26, 0.14));    // 0: нос / лицевая часть
        v.Add(new Vertex3D(-0.16, 1.38, 0.10));  // 1: висок левый
        v.Add(new Vertex3D(0.16, 1.38, 0.10));   // 2: висок правый
        v.Add(new Vertex3D(0.0, 1.54, 0.04));    // 3: макушка
        v.Add(new Vertex3D(0.0, 1.14, 0.10));    // 4: подбородок
        v.Add(new Vertex3D(-0.15, 1.36, -0.14)); // 5: затылок левый
        v.Add(new Vertex3D(0.15, 1.36, -0.14));  // 6: затылок правый
        v.Add(new Vertex3D(0.0, 1.52, -0.10));   // 7: макушка сзади

        f.Add(new PolygonFace([headBase + 4, headBase + 0, headBase + 1], FaceMaterialType.VisorFace));
        f.Add(new PolygonFace([headBase + 4, headBase + 2, headBase + 0], FaceMaterialType.VisorFace));
        f.Add(new PolygonFace([headBase + 0, headBase + 3, headBase + 1], FaceMaterialType.VisorFace));
        f.Add(new PolygonFace([headBase + 0, headBase + 2, headBase + 3], FaceMaterialType.VisorFace));
        f.Add(new PolygonFace([headBase + 1, headBase + 3, headBase + 7, headBase + 5], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([headBase + 3, headBase + 2, headBase + 6, headBase + 7], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([headBase + 5, headBase + 7, headBase + 6], FaceMaterialType.ArmorSecondary));

        // Головные уборы и особенности героев:
        if (name.Contains("vindicta") || name.Contains("hornet"))
        {
            // Остроконечный капюшон / ведьминский силуэт Vindicta
            var hoodTop = v.Count;
            v.Add(new Vertex3D(0.0, 1.76, -0.06)); // Пик капюшона
            v.Add(new Vertex3D(-0.24, 1.48, 0.05)); // Левый край капюшона
            v.Add(new Vertex3D(0.24, 1.48, 0.05));  // Правый край капюшона
            f.Add(new PolygonFace([headBase + 3, hoodTop + 1, hoodTop + 0], FaceMaterialType.ClothCape));
            f.Add(new PolygonFace([headBase + 3, hoodTop + 0, hoodTop + 2], FaceMaterialType.ClothCape));
        }
        else if (name.Contains("infernus") || name.Contains("inferno"))
        {
            // Шляпа-федора и огненный гребень Infernus
            var hatBase = v.Count;
            v.Add(new Vertex3D(-0.30, 1.50, 0.04));
            v.Add(new Vertex3D(0.30, 1.50, 0.04));
            v.Add(new Vertex3D(0.0, 1.76, 0.02)); // Огненное пламя на шляпе
            f.Add(new PolygonFace([headBase + 3, hatBase + 0, hatBase + 2], FaceMaterialType.AccentGlow));
            f.Add(new PolygonFace([headBase + 3, hatBase + 2, hatBase + 1], FaceMaterialType.AccentGlow));
        }
        else if (name.Contains("abrams"))
        {
            // Изогнутые демонические рога Abrams
            var hornBase = v.Count;
            v.Add(new Vertex3D(-0.35, 1.72, -0.15)); // Левый рог
            v.Add(new Vertex3D(0.35, 1.72, -0.15));  // Правый рог
            f.Add(new PolygonFace([headBase + 1, headBase + 3, hornBase + 0], FaceMaterialType.ArmorPrimary));
            f.Add(new PolygonFace([headBase + 2, hornBase + 1, headBase + 3], FaceMaterialType.ArmorPrimary));
        }

        // -------------------------------------------------------------
        // 2. ШЕЯ И ПЛЕЧЕВОЙ ПОЯС
        // -------------------------------------------------------------
        double shoulderW = (name.Contains("abrams") || name.Contains("mo")) ? 0.52 : 0.38;
        double chestDepth = (name.Contains("abrams") || name.Contains("mo")) ? 0.28 : 0.20;

        var torsoBase = v.Count;
        v.Add(new Vertex3D(-shoulderW, 0.98, 0.14));        // 0: левое плечо спереди
        v.Add(new Vertex3D(shoulderW, 0.98, 0.14));         // 1: правое плечо спереди
        v.Add(new Vertex3D(-shoulderW * 0.88, 0.98, -0.16));// 2: левое плечо сзади
        v.Add(new Vertex3D(shoulderW * 0.88, 0.98, -0.16)); // 3: правое плечо сзади
        v.Add(new Vertex3D(0.0, 0.88, chestDepth));          // 4: центр грудины / ядро
        v.Add(new Vertex3D(-0.25, 0.38, 0.12));             // 5: талия левая перед
        v.Add(new Vertex3D(0.25, 0.38, 0.12));              // 6: талия правая перед
        v.Add(new Vertex3D(0.0, 0.34, 0.16));               // 7: пряжка ремня
        v.Add(new Vertex3D(-0.22, 0.38, -0.14));            // 8: талия левая зад
        v.Add(new Vertex3D(0.22, 0.38, -0.14));             // 9: талия правая зад

        // Нагрудник (Грудь)
        f.Add(new PolygonFace([torsoBase + 0, torsoBase + 4, torsoBase + 7, torsoBase + 5], FaceMaterialType.ArmorPrimary));
        f.Add(new PolygonFace([torsoBase + 4, torsoBase + 1, torsoBase + 6, torsoBase + 7], FaceMaterialType.ArmorPrimary));
        f.Add(new PolygonFace([torsoBase + 0, torsoBase + 1, torsoBase + 4], FaceMaterialType.ArmorSecondary));

        // Энергетический реактор / символ в центре груди
        f.Add(new PolygonFace([torsoBase + 4, torsoBase + 7, torsoBase + 6], FaceMaterialType.AccentGlow));

        // Спина и бока
        f.Add(new PolygonFace([torsoBase + 2, torsoBase + 3, torsoBase + 1, torsoBase + 0], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([torsoBase + 3, torsoBase + 2, torsoBase + 8, torsoBase + 9], FaceMaterialType.ArmorPrimary));
        f.Add(new PolygonFace([torsoBase + 0, torsoBase + 2, torsoBase + 8, torsoBase + 5], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([torsoBase + 1, torsoBase + 6, torsoBase + 9, torsoBase + 3], FaceMaterialType.ArmorSecondary));

        // -------------------------------------------------------------
        // 3. ОСОБЕННОСТИ СПИНЫ (КРЫЛЬЯ VINDICTA, БАЛЛОНЫ KELVIN, ПЛАЩ INFERNUS)
        // -------------------------------------------------------------
        if (name.Contains("vindicta") || name.Contains("hornet"))
        {
            // Эфирные призрачные крылья Vindicta (3-ярусные перья)
            var wingBase = v.Count;
            // Левое крыло
            v.Add(new Vertex3D(-0.85, 1.35, -0.22)); // 0: Верхнее перо
            v.Add(new Vertex3D(-1.05, 0.85, -0.25)); // 1: Среднее перо
            v.Add(new Vertex3D(-0.75, 0.40, -0.20)); // 2: Нижнее перо

            // Правое крыло
            v.Add(new Vertex3D(0.85, 1.35, -0.22));  // 3: Верхнее перо
            v.Add(new Vertex3D(1.05, 0.85, -0.25));  // 4: Среднее перо
            v.Add(new Vertex3D(0.75, 0.40, -0.20));  // 5: Нижнее перо

            // Левое крыло полигоны
            f.Add(new PolygonFace([torsoBase + 2, wingBase + 0, wingBase + 1], FaceMaterialType.AccentGlow));
            f.Add(new PolygonFace([torsoBase + 2, wingBase + 1, wingBase + 2], FaceMaterialType.ClothCape));
            f.Add(new PolygonFace([torsoBase + 8, torsoBase + 2, wingBase + 2], FaceMaterialType.ClothCape));

            // Правое крыло полигоны
            f.Add(new PolygonFace([torsoBase + 3, wingBase + 4, wingBase + 3], FaceMaterialType.AccentGlow));
            f.Add(new PolygonFace([torsoBase + 3, wingBase + 5, wingBase + 4], FaceMaterialType.ClothCape));
            f.Add(new PolygonFace([torsoBase + 9, wingBase + 5, torsoBase + 3], FaceMaterialType.ClothCape));
        }
        else if (name.Contains("kelvin"))
        {
            // Криогенный баллон на спине Kelvin
            var tankBase = v.Count;
            v.Add(new Vertex3D(0.0, 1.05, -0.34));
            v.Add(new Vertex3D(-0.16, 0.55, -0.30));
            v.Add(new Vertex3D(0.16, 0.55, -0.30));
            f.Add(new PolygonFace([torsoBase + 2, torsoBase + 3, tankBase + 0], FaceMaterialType.ArmorSecondary));
            f.Add(new PolygonFace([tankBase + 0, tankBase + 1, tankBase + 2], FaceMaterialType.AccentGlow));
        }
        else if (name.Contains("infernus") || name.Contains("inferno"))
        {
            // Полы длинного плаща Infernus
            var coatBase = v.Count;
            v.Add(new Vertex3D(-0.35, -0.55, -0.18));
            v.Add(new Vertex3D(0.35, -0.55, -0.18));
            f.Add(new PolygonFace([torsoBase + 8, torsoBase + 9, coatBase + 1, coatBase + 0], FaceMaterialType.ClothCape));
        }

        // -------------------------------------------------------------
        // 4. РУКИ И ОРУЖИЕ
        // -------------------------------------------------------------
        var armsBase = v.Count;
        // Левая рука (Поддерживает оружие или в стойке)
        v.Add(new Vertex3D(-shoulderW - 0.16, 0.55, 0.08));  // 0: левый локоть
        v.Add(new Vertex3D(-shoulderW - 0.12, 0.18, 0.28));  // 1: левое запястье (вперед)
        v.Add(new Vertex3D(-shoulderW - 0.08, 0.12, 0.32));  // 2: левая кисть

        f.Add(new PolygonFace([torsoBase + 0, armsBase + 0, torsoBase + 2], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([armsBase + 0, armsBase + 1, armsBase + 2], FaceMaterialType.ArmorPrimary));

        // Правая рука (Держит оружие)
        v.Add(new Vertex3D(shoulderW + 0.18, 0.55, 0.08));   // 3: правый локоть
        v.Add(new Vertex3D(shoulderW + 0.10, 0.22, 0.30));   // 4: правое запястье
        v.Add(new Vertex3D(shoulderW + 0.08, 0.18, 0.35));   // 5: правая кисть

        f.Add(new PolygonFace([torsoBase + 1, torsoBase + 3, armsBase + 3], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([armsBase + 3, armsBase + 4, armsBase + 5], FaceMaterialType.ArmorPrimary));

        // Оружие в руках (Снайперская винтовка Vindicta, дробовик Abrams или револьверы)
        var weaponBase = v.Count;
        if (name.Contains("vindicta") || name.Contains("hornet"))
        {
            // Длинная снайперская винтовка Vindicta со снайперским прицелом
            v.Add(new Vertex3D(-0.45, 0.05, 0.15));  // 0: приклад
            v.Add(new Vertex3D(0.25, 0.32, 0.35));   // 1: ствольная коробка и прицел
            v.Add(new Vertex3D(0.85, 0.55, 0.55));   // 2: дуло винтовки
            f.Add(new PolygonFace([weaponBase + 0, weaponBase + 1, weaponBase + 2], FaceMaterialType.WeaponMetal));
            f.Add(new PolygonFace([weaponBase + 1, armsBase + 2, armsBase + 5], FaceMaterialType.AccentGlow));
        }
        else
        {
            // Боевой карабин / дробовик
            v.Add(new Vertex3D(0.05, 0.12, 0.22));
            v.Add(new Vertex3D(0.25, 0.20, 0.35));
            v.Add(new Vertex3D(0.65, 0.30, 0.52));
            f.Add(new PolygonFace([weaponBase + 0, weaponBase + 1, weaponBase + 2], FaceMaterialType.WeaponMetal));
        }

        // -------------------------------------------------------------
        // 5. ПОЯС, НОГИ И БОЕВЫЕ САПОГИ
        // -------------------------------------------------------------
        var legsBase = v.Count;
        v.Add(new Vertex3D(-0.18, -0.05, 0.06)); // 0: левое колено
        v.Add(new Vertex3D(0.18, -0.05, 0.06));  // 1: правое колено
        v.Add(new Vertex3D(-0.20, -0.65, 0.04)); // 2: левая голень
        v.Add(new Vertex3D(0.20, -0.65, 0.04));  // 3: правая голень

        // Сапоги (стоят на платформе baseY = -1.18)
        v.Add(new Vertex3D(-0.22, -1.16, 0.18)); // 4: левый носок сапога
        v.Add(new Vertex3D(0.22, -1.16, 0.18));  // 5: правый носок сапога
        v.Add(new Vertex3D(-0.22, -1.16, -0.08));// 6: левый каблук
        v.Add(new Vertex3D(0.22, -1.16, -0.08)); // 7: правый каблук

        // Бедра
        f.Add(new PolygonFace([torsoBase + 5, torsoBase + 7, legsBase + 0], FaceMaterialType.ArmorPrimary));
        f.Add(new PolygonFace([torsoBase + 7, torsoBase + 6, legsBase + 1], FaceMaterialType.ArmorPrimary));
        f.Add(new PolygonFace([torsoBase + 8, torsoBase + 5, legsBase + 0], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([torsoBase + 6, torsoBase + 9, legsBase + 1], FaceMaterialType.ArmorSecondary));

        // Наколенники и голени
        f.Add(new PolygonFace([legsBase + 0, legsBase + 2, legsBase + 4], FaceMaterialType.ArmorSecondary));
        f.Add(new PolygonFace([legsBase + 1, legsBase + 3, legsBase + 5], FaceMaterialType.ArmorSecondary));

        // Сапоги (Каблук и подошва)
        f.Add(new PolygonFace([legsBase + 2, legsBase + 6, legsBase + 4], FaceMaterialType.ArmorPrimary));
        f.Add(new PolygonFace([legsBase + 3, legsBase + 5, legsBase + 7], FaceMaterialType.ArmorPrimary));

        return (v.ToArray(), f.ToArray());
    }
}
