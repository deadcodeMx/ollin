using Ollin.Core.Scripting;
using Xunit;

namespace Ollin.Tests;

public sealed class GraphArgumentParserTests
{
    [Fact]
    public void RespetaComasDentroDeTextosYParentesis()
    {
        var arguments = GraphArgumentParser.Split("\"Hola, mundo\", Math.Max(1, 2), 3");
        Assert.Equal(3, arguments.Count);
        Assert.Equal("\"Hola, mundo\"", arguments[0]);
        Assert.Equal("Math.Max(1, 2)", arguments[1]);
        Assert.Equal("3", arguments[2]);
    }

    [Fact]
    public void RespetaComillasEscapadas()
    {
        var arguments = GraphArgumentParser.Split("\"dice \\\"hola\\\", amigo\", 1");
        Assert.Equal(2, arguments.Count);
        Assert.Contains("amigo", arguments[0]);
    }

    [Fact]
    public void VacioDevuelveListaVacia()
    {
        Assert.Empty(GraphArgumentParser.Split(""));
        Assert.Empty(GraphArgumentParser.Split("   "));
    }
}
