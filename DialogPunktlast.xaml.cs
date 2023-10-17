using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Durchlauftraeger;

public partial class DialogPunktlast
{
    private readonly Modell _dlt;
    private int _index;
    private readonly bool _exists;
    private double _position;
    private double _punktlastwert;
    private Übertragungspunkt _übertragungsPunkt = null!;
    private readonly Berechnung? _berechnung;

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

    public DialogPunktlast(Modell dlt, int index, Berechnung? berechnung, Panel dltVisuell)
    {
        InitializeComponent();
        _dlt = dlt;
        _index = index;
        _exists = true;
        // aktiviere Ereignishandler für Canvas
        dltVisuell.Background = System.Windows.Media.Brushes.Transparent;
        _berechnung = berechnung;
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Position.Text)) _position = double.Parse(Position.Text);
        if (!string.IsNullOrEmpty(Lastwert.Text)) _punktlastwert = double.Parse(Lastwert.Text);
        // Lastangriffspunkt neu (existiert noch nicht)
        if (!_exists)
        {
            // check, ob Übertragungspunkt am Lastangriffspunkt vorhanden ist oder nicht
            var vorhanden = false;
            for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
            {
                if (Math.Abs(_dlt.Übertragungspunkte[i].Position - _position) > double.Epsilon) continue;
                vorhanden = true;
                _index = i;
                break;
            }

            if (vorhanden)
            {
                _dlt.Übertragungspunkte[_index].Punktlast[3] = -_punktlastwert;
            }
            else
            {
                var punktlast = new double[4];
                punktlast[3] = -_punktlastwert;
                var lk = new double[4];
                Array.Copy(punktlast, lk, punktlast.Length);
                _übertragungsPunkt = new Übertragungspunkt(_position, punktlast)
                {
                    Position = _position,
                    Typ = 1,
                    Punktlast = punktlast,
                    Lk = lk
                };
                _dlt.Übertragungspunkte.Add(_übertragungsPunkt);

                // ordne die Übertragungspunkte in aufsteigender x-Richtung
                IComparer<Übertragungspunkt> comparer = new MainWindow.OrdneAufsteigendeKoordinaten();
                _dlt.Übertragungspunkte.Sort(comparer);
                _index = _dlt.Übertragungspunkte.IndexOf(_übertragungsPunkt);
                _dlt.Übertragungspunkte[_index].Lastwert = _dlt.Übertragungspunkte[_index + 1].Lastwert;
                if (_dlt.Übertragungspunkte[_index].Lastwert > double.Epsilon)
                {
                    _dlt.Übertragungspunkte[_index].Lastlänge = _dlt.Übertragungspunkte[_index].Position
                                                              - _dlt.Übertragungspunkte[_index-1].Position;
                    _dlt.Übertragungspunkte[_index].Linienlast = Linienlast(_dlt.Übertragungspunkte[_index].Lastlänge,
                        _dlt.Übertragungspunkte[_index].Lastwert);
                    _dlt.Übertragungspunkte[_index + 1].Lastlänge = _dlt.Übertragungspunkte[_index+1].Position
                                                                  - _dlt.Übertragungspunkte[_index].Position;
                    _dlt.Übertragungspunkte[_index+1].Linienlast = Linienlast(_dlt.Übertragungspunkte[_index+1].Lastlänge,
                        _dlt.Übertragungspunkte[_index+1].Lastwert);
                }
            }
        }
        // gegebener (existierender) Lastangriffspunkt mit _index
        else
        {
            var pi = _dlt.Übertragungspunkte[_index];
            // check, ob Position verändert wurde
            if (Math.Abs(pi.Position - _position) > double.Epsilon)
            {
                // finde Übertragungspunkt an neuer Position des Lastangriffspunkt
                //var vorhanden = _dlt.Übertragungspunkte.Any(punkt 
                //    => !(Math.Abs(punkt.Position - _position) > double.Epsilon));
                var vorhanden = false;
                var indexVorhanden=1;
                for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
                {
                    if (Math.Abs(_dlt.Übertragungspunkte[i].Position - _position) > double.Epsilon) continue;
                    vorhanden = true;
                    indexVorhanden = i;
                    break;
                }
                var punktlast = new double[4];
                punktlast[3] = -_punktlastwert;
                if (!vorhanden)
                {
                    var lk = new double[4];
                    Array.Copy(punktlast, lk, punktlast.Length);
                    var lastangriffspunkt = new Übertragungspunkt(_position, punktlast)
                    {
                        Position = _position,
                        Typ = 1,
                        Punktlast = punktlast,
                        Lk = lk
                    };

                    // falls dieser noch nicht existiert, füg neuen hinzu
                    _dlt.Übertragungspunkte.Add(lastangriffspunkt);
                }
                else
                {
                    _dlt.Übertragungspunkte[indexVorhanden].Punktlast = punktlast;
                    _dlt.Übertragungspunkte.RemoveAt(_index);
                }
            }

            // check, ob der Übertragunsgpunkt auf einer Linienlast liegt
            if (_dlt.Übertragungspunkte[_index+1].Lastwert == 0)
                _dlt.Übertragungspunkte.RemoveAt(_index);
        }
        Close();
        _berechnung?.Neuberechnung();
}

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        switch (_dlt.Übertragungspunkte[_index].Lastlänge)
        {
            // Punktlast im Bereich einer Gleichlast
            case > 0 
            when _dlt.Übertragungspunkte[_index+1].Lastlänge > 0:
                _dlt.Übertragungspunkte[_index+1].Lastlänge
                    += _dlt.Übertragungspunkte[_index].Lastlänge;
                _dlt.Übertragungspunkte[_index + 1].Linienlast =
                    Linienlast(_dlt.Übertragungspunkte[_index + 1].Lastlänge,
                        _dlt.Übertragungspunkte[_index + 1].Lastwert);
                _dlt.Übertragungspunkte.RemoveAt(_index);
                break;
            // Punktlast am Anfang einer Gleichlast
            case 0 
            when _dlt.Übertragungspunkte[_index+1].Lastlänge > 0:
            // Punktlast am Ende einer Gleichlast
            case > 0 
                 when _dlt.Übertragungspunkte[_index+1].Lastlänge == 0:
                _dlt.Übertragungspunkte[_index].Punktlast = new double[4];
                break;
            // Punktlast alleinstehend
            default:
                _dlt.Übertragungspunkte.RemoveAt(_index);
                break;
        }
        _dlt.KeineLast = MainWindow.Berechnung!.CheckLasten();
        _berechnung?.Neuberechnung();
        Close();
    }
    private static double[] Linienlast(double länge, double wert)
    {
        const double ei = 1;
        var linienlast = new double[4];
        linienlast[0] = wert * Math.Pow(länge, 4) / 24 / ei;
        linienlast[1] = wert * Math.Pow(länge, 3) / 6 / ei;
        linienlast[2] = -wert * Math.Pow(länge, 2) / 2;
        linienlast[3] = -wert * länge;
        return linienlast;
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