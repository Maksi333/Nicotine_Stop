using Microsoft.Maui.Graphics;

namespace Nicotine_Stop.Controls;

/// <summary>Static scatter of celebration confetti (frames 1g / 1t). Purely decorative.</summary>
public class ConfettiView : GraphicsView
{
    public ConfettiView()
    {
        Drawable = new ConfettiDrawable();
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;
    }

    private sealed class ConfettiDrawable : IDrawable
    {
        private static readonly Color[] Palette =
        {
            Color.FromArgb("#FFB627"), Color.FromArgb("#FF6B5E"), Color.FromArgb("#8B7CF6"),
            Color.FromArgb("#BDF0D4"), Color.FromArgb("#4C9EF5"), Colors.White,
        };

        private readonly record struct Piece(float Fx, float Fy, int Color, float Size, float Angle, bool Round);
        private readonly Piece[] _pieces;

        public ConfettiDrawable()
        {
            var rng = new Random(20260713);
            _pieces = new Piece[40];
            for (int i = 0; i < _pieces.Length; i++)
            {
                _pieces[i] = new Piece(
                    (float)rng.NextDouble(),
                    (float)(rng.NextDouble() * 0.55),         // upper portion of the screen
                    rng.Next(Palette.Length),
                    6 + (float)rng.NextDouble() * 6,
                    (float)(rng.NextDouble() * 360),
                    rng.NextDouble() < 0.35);
            }
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            foreach (var p in _pieces)
            {
                float x = rect.X + p.Fx * rect.Width;
                float y = rect.Y + p.Fy * rect.Height;
                canvas.FillColor = Palette[p.Color];
                if (p.Round)
                {
                    canvas.FillCircle(x, y, p.Size * 0.4f);
                }
                else
                {
                    canvas.SaveState();
                    canvas.Rotate(p.Angle, x, y);
                    canvas.FillRoundedRectangle(x - p.Size * 0.3f, y - p.Size * 0.6f, p.Size * 0.6f, p.Size * 1.2f, 2);
                    canvas.RestoreState();
                }
            }
        }
    }
}
