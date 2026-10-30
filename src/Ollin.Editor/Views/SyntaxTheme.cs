namespace Ollin.Editor.Views;

/// <summary>
/// Tema de resaltado para C# pensado para fondo oscuro. Se carga en el editor de
/// scripts al arrancar.
/// </summary>
internal static class SyntaxTheme
{
    public const string CSharpDark = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="C# Dark" extensions=".cs"
            xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
          <Color name="Comment" foreground="#6A9955" fontStyle="italic" />
          <Color name="String" foreground="#CE9178" />
          <Color name="Keyword" foreground="#C586C0" />
          <Color name="Type" foreground="#4EC9B0" />
          <Color name="Number" foreground="#B5CEA8" />
          <Color name="Preprocessor" foreground="#9B9B9B" />

          <RuleSet>
            <Span color="Comment" begin="//" />
            <Span color="Comment" multiline="true" begin="/\*" end="\*/" />
            <Span color="String" begin="&quot;" end="&quot;" escapeCharacter="\" />
            <Span color="String" begin="'" end="'" escapeCharacter="\" />

            <Rule color="Preprocessor">^\s*\#\w+</Rule>

            <Rule color="Keyword">\b(abstract|as|async|await|base|bool|break|byte|case|catch|char|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|get|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|record|ref|return|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield)\b</Rule>

            <Rule color="Type">\b(Vector2|Vector3|Vector4|Entity|Scene|Transform2D|SpriteRenderer|Camera2D|SpriteAnimator|Tilemap|BoxCollider|Rigidbody2D|ScriptComponent|OllinScript|Input|Time|KeyCode|Debug|DebugDraw|MathF|Color|WaitForSeconds)\b</Rule>

            <Rule color="Number">\b(0x[0-9a-fA-F]+|\d+(\.\d+)?)[fFdDmM]?\b</Rule>
          </RuleSet>
        </SyntaxDefinition>
        """;
}
