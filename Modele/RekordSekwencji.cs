using System;
using System.Linq;
namespace FastaAnalyzer.Modele;

public class RekordSekwencji
{
    public string Nazwa { get; set; } = string.Empty;

    public string Sekwencja { get; set; } = string.Empty;

    public int Dlugosc => Sekwencja.Length;

    public int LiczbaA => PoliczZasade('A');

    public int LiczbaT => PoliczZasade('T');

    public int LiczbaG => PoliczZasade('G');

    public int LiczbaC => PoliczZasade('C');

    public int LiczbaKodonow => Dlugosc / 3;

    public double ZawartoscGcProcent
    {
        get
        {
            if (Dlugosc == 0)
                return 0;

            return Math.Round(((double)(LiczbaG + LiczbaC) / Dlugosc) * 100, 2);
        }
    }

    private int PoliczZasade(char zasada)
    {
        return Sekwencja.Count(znak => char.ToUpperInvariant(znak) == zasada);
    }

    public override string ToString()
    {
        return $"{Nazwa} — długość: {Dlugosc}";
    }
}