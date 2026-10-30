using System.Drawing;
using System.Windows.Forms;

namespace Ollin.Setup;

/// <summary>Ventana del instalador.</summary>
internal sealed class SetupForm : Form
{
    private readonly Label _status;
    private readonly Button _install;
    private readonly Button _close;

    public SetupForm()
    {
        var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ollin");

        Text = "Ollin 0.4.0 beta — Instalador";
        ClientSize = new Size(520, 250);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(21, 25, 34);
        ForeColor = Color.FromArgb(231, 234, 240);
        Font = new Font("Segoe UI", 10f);

        var title = new Label
        {
            Text = "Ollin",
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.FromArgb(233, 162, 59),
            AutoSize = true,
            Location = new Point(26, 18),
        };

        var subtitle = new Label
        {
            Text = "Motor de juego 2D · versión 0.4.0 beta",
            ForeColor = Color.FromArgb(139, 148, 167),
            AutoSize = true,
            Location = new Point(30, 68),
        };

        var info = new Label
        {
            Text = $"Se instalará en:\n{installDir}",
            Bounds = new Rectangle(30, 96, 460, 40),
            ForeColor = Color.FromArgb(205, 212, 224),
        };

        _status = new Label
        {
            Text = "Pulsa Instalar para copiar los archivos y crear los accesos directos.",
            Bounds = new Rectangle(30, 140, 460, 40),
            ForeColor = Color.FromArgb(139, 148, 167),
        };

        _install = new Button
        {
            Text = "Instalar",
            Bounds = new Rectangle(300, 196, 96, 34),
            BackColor = Color.FromArgb(233, 162, 59),
            ForeColor = Color.FromArgb(26, 18, 6),
            FlatStyle = FlatStyle.Flat,
        };
        _install.Click += (_, _) => StartInstall(installDir);

        _close = new Button
        {
            Text = "Cancelar",
            Bounds = new Rectangle(404, 196, 96, 34),
            FlatStyle = FlatStyle.Flat,
        };
        _close.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { title, subtitle, info, _status, _install, _close });
    }

    private void StartInstall(string installDir)
    {
        _install.Enabled = false;
        _close.Enabled = false;
        _status.Text = "Instalando…";

        Task.Run(() => Program.Install(out var dir) is null ? dir : null).ContinueWith(task =>
        {
            Invoke(() =>
            {
                var dir = task.Result;
                if (dir is null)
                {
                    _status.Text = "No se pudo instalar. Revisa los permisos de la carpeta.";
                    _install.Enabled = true;
                    _close.Enabled = true;
                    return;
                }

                _status.Text = "Instalado. Abriendo Ollin…";
                Program.Launch(dir);
                Close();
            });
        });
    }
}