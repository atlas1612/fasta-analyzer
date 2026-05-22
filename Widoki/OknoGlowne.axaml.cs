using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CsvHelper;
using FastaAnalyzer.Modele;
using FastaAnalyzer.Uslugi;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Newtonsoft.Json;

namespace FastaAnalyzer.Widoki;

public partial class OknoGlowne : Window
{
    private readonly List<RekordSekwencji> _rekordy = new();

    public OknoGlowne()
    {
        InitializeComponent();
        OdswiezWidok();
    }

    private async void WczytajPlikiFasta_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var pliki = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Wybierz pliki FASTA",
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Pliki FASTA")
                    {
                        Patterns = new List<string> { "*.fasta", "*.fa", "*.fna", "*.txt" }
                    }
                }
            });

            if (pliki.Count == 0)
                return;

            _rekordy.Clear();

            foreach (var plik in pliki)
            {
                await using var strumien = await plik.OpenReadAsync();
                using var czytnik = new StreamReader(strumien);

                var zawartosc = await czytnik.ReadToEndAsync();
                var sparsowaneRekordy = ParserFasta.Parsuj(zawartosc);

                _rekordy.AddRange(sparsowaneRekordy);
            }

            OdswiezWidok();

            TekstStatusu.Text = $"Wczytano sekwencji: {_rekordy.Count}";
        }
        catch (Exception wyjatek)
        {
            TekstStatusu.Text = "Błąd wczytywania pliku.";
            await PokazKomunikat("Błąd", wyjatek.Message);
        }
    }

    private async void EksportCsv_Click(object? sender, RoutedEventArgs e)
    {
        if (_rekordy.Count == 0)
        {
            await PokazKomunikat("Brak danych", "Najpierw wczytaj plik FASTA.");
            return;
        }

        var plik = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Zapisz wyniki jako CSV",
            SuggestedFileName = "analiza-fasta.csv",
            DefaultExtension = "csv"
        });

        if (plik == null)
            return;

        await using var strumien = await plik.OpenWriteAsync();
        await using var zapis = new StreamWriter(strumien);

        using var csv = new CsvWriter(zapis, CultureInfo.InvariantCulture);

        var daneDoEksportu = _rekordy.Select(rekord => new
        {
            rekord.Nazwa,
            rekord.Dlugosc,
            rekord.ZawartoscGcProcent,
            rekord.LiczbaKodonow,
            rekord.LiczbaA,
            rekord.LiczbaT,
            rekord.LiczbaG,
            rekord.LiczbaC
        });

        await csv.WriteRecordsAsync(daneDoEksportu);

        TekstStatusu.Text = "Wyeksportowano CSV.";
    }

    private async void EksportJson_Click(object? sender, RoutedEventArgs e)
    {
        if (_rekordy.Count == 0)
        {
            await PokazKomunikat("Brak danych", "Najpierw wczytaj plik FASTA.");
            return;
        }

        var plik = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Zapisz wyniki jako JSON",
            SuggestedFileName = "analiza-fasta.json",
            DefaultExtension = "json"
        });

        if (plik == null)
            return;

        var daneDoEksportu = _rekordy.Select(rekord => new
        {
            rekord.Nazwa,
            rekord.Sekwencja,
            rekord.Dlugosc,
            rekord.ZawartoscGcProcent,
            rekord.LiczbaKodonow,
            rekord.LiczbaA,
            rekord.LiczbaT,
            rekord.LiczbaG,
            rekord.LiczbaC
        });

        var json = JsonConvert.SerializeObject(daneDoEksportu, Formatting.Indented);

        await using var strumien = await plik.OpenWriteAsync();
        await using var zapis = new StreamWriter(strumien);

        await zapis.WriteAsync(json);

        TekstStatusu.Text = "Wyeksportowano JSON.";
    }

    private void ListaSekwencji_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ListaSekwencji.SelectedItem is not RekordSekwencji wybranyRekord)
            return;

        TekstSzczegolow.Text =
            $"Nazwa: {wybranyRekord.Nazwa}\n" +
            $"Długość sekwencji: {wybranyRekord.Dlugosc}\n" +
            $"Zawartość GC: {wybranyRekord.ZawartoscGcProcent}%\n" +
            $"Liczba kodonów: {wybranyRekord.LiczbaKodonow}\n" +
            $"A: {wybranyRekord.LiczbaA}\n" +
            $"T: {wybranyRekord.LiczbaT}\n" +
            $"G: {wybranyRekord.LiczbaG}\n" +
            $"C: {wybranyRekord.LiczbaC}";

        PodgladSekwencji.Text = wybranyRekord.Sekwencja;
    }

    private void OdswiezWidok()
    {
        ListaSekwencji.ItemsSource = null;
        ListaSekwencji.ItemsSource = _rekordy;

        AktualizujWykres();

        if (_rekordy.Count == 0)
        {
            TekstSzczegolow.Text = "Wybierz sekwencję z listy po lewej stronie.";
            PodgladSekwencji.Text = string.Empty;
        }
    }

    private void AktualizujWykres()
    {
        WykresDlugosci.Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Długość",
                Values = _rekordy.Select(rekord => (double)rekord.Dlugosc).ToArray()
            }
        };

        WykresDlugosci.XAxes = new Axis[]
        {
            new Axis
            {
                Labels = _rekordy.Select(rekord => SkrocNazwe(rekord.Nazwa)).ToArray(),
                LabelsRotation = 45
            }
        };

        WykresDlugosci.YAxes = new Axis[]
        {
            new Axis
            {
                Name = "Liczba znaków"
            }
        };
    }

    private static string SkrocNazwe(string nazwa)
    {
        if (nazwa.Length <= 18)
            return nazwa;

        return nazwa[..18] + "...";
    }

    private async Task PokazKomunikat(string tytul, string tresc)
    {
        var oknoDialogowe = new Window
        {
            Title = tytul,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = tresc,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                    }
                }
            }
        };

        if (oknoDialogowe.Content is StackPanel panel && panel.Children[1] is Button przycisk)
        {
            przycisk.Click += (_, _) => oknoDialogowe.Close();
        }

        await oknoDialogowe.ShowDialog(this);
    }
}