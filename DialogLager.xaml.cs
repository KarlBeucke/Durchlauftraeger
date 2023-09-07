using System.Windows;

namespace Durchlauftraeger;

public partial class DialogLager
{
    private readonly Modell _dlt;
    private readonly int _index;
    private readonly bool _exists;
    private double _position;
    public DialogLager(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = false;
        LagerPosition.Focus();
    }
    public DialogLager(Modell dlt, int index)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = true;
        _index = index;
        LagerPosition.Focus();
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (_exists) _dlt.Übertragungspunkte.RemoveAt(_index);

        if (!string.IsNullOrEmpty(LagerPosition.Text)) _position = double.Parse(LagerPosition.Text);

        var übertragungsPunkt = new Übertragungspunkt(_position)
        {
            Position = _position,
            Typ = 3
        };
        _dlt.Übertragungspunkte.Add(übertragungsPunkt);
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
}