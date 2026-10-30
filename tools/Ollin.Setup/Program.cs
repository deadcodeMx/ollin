using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace Ollin.Setup;

internal static class Program
{
    private const string AppName = "Ollin";
    private const string DisplayName = "Ollin 0.4.0 beta";
    private const string Version = "0.4.0";

    [STAThread]
    private static int Main(string[] args)
    {
        var silent = args.Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));
        var uninstall = args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase));

        if (uninstall)
        {
            return Uninstall(silent);
        }

        if (silent)
        {
            var error = Install(out _);
            return error is null ? 0 : 1;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
        return 0;
    }

    /// <summary>Instala la aplicación en %LOCALAPPDATA%\Ollin.</summary>
    public static string? Install(out string installDir)
    {
        installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

        try
        {
            Directory.CreateDirectory(installDir);

            using var stream = OpenPayload();
            ZipFile.ExtractToDirectory(stream, installDir, overwriteFiles: true);

            var appExe = Path.Combine(installDir, "Ollin.Editor.exe");
            if (!File.Exists(appExe))
            {
                return "El paquete no contiene Ollin.Editor.exe.";
            }

            // El propio instalador se copia para poder desinstalar.
            var uninstaller = Path.Combine(installDir, "Desinstalar.exe");
            var current = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(current) && !string.Equals(current, uninstaller, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(current, uninstaller, overwrite: true);
            }

            CreateShortcut(Path.Combine(StartMenuFolder(), "Ollin.lnk"), appExe, installDir);
            CreateShortcut(Path.Combine(DesktopFolder(), "Ollin.lnk"), appExe, installDir);
            WriteUninstallEntry(installDir);
            return null;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
    }

    public static int Uninstall(bool silent)
    {
        var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        var self = Environment.ProcessPath ?? string.Empty;

        // No se puede borrar la carpeta mientras el propio desinstalador vive dentro.
        if (self.StartsWith(installDir, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var temp = Path.Combine(Path.GetTempPath(), $"Ollin-Uninstall-{Guid.NewGuid():N}.exe");
                File.Copy(self, temp, overwrite: true);
                var arguments = "--uninstall" + (silent ? " --silent" : string.Empty);
                Process.Start(new ProcessStartInfo(temp, arguments) { UseShellExecute = false });
                return 0;
            }
            catch (Exception exception)
            {
                if (!silent)
                {
                    MessageBox.Show($"No se pudo iniciar la desinstalación: {exception.Message}", DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return 1;
            }
        }

        try
        {
            DeleteShortcut(Path.Combine(StartMenuFolder(), "Ollin.lnk"));
            DeleteShortcut(Path.Combine(DesktopFolder(), "Ollin.lnk"));

            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Ollin", writable: true))
            {
                key?.DeleteSubKeyTree(string.Empty, throwOnMissingSubKey: false);
            }

            if (Directory.Exists(installDir))
            {
                Directory.Delete(installDir, recursive: true);
            }

            if (!silent)
            {
                MessageBox.Show("Ollin se desinstaló correctamente.", DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return 0;
        }
        catch (Exception exception)
        {
            if (!silent)
            {
                MessageBox.Show($"No se pudo desinstalar: {exception.Message}", DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return 1;
        }
    }

    public static void Launch(string installDir)
    {
        var appExe = Path.Combine(installDir, "Ollin.Editor.exe");
        if (File.Exists(appExe))
        {
            Process.Start(new ProcessStartInfo(appExe) { WorkingDirectory = installDir, UseShellExecute = true });
        }
    }

    private static Stream OpenPayload()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames().First(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));
        return assembly.GetManifestResourceStream(name)!;
    }

    private static void WriteUninstallEntry(string installDir)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Ollin");
        key.SetValue("DisplayName", DisplayName);
        key.SetValue("DisplayVersion", Version);
        key.SetValue("Publisher", "DeadCodeMx");
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", Path.Combine(installDir, "Ollin.Editor.exe"));
        key.SetValue("UninstallString", $"\"{Path.Combine(installDir, "Desinstalar.exe")}\" --uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static string StartMenuFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs");

        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string DesktopFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void CreateShortcut(string linkPath, string targetPath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(linkPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = DisplayName;
        shortcut.IconLocation = targetPath;
        shortcut.Save();
    }

    private static void DeleteShortcut(string linkPath)
    {
        if (File.Exists(linkPath))
        {
            File.Delete(linkPath);
        }
    }
}
