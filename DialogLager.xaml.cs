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
    public DialogLager(Modell dlt)
    {
        _dlt = dlt;
        InitializeComponent();
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
        if (_exists) LagerLöschen();

        // finde nächsten Übertragungspunkt nach der neuen Position
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt => !(punkt.Position < _position)))
        {
            _nextPunkt = punkt;
            _lastLängeAlt = _nextPunkt.Lastlänge;
            break;
        }

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

            if (_nextPunkt is { Lastlänge: > 0 })
            {
                _nextPunkt.Lastlänge = _nextPunkt.Position - _position;
                _nextPunkt.Linienlast =
                    Werkzeuge.Linienlast(_nextPunkt.Lastlänge, _nextPunkt.Lastwert);
                _lagerPunkt.Lastlänge = _lastLängeAlt - _nextPunkt.Lastlänge;
                _lagerPunkt.Lastwert = _nextPunkt.Lastwert;
                _lagerPunkt.Linienlast =
                    Werkzeuge.Linienlast(_lagerPunkt.Lastlänge, _lagerPunkt.Lastwert);
            }
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
        var pi = _dlt.Übertragungspunkte[_index];
        var pip1 = _dlt.Übertragungspunkte[_index+1];
        // freie Enden werden nicht unterstützt
        if (_index <= 0 || _index >= _dlt.Übertragungspunkte.Count - 1) return;

        // Übertragungspunkt wird Lastpunkt, ggf. Längen zusammenführen
        if (pip1.Lastlänge != 0)
        {
            if (Math.Abs(pip1.Lastwert - pi.Lastwert) < double.Epsilon)
            {
                pip1.Lastlänge += _dlt.Übertragungspunkte[_index].Lastlänge;
                pip1.Linienlast = Werkzeuge.Linienlast(pip1.Lastlänge, pip1.Lastwert);
                _dlt.Übertragungspunkte.RemoveAt(_index);
            }
            else pi.Typ = 1;
        }
        else if (pi.Lastlänge != 0) pi.Typ = 1;
    }
}