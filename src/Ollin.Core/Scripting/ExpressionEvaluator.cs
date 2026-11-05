using System.Globalization;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;

namespace Ollin.Core.Scripting;

/// <summary>Contexto de evaluación de expresiones del grafo.</summary>
public sealed class ExpressionContext
{
    public GameState? State { get; init; }

    public Entity? Player { get; init; }
    public IReadOnlyDictionary<string, object>? Locals { get; init; }
}

/// <summary>
/// Evaluador pequeño para el grafo visual: números, textos, booleanos, variables,
/// comparaciones, aritmética y la función <c>has("objeto")</c>.
/// </summary>
public static class ExpressionEvaluator
{
    public static object Evaluate(string expression, ExpressionContext context)
    {
        TryEvaluate(expression, context, out var value, out _);
        return value;
    }

    /// <summary>Evalúa y devuelve un error de sintaxis en lugar de silenciarlo.</summary>
    /// <summary>Identificadores y funciones usadas en una expresión (para validación).</summary>
    public sealed record ExpressionAnalysis(IReadOnlyList<string> Identifiers, IReadOnlyList<string> Functions);

    public static ExpressionAnalysis Analyze(string expression)
    {
        var identifiers = new List<string>();
        var functions = new List<string>();

        try
        {
            var tokens = Tokenize(expression);

            for (var i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Kind != TokenKind.Identifier)
                {
                    continue;
                }

                var name = tokens[i].Text;

                if (name is "true" or "True" or "false" or "False")
                {
                    continue;
                }

                if (tokens[i + 1].Kind == TokenKind.Operator && tokens[i + 1].Text == "(")
                {
                    functions.Add(name);
                }
                else
                {
                    identifiers.Add(name);
                }
            }
        }
        catch
        {
            // El error de sintaxis lo reporta Validate.
        }

        return new ExpressionAnalysis(identifiers, functions);
    }
    public static bool TryEvaluate(string expression, ExpressionContext context, out object value, out string? error)
    {
        try
        {
            var tokens = Tokenize(expression);
            var parser = new Parser(tokens, context);
            value = parser.ParseExpression();

            if (parser.HasMore)
            {
                throw new FormatException("Sobran tokens en la expresión.");
            }

            error = null;
            return true;
        }
        catch (Exception exception)
        {
            value = 0f;
            error = exception.Message;
            return false;
        }
    }

    /// <summary>Valida la sintaxis sin necesitar una partida en curso.</summary>
    public static string? Validate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return "La expresión está vacía.";
        }

        return TryEvaluate(expression, new ExpressionContext(), out _, out var error) ? null : error;
    }

    public static bool EvaluateBool(string expression, ExpressionContext context) =>
        ToBool(Evaluate(expression, context));

    public static float EvaluateNumber(string expression, ExpressionContext context) =>
        ToNumber(Evaluate(expression, context));

    public static string Format(object value) => value switch
    {
        bool flag => flag ? "1" : "0",
        float number => number.ToString("0.####", CultureInfo.InvariantCulture),
        double number => number.ToString("0.####", CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? string.Empty,
    };

    public static bool ToBool(object value) => value switch
    {
        bool flag => flag,
        float number => MathF.Abs(number) > 0.0001f,
        string text => text is "1" or "true" or "True" or "sí" or "si",
        _ => false,
    };

    public static float ToNumber(object value) => value switch
    {
        float number => number,
        bool flag => flag ? 1f : 0f,
        string text when float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
        _ => 0f,
    };

    private static List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        var index = 0;

        while (index < source.Length)
        {
            var current = source[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (char.IsDigit(current) || (current == '.' && index + 1 < source.Length && char.IsDigit(source[index + 1])))
            {
                var start = index;

                while (index < source.Length && (char.IsDigit(source[index]) || source[index] == '.'))
                {
                    index++;
                }

                var number = float.Parse(source[start..index], CultureInfo.InvariantCulture);
                tokens.Add(new Token(TokenKind.Number, number.ToString(CultureInfo.InvariantCulture)));
                continue;
            }

            if (current == '"')
            {
                index++;
                var text = new System.Text.StringBuilder();

                while (index < source.Length && source[index] != '"')
                {
                    if (source[index] == '\\' && index + 1 < source.Length)
                    {
                        index++;
                    }

                    text.Append(source[index]);
                    index++;
                }

                if (index < source.Length) index++;
                tokens.Add(new Token(TokenKind.String, text.ToString()));
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                var start = index;

                while (index < source.Length && (char.IsLetterOrDigit(source[index]) || source[index] == '_'))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Identifier, source[start..index]));
                continue;
            }

            if (index + 1 < source.Length)
            {
                var pair = source.Substring(index, 2);

                if (pair is "==" or "!=" or "<=" or ">=" or "&&" or "||")
                {
                    tokens.Add(new Token(TokenKind.Operator, pair));
                    index += 2;
                    continue;
                }
            }

            if ("+-*/%()<>=!".Contains(current))
            {
                tokens.Add(new Token(TokenKind.Operator, current.ToString()));
                index++;
                continue;
            }

            throw new FormatException($"Carácter no válido: {current}");
        }

        tokens.Add(new Token(TokenKind.End, string.Empty));
        return tokens;
    }

    private enum TokenKind
    {
        Number,
        String,
        Identifier,
        Operator,
        End,
    }

    private readonly record struct Token(TokenKind Kind, string Text);

    private sealed class Parser
    {
        private readonly List<Token> _tokens;
        private readonly ExpressionContext _context;
        private int _position;

        public Parser(List<Token> tokens, ExpressionContext context)
        {
            _tokens = tokens;
            _context = context;
        }

        public bool HasMore => Current.Kind != TokenKind.End;

        private Token Current => _tokens[System.Math.Min(_position, _tokens.Count - 1)];

        private Token Advance()
        {
            var token = Current;
            if (_position < _tokens.Count - 1) _position++;
            return token;
        }

        private bool Match(string op)
        {
            if (Current.Kind == TokenKind.Operator && Current.Text == op)
            {
                Advance();
                return true;
            }

            return false;
        }

        public object ParseExpression() => ParseOr();

        private object ParseOr()
        {
            var left = ParseAnd();

            while (Match("||"))
            {
                var right = ParseAnd();
                left = ToBool(left) || ToBool(right);
            }

            return left;
        }

        private object ParseAnd()
        {
            var left = ParseEquality();

            while (Match("&&"))
            {
                var right = ParseEquality();
                left = ToBool(left) && ToBool(right);
            }

            return left;
        }

        private object ParseEquality()
        {
            var left = ParseComparison();

            while (Current.Kind == TokenKind.Operator && Current.Text is "==" or "!=")
            {
                var op = Advance().Text;
                var right = ParseComparison();
                var equal = AreEqual(left, right);
                left = op == "==" ? equal : !equal;
            }

            return left;
        }

        private object ParseComparison()
        {
            var left = ParseAdditive();

            while (Current.Kind == TokenKind.Operator && Current.Text is "<" or "<=" or ">" or ">=")
            {
                var op = Advance().Text;
                var right = ParseAdditive();
                var a = ToNumber(left);
                var b = ToNumber(right);
                left = op switch
                {
                    "<" => a < b,
                    "<=" => a <= b,
                    ">" => a > b,
                    _ => a >= b,
                };
            }

            return left;
        }

        private object ParseAdditive()
        {
            var left = ParseMultiplicative();

            while (Current.Kind == TokenKind.Operator && Current.Text is "+" or "-")
            {
                var op = Advance().Text;
                var right = ParseMultiplicative();

                if (op == "+" && (left is string || right is string))
                {
                    left = $"{left}{right}";
                }
                else
                {
                    left = ToNumber(left) + (op == "+" ? ToNumber(right) : -ToNumber(right));
                }
            }

            return left;
        }

        private object ParseMultiplicative()
        {
            var left = ParseUnary();

            while (Current.Kind == TokenKind.Operator && Current.Text is "*" or "/" or "%")
            {
                var op = Advance().Text;
                var right = ParseUnary();
                var a = ToNumber(left);
                var b = ToNumber(right);
                left = op switch
                {
                    "*" => a * b,
                    "/" => MathF.Abs(b) < 0.00001f ? 0f : a / b,
                    _ => MathF.Abs(b) < 0.00001f ? 0f : a % b,
                };
            }

            return left;
        }

        private object ParseUnary()
        {
            if (Match("!")) return !ToBool(ParseUnary());
            if (Match("-")) return -ToNumber(ParseUnary());
            return ParsePrimary();
        }

        private object ParsePrimary()
        {
            var token = Current;

            if (token.Kind == TokenKind.Number)
            {
                Advance();
                return float.Parse(token.Text, CultureInfo.InvariantCulture);
            }

            if (token.Kind == TokenKind.String)
            {
                Advance();
                return token.Text;
            }

            if (token.Kind == TokenKind.Identifier)
            {
                Advance();
                var name = token.Text;

                if (name is "true" or "True") return true;
                if (name is "false" or "False") return false;

                if (Current.Kind == TokenKind.Operator && Current.Text == "(")
                {
                    Advance();
                    var args = new List<object>();

                    while (!(Current.Kind == TokenKind.Operator && Current.Text == ")"))
                    {
                        args.Add(ParseExpression());

                        if (Current.Kind == TokenKind.Operator && Current.Text == ",")
                        {
                            Advance();
                            continue;
                        }

                        break;
                    }

                    if (!Match(")")) throw new FormatException("Falta )");
                    return Call(name, args);
                }

                return Resolve(name);
            }

            if (Match("("))
            {
                var value = ParseExpression();
                if (!Match(")")) throw new FormatException("Falta )");
                return value;
            }

            throw new FormatException("Se esperaba un valor");
        }

        private object Resolve(string name)
        {
            if (_context.Locals is { } locals && locals.TryGetValue(name, out var local)) return local;

            var lower = name.ToLowerInvariant();

            if (lower is "vida" or "health")
            {
                return GameplayReflection.Health(_context.Player);
            }

            if (lower is "vidamax" or "healthmax")
            {
                return GameplayReflection.HealthMax(_context.Player);
            }

            if (lower is "vidas" or "lives")
            {
                return GameplayReflection.Lives(_context.Player);
            }

            var raw = _context.State?.Get(name);

            if (raw is null)
            {
                return 0f;
            }

            if (raw is "1" or "0") return raw == "1";
            if (raw is "true" or "True" or "false" or "False") return raw is "true" or "True";

            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : raw;
        }

        private object Call(string name, List<object> args)
        {
            if (name is "has" or "tiene")
            {
                var item = args.Count > 0 ? args[0]?.ToString() ?? string.Empty : string.Empty;
                return GameplayReflection.HasItem(_context.Player, item);
            }

            if (name is "min" && args.Count == 2) return MathF.Min(ToNumber(args[0]), ToNumber(args[1]));
            if (name is "max" && args.Count == 2) return MathF.Max(ToNumber(args[0]), ToNumber(args[1]));

            return 0f;
        }

        private static bool AreEqual(object left, object right)
        {
            if (left is string || right is string)
            {
                return string.Equals(left?.ToString(), right?.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (left is bool || right is bool)
            {
                return ToBool(left) == ToBool(right);
            }

            return MathF.Abs(ToNumber(left) - ToNumber(right)) < 0.0001f;
        }
    }
}
