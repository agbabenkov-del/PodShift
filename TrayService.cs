using System;
using System.Drawing;
using System.Windows.Forms;

namespace PodShift;

/// <summary>
/// Notification-area icon and its context menu. Raises events; the window decides what to do.
/// </summary>
internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _connectItem;
    private readonly ToolStripMenuItem _topmostItem;
    private readonly Icon _icon;
    private readonly bool _ownsIcon;
    private bool _disposed;

    public event EventHandler? ShowRequested;
    public event EventHandler? ConnectToggleRequested;
    public event EventHandler? TopmostToggleRequested;
    public event EventHandler? ExitRequested;

    public TrayService()
    {
        var showItem = new ToolStripMenuItem("Показать PodShift");
        showItem.Font = new Font(showItem.Font, FontStyle.Bold);
        showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

        _connectItem = new ToolStripMenuItem("Подключить к ПК") { Visible = false };
        _connectItem.Click += (_, _) => ConnectToggleRequested?.Invoke(this, EventArgs.Empty);

        _topmostItem = new ToolStripMenuItem("Поверх окон");
        _topmostItem.Click += (_, _) => TopmostToggleRequested?.Invoke(this, EventArgs.Empty);

        var exitItem = new ToolStripMenuItem("Выход");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        _menu = new ContextMenuStrip();
        _menu.Items.Add(showItem);
        _menu.Items.Add(_connectItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_topmostItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        _icon = LoadIcon(out _ownsIcon);

        _notifyIcon = new NotifyIcon
        {
            Text = "PodShift",
            Icon = _icon,
            ContextMenuStrip = _menu,
            Visible = true
        };

        _notifyIcon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    public void SetConnectAction(string? text, bool enabled)
    {
        if (_disposed)
            return;

        _connectItem.Visible = text is not null;

        if (text is not null)
            _connectItem.Text = text;

        _connectItem.Enabled = enabled;
    }

    public void SetTopmost(bool topmost)
    {
        if (_disposed)
            return;

        _topmostItem.Checked = topmost;
    }

    public void ShowBalloon(string message)
    {
        if (_disposed || string.IsNullOrWhiteSpace(message))
            return;

        var text = message.Length > 250 ? message[..250] + "…" : message;
        _notifyIcon.ShowBalloonTip(4000, "PodShift", text, ToolTipIcon.None);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();

        if (_ownsIcon)
            _icon.Dispose();
    }

    private static Icon LoadIcon(out bool ownsIcon)
    {
        try
        {
            var path = Environment.ProcessPath;
            if (path is not null)
            {
                var icon = Icon.ExtractAssociatedIcon(path);
                if (icon is not null)
                {
                    ownsIcon = true;
                    return icon;
                }
            }
        }
        catch (Exception)
        {
        }

        ownsIcon = false;
        return SystemIcons.Application;
    }
}
