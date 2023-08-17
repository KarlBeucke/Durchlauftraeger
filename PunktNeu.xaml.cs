using System.Windows.Controls;
using System.Windows.Shapes;

namespace Durchlauftraeger;

public partial class PunktNeu
{
    private readonly Modell? _dlt;
    private readonly Panel? _dltVisuell;
    private readonly Ellipse _pilot;
    private readonly Berechnung? _berechnung;
    public PunktNeu(Modell? dlt, Berechnung berechnung, Panel dltVisuell, Ellipse pilot)
    {
        InitializeComponent();
        _dlt = dlt;
        _dltVisuell = dltVisuell;
        _pilot = pilot;
        // aktiviere Ereignishandler für Canvas
        dltVisuell.Background = System.Windows.Media.Brushes.Transparent;
        _berechnung = berechnung;
        Show();
    }

    private void BtnDialogOk_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var punktId = int.Parse(PunktId.Text);
        if (Position.Text.Length > 0) _dlt!.Übertragungspunkte[punktId].Position = double.Parse(Position.Text);

        // entferne Steuerungsknoten und deaktiviere Ereignishandler für Canvas
        _dltVisuell!.Children.Remove(_pilot);
        _dltVisuell.Background = null;
        _berechnung?.Neuberechnung();
        Close();
    }

    private void BtnDialogCancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // entferne Steuerungsknoten und deaktiviere Ereignishandler für Canvas
        _dltVisuell!.Children.Remove(_pilot);
        _dltVisuell.Background = null;
        Close();
    }
}
