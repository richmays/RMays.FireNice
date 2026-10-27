using System.Drawing.Drawing2D;
using FireNice.Core;

namespace FireNice.UI;

/// <summary>
/// Draws the level, scaled to fit the panel, with simple shapes.
/// </summary>
public sealed class GamePanel : Panel
{
    private static readonly Color Background = Color.FromArgb(18, 20, 34);
    private static readonly Color AirColor = Color.FromArgb(30, 34, 56);
    private static readonly Color BlockColor = Color.FromArgb(104, 106, 122);
    private static readonly Color MortarColor = Color.FromArgb(72, 74, 88);
    private static readonly Color IceColor = Color.FromArgb(178, 226, 255);
    private static readonly Color IceEdgeColor = Color.FromArgb(96, 166, 226);
    private static readonly Color ClampColor = Color.FromArgb(34, 98, 180);
    private static readonly Color FlameOuter = Color.FromArgb(240, 84, 32);
    private static readonly Color FlameInner = Color.FromArgb(255, 210, 70);
    private static readonly Color JarUnlitBody = Color.FromArgb(70, 120, 214);
    private static readonly Color JarUnlitRim = Color.FromArgb(36, 66, 140);
    private static readonly Color JarLitBody = Color.FromArgb(214, 44, 30);
    private static readonly Color JarLitRim = Color.FromArgb(130, 24, 16);
    private static readonly Color JarLitGlow = Color.FromArgb(255, 140, 30);
    private static readonly Color CrateColor = Color.FromArgb(132, 140, 150);
    private static readonly Color CrateEdgeColor = Color.FromArgb(62, 66, 74);
    private static readonly Color CrateRivetColor = Color.FromArgb(190, 196, 204);
    private static readonly Color PipeColor = Color.FromArgb(70, 178, 110);
    private static readonly Color PipeLitColor = Color.FromArgb(130, 230, 160);
    private static readonly Color PipeRimColor = Color.FromArgb(52, 140, 86);
    private static readonly Color PipeEdgeColor = Color.FromArgb(24, 70, 42);
    private static readonly Color CoatColor = Color.FromArgb(64, 170, 96);
    private static readonly Color SkinColor = Color.FromArgb(250, 214, 170);
    private static readonly Color DeadColor = Color.FromArgb(120, 120, 120);

    private GameState? game;

    public GamePanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Background;
    }

    public GameState? Game
    {
        get => game;
        set
        {
            game = value;
            Invalidate();
        }
    }

    /// <summary>Smooths movement between cells. Without one, everything is drawn in its cell.</summary>
    public Animator? Animator { get; set; }

    /// <summary>The clock the animator's times are measured on, in milliseconds.</summary>
    public Func<long> Now { get; set; } = () => 0;

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Background);
        if (game is null)
        {
            return;
        }

        long now = Now();
        Level level = game.Level;
        int cellSize = Math.Max(4, Math.Min(ClientSize.Width / level.Width, ClientSize.Height / level.Height));
        int originX = (ClientSize.Width - (cellSize * level.Width)) / 2;
        int originY = (ClientSize.Height - (cellSize * level.Height)) / 2;
        Rectangle CellRect(int x, int y) => new(originX + (x * cellSize), originY + (y * cellSize), cellSize, cellSize);
        Rectangle RectAt(PointF p) => new(
            (int)Math.Round(originX + (p.X * cellSize)),
            (int)Math.Round(originY + (p.Y * cellSize)),
            cellSize,
            cellSize);

        // Background first, then things that never move, then things that may be mid-glide on top.
        using (var air = new SolidBrush(AirColor))
        {
            g.FillRectangle(air, originX, originY, cellSize * level.Width, cellSize * level.Height);
        }

        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                if (level[x, y].IsFixed)
                {
                    DrawCell(g, level[x, y], CellRect(x, y));
                }
            }
        }

        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                Cell cell = level[x, y];
                if (cell.Type is CellType.Ice or CellType.Crate or CellType.Fire)
                {
                    PointF at = Animator?.CellPosition(x, y, now) ?? new PointF(x, y);
                    DrawCell(g, cell, RectAt(at));
                }
            }
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (game.CanAct)
        {
            var (tx, ty) = game.ActionTarget;
            if (level.InBounds(tx, ty) && level[tx, ty].Type is CellType.Air or CellType.Ice)
            {
                DrawTarget(g, CellRect(tx, ty));
            }
        }

        Rectangle playerCell = CellRect(game.Player.X, game.Player.Y);
        if (game.IsInPipe)
        {
            // The player is hidden inside; the pipe swells to show where they are.
            g.SmoothingMode = SmoothingMode.None;
            DrawPipe(g, level[game.Player.X, game.Player.Y].PipeOpenings, playerCell, occupied: true);
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }
        else
        {
            PointF at = Animator?.PlayerPosition(now) ?? new PointF(game.Player.X, game.Player.Y);
            DrawPlayer(g, game.Player, RectAt(at));
        }

        DrawMessage(g);
    }

    /// <summary>Draws a cell's contents (not its air background) in the given rectangle.</summary>
    private static void DrawCell(Graphics g, Cell cell, Rectangle r)
    {
        g.SmoothingMode = SmoothingMode.None;
        switch (cell.Type)
        {
            case CellType.Fire:
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawFire(g, r);
                break;
            case CellType.Block:
                DrawBlock(g, r);
                break;
            case CellType.Ice:
                DrawIce(g, cell, r);
                break;
            case CellType.UnlitJar:
                DrawJar(g, r, lit: false);
                break;
            case CellType.LitJar:
                DrawJar(g, r, lit: true);
                break;
            case CellType.Crate:
                DrawCrate(g, r);
                break;
            default:
                if (cell.IsPipe)
                {
                    DrawPipe(g, cell.PipeOpenings, r, occupied: false);
                }

                break;
        }
    }

    /// <summary>
    /// A green tube with an arm toward each opening and a rim at each mouth. A pipe the player is
    /// inside is drawn slightly larger and brighter.
    /// </summary>
    private static void DrawPipe(Graphics g, Openings openings, RectangleF r, bool occupied)
    {
        if (occupied)
        {
            r.Inflate(r.Width * 0.12f, r.Height * 0.12f);
        }

        float w = r.Width;
        float tube = w * 0.5f;
        float cx = r.X + (w / 2);
        float cy = r.Y + (r.Height / 2);
        float edge = Math.Max(1.5f, w / 20);
        float rimDepth = w * 0.13f;
        float rimOverhang = w * 0.07f;

        var arms = new List<RectangleF> { new(cx - (tube / 2), cy - (tube / 2), tube, tube) };
        var rims = new List<RectangleF>();
        if (openings.HasFlag(Openings.Up))
        {
            arms.Add(new RectangleF(cx - (tube / 2), r.Top, tube, r.Height / 2));
            rims.Add(new RectangleF(cx - (tube / 2) - rimOverhang, r.Top, tube + (2 * rimOverhang), rimDepth));
        }

        if (openings.HasFlag(Openings.Down))
        {
            arms.Add(new RectangleF(cx - (tube / 2), cy, tube, r.Height / 2));
            rims.Add(new RectangleF(cx - (tube / 2) - rimOverhang, r.Bottom - rimDepth, tube + (2 * rimOverhang), rimDepth));
        }

        if (openings.HasFlag(Openings.Left))
        {
            arms.Add(new RectangleF(r.Left, cy - (tube / 2), w / 2, tube));
            rims.Add(new RectangleF(r.Left, cy - (tube / 2) - rimOverhang, rimDepth, tube + (2 * rimOverhang)));
        }

        if (openings.HasFlag(Openings.Right))
        {
            arms.Add(new RectangleF(cx, cy - (tube / 2), w / 2, tube));
            rims.Add(new RectangleF(r.Right - rimDepth, cy - (tube / 2) - rimOverhang, rimDepth, tube + (2 * rimOverhang)));
        }

        using var outline = new SolidBrush(PipeEdgeColor);
        using var body = new SolidBrush(occupied ? PipeLitColor : PipeColor);
        using var rim = new SolidBrush(occupied ? PipeColor : PipeRimColor);

        foreach (RectangleF arm in arms)
        {
            g.FillRectangle(outline, RectangleF.Inflate(arm, edge, edge));
        }

        foreach (RectangleF arm in arms)
        {
            g.FillRectangle(body, arm);
        }

        foreach (RectangleF mouth in rims)
        {
            g.FillRectangle(outline, RectangleF.Inflate(mouth, edge, edge));
            g.FillRectangle(rim, mouth);
        }
    }

    /// <summary>A riveted steel box with a cross brace.</summary>
    private static void DrawCrate(Graphics g, Rectangle r)
    {
        int inset = Math.Max(1, r.Width / 14);
        var box = Rectangle.FromLTRB(r.Left + inset, r.Top + inset, r.Right - inset, r.Bottom - inset);
        int frame = Math.Max(2, r.Width / 8);

        using var plate = new SolidBrush(CrateColor);
        using var edge = new SolidBrush(CrateEdgeColor);
        using var brace = new Pen(CrateEdgeColor, Math.Max(2, r.Width / 10));
        using var rivet = new SolidBrush(CrateRivetColor);
        using var shine = new Pen(Color.FromArgb(90, Color.White), Math.Max(1, r.Width / 24));

        g.FillRectangle(edge, box);
        var inner = Rectangle.Inflate(box, -frame, -frame);
        g.FillRectangle(plate, inner);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawLine(brace, inner.Left, inner.Top, inner.Right, inner.Bottom);
        g.DrawLine(brace, inner.Right, inner.Top, inner.Left, inner.Bottom);
        g.DrawLine(shine, box.Left + 1, box.Top + 1, box.Right - 2, box.Top + 1);

        float size = Math.Max(2f, r.Width / 11f);
        float offset = (frame - size) / 2f;
        foreach (var (rx, ry) in new[] { (box.Left, box.Top), (box.Right - frame, box.Top), (box.Left, box.Bottom - frame), (box.Right - frame, box.Bottom - frame) })
        {
            g.FillEllipse(rivet, rx + offset, ry + offset, size, size);
        }

        g.SmoothingMode = SmoothingMode.None;
    }

    /// <summary>A rounded pot with a neck. Unlit jars are blue; lit jars glow red and orange.</summary>
    private static void DrawJar(Graphics g, Rectangle r, bool lit)
    {
        float w = r.Width;
        PointF At(float fx, float fy) => new(r.X + (fx * w), r.Y + (fy * r.Height));

        Color body = lit ? JarLitBody : JarUnlitBody;
        Color rim = lit ? JarLitRim : JarUnlitRim;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bodyBrush = new SolidBrush(body);
        using var rimBrush = new SolidBrush(rim);
        using var outline = new Pen(rim, Math.Max(1, w / 18));

        var belly = new RectangleF(At(0.1f, 0.3f), new SizeF(w * 0.8f, r.Height * 0.66f));
        var neck = new RectangleF(At(0.3f, 0.14f), new SizeF(w * 0.4f, r.Height * 0.24f));
        var lip = new RectangleF(At(0.22f, 0.06f), new SizeF(w * 0.56f, r.Height * 0.12f));

        g.FillEllipse(bodyBrush, belly);
        g.FillRectangle(bodyBrush, neck);
        g.DrawEllipse(outline, belly);
        g.FillRectangle(rimBrush, lip);

        if (lit)
        {
            using var glow = new SolidBrush(JarLitGlow);
            using var core = new SolidBrush(FlameInner);
            g.FillEllipse(glow, belly.X + (w * 0.16f), belly.Y + (w * 0.12f), belly.Width - (w * 0.32f), belly.Height - (w * 0.22f));
            g.FillEllipse(core, belly.X + (w * 0.3f), belly.Y + (w * 0.24f), belly.Width - (w * 0.6f), belly.Height - (w * 0.44f));
        }
        else
        {
            using var shine = new SolidBrush(Color.FromArgb(110, Color.White));
            g.FillEllipse(shine, belly.X + (w * 0.12f), belly.Y + (w * 0.1f), w * 0.16f, w * 0.26f);
        }

        g.SmoothingMode = SmoothingMode.None;
    }

    private static void DrawBlock(Graphics g, Rectangle r)
    {
        using var fill = new SolidBrush(BlockColor);
        using var mortar = new Pen(MortarColor, Math.Max(1, r.Width / 16));
        g.FillRectangle(fill, r);

        int midY = r.Top + (r.Height / 2);
        g.DrawRectangle(mortar, r.X, r.Y, r.Width - 1, r.Height - 1);
        g.DrawLine(mortar, r.Left, midY, r.Right, midY);
        g.DrawLine(mortar, r.Left + (r.Width / 2), r.Top, r.Left + (r.Width / 2), midY);
        g.DrawLine(mortar, r.Left + (r.Width / 4), midY, r.Left + (r.Width / 4), r.Bottom);
        g.DrawLine(mortar, r.Left + (3 * r.Width / 4), midY, r.Left + (3 * r.Width / 4), r.Bottom);
    }

    /// <summary>
    /// Ice is drawn inset from the cell edges, except on attached sides, which reach the edge
    /// and get a dark "clamp" so pushable ice is easy to tell from attached ice.
    /// </summary>
    private static void DrawIce(Graphics g, Cell cell, Rectangle r)
    {
        int inset = Math.Max(1, r.Width / 12);
        var body = Rectangle.FromLTRB(
            r.Left + (cell.AttachedLeft ? 0 : inset),
            r.Top + inset,
            r.Right - (cell.AttachedRight ? 0 : inset),
            r.Bottom - inset);

        using var fill = new SolidBrush(IceColor);
        using var shine = new SolidBrush(Color.FromArgb(150, Color.White));
        using var edge = new Pen(IceEdgeColor, Math.Max(1, r.Width / 20));
        using var clamp = new SolidBrush(ClampColor);

        g.FillRectangle(fill, body);
        g.FillRectangle(shine, body.Left + inset, body.Top + inset, Math.Max(2, body.Width / 2), Math.Max(2, body.Height / 7));
        g.FillRectangle(shine, body.Left + inset, body.Top + inset, Math.Max(2, body.Width / 9), Math.Max(2, body.Height / 3));

        g.DrawLine(edge, body.Left, body.Top, body.Right - 1, body.Top);
        g.DrawLine(edge, body.Left, body.Bottom - 1, body.Right - 1, body.Bottom - 1);
        if (!cell.AttachedLeft)
        {
            g.DrawLine(edge, body.Left, body.Top, body.Left, body.Bottom - 1);
        }

        if (!cell.AttachedRight)
        {
            g.DrawLine(edge, body.Right - 1, body.Top, body.Right - 1, body.Bottom - 1);
        }

        int clampWidth = Math.Max(2, r.Width / 7);
        int clampTop = r.Top + (r.Height / 4);
        if (cell.AttachedLeft)
        {
            g.FillRectangle(clamp, r.Left, clampTop, clampWidth, r.Height / 2);
        }

        if (cell.AttachedRight)
        {
            g.FillRectangle(clamp, r.Right - clampWidth, clampTop, clampWidth, r.Height / 2);
        }
    }

    private static void DrawFire(Graphics g, Rectangle r)
    {
        PointF At(float fx, float fy) => new(r.X + (fx * r.Width), r.Y + (fy * r.Height));

        using var outer = new SolidBrush(FlameOuter);
        using var inner = new SolidBrush(FlameInner);
        g.FillClosedCurve(outer, new[] { At(0.5f, 0.08f), At(0.66f, 0.38f), At(0.82f, 0.62f), At(0.7f, 0.9f), At(0.3f, 0.9f), At(0.18f, 0.62f), At(0.36f, 0.4f) });
        g.FillClosedCurve(inner, new[] { At(0.5f, 0.42f), At(0.64f, 0.7f), At(0.56f, 0.86f), At(0.44f, 0.86f), At(0.36f, 0.7f) });
    }

    private static void DrawTarget(Graphics g, Rectangle r)
    {
        using var pen = new Pen(Color.FromArgb(120, Color.White), Math.Max(1, r.Width / 24)) { DashStyle = DashStyle.Dash };
        int inset = Math.Max(2, r.Width / 10);
        g.DrawRectangle(pen, r.X + inset, r.Y + inset, r.Width - (2 * inset), r.Height - (2 * inset));
    }

    private static void DrawPlayer(Graphics g, Player player, Rectangle r)
    {
        float w = r.Width;
        bool right = player.Facing == Direction.Right;
        var body = new RectangleF(r.X + (w * 0.24f), r.Y + (w * 0.46f), w * 0.52f, w * 0.52f);
        var head = new RectangleF(r.X + (w * 0.28f), r.Y + (w * 0.06f), w * 0.44f, w * 0.44f);

        using var coat = new SolidBrush(player.IsAlive ? CoatColor : DeadColor);
        using var skin = new SolidBrush(player.IsAlive ? SkinColor : DeadColor);
        using var dark = new SolidBrush(Color.FromArgb(30, 30, 40));
        using var darkPen = new Pen(Color.FromArgb(30, 30, 40), Math.Max(1, w / 20));

        g.FillEllipse(coat, body);
        g.FillEllipse(skin, head);

        // Nose pokes out on the facing side.
        float noseX = right ? head.Right - (w * 0.02f) : head.Left + (w * 0.02f);
        float noseTip = right ? head.Right + (w * 0.1f) : head.Left - (w * 0.1f);
        float noseY = head.Top + (w * 0.24f);
        g.FillPolygon(skin, new[] { new PointF(noseX, noseY - (w * 0.06f)), new PointF(noseTip, noseY), new PointF(noseX, noseY + (w * 0.05f)) });

        float eyeX = right ? head.Right - (w * 0.16f) : head.Left + (w * 0.09f);
        float eyeY = head.Top + (w * 0.12f);
        float eye = w * 0.07f;
        if (player.IsAlive)
        {
            g.FillEllipse(dark, eyeX, eyeY, eye, eye);
        }
        else
        {
            g.DrawLine(darkPen, eyeX - eye, eyeY - eye, eyeX + (2 * eye), eyeY + (2 * eye));
            g.DrawLine(darkPen, eyeX + (2 * eye), eyeY - eye, eyeX - eye, eyeY + (2 * eye));
        }
    }

    private void DrawMessage(Graphics g)
    {
        string? message = game!.Status switch
        {
            GameStatus.Won => "All fires are out!  Press Enter for the next level.",
            GameStatus.Lost => "Ouch, too hot!  Press R to reset.",
            _ => null,
        };
        if (message is null)
        {
            return;
        }

        float fontSize = Math.Clamp(ClientSize.Width / 40f, 10f, 28f);
        using var font = new Font(Font.FontFamily, fontSize, FontStyle.Bold);
        SizeF size = g.MeasureString(message, font);
        var band = new RectangleF(0, (ClientSize.Height - size.Height) / 2 - 16, ClientSize.Width, size.Height + 32);
        using var shade = new SolidBrush(Color.FromArgb(190, 0, 0, 0));
        using var text = new SolidBrush(game.Status == GameStatus.Won ? Color.FromArgb(150, 225, 255) : Color.FromArgb(255, 150, 110));
        g.FillRectangle(shade, band);
        g.DrawString(message, font, text, (ClientSize.Width - size.Width) / 2, band.Top + 16);
    }
}
