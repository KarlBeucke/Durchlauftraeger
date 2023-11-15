using System;
using System.Linq;
using System.Windows;

namespace Durchlauftraeger;

public partial class DialogLager
{
    private readonly Modell _dlt;
    private Übertragungspunkt _lagerPunkt = null!;
    private Übertragungspunkt _nextPunkt = null!;
    private double _lastLängeAlt;
    private readonly int _index;
    private readonly bool _exists;
    private double _position;
    private double[] _linienlast = new double[4];
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
        if (!string.IsNullOrEmpty(LagerPosition.Text)) _position = double.Parse(LagerPosition.Text);

        // check, ob Lager sich unter Gleichlast befindet
        foreach (var punkt in _dlt.Übertragungspunkte.
                     Where(punkt => !(_position > punkt.Position)))
        {
            // nextPunkt ist nächster Übertragungspunkt nach Lager
            _nextPunkt = punkt;
            _lastLängeAlt = _nextPunkt.Lastlänge;
            break;
        }
        
        // neues Lager
        if (!_exists)
        {
            // finde Übertragungspunkt am Lagerpunkt
            var vorhanden = false;
            foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                         => !(Math.Abs(punkt.Position - _position) > double.Epsilon)))
            {
                vorhanden = true;
                // falls dieser schon existiert, merk Lagerpunkt
                _lagerPunkt = punkt;
                break;
            }
            // bisher noch kein Übertragungspunkt am Lagerpunkt vorhanden
            if (!vorhanden)
            {
                var lastWert = _nextPunkt.Lastwert;
                if (_nextPunkt.Lastlänge > 0)
                {
                    _nextPunkt.Lastlänge = _nextPunkt.Position - _position;
                    _nextPunkt.Linienlast = 
                        Linienlast(_nextPunkt.Lastlänge, _nextPunkt.Lastwert);
                }

                var lastLänge = _lastLängeAlt - _nextPunkt.Lastlänge;
                _lagerPunkt = new Übertragungspunkt(_position)
                {
                    Typ = 3,
                    Lastwert = lastWert,
                    Lastlänge = _lastLängeAlt - _nextPunkt.Lastlänge,
                    Linienlast = Linienlast(lastLänge, lastWert)
                };
                _dlt.Übertragungspunkte.Add(_lagerPunkt);
            }
            // Lagerpunkt bisher nicht vorhanden
            else
            {
                _lagerPunkt.Typ = 3;
                _lagerPunkt.Lastlänge = _lastLängeAlt;
            }
        }
        // existierendes Lager mit index ausgewählt
        else
        {
            // Übertragungspunkt wird Lastpunkt, ggf. Längen zusamenführen
            if(_dlt.Übertragungspunkte[_index+1].Lastlänge != 0)
            {
                _dlt.Übertragungspunkte[_index+1].Lastlänge += _dlt.Übertragungspunkte[_index].Lastlänge;
                _dlt.Übertragungspunkte[_index].Typ = 1;
            }
            // Übertragungspunkt wird Lastpunkt
            else if (_dlt.Übertragungspunkte[_index].Lastlänge == 0)
            {
                _dlt.Übertragungspunkte[_index].Typ = 1;
            }
            // entferne Lagerpunkt, wenn nicht unter Gleichlast
            else
            {
                _dlt.Übertragungspunkte.RemoveAt(_index);
            }

            _lagerPunkt = new Übertragungspunkt(_position)
            {
                Typ = 3
            };
            _dlt.Übertragungspunkte.Add(_lagerPunkt);
        }
        Close();
    }
    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        // Übertragungspunkt wird Lastpunkt, ggf. Längen zusammenführen
        if(_dlt.Übertragungspunkte[_index+1].Lastlänge != 0)
        {
            _dlt.Übertragungspunkte[_index+1].Lastlänge += _dlt.Übertragungspunkte[_index].Lastlänge;
            _dlt.Übertragungspunkte[_index + 1].Linienlast = 
                Linienlast(_dlt.Übertragungspunkte[_index+1].Lastlänge, _dlt.Übertragungspunkte[_index+1].Lastwert);
            
            if(_dlt.Übertragungspunkte[_index].Lastlänge > 0) _dlt.Übertragungspunkte.RemoveAt(_index);
            else _dlt.Übertragungspunkte[_index].Typ = 1;
        }
        else switch (_dlt.Übertragungspunkte[_index].Lastlänge)
        {
            // Übertragungspunkt wird Lastpunkt
            case 0:
                _dlt.Übertragungspunkte.RemoveAt(_index);
                break;
            case > 0:
                _dlt.Übertragungspunkte[_index].Typ = 1;
                break;
        }
        Close();
    }

    private double[] Linienlast(double länge, double wert)
    {
        _linienlast = new double[4];
        const double ei = 1;
        _linienlast[0] = wert * Math.Pow(länge, 4) / 24 / ei;
        _linienlast[1] = wert * Math.Pow(länge, 3) / 6 / ei;
        _linienlast[2] = -wert * Math.Pow(länge, 2) / 2;
        _linienlast[3] = -wert * länge;
        return _linienlast;
    }
}