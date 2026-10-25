using ProyectoCine.Services;

namespace ProyectoCine.Tests;

public class ButacaHelperTests
{
    [Theory]
    [InlineData("a1", 100, "A1")]
    [InlineData("B10", 100, "B10")]
    [InlineData("q3", 300, "Q3")]
    public void TryNormalizarButaca_Valida_RetornaCodigoNormalizado(string entrada, int capacidad, string esperado)
    {
        var ok = ButacaHelper.TryNormalizarButaca(entrada, capacidad, out var codigo);

        Assert.True(ok);
        Assert.Equal(esperado, codigo);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("A0", 100)]
    [InlineData("A999", 100)]
    [InlineData("***", 100)]
    [InlineData("B5", 10)]
    public void TryNormalizarButaca_Invalida_RetornaFalse(string entrada, int capacidad)
    {
        var ok = ButacaHelper.TryNormalizarButaca(entrada, capacidad, out var codigo);

        Assert.False(ok);
        Assert.Equal(string.Empty, codigo);
    }

    [Theory]
    [InlineData("A", 1)]
    [InlineData("Z", 26)]
    [InlineData("AA", 27)]
    [InlineData("AB", 28)]
    public void ConvertirFilaANumero_CalculaCorrectamente(string fila, int esperado)
    {
        var numero = ButacaHelper.ConvertirFilaANumero(fila);

        Assert.Equal(esperado, numero);
    }
}
