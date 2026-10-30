namespace Ollin.Scripting;

/// <summary>Entrada del autocompletado de código.</summary>
public sealed record CompletionEntry(string Text, string Description);

/// <summary>
/// Catálogo de palabras, tipos y miembros de la API de Ollin que el editor usa
/// para autocompletar scripts. Vive aquí para poder probarlo sin la interfaz.
/// </summary>
public static class CompletionCatalog
{
    public static IReadOnlyList<CompletionEntry> Keywords { get; } = new CompletionEntry[]
    {
        new("public", "Modificador de acceso"),
        new("private", "Modificador de acceso"),
        new("protected", "Modificador de acceso"),
        new("static", "Miembro estático"),
        new("sealed", "Clase sellada"),
        new("override", "Sobrescribe un método"),
        new("virtual", "Método virtual"),
        new("readonly", "Campo de solo lectura"),
        new("class", "Declara una clase"),
        new("struct", "Declara una estructura"),
        new("namespace", "Espacio de nombres"),
        new("using", "Importa un espacio de nombres"),
        new("void", "Sin valor de retorno"),
        new("float", "Número decimal de 32 bits"),
        new("int", "Entero de 32 bits"),
        new("bool", "Verdadero o falso"),
        new("string", "Cadena de texto"),
        new("var", "Tipo inferido"),
        new("new", "Crea una instancia"),
        new("return", "Devuelve un valor"),
        new("if", "Condicional"),
        new("else", "Alternativa del condicional"),
        new("for", "Bucle con contador"),
        new("foreach", "Bucle sobre una colección"),
        new("while", "Bucle mientras se cumpla"),
        new("switch", "Selección múltiple"),
        new("true", "Booleano verdadero"),
        new("false", "Booleano falso"),
        new("null", "Referencia nula"),
        new("this", "Instancia actual"),
    };

    public static IReadOnlyList<CompletionEntry> Types { get; } = new CompletionEntry[]
    {
        new("Vector2", "Vector de dos componentes"),
        new("Entity", "Entidad de la escena"),
        new("Scene", "Escena actual"),
        new("Transform2D", "Posición, rotación y escala"),
        new("SpriteRenderer", "Dibuja un sprite"),
        new("SpriteAnimator", "Animación por cuadros"),
        new("Camera2D", "Cámara ortográfica"),
        new("BoxCollider", "Caja de colisión"),
        new("Rigidbody2D", "Cuerpo físico"),
        new("Tilemap", "Mapa de mosaicos"),
        new("AudioSource", "Fuente de sonido"),
        new("RotatorComponent", "Gira la entidad"),
        new("OscillatorComponent", "Oscila la entidad"),
        new("ScriptComponent", "Enlaza un script"),
        new("KeyCode", "Teclas del teclado"),
    };

    public static IReadOnlyList<CompletionEntry> Api { get; } = new CompletionEntry[]
    {
        new("Input.GetKey(KeyCode.A)", "¿Está pulsada la tecla?"),
        new("Input.GetKeyDown(KeyCode.Space)", "¿Se pulsó este frame?"),
        new("Input.MousePosition", "Posición del ratón"),
        new("Time.DeltaTime", "Segundos del último frame"),
        new("Time.Elapsed", "Tiempo desde el inicio"),
        new("Entity.Transform", "Transformación de la entidad"),
        new("Entity.GetComponent<T>()", "Obtiene un componente"),
        new("Scene.Find(\"\")", "Busca una entidad por nombre"),
        new("StartCoroutine(WaitForSeconds(1f))", "Espera sin bloquear"),
        new("Debug(\"\")", "Mensaje en la consola"),
        new("Warn(\"\")", "Aviso en la consola"),
        new("Error(\"\")", "Error en la consola"),
        new("Audio.Play(\"assets/audio/sonido.wav\")", "Reproduce un efecto"),
        new("Audio.Music(\"assets/audio/musica.wav\")", "Música de fondo"),
        new("Audio.StopAll()", "Detiene todo el audio"),
    };

    /// <summary>Miembros sugeridos después de un punto.</summary>
    public static IReadOnlyList<CompletionEntry> Members { get; } = new CompletionEntry[]
    {
        new("Position", "Posición en el mundo"),
        new("Rotation", "Rotación en radianes"),
        new("Scale", "Escala"),
        new("Name", "Nombre"),
        new("Transform", "Transformación"),
        new("Id", "Identificador"),
        new("Active", "¿Activa?"),
        new("Velocity", "Velocidad"),
        new("Size", "Tamaño"),
        new("Color", "Color"),
        new("Tint", "Tinte del sprite"),
        new("TexturePath", "Ruta de la textura"),
        new("SortingOrder", "Orden de dibujo"),
        new("Enabled", "¿Componente activo?"),
        new("GetComponent", "Obtiene un componente"),
        new("AddComponent", "Añade un componente"),
        new("Find", "Busca una entidad"),
        new("Play", "Reproduce"),
        new("Stop", "Detiene"),
    };

    /// <summary>Lista combinada para Ctrl+Espacio, incluyendo entidades y scripts del proyecto.</summary>
    public static IReadOnlyList<CompletionEntry> All(
        IEnumerable<string>? entities = null,
        IEnumerable<string>? scripts = null)
    {
        var items = new List<CompletionEntry>();
        items.AddRange(Keywords);
        items.AddRange(Types);
        items.AddRange(Api);

        foreach (var script in scripts ?? Array.Empty<string>())
        {
            if (!items.Any(i => i.Text == script))
            {
                items.Add(new CompletionEntry(script, "Script del proyecto"));
            }
        }

        foreach (var entity in entities ?? Array.Empty<string>())
        {
            var literal = $"\"{entity}\"";
            if (!items.Any(i => i.Text == literal))
            {
                items.Add(new CompletionEntry(literal, "Nombre de entidad de la escena"));
            }
        }

        return items;
    }
}
