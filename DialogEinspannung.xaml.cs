using System.Windows;

namespace Durchlauftraeger;

public partial class DialogEinspannung
{
    private readonly Modell? _dlt;

    public DialogEinspannung(Modell? dlt)
    {
        InitializeComponent();
        _dlt = dlt;
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (EinspannungAnfang.IsChecked != null && (bool)EinspannungAnfang.IsChecked
         && EinspannungEnde.IsChecked != null && (bool)EinspannungEnde.IsChecked)
        {
            _dlt!.AnfangFest = true;
            _dlt.EndeFest = true;
        }

        else if (EinspannungAnfang.IsChecked != null && (bool)EinspannungAnfang.IsChecked)
        {
            _dlt!.AnfangFest = true;
            _dlt.EndeFest = false;
        }

        else if (EinspannungEnde.IsChecked != null && (bool)EinspannungEnde.IsChecked)
        {
            _dlt!.EndeFest = true;
            _dlt.AnfangFest = false;
        }

        Close();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}