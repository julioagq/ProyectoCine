using System.Text.RegularExpressions;

namespace ProyectoCine.Services
{
    public static class ButacaHelper
    {
        public static bool TryNormalizarButaca(string codigoEntrada, int capacidadSala, out string codigoNormalizado)
        {
            codigoNormalizado = string.Empty;
            if (string.IsNullOrWhiteSpace(codigoEntrada) || capacidadSala <= 0)
            {
                return false;
            }

            var codigo = codigoEntrada.Trim().ToUpperInvariant();
            var match = Regex.Match(codigo, "^([A-Z]{1,2})([1-9][0-9]{0,2})$");
            if (!match.Success)
            {
                return false;
            }

            var filaTexto = match.Groups[1].Value;
            if (!int.TryParse(match.Groups[2].Value, out var columna))
            {
                return false;
            }

            var columnasPorFila = ObtenerColumnasPorCapacidad(capacidadSala);
            if (columna < 1 || columna > columnasPorFila)
            {
                return false;
            }

            var fila = ConvertirFilaANumero(filaTexto);
            if (fila <= 0)
            {
                return false;
            }

            var numeroAsiento = ((fila - 1) * columnasPorFila) + columna;
            if (numeroAsiento > capacidadSala)
            {
                return false;
            }

            codigoNormalizado = $"{filaTexto}{columna}";
            return true;
        }

        public static int ObtenerColumnasPorCapacidad(int capacidadSala)
        {
            if (capacidadSala <= 0)
            {
                return 1;
            }

            return Math.Clamp((int)Math.Ceiling(Math.Sqrt(capacidadSala)), 4, 12);
        }

        public static int ConvertirFilaANumero(string filaTexto)
        {
            var acumulado = 0;
            foreach (var ch in filaTexto)
            {
                acumulado = (acumulado * 26) + (ch - 'A' + 1);
            }

            return acumulado;
        }
    }
}
