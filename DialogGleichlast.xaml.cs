using System;
using System.Linq;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogGleichlast
{
    private readonly Modell? _dlt;
    private double _anfang;
    private double _länge;
    private double _lastwert;
    private int _index;

    public DialogGleichlast(Modell? dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        Anfang.Focus();
    }
    public DialogGleichlast(Modell? dlt, int index)
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
            if (_dlt!.Übertragungspunkte[_index].Typ != 3) _dlt.Übertragungspunkte.RemoveAt(_index);
            if (_dlt!.Übertragungspunkte[_index - 1].Typ != 3) _dlt.Übertragungspunkte.RemoveAt(_index - 1);
        }
        if (!string.IsNullOrEmpty(Anfang.Text)) _anfang = double.Parse(Anfang.Text);
        if (!string.IsNullOrEmpty(Länge.Text)) _länge = double.Parse(Länge.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _lastwert = double.Parse(Lastwert.Text);

        var linienlast = new double[4];
        const double ei = 1;
        linienlast[0] = _lastwert * Math.Pow(_länge, 4) / 24 / ei;
        linienlast[1] = _lastwert * Math.Pow(_länge, 3) / 6 / ei;
        linienlast[2] = -_lastwert * Math.Pow(_länge, 2) / 2;
        linienlast[3] = -_lastwert * _länge;

        // Anfangspunkt der Gleichlast
        var übertragungsPunktA = new Übertragungspunkt(_anfang)
        {
            Typ = 1,
            Linienlast = new double[4],
            LastÜ = new double[4],
            Zl = new double[4],
            Zr = new double[4]
        };

        // Test, ob Anfangspunkt schon existiert als Übertragungspunkt
        var exists = _dlt!.Übertragungspunkte
            .Where((_, i) => !(Math.Abs(_anfang - _dlt.Übertragungspunkte[i].Position) > double.Epsilon)).Any();
        if (!exists) { _dlt?.Übertragungspunkte.Add(übertragungsPunktA); }

        // Endpunkt der Gleichlast
        exists = false;
        var übertragungsPunktE = new Übertragungspunkt(_anfang + _länge, linienlast)
        {
            Typ = 1,
            Punktlast = new double[4],
            Linienlast = linienlast,
            Lastlänge = _länge,
            Lastwert = _lastwert,
            LastÜ = new double[4],
            Zl = new double[4],
            Zr = new double[4]
        };
        // Test, ob der Endpunkt schon existiert, mit index
        for (var i = 0; i < _dlt!.Übertragungspunkte.Count; i++)
        {
            if (Math.Abs(_anfang + _länge - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
            _index = i;
            exists = true;
            break;
        }

        if (!exists) { _dlt?.Übertragungspunkte.Add(übertragungsPunktE); }
        else
        {
            _dlt.Übertragungspunkte[_index].Lastlänge = _länge;
            _dlt.Übertragungspunkte[_index].Lastwert = _lastwert;
            _dlt.Übertragungspunkte[_index].Linienlast = linienlast;
        }
        Close();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        if (_dlt!.Übertragungspunkte[_index - 1].Typ == 1 && _dlt.Übertragungspunkte[_index - 1].Linienlast == null) _dlt.Übertragungspunkte.RemoveAt(_index - 1);
        if (_dlt!.Übertragungspunkte[_index].Typ == 1) _dlt.Übertragungspunkte.RemoveAt(_index);
        Close();
    }
}