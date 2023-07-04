using System;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogPunktlast
{
    private readonly Modell? _dlt;
    private readonly int _index;
    private readonly bool _exists;
    private double _position;
    private double _lastwert;
    private Übertragungspunkt _übertragungsPunkt = null!;

    public DialogPunktlast(Modell? dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = false;
        Position.Focus();
    }
    public DialogPunktlast(Modell? dlt, int index)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = true;
        _index = index;
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (_exists) _dlt?.Übertragungspunkte.RemoveAt(_index);

        if (!string.IsNullOrEmpty(Position.Text)) _position = double.Parse(Position.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _lastwert = double.Parse(Lastwert.Text);

        var punktlast = new double[4];
        punktlast[3] = -_lastwert;
        var lk = new double[4];
        Array.Copy(punktlast, lk, punktlast.Length);
        _übertragungsPunkt = new Übertragungspunkt(_position, punktlast)
        {
            Position = _position,
            Typ = 1,
            Lastlänge = 0,
            Lastwert = punktlast[3],
            Last = punktlast,
            LastÜ = new double[4],
            ZL = new double[4],
            ZR = new double[4],
            Lk = lk
        };
        _dlt?.Übertragungspunkte.Add(_übertragungsPunkt);
        Close();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        _dlt?.Übertragungspunkte.RemoveAt(_index);
        Close();
    }

    //private void LastwertLostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (string.IsNullOrEmpty(Position.Text)) return;
    //    if (string.IsNullOrEmpty(Lastwert.Text)) return;
    //    var position = double.Parse(Position.Text);
    //    var lastwert = -double.Parse(Lastwert.Text);
    //    for (var i = 0; i < _dlt!.Übertragungspunkte.Count; i++)
    //    {
    //        if (Math.Abs(position - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
    //        _ = MessageBox.Show("Punktlast vorhanden: Lastwert ändern", "Durchlaufträger");
    //        _dlt.Übertragungspunkte[i].Lastwert = lastwert;
    //    }
    //}
    //private void PositionLostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (string.IsNullOrEmpty(Position.Text)) return;
    //    var position = double.Parse(Position.Text);
    //    for (var i = 0; i < _dlt!.Übertragungspunkte.Count; i++)
    //    {
    //        if (Math.Abs(position - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
    //        _ = MessageBox.Show("Punktlast vorhanden: löschen oder Position ändern", "Durchlaufträger");
    //        _dlt?.Übertragungspunkte.RemoveAt(i);
    //    }
    //}
}