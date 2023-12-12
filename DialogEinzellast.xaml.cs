using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Durchlauftraeger;

public partial class DialogEinzellast
{
    private readonly Modell _dlt;
    private readonly Panel? _dltVisuell;
    private int _index;
    private readonly bool _exists;
    private double _position;
    private double _punktlastwert;
    private readonly Berechnung? _berechnung;
    public readonly UIElement? Punkt;

    public DialogEinzellast(Modell dlt)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = false;
        Position.Focus();
        MainWindow.PunktlastOffen = true;
    }
    public DialogEinzellast(Modell dlt, int index)
    {
        InitializeComponent();
        _dlt = dlt;
        _exists = true;
        _index = index;
        Position.Focus();
        MainWindow.PunktlastOffen = true;
    }
    public DialogEinzellast(Modell dlt, int index, Berechnung? berechnung, Panel dltVisuell)
    {
        InitializeComponent();
        _dlt = dlt;
        _dltVisuell = dltVisuell;
        _index = index;
        _exists = true;

        Punkt = new Ellipse
        {
            Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            Width = 10,
            Height = 10
        };
        Position.Focus();

        // aktiviere Ereignishandler für Canvas
        //dltVisuell.Background = Brushes.Transparent;
        _berechnung = berechnung;
        MainWindow.PunktlastOffen = true;
    }

    private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(Position.Text, out _position))
        {
            Position.Text = "";
            _ = MessageBox.Show("Lastposition muss definiert sein muss definiert sein", "Durchlaufträger");
            return;
        }
        if (!double.TryParse(Lastwert.Text, out _punktlastwert))
        {
            Lastwert.Text = "";
            _ = MessageBox.Show("Lastwert muss definiert sein muss definiert sein", "Durchlaufträger");
            return;
        }

        // Lage der Einzellast unverändert, nur Lastwert geändert
        if (_exists)
        {
            // falls Position unverändert, ändere nur Lastwert
            if (Math.Abs(_dlt.Übertragungspunkte[_index].Position - _position) < double.Epsilon)
            {
                _dlt.Übertragungspunkte[_index].Punktlast[3] = -_punktlastwert;
                _dltVisuell?.Children.Remove(Punkt);
                Close();
                _berechnung?.Neuberechnung();
                return;
            }
        }

        // lösch ggf existierende Einzellast mit _index
        if (_exists)
        {
            LöschEinzellast();
        }

        // füg Einzellast neu hinzu
        // check, ob Übertragungspunkt am Lastangriffspunkt vorhanden ist oder nicht
        var vorhanden = false;
        for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
        {
            if (Math.Abs(_dlt.Übertragungspunkte[i].Position - _position) > double.Epsilon) continue;
            vorhanden = true;
            _index = i;
            break;
        }
        // falls vorhanden, füge Lastwert hinzu
        if (vorhanden)
        {
            _dlt.Übertragungspunkte[_index].Punktlast[3] = -_punktlastwert;
        }
        // sonst, füge neuen Übertragungspunkt hinzu
        else
        {
            var punktlast = new double[4];
            punktlast[3] = -_punktlastwert;
            var lk = new double[4];
            Array.Copy(punktlast, lk, punktlast.Length);
            var übertragungsPunkt = new Übertragungspunkt(_position, punktlast)
            {
                Position = _position,
                Typ = 1,
                Punktlast = punktlast,
                Lk = lk
            };
            _dlt.Übertragungspunkte.Add(übertragungsPunkt);

            // ordne die Übertragungspunkte in aufsteigender x-Richtung
            IComparer<Übertragungspunkt> comparer = new MainWindow.OrdneAufsteigendeKoordinaten();
            _dlt.Übertragungspunkte.Sort(comparer);

            // Punktlast im Bereich einer Gleichlast, erfordert Anpassung der Längen
            _index = _dlt.Übertragungspunkte.IndexOf(übertragungsPunkt);
            _dlt.Übertragungspunkte[_index].Lastwert = _dlt.Übertragungspunkte[_index + 1].Lastwert;
            if (_dlt.Übertragungspunkte[_index].Lastwert > double.Epsilon)
            {
                _dlt.Übertragungspunkte[_index].Lastlänge = _dlt.Übertragungspunkte[_index].Position
                                                          - _dlt.Übertragungspunkte[_index - 1].Position;
                _dlt.Übertragungspunkte[_index].Linienlast = Werkzeuge.Linienlast(_dlt.Übertragungspunkte[_index].Lastlänge,
                    _dlt.Übertragungspunkte[_index].Lastwert);
                _dlt.Übertragungspunkte[_index + 1].Lastlänge = _dlt.Übertragungspunkte[_index + 1].Position
                                                              - _dlt.Übertragungspunkte[_index].Position;
                _dlt.Übertragungspunkte[_index + 1].Linienlast = Werkzeuge.Linienlast(_dlt.Übertragungspunkte[_index + 1].Lastlänge,
                    _dlt.Übertragungspunkte[_index + 1].Lastwert);
            }
        }
        MainWindow.PunktlastOffen = false;
        Close();
        _berechnung?.Neuberechnung();
    }

    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        _dltVisuell?.Children.Remove(Punkt);
        MainWindow.PunktlastOffen = false;
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        if (Position.Text == "") return;
        LöschEinzellast();
        _dlt.KeineLast = _berechnung!.CheckLasten();
        _berechnung?.Neuberechnung();
        MainWindow.PunktlastOffen = false;
        Close();
    }

    private void LöschEinzellast()
    {
        switch (_dlt.Übertragungspunkte[_index].Lastlänge)
        {
            // Punktlast im Bereich einer Gleichlast
            case > 0
                when _dlt.Übertragungspunkte[_index + 1].Lastlänge > 0:
                _dlt.Übertragungspunkte[_index + 1].Lastlänge
                    += _dlt.Übertragungspunkte[_index].Lastlänge;
                _dlt.Übertragungspunkte[_index + 1].Linienlast =
                    Werkzeuge.Linienlast(_dlt.Übertragungspunkte[_index + 1].Lastlänge,
                        _dlt.Übertragungspunkte[_index + 1].Lastwert);
                _dlt.Übertragungspunkte.RemoveAt(_index);
                break;
            // Punktlast am Anfang einer Gleichlast
            case 0
                when _dlt.Übertragungspunkte[_index + 1].Lastlänge > 0:
            // Punktlast am Ende einer Gleichlast
            case > 0
                when _dlt.Übertragungspunkte[_index + 1].Lastlänge == 0:
                _dlt.Übertragungspunkte[_index].Punktlast = new double[4];
                break;
            // Punktlast alleinstehend
            default:
                _dlt.Übertragungspunkte.RemoveAt(_index);
                break;
        }
    }

    private void PositionTest(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(Position.Text, out var position))
        {
            _ = MessageBox.Show("Lastposition muss definiert sein", "Eingabe einer Einzellast",
            MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
            Position.Text = ""; return;
        }
        if (!(position < 0) && !(position > _dlt.Trägerlänge)) return;
        Position.Text = "";
        _ = MessageBox.Show("Lastposition außerhalb des Trägers", "Eingabe einer Einzellast",
            MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
    }
}