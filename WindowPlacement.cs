using System;
using System.Windows;
using Forms = System.Windows.Forms;

namespace PodShift;

/// <summary>
/// Keeps the widget on a visible monitor work area after monitor or layout changes.
/// </summary>
internal static class WindowPlacement
{
    public static Point ClampToWorkArea(
        double left,
        double top,
        double width,
        double height,
        DpiScale dpi)
    {
        var window = new Rect(left, top, width, height);

        Rect? best = null;
        Rect? primary = null;
        var bestArea = 0.0;

        foreach (var screen in Forms.Screen.AllScreens)
        {
            var workArea = ToDips(screen.WorkingArea, dpi);

            if (screen.Primary)
                primary = workArea;

            var overlap = Rect.Intersect(window, workArea);
            var area = overlap.IsEmpty ? 0 : overlap.Width * overlap.Height;

            if (area > bestArea)
            {
                bestArea = area;
                best = workArea;
            }
        }

        var target = best ?? primary ?? SystemParameters.WorkArea;

        var x = Math.Min(Math.Max(left, target.Left), Math.Max(target.Left, target.Right - width));
        var y = Math.Min(Math.Max(top, target.Top), Math.Max(target.Top, target.Bottom - height));

        return new Point(x, y);
    }

    private static Rect ToDips(System.Drawing.Rectangle pixels, DpiScale dpi)
    {
        return new Rect(
            pixels.X / dpi.DpiScaleX,
            pixels.Y / dpi.DpiScaleY,
            pixels.Width / dpi.DpiScaleX,
            pixels.Height / dpi.DpiScaleY);
    }
}
