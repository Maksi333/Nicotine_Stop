using Microsoft.Maui.Graphics;

namespace Nicotine_Stop.Controls;

public enum PuffExpression { Normal, Happy, Thinking }

/// <summary>
/// "Puff" — the SnusStop mascot, drawn from primitives so it scales crisply and needs no image
/// asset. Ships as the real brand mascot; swap the drawable for illustrated art later if desired.
/// </summary>
public class PuffMascot : GraphicsView
{
    public static readonly BindableProperty ExpressionProperty = BindableProperty.Create(
        nameof(Expression), typeof(PuffExpression), typeof(PuffMascot), PuffExpression.Normal, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty CheeksProperty = BindableProperty.Create(
        nameof(Cheeks), typeof(bool), typeof(PuffMascot), false, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty FloatProperty = BindableProperty.Create(
        nameof(Float), typeof(bool), typeof(PuffMascot), false, propertyChanged: OnFloatChanged);

    public static readonly BindableProperty DiameterProperty = BindableProperty.Create(
        nameof(Diameter), typeof(double), typeof(PuffMascot), 120d, propertyChanged: OnDiameterChanged);

    private readonly PuffDrawable _drawable = new();
    private bool _floating;

    public PuffMascot()
    {
        Drawable = _drawable;
        WidthRequest = 120;
        HeightRequest = 120;
        BackgroundColor = Colors.Transparent;
        Loaded += (_, _) => { if (Float) StartFloat(); };
        Unloaded += (_, _) => _floating = false;
    }

    public PuffExpression Expression { get => (PuffExpression)GetValue(ExpressionProperty); set => SetValue(ExpressionProperty, value); }
    public bool Cheeks { get => (bool)GetValue(CheeksProperty); set => SetValue(CheeksProperty, value); }
    public bool Float { get => (bool)GetValue(FloatProperty); set => SetValue(FloatProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }

    private static void OnVisualChanged(BindableObject b, object o, object n)
    {
        var m = (PuffMascot)b;
        m._drawable.Expression = m.Expression;
        m._drawable.Cheeks = m.Cheeks;
        m.Invalidate();
    }

    private static void OnDiameterChanged(BindableObject b, object o, object n)
    {
        var m = (PuffMascot)b;
        m.WidthRequest = m.Diameter;
        m.HeightRequest = m.Diameter;
        m.Invalidate();
    }

    private static void OnFloatChanged(BindableObject b, object o, object n)
    {
        var m = (PuffMascot)b;
        if (m.Float && m.IsLoaded) m.StartFloat();
        else m._floating = false;
    }

    private async void StartFloat()
    {
        if (_floating) return;
        _floating = true;
        try
        {
            while (_floating)
            {
                await this.TranslateTo(0, -7, 1600, Easing.SinInOut);
                await this.TranslateTo(0, 0, 1600, Easing.SinInOut);
            }
        }
        catch { /* view torn down mid-animation */ }
    }
}

public class PuffDrawable : IDrawable
{
    public PuffExpression Expression { get; set; } = PuffExpression.Normal;
    public bool Cheeks { get; set; }

    private static readonly Color Mint = Color.FromArgb("#BDF0D4");
    private static readonly Color Ink = Color.FromArgb("#17322B");
    private static readonly Color Cheek = Color.FromArgb("#FFB1A6");
    private static readonly Color Shade = Color.FromRgba(20, 179, 107, 46);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float d = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (d <= 0) return;
        float ox = dirtyRect.X + (dirtyRect.Width - d) / 2f;
        float oy = dirtyRect.Y + (dirtyRect.Height - d) / 2f;
        float X(float f) => ox + f * d;
        float Y(float f) => oy + f * d;
        float S(float f) => f * d;

        // Face
        canvas.FillColor = Mint;
        canvas.FillEllipse(ox, oy, d, d);

        // Soft inset shade at the bottom, clipped to the face
        var face = new PathF();
        face.AppendCircle(ox + d / 2f, oy + d / 2f, d / 2f);
        canvas.SaveState();
        canvas.ClipPath(face);
        canvas.FillColor = Shade;
        canvas.FillEllipse(ox - d * 0.1f, oy + d * 0.66f, d * 1.2f, d * 0.5f);
        canvas.RestoreState();

        // Cheeks
        if (Cheeks)
        {
            canvas.FillColor = Cheek.WithAlpha(0.85f);
            canvas.FillEllipse(X(0.15f), Y(0.55f), S(0.14f), S(0.09f));
            canvas.FillEllipse(X(0.71f), Y(0.55f), S(0.14f), S(0.09f));
        }

        // Eyes
        canvas.StrokeLineCap = LineCap.Round;
        if (Expression == PuffExpression.Happy)
        {
            DrawHappyEye(canvas, X(0.27f), Y(0.45f), S(0.14f), S(0.08f));
            DrawHappyEye(canvas, X(0.59f), Y(0.45f), S(0.14f), S(0.08f));
        }
        else
        {
            canvas.FillColor = Ink;
            canvas.FillEllipse(X(0.27f), Y(0.37f), S(0.11f), S(0.14f));
            canvas.FillEllipse(X(0.62f), Y(0.37f), S(0.11f), S(0.14f));
        }

        // Smile (∪)
        canvas.StrokeColor = Ink;
        canvas.StrokeSize = S(0.035f);
        var smile = new PathF();
        smile.MoveTo(X(0.38f), Y(0.60f));
        smile.QuadTo(X(0.50f), Y(0.74f), X(0.62f), Y(0.60f));
        canvas.DrawPath(smile);
    }

    private static void DrawHappyEye(ICanvas canvas, float x, float y, float w, float h)
    {
        canvas.StrokeColor = Ink;
        canvas.StrokeSize = h * 0.7f;
        var p = new PathF();
        p.MoveTo(x, y + h);
        p.QuadTo(x + w / 2f, y - h * 0.4f, x + w, y + h);
        canvas.DrawPath(p);
    }
}
