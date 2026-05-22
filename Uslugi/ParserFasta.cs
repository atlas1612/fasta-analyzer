using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FastaAnalyzer.Modele;

namespace FastaAnalyzer.Uslugi;

public static class ParserFasta
{
    public static List<RekordSekwencji> Parsuj(string zawartoscPliku)
    {
        if (string.IsNullOrWhiteSpace(zawartoscPliku))
            throw new InvalidDataException("Plik jest pusty.");

        var rekordy = new List<RekordSekwencji>();

        string? aktualnaNazwa = null;
        var aktualnaSekwencja = new StringBuilder();

        var linie = zawartoscPliku
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n');

        foreach (var surowaLinia in linie)
        {
            var linia = surowaLinia.Trim();

            if (string.IsNullOrWhiteSpace(linia))
                continue;

            if (linia.StartsWith(">"))
            {
                ZapiszAktualnyRekord(rekordy, aktualnaNazwa, aktualnaSekwencja);

                aktualnaNazwa = linia[1..].Trim();

                if (string.IsNullOrWhiteSpace(aktualnaNazwa))
                    throw new InvalidDataException("Nagłówek FASTA nie może być pusty.");

                aktualnaSekwencja.Clear();
            }
            else
            {
                if (aktualnaNazwa == null)
                    throw new InvalidDataException("Niepoprawny format FASTA. Plik musi zaczynać się od nagłówka rozpoczynającego się znakiem '>'.");

                if (linia.Any(char.IsWhiteSpace))
                    throw new InvalidDataException("Linia sekwencji nie może zawierać spacji ani tabulatorów.");

                aktualnaSekwencja.Append(linia.ToUpperInvariant());
            }
        }

        ZapiszAktualnyRekord(rekordy, aktualnaNazwa, aktualnaSekwencja);

        if (rekordy.Count == 0)
            throw new InvalidDataException("Nie znaleziono żadnych rekordów FASTA.");

        return rekordy;
    }

    private static void ZapiszAktualnyRekord(
        List<RekordSekwencji> rekordy,
        string? aktualnaNazwa,
        StringBuilder aktualnaSekwencja)
    {
        if (aktualnaNazwa == null)
            return;

        if (aktualnaSekwencja.Length == 0)
            throw new InvalidDataException($"Sekwencja '{aktualnaNazwa}' jest pusta.");

        rekordy.Add(new RekordSekwencji
        {
            Nazwa = aktualnaNazwa,
            Sekwencja = aktualnaSekwencja.ToString()
        });
    }
}