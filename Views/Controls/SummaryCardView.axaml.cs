using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HowsMyMoney.Views.Controls;

/// <summary>
/// Tarjeta de resumen del Dashboard (Total Inversiones, Total Invertido, etc.).
/// Diseño "minimal con borde de acento": fondo oscuro uniforme consistente con el
/// resto de la app, un borde de color fino por categoría en vez de un degradado
/// completo, número grande como protagonista. Es una sola definición parametrizable
/// por propiedades, así que las 4 tarjetas comparten exactamente el mismo look.
/// </summary>
public partial class SummaryCardView : UserControl
{
    public static readonly StyledProperty<string?> IconDataProperty =
        AvaloniaProperty.Register<SummaryCardView, string?>(nameof(IconData));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SummaryCardView, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<SummaryCardView, string?>(nameof(Value));

    /// <summary>Texto chico opcional junto al valor (ej. "(+19.8%)").</summary>
    public static readonly StyledProperty<string?> TrendProperty =
        AvaloniaProperty.Register<SummaryCardView, string?>(nameof(Trend));

    /// <summary>Color del borde izquierdo y del ícono.</summary>
    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<SummaryCardView, IBrush?>(nameof(AccentBrush));

    /// <summary>Color del valor principal y del trend (blanco por defecto; verde/rojo para ganancia-pérdida).</summary>
    public static readonly StyledProperty<IBrush?> ValueBrushProperty =
        AvaloniaProperty.Register<SummaryCardView, IBrush?>(nameof(ValueBrush), Brushes.White);

    public string? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Trend
    {
        get => GetValue(TrendProperty);
        set => SetValue(TrendProperty, value);
    }

    public IBrush? AccentBrush
    {
        get => GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public SummaryCardView()
    {
        InitializeComponent();
    }
}
