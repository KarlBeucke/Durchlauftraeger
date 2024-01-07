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
    private double _lastWertAlt;
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
        if (_exists)
        {
            if (_index == 0)
            {
                _ = MessageBox.Show("Startposition kann nicht verschoben werden", "Durchlaufträger");
                return;
            }

            if (_index < _dlt.Übertragungspunkte.Count - 1)
            {
                if (_position > _dlt.Trägerlänge) _dlt.Trägerlänge = _position;
                {
                    _lastWertAlt = _dlt.Übertragungspunkte[_index + 1].Q;
                    LagerLöschen();
                    NeuesLager();
                }
            }
            if (_index == _dlt.Übertragungspunkte.Count - 1)
            {
                var pi = _dlt.Übertragungspunkte[_index];
                var pim1 = _dlt.Übertragungspunkte[_index - 1];
                if (_position > _dlt.Trägerlänge) _dlt.Trägerlänge = _position;

                switch (pi.Q)
                {
                    // Endlager ohne Gleichlast nach rechts verschoben
                    case 0 when _position > pi.Position:
                        pi.Position = _position;
                        _dlt.Trägerlänge = _position;
                        break;
                    // Endlager ohne Gleichlast nach links verschoben
                    case 0 when _position < pi.Position && _position > pim1.Position:
                        pi.Position = _position;
                        _dlt.Trägerlänge = _position;
                        _dlt.Trägerlänge = _position;
                        break;
                    // Endlager ohne Gleichlast auf Endpunkt einer Gleichlast verschoben
                    case 0 when _position < pi.Position
                                && Math.Abs(_position - pim1.Position) < double.Epsilon:
                        {
                            if (pim1.Q > 0)
                            {
                                pim1.Typ = 3;
                                _dlt.Übertragungspunkte.RemoveAt(_dlt.Übertragungspunkte.Count - 1);
                            }
                            _dlt.Trägerlänge = _position;
                            break;
                        }
                    // Endlager ohne Gleichlast unter eine Gleichlast verschoben
                    case 0 when _position < pi.Position
                                && _position < pim1.Position:
                        {
                            if (pim1.Q > 0)
                            {
                                _ = MessageBox.Show("Endpunkt unter Gleichlast, " +
                                                  "nicht implementiert", "Durchlaufträger");
                            }
                            break;
                        }
                    // Endlager mit Gleichlast nach rechts
                    case > 0 when _position > pi.Position:
                        pi.Typ = 1;
                        _lagerPunkt = new Übertragungspunkt(_position) { Typ = 3 };
                        _dlt.Übertragungspunkte.Add(_lagerPunkt);
                        _dlt.Trägerlänge = _position;
                        break;
                    // Endlager mit Gleichlast nach links 
                    case > 0 when _position < pi.Position:
                        //pi.Lastlänge -= pi.Position - _position;
                        pi.Position = _position;
                        _dlt.Trägerlänge = _position;
                        break;
                }
            }
        }
        else
        {
            // neues Lager definieren
            if (_position <= _dlt.Trägerlänge) NeuesLager();
            else _ = MessageBox.Show("neue Lagerposition außerhalb des Trägers", "Durchlaufträger");
        }
        Close();
    }

    private void NeuesLager()
    {
        if (Math.Abs(_position - _dlt.Trägerlänge) < double.Epsilon)
        {
            _dlt.Übertragungspunkte[^1].Typ = 3;
            _dlt.EndeFrei = false;
            return;
        }
        // finde nächsten Übertragungspunkt nach der neuen Position
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => !(punkt.Position < _position)))
        {
            _nextPunkt = punkt;
            _lastLängeAlt = _nextPunkt.Lastlänge;
            break;
        }

        // neues Lager, finde Übertragungspunkt am Lagerpunkt
        var vorhanden = false;
        foreach (var punkt in _dlt.Übertragungspunkte.Where(punkt
                     => (Math.Abs(punkt.Position - _position) < double.Epsilon)))
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
                Typ = 3,
                Q = _nextPunkt.Q
            };
            _dlt.Übertragungspunkte.Add(_lagerPunkt);

            if (_nextPunkt is { Lastlänge: > 0 })
            {
                _nextPunkt.Lastlänge = _nextPunkt.Position - _position;
                _lagerPunkt.Lastlänge = _lastLängeAlt - _nextPunkt.Lastlänge;
                _lagerPunkt.Q = _lagerPunkt.Q;
            }
        }
        // Übertragungspunkt vorhanden
        else
        {
            _lagerPunkt.Typ = 3;
            _lagerPunkt.Lastlänge = _lastLängeAlt;
        }
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
        // nicht an Startpunkt
        if (_index < 1)
        {
            _ = MessageBox.Show("Startposition kann nicht gelöscht werden, freie Enden werden nicht unterstützt", "Durchlaufträger");
            return;
        }

        var pi = _dlt.Übertragungspunkte[_index];
        // Endlager
        if (Math.Abs(_dlt.Trägerlänge - pi.Position) < double.Epsilon)
        {
            pi.Typ = 1;
            _dlt.EndeFrei = true;
            return;
        }

        var pip1 = _dlt.Übertragungspunkte[_index + 1];
        // keine Gleichlast am Lager
        if (pi.Q == 0 && pip1.Q == 0)
        {
            _dlt.Übertragungspunkte.RemoveAt(_index);
        }

        // gleicher Wert der Gleichlast vor und hinter Lager
        else if (Math.Abs(pip1.Q - pi.Q) < double.Epsilon)
        {
            if (pi.Q > 0) _dlt.Übertragungspunkte.RemoveAt(_index);
            else
            {
                pi.Typ = 1;
                pi.Q = _lastWertAlt;
            }
        }

        // Lager entweder am Anfang oder Ende der Gleichlast
        else if (pip1.Q != 0 || pi.Q != 0)
        {
            pi.Typ = 1;
        }
    }
}