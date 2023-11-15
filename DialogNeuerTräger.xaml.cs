using System.Windows;

namespace Durchlauftraeger;

public partial class DialogNeuerTräger
{
    private readonly Modell _dlt;
    public bool Ok = true;
    public DialogNeuerTräger(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        Gesamtlänge.Focus();
        _dlt.Übertragungspunkte.Clear();
    }
    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (Gesamtlänge.Text.Length > 0) { _dlt.Trägerlänge = double.Parse(Gesamtlänge.Text); }
        else
        {
            _ = MessageBox.Show("Länge muss definiert werden", "Durchlaufträger");
            return;
        }

        if (EinspannungAnfang.IsChecked != null && (bool)EinspannungAnfang.IsChecked
            && EinspannungEnde.IsChecked != null && (bool)EinspannungEnde.IsChecked)
        { _dlt.AnfangFest = true; _dlt.EndeFest = true; }

        else if (EinspannungAnfang.IsChecked != null && (bool)EinspannungAnfang.IsChecked)
        { _dlt.AnfangFest = true; _dlt.EndeFest = false; }

        else if (EinspannungEnde.IsChecked != null && (bool)EinspannungEnde.IsChecked)
        { _dlt.AnfangFest = false; _dlt.EndeFest = true; }

        else
        { _dlt.AnfangFest = false; _dlt.EndeFest = false; }

        NeueEinspannung();
        Close();
    }
    private void NeuesLager(double position)
    {
        // gelenkige Lagerung am Lager, w = M = 0
        var übertragungsPunkt = new Übertragungspunkt(position)
        {
            Position = position,
            Typ = 3
        };
        _dlt.Übertragungspunkte.Add(übertragungsPunkt);
    }

    private void NeueEinspannung()
    {
        if (_dlt.AnfangFest)
        {
            // eingespannter Rand am Trägeranfang, wa = phia = 0
            var zStartFest = new double[4, 2];
            zStartFest[2, 0] = 1;
            zStartFest[3, 1] = 1;
            // zaL = zStartFest * (Ma, Qa)
            var übertragungsPunkt = new Übertragungspunkt(0)
            {
                Typ = 3,
                Punktlast = new double[4],
                Linienlast = new double[4],
                Z = zStartFest,
                Zr = new double[4],
                Lk = new double[4],
                LastÜ = new double[4],
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);

            NeuesLager(_dlt.Trägerlänge);
        }
        else
        {
            // gelenkiger Rand am Trägeranfang, wa = Ma = 0
            var zStartGelenk = new double[4, 2];
            zStartGelenk[1, 0] = 1;
            zStartGelenk[3, 1] = 1;
            // zaL = zStartGelenk * (phia, Qa)
            var übertragungsPunkt = new Übertragungspunkt(0)
            {
                Typ = 3,
                Punktlast = new double[4],
                Linienlast = new double[4],
                Z = zStartGelenk,
                Zr = new double[4],
                Lk = new double[4],
                LastÜ = new double[4],
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);

            NeuesLager(_dlt.Trägerlänge);
        }
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Ok = false;
        Close();
    }
}