using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

/// <summary>Guía rápida para empezar un proyecto 2D en Ollin.</summary>
public sealed class QuickGuideWindow : Window
{
    public QuickGuideWindow()
    {
        Title = "Guía rápida 2D"; Width = 560; Height = 560; MinWidth = 480; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#121722"));
        Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));

        var content = new StackPanel { Margin = new Thickness(24), Spacing = 10 };
        content.Children.Add(new TextBlock { Text = "Empieza en 2D", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#F2A93B")) });
        content.Children.Add(new TextBlock { Text = "Ollin trabaja en modo 2D. 3D y 2.5D están ocultos.", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) });
        Add(content, "1. Escena", "Crea o abre una escena. La jerarquía muestra entidades y sus componentes.");
        Add(content, "2. Sprite", "Añade una entidad, asígnale textura y colócala en el viewport. Usa la rueda para zoom y Alt+arrastrar para mover la cámara.");
        Add(content, "3. Cámara", "Añade Camera2D a una entidad. Marca IsMain para controlar la vista del juego.");
        Add(content, "4. Física 2D", "Usa BoxCollider, Rigidbody2D y CharacterBody2D. Las colisiones se configuran por capas y máscaras.");
        Add(content, "5. UI", "Añade UiCanvas y edita botones, etiquetas y anclas con el editor visual.");
        Add(content, "6. Código", "Añade un ScriptComponent y escribe C#. Todo el motor es accesible desde código.");
        Add(content, "7. Grafo", "El grafo de nodos es el puente visual: edita la lógica y úsalo para aprender cómo se traduce a código.");
        var close = new Button { Content = "Entendido", HorizontalAlignment = HorizontalAlignment.Right, Background = new SolidColorBrush(Color.Parse("#F2A93B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        close.Click += (_, _) => Close();
        content.Children.Add(close);
        Content = new ScrollViewer { Content = content };
    }

    private static void Add(StackPanel panel, string title, string text)
    {
        panel.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
        panel.Children.Add(new TextBlock { Text = text, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.Parse("#B8C0D0")) });
    }
}
