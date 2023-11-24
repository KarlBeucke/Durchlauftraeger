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
        
        // finde nächsten Übertragungspunkt nach der neuen Position
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt => !(punkt.Position < _position)))
        {
            _nextPunkt = punkt;
            _lastLängeAlt = _nextPunkt.Lastlänge;
            break;
        }

        if (_exists) LagerLöschen();
        // neues Lager, finde Übertragungspunkt am Lagerpunkt
        var vorhanden = false;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => !(Math.Abs(punkt.Position - _position) > double.Epsilon)))
        {
            vorhanden = true;
            _lagerPunkt = punkt;
            break;
        }
        // bisher noch kein Übertragungspunkt am Lagerpunkt vorhanden
        if (!vorhanden)
        {
            _lagerPunkt = new Übertragungspunkt(_position)
            {
                Typ = 3
            };
            _dlt.Übertragungspunkte.Add(_lagerPunkt);
            if (_nextPunkt.Lastlänge > 0)
            {
                _nextPunkt.Lastlänge = _nextPunkt.Position - _position;
                _nextPunkt.Linienlast = 
                    Linienlast(_nextPunkt.Lastlänge, _nextPunkt.Lastwert);
                _lagerPunkt.Lastlänge = _lastLängeAlt - _nextPunkt.Lastlänge;
                _lagerPunkt.Lastwert = _nextPunkt.Lastwert;
                _lagerPunkt.Linienlast = 
                    Linienlast(_lagerPunkt.Lastlänge, _lagerPunkt.Lastwert);}
        }
        // Übertragungspunkt vorhanden
        else
        {
            _lagerPunkt.Typ = 3;
            _lagerPunkt.Lastlänge = _lastLängeAlt;
        }
        Close();
    }
    private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnLöschen_Click(object sender, RoutedEventArgs e)
    {
        LagerLöschen();
        Close();
    }

    private void LagerLöschen()
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