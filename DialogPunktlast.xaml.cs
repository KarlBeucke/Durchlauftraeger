using System;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogPunktlast
{
    private readonly Modell _dlt;
    private readonly int _index;
    private readonly bool _exists;
    private double _position;
    private double _lastwert;
    private Übertragungspunkt _übertragungsPunkt = null!;

    public DialogPunktlast(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = false;
        Position.Focus();
    }
    public DialogPunktlast(Modell dlt, int index)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = true;
        _index = index;
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (_exists) _dlt.Übertragungspunkte.RemoveAt(_index);

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
            Punktlast = punktlast,
            Lk = lk
        };
        _dlt.Übertragungspunkte.Add(_übertragungsPunkt);
        Close();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        _dlt.Übertragungspunkte.RemoveAt(_index);
        Close();
    }

    private void PositionTest(object sender, RoutedEventArgs e)
    {
        var position = double.Parse(Position.Text);
        if (!(position < 0) && !(position > _dlt.Trägerlänge)) return;
        Position.Text = "";
        _ = MessageBox.Show("Lastposition außerhalb des Trägers", "Eingabe einer Punktlast",
            MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
    }
}