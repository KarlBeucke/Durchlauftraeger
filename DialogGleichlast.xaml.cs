using System;
using System.Linq;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogGleichlast
{
    private readonly Modell _dlt;
    private double _anfang;
    private double _länge;
    private double _lastwert;
    private int _index;
    private double[] _linienlast = new double[4];

    public DialogGleichlast(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        Anfang.Focus();
    }
    public DialogGleichlast(Modell dlt, int index)
    {
        InitializeComponent();
        _dlt = dlt;
        _index = index;
        Anfang.Focus();
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (_index != 0)
        {
            var pi = _dlt.Übertragungspunkte[_index];
            var pim1 = _dlt.Übertragungspunkte[_index - 1];
            if (pi.Typ != 3) _dlt.Übertragungspunkte.RemoveAt(_index);
            if (pim1.Typ != 3) _dlt.Übertragungspunkte.RemoveAt(_index - 1);
        }
        if (!string.IsNullOrEmpty(Anfang.Text)) _anfang = double.Parse(Anfang.Text);
        if (!string.IsNullOrEmpty(Länge.Text)) _länge = double.Parse(Länge.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _lastwert = double.Parse(Lastwert.Text);

        const double ei = 1;
        _linienlast[0] = _lastwert * Math.Pow(_länge, 4) / 24 / ei;
        _linienlast[1] = _lastwert * Math.Pow(_länge, 3) / 6 / ei;
        _linienlast[2] = -_lastwert * Math.Pow(_länge, 2) / 2;
        _linienlast[3] = -_lastwert * _länge;

        // Anfangspunkt der Gleichlast
        var übertragungsPunktA = new Übertragungspunkt(_anfang);
        // Test, ob Anfangspunkt schon existiert als Übertragungspunkt
        var exists = _dlt.Übertragungspunkte
            .Where((_, i) => !(Math.Abs(_anfang - _dlt.Übertragungspunkte[i].Position) > double.Epsilon)).Any();
        if (!exists)
        {
            übertragungsPunktA.Typ = 1;
            _dlt.Übertragungspunkte.Add(übertragungsPunktA);
        }

        exists = false;
        // Test, ob Endpunkt schon existiert, mit index
        for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
        {
            if (Math.Abs(_anfang + _länge - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
            exists = true;
            _index = i;
            _dlt.Übertragungspunkte[_index].Lastlänge = _länge;
            _dlt.Übertragungspunkte[_index].Lastwert = _lastwert;
            _dlt.Übertragungspunkte[_index].Linienlast = _linienlast;
            break;
        }

        if (!exists)
        {
            var übertragungsPunktE = new Übertragungspunkt(_anfang + _länge)
            {
                Typ = 1,
                Lastlänge = _länge,
                Lastwert = _lastwert,
                Linienlast = _linienlast
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunktE);
        }
        Close();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        var pi = _dlt.Übertragungspunkte[_index];
        int indexA = 0;

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

        if (pi.Typ == 1)
        {
            _dlt.Übertragungspunkte.RemoveAt(_index);
        }
        if (piA.Typ == 1)
        {
            _dlt.Übertragungspunkte.RemoveAt(indexA);
        }
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