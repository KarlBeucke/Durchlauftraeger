using System.Globalization;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogNeuerTräger
{
    private readonly Modell _dlt;
    private double _länge;
    private double _ei;
    private readonly bool _ändern;
    public bool Ok = true;
    public DialogNeuerTräger(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        AnfangGelenkig.IsChecked = true;
        EndeGelenkig.IsChecked = true;
        EI.Text = 1.0.ToString(CultureInfo.CurrentCulture);
        Gesamtlänge.Focus();
        _dlt.Übertragungspunkte.Clear();
    }
    public DialogNeuerTräger(Modell dlt, bool ändern)
    {
        InitializeComponent();
        _dlt = dlt;
        _ändern = ändern;
    }
    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(Gesamtlänge.Text, out _länge))
        {
            Gesamtlänge.Text = "";
            _ = MessageBox.Show("Länge muss positiv definiert werden", "Durchlaufträger");
            return;
        }
        _dlt.Trägerlänge = _länge;

        _dlt.EndeFrei = false;
        if ((bool)AnfangGelenkig.IsChecked!) _dlt.AnfangFest = false;
        else if ((bool)AnfangEingespannt.IsChecked!) _dlt.AnfangFest = true;
        if ((bool)EndeGelenkig.IsChecked!) _dlt.EndeFest = false;
        else if ((bool)EndeEingespannt.IsChecked!) _dlt.EndeFest = true;
        if ((bool)EndeFrei.IsChecked!) { _dlt.EndeFrei = true; _dlt.EndeFest = false; }

        if (!_ändern) { NeueRandbedingung(); }
        else { _dlt.Übertragungspunkte[^1].Position = _dlt.Trägerlänge; }

        if (!double.TryParse(EI.Text, out _ei))
        {
            EI.Text = "1.0";
            _ = MessageBox.Show("Eingabe von EI ungültig", "Durchlaufträger");
        }
        else _dlt.EI = _ei;
        Close();
    }

    private void NeueRandbedingung()
    {
        Übertragungspunkt übertragungsPunkt;
        if (_dlt.AnfangFest)
        {
            // eingespannter Rand am Trägeranfang, wa = phia = 0
            var zStartFest = new double[4, 2];
            zStartFest[2, 0] = 1;
            zStartFest[3, 1] = 1;
            // zaL = zStartFest * (Ma, Qa)
            übertragungsPunkt = new Übertragungspunkt(0)
            {
                Typ = 3,
                Punktlast = new double[4],
                Z = zStartFest,
                Zr = new double[4],
                Lk = new double[4],
                LastÜ = new double[4],
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);
        }
        else
        {
            // gelenkiger Rand am Trägeranfang, wa = Ma = 0
            var zStartGelenk = new double[4, 2];
            zStartGelenk[1, 0] = 1;
            zStartGelenk[3, 1] = 1;
            // zaL = zStartGelenk * (phia, Qa)
            übertragungsPunkt = new Übertragungspunkt(0)
            {
                Typ = 3,
                Punktlast = new double[4],
                Z = zStartGelenk,
                Zr = new double[4],
                Lk = new double[4],
                LastÜ = new double[4],
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);
        }

        übertragungsPunkt = new Übertragungspunkt(_dlt.Trägerlänge);
        if (_dlt.EndeFest)
        {
            übertragungsPunkt.Typ = 3;
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);
        }
        else
        {
            übertragungsPunkt.Typ = 1;
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);
        }
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Ok = false;
        Close();
    }

    private void AnfangGelenkigClick(object sender, RoutedEventArgs e)
    {
        if ((bool)AnfangGelenkig.IsChecked!)
        { AnfangGelenkig.IsChecked = true; AnfangEingespannt.IsChecked = false; }
        else if ((bool)!AnfangGelenkig.IsChecked!)
        { AnfangGelenkig.IsChecked = false; AnfangEingespannt.IsChecked = true; }
    }

    private void AnfangEingespanntClick(object sender, RoutedEventArgs e)
    {
        if ((bool)AnfangEingespannt.IsChecked!)
        { AnfangGelenkig.IsChecked = false; AnfangEingespannt.IsChecked = true; }
        else if ((bool)!AnfangEingespannt.IsChecked!)
        { AnfangGelenkig.IsChecked = true; AnfangEingespannt.IsChecked = false; }
    }

    private void EndeGelenkigClick(object sender, RoutedEventArgs e)
    {
        if ((bool)EndeGelenkig.IsChecked!)
        { EndeGelenkig.IsChecked = true; EndeEingespannt.IsChecked = false; EndeFrei.IsChecked = false; }
        else if ((bool)!EndeGelenkig.IsChecked!)
        { EndeGelenkig.IsChecked = false; EndeEingespannt.IsChecked = true; EndeFrei.IsChecked = false; }
    }

    private void EndeEingespanntClick(object sender, RoutedEventArgs e)
    {
        if ((bool)EndeEingespannt.IsChecked!)
        { EndeGelenkig.IsChecked = false; EndeEingespannt.IsChecked = true; EndeFrei.IsChecked = false; }
        else if ((bool)!EndeEingespannt.IsChecked!)
        { EndeGelenkig.IsChecked = true; EndeEingespannt.IsChecked = false; EndeFrei.IsChecked = false; }
    }

    private void EndeFreiClick(object sender, RoutedEventArgs e)
    {
        if ((bool)EndeFrei.IsChecked!)
        { EndeGelenkig.IsChecked = false; EndeEingespannt.IsChecked = false; EndeFrei.IsChecked = true; }
        else if ((bool)!EndeFrei.IsChecked!)
        { EndeGelenkig.IsChecked = true; EndeEingespannt.IsChecked = false; EndeFrei.IsChecked = false; }
    }
}