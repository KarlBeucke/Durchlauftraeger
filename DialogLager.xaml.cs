using System.Windows;

namespace Durchlauftraeger;

public partial class DialogLager
{
    private readonly Modell? _dlt;
    private readonly int _index;
    private readonly bool _exists;
    private double _position;
    public DialogLager(Modell? dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = false;
        LagerPosition.Focus();
    }
    public DialogLager(Modell? dlt, int index)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = true;
        _index = index;
        LagerPosition.Focus();
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (_exists) _dlt?.Übertragungspunkte.RemoveAt(_index);

        if (!string.IsNullOrEmpty(LagerPosition.Text)) _position = double.Parse(LagerPosition.Text);

        var übertragungsPunkt = new Übertragungspunkt(_position)
        {
            Position = _position,
            Typ = 3,
            Last = new double[4],
            ZL = new double[4],
            ZR = new double[4],
            Last = new double[4],
            Lk = new double[4]
        };
        _dlt?.Übertragungspunkte.Add(übertragungsPunkt);
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

    //private void LagerPositionLostFocus(object sender, RoutedEventArgs e)
    //{
    //    if (string.IsNullOrEmpty(LagerPosition.Text)) return;
    //    var position = double.Parse(LagerPosition.Text);
    //    for (var i = 0; i < _dlt!.Übertragungspunkte.Count; i++)
    //    {
    //        if (Math.Abs(position - _dlt.Übertragungspunkte[i].Position) > double.Epsilon) continue;
    //        _ = MessageBox.Show("Lager vorhanden: löschen oder Position ändern", "Durchlaufträger");
    //        _dlt?.Übertragungspunkte.RemoveAt(i);
    //    }
    //}
}