namespace Durchlauftraeger;

public partial class PunktNeu
{
    private readonly Modell? _dlt;
    private readonly Berechnung? _berechnung;
    public PunktNeu(Modell? dlt, Berechnung berechnung)
    {
        InitializeComponent();
        _dlt = dlt;
        // aktiviere Ereignishandler für Canvas
        MainWindow.DltVisual!.Background = System.Windows.Media.Brushes.Transparent;
        _berechnung = berechnung;
        Show();
    }

    private void BtnDialogOk_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var punktId = int.Parse(PunktId.Text);
        if (Position.Text.Length > 0) _dlt!.Übertragungspunkte[punktId].Position = double.Parse(Position.Text);

        // entferne Steuerungsknoten und deaktiviere Ereignishandler für Canvas
        MainWindow.DltVisual!.Children.Remove(MainWindow.Pilot);
        MainWindow.DltVisual.Background = null;
        _berechnung?.Neuberechnung();
        Close();
    }

    private void BtnDialogCancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // entferne Steuerungsknoten und deaktiviere Ereignishandler für Canvas
        MainWindow.DltVisual!.Children.Remove(MainWindow.Pilot);
        MainWindow.DltVisual.Background = null;
        Close();
    }
}
