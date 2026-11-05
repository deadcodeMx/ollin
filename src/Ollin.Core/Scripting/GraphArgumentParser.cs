namespace Ollin.Core.Scripting;

/// <summary>Separa argumentos de una llamada respetando textos y paréntesis anidados.</summary>
public static class GraphArgumentParser
{
    public static IReadOnlyList<string> Split(string? text)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        var current = new System.Text.StringBuilder();
        var inString = false;
        var escaped = false;
        var depth = 0;

        foreach (var character in text)
        {
            if (inString)
            {
                current.Append(character);
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') inString = false;
                continue;
            }

            switch (character)
            {
                case '"':
                    inString = true;
                    current.Append(character);
                    break;
                case '(' or '[' or '{':
                    depth++;
                    current.Append(character);
                    break;
                case ')' or ']' or '}':
                    depth = System.Math.Max(0, depth - 1);
                    current.Append(character);
                    break;
                case ',' when depth == 0:
                    var argument = current.ToString().Trim();
                    if (argument.Length > 0) result.Add(argument);
                    current.Clear();
                    break;
                default:
                    current.Append(character);
                    break;
            }
        }

        var last = current.ToString().Trim();
        if (last.Length > 0) result.Add(last);
        return result;
    }
}
