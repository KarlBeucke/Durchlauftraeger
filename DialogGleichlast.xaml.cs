using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogGleichlast
{
    private readonly Modell _dlt;
    private readonly bool _exists;
    private double _anfang;
    private double _länge;
    private double _lastwert;
    private int _endIndex, _anfangIndex;
    private readonly Berechnung? _berechnung;

    public DialogGleichlast(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = false;
        Anfang.Focus();
    }
    public DialogGleichlast(Modell dlt, int anfangIndex, int endIndex, Berechnung? berechnung)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = true;
        _anfangIndex = anfangIndex;
        _endIndex = endIndex;
        _berechnung = berechnung;
        _anfang = _dlt.Übertragungspunkte[_anfangIndex].Position;
        Anfang.Focus();
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {

        if (string.IsNullOrEmpty(Anfang.Text) || string.IsNullOrEmpty(Länge.Text)) return;
        _anfang = double.Parse(Anfang.Text);
        _länge = double.Parse(Länge.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _lastwert = double.Parse(Lastwert.Text);

        // Lage der Gleichlast unverändert, nur Lastwert geändert
        if (_exists)
        {
            var piE = _dlt.Übertragungspunkte[_endIndex];
            if (Math.Abs(_dlt.Übertragungspunkte[_endIndex].Position - (_anfang + _länge)) < double.Epsilon
                && Math.Abs(_dlt.Übertragungspunkte[_anfangIndex].Position - _anfang) < double.Epsilon)
            {
                piE.Q = _lastwert;
                Close();
                return;
            }
            LöschLinienlast();
        }

        // überlappende Gleichlasten werden nicht unterstützt
        int i;
        // check Anfangspunkt der Gleichlast
        for (i = 1; i < _dlt.Übertragungspunkte.Count; i++)
        {
            var pi = _dlt.Übertragungspunkte[i];
            if (!(pi.Position >= _anfang)) continue;
            if (pi.Q > 0)
            {
                _ = MessageBox.Show("überlappende Gleichlasten werden nicht unterstützt", " Eingabe Gleichlast");
                Anfang.Text = "";
                Close();
                return;
            }
            break;
        }
        // check Endpunkt der Gleichlast
        for (var k = i; k < _dlt.Übertragungspunkte.Count; k++)
        {
            var pk = _dlt.Übertragungspunkte[k];
            if (!(pk.Position >= _anfang + _länge)) continue;
            if (pk.Q > 0)
            {
                _ = MessageBox.Show("überlappende Gleichlasten werden nicht unterstützt",
                    " Eingabe Gleichlast");
                Anfang.Text = "";
                Close();
                return;
            }
            break;
        }

        // neue Gleichlast hinzufügen
        // finde Übertragungspunkt am Anfang der Gleichlast
        var vorhanden = false;
        Übertragungspunkt? übertragungsPunktA = null, übertragungsPunktE = null;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => !(Math.Abs(punkt.Position - _anfang) > double.Epsilon)))
        {
            vorhanden = true;
            // falls dieser schon existiert, merk Anfangspunkt der Gleichlast
            übertragungsPunktA = punkt;
            break;
        }
        if (!vorhanden)
        {
            übertragungsPunktA = new Übertragungspunkt(_anfang) { Typ = 1 };
            _dlt.Übertragungspunkte.Add(übertragungsPunktA);
        }

        // finde Übertragungspunkt am Ende der Gleichlast, evtl. neue Länge
        vorhanden = false;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => !(Math.Abs(punkt.Position - (_anfang + _länge)) > double.Epsilon)))
        {
            vorhanden = true;
            übertragungsPunktE = punkt;
            break;
        }

        // neuen Endpunkt hinzufügen
        if (!vorhanden)
        {
            übertragungsPunktE = new Übertragungspunkt(_anfang + _länge)
            {
                Typ = 1,
                Lastlänge = _länge,
                Q = _lastwert,
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunktE);
        }
        else
        {
            übertragungsPunktE!.Lastlänge = _länge;
            übertragungsPunktE.Q = _lastwert;
        }

        // ordne die Übertragungspunkte in aufsteigender x-Richtung
        IComparer<Übertragungspunkt> comparer = new MainWindow.OrdneAufsteigendeKoordinaten();
        _dlt.Übertragungspunkte.Sort(comparer);
        _anfangIndex = _dlt.Übertragungspunkte.IndexOf(übertragungsPunktA!);
        _endIndex = _dlt.Übertragungspunkte.IndexOf(übertragungsPunktE);

        Close();
        _berechnung?.Neuberechnung();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        if (Anfang.Text == "" || Länge.Text == "") return;
        LöschLinienlast();
        _dlt.KeineLast = _berechnung!.CheckLasten();
        Close();
        _berechnung?.Neuberechnung();
    }

    private void LöschLinienlast()
    {
        var pi = _dlt.Übertragungspunkte[_endIndex];
        var indexA = 0;

        // finde Übertragungspunkt am Anfang der Gleichlast
        for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
        {
            if (Math.Abs(_dlt.Übertragungspunkte[i].Position -
                         (pi.Position - pi.Lastlänge)) > double.Epsilon) continue;
            indexA = i;
            break;
        }
        var piA = _dlt.Übertragungspunkte[indexA];

        // Übertragungspunkt am Ende
        pi.Lastlänge = 0;
        pi.Q = 0;
        if (pi.Typ == 1
            && _dlt.Übertragungspunkte[_endIndex + 1].Q == 0
            && pi.Punktlast.Sum() == 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(_endIndex);
        }

        // Übertragungspunkt am Anfang löschen, falls dort keine Punktlast ist
        if (piA is { Typ: 1, Q: 0 } && piA.Punktlast.Sum() == 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(indexA);
        }
    }

    private void AnfangTest(object sender, RoutedEventArgs e)
    {
        if (Anfang.Text == "") return;

        // check, ob Anfangswert der Gleichlast ein Koordinatenwert ist
        if (!double.TryParse(Anfang.Text, out var anfangNeu))
        {
            Anfang.Text = "";
            return;
        }

        // neuer Anfangswert muss zwischen 0 und Trägerlänge liegen
        if (anfangNeu < 0 || anfangNeu > _dlt.Trägerlänge)
        {
            _ = MessageBox.Show("Anfang der Gleichlast außerhalb des Trägers", "Eingabe einer Gleichlast");
            Anfang.Text = "";
            return;
        }

        // falls Anfangsposition geändert, ggf. Länge anpassen
        if (Länge.Text == "") return;
        // Anfangskoordinate nicht geändert, nur Länge der Gleichlast
        if (Math.Abs(_anfang - anfangNeu) < double.Epsilon) return;
        var längeNeu = double.Parse(Länge.Text) - (anfangNeu - _anfang);
        Länge.Text = längeNeu.ToString("G4");
    }
    private void EndeTest(object sender, RoutedEventArgs e)
    {
        // Eingabe keine gültige Koordinate, Eingabe leer
        if (!double.TryParse(Anfang.Text, out var anfang))
        {
            Anfang.Text = "";
            return;
        }
        // Länge kein gültiger Wert, Länge leer
        if (!double.TryParse(Länge.Text, out var länge))
        {
            Länge.Text = "";
            return;
        }
        // Ende der Gleichlast muss auf Träger liegen
        var ende = anfang + länge;
        if (!(ende > _dlt.Trägerlänge) && !(ende < anfang)) return;
        _ = MessageBox.Show("Ende der Gleichlast außerhalb des Trägers ", "Eingabe einer Gleichlast");
        Länge.Text = "";
    }
}