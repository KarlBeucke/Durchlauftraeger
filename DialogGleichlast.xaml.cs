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
            Last = new double[4],
            LastÜ = new double[4],
            ZL = new double[4],
            ZR = new double[4]
        };

        // Test, ob Anfangspunkt schon existiert als Übertragungspunkt
        bool exists = false;
        if (_dlt!.Übertragungspunkte
            .Where((_, i) => !(Math.Abs(_anfang - _dlt.Übertragungspunkte[i].Position) > double.Epsilon)).Any())
        { exists = true; }
        if (!exists) { _dlt?.Übertragungspunkte.Add(übertragungsPunktA); }

        // Endpunkt der Gleichlast
        exists = false;
        var übertragungsPunktE = new Übertragungspunkt(_anfang + _länge, linienlast)
        {
            Typ = 1,
            Lastlänge = _länge,
            Lastwert = _lastwert,
            LastÜ = new double[4],
            ZL = new double[4],
            ZR = new double[4]
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
            _dlt.Übertragungspunkte[_index].Last = linienlast;
        }
        Close();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        if (_dlt!.Übertragungspunkte[_index - 1].Typ == 1 && _dlt.Übertragungspunkte[_index - 1].Last == null) _dlt.Übertragungspunkte.RemoveAt(_index - 1);
        if (_dlt!.Übertragungspunkte[_index].Typ == 1) _dlt.Übertragungspunkte.RemoveAt(_index);
        Close();
    }

    //private void AnfangLostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (string.IsNullOrEmpty(Anfang.Text)) return;
    //    var position = double.Parse(Anfang.Text);
    //    for (var i = 0; i < _dlt!.Übertragungspunkte.Count-2; i++)
    //    {
    //        if (Math.Abs(position - _dlt.Übertragungspunkte[i].Position) > double.Epsilon
    //            && _dlt.Übertragungspunkte[i].Typ == 3) continue;
    //        _ = MessageBox.Show("Gleichlast vorhanden: löschen oder Anfangsposition ändern", "Durchlaufträger");
    //        _dlt?.Übertragungspunkte.RemoveAt(i);
    //    }
    //}
    //private void LängeLostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (string.IsNullOrEmpty(Länge.Text)) return;
    //    var länge = double.Parse(Länge.Text);
    //    var position = double.Parse(Anfang.Text)+länge;
    //    for (var i = 1; i < _dlt!.Übertragungspunkte.Count; i++)
    //    {
    //        if (Math.Abs(position - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
    //        _ = MessageBox.Show("Gleichlast vorhanden: löschen oder Lastlänge ändern", "Durchlaufträger");
    //        _dlt?.Übertragungspunkte.RemoveAt(i);
    //        _dlt?.Übertragungspunkte.RemoveAt(i - 1);
    //    }
    //}
    //private void LastwertLostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (string.IsNullOrEmpty(Anfang.Text)) return;
    //    if (string.IsNullOrEmpty(Lastwert.Text)) return;
    //    var position = double.Parse(Anfang.Text);
    //    var lastwert = double.Parse(Lastwert.Text);
    //    for (var i = 0; i < _dlt!.Übertragungspunkte.Count; i++)
    //    {
    //        if (Math.Abs(position - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
    //        _dlt.Übertragungspunkte[i].Lastwert = lastwert;
    //    }
    //}

    //private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    //{
    //    var lastwert = double.Parse(Lastwert.Text);
    //    var position = double.Parse(Anfang.Text);
    //    var länge = double.Parse(Länge.Text);
    //    var linienlast = new double[4];

    //    // Anfangspunkt der Gleichlast
    //    var exist = _dlt!.Übertragungspunkte.Any(item =>
    //        Math.Abs(item.Position - position) < Math.Abs(double.Epsilon));
    //    if (!exist)
    //    {
    //        _dlt.ÜbertragungsPunkt = new Übertragungspunkt(position, linienlast)
    //        {
    //            Typ = 0,
    //            ZL = new double[4],
    //            ZR = new double[4]
    //        };
    //        _dlt.Übertragungspunkte.Add(_dlt.ÜbertragungsPunkt);
    //    }

    //    // Endpunkt der Gleichlast
    //    exist = _dlt!.Übertragungspunkte.Any(item =>
    //        Math.Abs(item.Position - (position + länge)) < Math.Abs(double.Epsilon));
    //    if (!exist)
    //    {
    //        const double ei = 1;
    //        linienlast[0] = lastwert * länge * länge * länge * länge / 24 / ei;
    //        linienlast[1] = lastwert * länge * länge * länge / 6 / ei;
    //        linienlast[2] = -lastwert * länge * länge / 2;
    //        linienlast[3] = -lastwert * länge;
    //        _dlt.ÜbertragungsPunkt = new Übertragungspunkt(position + länge, linienlast)
    //        {
    //            Typ = 1,
    //            Lastlänge = länge,
    //            Lastwert = lastwert,
    //            ZL = new double[4],
    //            ZR = new double[4]
    //        };
    //        _dlt.Übertragungspunkte.Add(_dlt.ÜbertragungsPunkt);
    //    }
    //    Close();
    //}

    //private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    //{
    //    Close();
    //}

    //private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    //{
    //    Anfang.Text = "";
    //}
}