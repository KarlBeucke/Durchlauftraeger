using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogGleichlast
{
    private readonly Modell _dlt;
    private double _anfang;
    private double _länge;
    private double _lastwert;
    private int _endIndex, _anfangIndex;
    private double[] _linienlast = new double[4];

    public DialogGleichlast(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        Anfang.Focus();
    }
    public DialogGleichlast(Modell dlt, int endIndex)
    {
        InitializeComponent();
        _dlt = dlt;
        _endIndex = endIndex;
        Anfang.Focus();
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Anfang.Text)) _anfang = double.Parse(Anfang.Text);
        if (!string.IsNullOrEmpty(Länge.Text)) _länge = double.Parse(Länge.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _lastwert = double.Parse(Lastwert.Text);

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
                     => !(Math.Abs(punkt.Position - _anfang) > double.Epsilon)))
        {
            exists = true;
            // falls dieser schon existiert, merk Anfangspunkt der Gleichlast
            übertragungsPunktA = punkt;
            break;
        }
        if (!exists)
        {
            übertragungsPunktA = new Übertragungspunkt(_anfang)
            {
                Typ = 1
            };
            // falls dieser noch nicht existiert, füg ihn hinzu
            _dlt.Übertragungspunkte.Add(übertragungsPunktA);
        }

        // lösch "alten" Endpunkt falls kein Lager
        if (_endIndex > 0 && _dlt.Übertragungspunkte[_endIndex].Typ == 1)
        {
            _dlt.Übertragungspunkte.RemoveAt(_endIndex);
        }
        // finde Übertragungspunkt am Ende der Gleichlast, evtl. neue Länge
        _linienlast = Linienlast(_länge, _lastwert);
        exists = false;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt 
                     => !(Math.Abs(punkt.Position - (_anfang + _länge)) > double.Epsilon)))
        {
            exists = true;
            übertragungsPunktE = punkt;
            break;
        }

        if (!exists)
        {
            übertragungsPunktE = new Übertragungspunkt(_anfang + _länge)
            {
                Typ = 1,
                Lastlänge = _länge,
                Lastwert = _lastwert,
                Linienlast = Linienlast(_länge, _lastwert)
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunktE);
        }
        // ordne die Übertragungspunkte in aufsteigender x-Richtung
        IComparer<Übertragungspunkt> comparer = new MainWindow.OrdneAufsteigendeKoordinaten();
        _dlt.Übertragungspunkte.Sort(comparer);
        if (übertragungsPunktA != null) _anfangIndex = _dlt.Übertragungspunkte.IndexOf(übertragungsPunktA);
        if (übertragungsPunktE != null) _endIndex = _dlt.Übertragungspunkte.IndexOf(übertragungsPunktE);

        for (var k = 0; k < _endIndex - _anfangIndex; k++)
        {
            var piE = _dlt.Übertragungspunkte[_endIndex - k];
            var piA = _dlt.Übertragungspunkte[_endIndex - k - 1];
            piE.Lastlänge = piE.Position - piA.Position;
            piE.Lastwert = _lastwert;
            piE.Linienlast = Linienlast(piE.Lastlänge, piE.Lastwert);
        }
        Close();
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
        pi.Lastlänge = 0;
        pi.Lastwert = 0;
        pi.Linienlast = new double[4];

        if (pi.Typ == 1 
            && _dlt.Übertragungspunkte[_endIndex+1].Lastlänge == 0
            && pi.Punktlast.Sum()== 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(_endIndex);
        }
        if (piA.Typ == 1 && piA.Punktlast.Sum()== 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(indexA);
        }
        _dlt.KeineLast = MainWindow.Berechnung!.CheckLasten();
        Close();
    }

    private void AnfangTest(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(Anfang.Text, out var anfang))
        {
            Anfang.Text = "";
            return;
        }
        if (!(anfang < 0) && !(anfang > _dlt.Trägerlänge)) return;
        _ = MessageBox.Show("Anfang der Gleichlast außerhalb des Trägers", "Eingabe einer Gleichlast");
        Anfang.Text = "";
    }

    private void EndeTest(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(Anfang.Text, out var anfang))
        {
            Anfang.Text = "";
            return;
        }

        if (!double.TryParse(Länge.Text, out var länge))
        {
            Länge.Text = "";
            return;
        }
        var ende = anfang + länge;
        if (!(ende > _dlt.Trägerlänge) && !(ende < anfang)) return;
        _ = MessageBox.Show("Ende der Gleichlast vor Anfang oder außerhalb des Trägers ", "Eingabe einer Gleichlast");
        Länge.Text = "";
    }
}