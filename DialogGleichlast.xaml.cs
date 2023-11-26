using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogGleichlast
{
    private readonly Modell _dlt;
    private readonly double _anfang;
    private double _anfangNeu;
    private double _länge;
    private double _lastwert;
    private int _endIndex, _anfangIndex;
    private readonly int _anfangAlt;
    private double[] _linienlast = new double[4];
    private readonly Berechnung? _berechnung;

    public DialogGleichlast(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        Anfang.Focus();
    }
    public DialogGleichlast(Modell dlt, int anfangAlt, int endIndex, Berechnung? berechnung)
    {
        InitializeComponent();
        _dlt = dlt;
        _anfangAlt = anfangAlt;
        _endIndex = endIndex;
        _berechnung = berechnung;
        _anfang = _dlt.Übertragungspunkte[_anfangAlt].Position;
        Anfang.Focus();
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Anfang.Text) || string.IsNullOrEmpty(Länge.Text)) return;
        _anfangNeu = double.Parse(Anfang.Text);
        _länge = double.Parse(Länge.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _lastwert = double.Parse(Lastwert.Text);

        // Lage der Gleichlast unverändert, nur Lastwert geändert
        if (Math.Abs(_dlt.Übertragungspunkte[_endIndex].Position - (_anfang + _länge)) < double.Epsilon)
        {
            var piE = _dlt.Übertragungspunkte[_endIndex];
            piE.Lastwert = _lastwert;
            piE.Linienlast = Linienlast(piE.Lastlänge, piE.Lastwert);
            Close();
            return;
        }

        // finde Übertragungspunkt am Anfang der Gleichlast
        var exists = false;
        Übertragungspunkt? übertragungsPunktA = null, übertragungsPunktE = null;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => !(Math.Abs(punkt.Position - _anfangNeu) > double.Epsilon)))
        {
            exists = true;
            // falls dieser schon existiert, merk Anfangspunkt der Gleichlast
            übertragungsPunktA = punkt;
            break;
        }
        if (!exists)
        {
            übertragungsPunktA = new Übertragungspunkt(_anfangNeu)
            {
                Typ = 1
            };
            // falls dieser noch nicht existiert, füg ihn hinzu, lösch alten
            _dlt.Übertragungspunkte.Add(übertragungsPunktA);
            if (_dlt.Übertragungspunkte[_anfangAlt].Typ == 1)
                _dlt.Übertragungspunkte.RemoveAt(_anfangAlt);
        }

        // finde Übertragungspunkt am Ende der Gleichlast, evtl. neue Länge
        _linienlast = Linienlast(_länge, _lastwert);
        exists = false;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => !(Math.Abs(punkt.Position - (_anfangNeu + _länge)) > double.Epsilon)))
        {
            exists = true;
            übertragungsPunktE = punkt;
            break;
        }
        // ggf. neuen Endpunkt hinzufügen
        if (!exists)
        {
            übertragungsPunktE = new Übertragungspunkt(_anfangNeu + _länge)
            {
                Typ = 1,
                Lastlänge = _länge,
                Lastwert = _lastwert,
                Linienlast = Linienlast(_länge, _lastwert)
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunktE);
            // lösch "alten" Endpunkt falls kein Lager
            if (_endIndex > 0 && (_dlt.Übertragungspunkte[_endIndex].Typ == 1
                || _dlt.Übertragungspunkte[_endIndex].Lastlänge == 0))
            {
                _dlt.Übertragungspunkte.RemoveAt(_endIndex);
            }
        }
        // ordne die Übertragungspunkte in aufsteigender x-Richtung
        IComparer<Übertragungspunkt> comparer = new MainWindow.OrdneAufsteigendeKoordinaten();
        _dlt.Übertragungspunkte.Sort(comparer);
        if (übertragungsPunktA != null) _anfangIndex = _dlt.Übertragungspunkte.IndexOf(übertragungsPunktA);
        if (übertragungsPunktE != null) _endIndex = _dlt.Übertragungspunkte.IndexOf(übertragungsPunktE);

        // ggf. Längen an Übertragungspunkten zwischen Anfang und Ende anpassen
        for (var k = 0; k < _endIndex - _anfangIndex; k++)
        {
            var piE = _dlt.Übertragungspunkte[_endIndex - k];
            var piA = _dlt.Übertragungspunkte[_endIndex - k - 1];
            piE.Lastlänge = piE.Position - piA.Position;
            piE.Lastwert = _lastwert;
            piE.Linienlast = Linienlast(piE.Lastlänge, piE.Lastwert);
        }

        // überlappende Gleichlasten werden nicht unterstützt
        if (_dlt.Übertragungspunkte[_anfangIndex + 2].Lastlänge != 0 ||
            _dlt.Übertragungspunkte[_endIndex + 1].Lastlänge != 0)
        {
            _ = MessageBox.Show("überlappende Gleichlasten werden nicht unterstützt",
                            " Eingabe Gleichlast");
            Anfang.Text = "";
            Close();
            return;
        }
        Close();
        _berechnung?.Neuberechnung();
    }
    private double[] Linienlast(double länge, double wert)
    {
        _linienlast = new double[4];
        const double ei = 1;
        _linienlast[0] = wert * Math.Pow(länge, 4) / 24 / ei;
        _linienlast[1] = wert * Math.Pow(länge, 3) / 6 / ei;
        _linienlast[2] = -wert * Math.Pow(länge, 2) / 2;
        _linienlast[3] = -wert * länge;
        return _linienlast;
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        if (Anfang.Text == "" || Länge.Text == "") return;
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
        pi.Lastwert = 0;
        pi.Linienlast = new double[4];
        if (pi.Typ == 1
            && _dlt.Übertragungspunkte[_endIndex + 1].Lastlänge == 0
            && pi.Punktlast.Sum() == 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(_endIndex);
        }

        // Übertragungspunkt am Anfang löschen, falls dort keine Punktlast ist
        if (piA.Typ == 1 && piA.Punktlast.Sum() == 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(indexA);
        }
        _dlt.KeineLast = _berechnung!.CheckLasten();
        Close();
        _berechnung?.Neuberechnung();
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