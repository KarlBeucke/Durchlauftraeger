using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using static System.Windows.Media.Brushes;

namespace Durchlauftraeger;

public class Darstellung
{
    private readonly Modell _dlt;
    private readonly Canvas _visual;
    private double _screenV, _screenH;
    public double Auflösung;
    private double _maxX;
    private const int RandLinks = 60;
    public double PlazierungV1;
    private double _plazierungV2, _plazierungV3;
    public double PlazierungH;
    private int _endIndex;
    private double _momentenAuflösung;
    private double _querkraftAuflösung;
    private List<object> ÜPunkte { get; }
    private List<object> MomentenTexte { get; }
    private List<object> MomentenMaxTexte { get; }
    private List<object> QuerkraftTexte { get; }
    private List<object> KnotenIDs { get; }

    public Darstellung(Modell dlt, Canvas visual)
    {
        _dlt = dlt;
        _visual = visual;
        ÜPunkte = new List<object>();
        MomentenTexte = new List<object>();
        MomentenMaxTexte = new List<object>();
        QuerkraftTexte = new List<object>();
        KnotenIDs = new List<object>();
    }

    public void FestlegungAuflösung()
    {
        _screenH = _visual.ActualWidth;
        _screenV = _visual.ActualHeight;

        _maxX = _dlt.Trägerlänge;

        Auflösung = (_screenH - 2 * RandLinks) / _maxX;
        _momentenAuflösung = 1;
        _querkraftAuflösung = 1;

        PlazierungH = RandLinks;
        PlazierungV1 = (int)(0.2 * _screenV);
        _plazierungV2 = (int)(0.4 * _screenV);
        _plazierungV3 = (int)(0.7 * _screenV);
    }

    public void TrägerDarstellen()
    {
        var pathGeometry = new PathGeometry();
        var startPunkt = new Point(0, 0);
        var pathFigure = new PathFigure { StartPoint = startPunkt };
        var endPunkt = new Point(_dlt.Trägerlänge * Auflösung, 0);
        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape trägerPath = new Path()
        {
            Stroke = Black,
            StrokeThickness = 3,
            Data = pathGeometry
        };
        Canvas.SetLeft(trägerPath, PlazierungH);
        Canvas.SetTop(trägerPath, PlazierungV1);
        _visual.Children.Add(trägerPath);

        ÜbertragungspunkteAnzeigen();
        LagerZeichnen();
        if (_dlt.KeineLast == false) LastenZeichnen();
    }

    private void LagerZeichnen()
    {
        PathGeometry pathGeometry;
        var lagerKnoten = _dlt.Übertragungspunkte[0];

        // Trägeranfang
        if (_dlt.AnfangFest)
        {
            pathGeometry = DreiFesthaltungenZeichnen(lagerKnoten);
            var drehPunkt = TransformPunkt(lagerKnoten, Auflösung);
            pathGeometry.Transform = new RotateTransform(90, drehPunkt.X, drehPunkt.Y);
        }
        else
        {
            pathGeometry = EineFesthaltungZeichnen(lagerKnoten);
        }

        Shape path = new Path()
        {
            Name = "Lager0",
            Stroke = Green,
            StrokeThickness = 2,
            Data = pathGeometry
        };

        // setz oben/links Position zum Zeichnen auf dem Canvas
        Canvas.SetLeft(path, PlazierungH);
        Canvas.SetTop(path, PlazierungV1);
        // zeichne Shape
        _visual.Children.Add(path);

        // Trägerende
        _endIndex = _dlt.Übertragungspunkte.Count - 1;
        lagerKnoten = _dlt.Übertragungspunkte[_endIndex];
        if (_dlt.EndeFest)
        {
            pathGeometry = DreiFesthaltungenZeichnen(lagerKnoten);
            var drehPunkt = TransformPunkt(lagerKnoten, Auflösung);
            pathGeometry.Transform = new RotateTransform(-90, drehPunkt.X, drehPunkt.Y);
        }
        else
        {
            pathGeometry = EineFesthaltungZeichnen(lagerKnoten);
        }

        path = new Path()
        {
            Name = "Lager" + _endIndex.ToString("G"),
            Stroke = Green,
            StrokeThickness = 2,
            Data = pathGeometry
        };

        // setz oben/links Position zum Zeichnen auf dem Canvas
        Canvas.SetLeft(path, PlazierungH);
        Canvas.SetTop(path, PlazierungV1);
        // zeichne Shape
        _visual.Children.Add(path);


        //Lager im Spannbereich des Trägers, die den Träger in Felder unterteilen
        for (var index = 1; index < _endIndex; index++)
        {
            if (_dlt.Übertragungspunkte[index].Typ != 3) continue;
            lagerKnoten = _dlt.Übertragungspunkte[index];
            pathGeometry = EineFesthaltungZeichnen(lagerKnoten);
            path = new Path()
            {
                Name = "Lager" + index,
                Stroke = Green,
                StrokeThickness = 2,
                Data = pathGeometry
            };

            // setz oben/links Position zum Zeichnen auf dem Canvas
            Canvas.SetLeft(path, PlazierungH);
            Canvas.SetTop(path, PlazierungV1);
            // zeichne Shape
            _visual.Children.Add(path);
        }
    }
    private PathGeometry EineFesthaltungZeichnen(Übertragungspunkt lagerKnoten)
    {
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure();
        const int lagerSymbol = 20;

        var startPoint = TransformPunkt(lagerKnoten, Auflösung);
        pathFigure.StartPoint = startPoint;

        var endPoint = new Point(startPoint.X - lagerSymbol, startPoint.Y + lagerSymbol);
        pathFigure.Segments.Add(new LineSegment(endPoint, true));
        endPoint = new Point(endPoint.X + 2 * lagerSymbol, startPoint.Y + lagerSymbol);
        pathFigure.Segments.Add(new LineSegment(endPoint, true));
        pathFigure.Segments.Add(new LineSegment(startPoint, true));

        pathGeometry.Figures.Add(pathFigure);
        return pathGeometry;
    }
    //private PathGeometry ZweiFesthaltungenZeichnen(Übertragungspunkt lagerKnoten)
    //{
    //    var pathGeometry = new PathGeometry();
    //    var pathFigure = new PathFigure();
    //    const int lagerSymbol = 20;

    //    var startPoint = TransformPunkt(lagerKnoten, Auflösung);
    //    pathFigure.StartPoint = startPoint;

    //    var endPoint = new Point(startPoint.X - lagerSymbol, startPoint.Y + lagerSymbol);
    //    pathFigure.Segments.Add(new LineSegment(endPoint, true));
    //    endPoint = new Point(endPoint.X + 2 * lagerSymbol, startPoint.Y + lagerSymbol);
    //    pathFigure.Segments.Add(new LineSegment(endPoint, true));
    //    pathFigure.Segments.Add(new LineSegment(startPoint, true));

    //    startPoint = endPoint;
    //    pathFigure.Segments.Add(new LineSegment(startPoint, false));
    //    endPoint = new Point(startPoint.X - 5, startPoint.Y + 5);
    //    pathFigure.Segments.Add(new LineSegment(endPoint, true));

    //    pathFigure.Segments.Add(new LineSegment(new Point(startPoint.X - 10, startPoint.Y), false));
    //    pathFigure.Segments.Add(new LineSegment(new Point(endPoint.X - 10, endPoint.Y), true));

    //    pathFigure.Segments.Add(new LineSegment(new Point(startPoint.X - 20, startPoint.Y), false));
    //    pathFigure.Segments.Add(new LineSegment(new Point(endPoint.X - 20, endPoint.Y), true));

    //    pathFigure.Segments.Add(new LineSegment(new Point(startPoint.X - 30, startPoint.Y), false));
    //    pathFigure.Segments.Add(new LineSegment(new Point(endPoint.X - 30, endPoint.Y), true));

    //    pathFigure.Segments.Add(new LineSegment(new Point(startPoint.X - 40, startPoint.Y), false));
    //    pathFigure.Segments.Add(new LineSegment(new Point(endPoint.X - 40, endPoint.Y), true));

    //    pathGeometry.Figures.Add(pathFigure);
    //    return pathGeometry;
    //}
    private PathGeometry DreiFesthaltungenZeichnen(Übertragungspunkt lagerKnoten)
    {
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure();
        const int lagerSymbol = 20;

        var startPoint = TransformPunkt(lagerKnoten, Auflösung);

        startPoint = new Point(startPoint.X - lagerSymbol, startPoint.Y);
        pathFigure.StartPoint = startPoint;
        var endPoint = new Point(startPoint.X + 2 * lagerSymbol, startPoint.Y);
        pathFigure.Segments.Add(new LineSegment(endPoint, true));
        pathGeometry.Figures.Add(pathFigure);
        pathFigure = new PathFigure
        {
            StartPoint = startPoint
        };
        endPoint = new Point(startPoint.X - 10, startPoint.Y + 10);
        pathFigure.Segments.Add(new LineSegment(endPoint, true));
        pathGeometry.Figures.Add(pathFigure);
        for (var i = 0; i < 4; i++)
        {
            pathFigure = new PathFigure();
            startPoint = new Point(startPoint.X + 10, startPoint.Y);
            pathFigure.StartPoint = startPoint;
            endPoint = new Point(startPoint.X - 10, startPoint.Y + 10);
            pathFigure.Segments.Add(new LineSegment(endPoint, true));
            pathGeometry.Figures.Add(pathFigure);
        }

        return pathGeometry;
    }

    private void LastenZeichnen()
    {
        const int maxLastScreen = 50;
        var maxLastWert = 1.0;

        for (var i = 1; i < _endIndex + 1; i++)
        {
            if (Math.Abs(_dlt.Übertragungspunkte[i].Punktlast[3]) > Math.Abs(maxLastWert))
                maxLastWert = Math.Abs(_dlt.Übertragungspunkte[i].Punktlast[3]);
            if (Math.Abs(_dlt.Übertragungspunkte[i].Lastwert) > Math.Abs(maxLastWert))
                maxLastWert = Math.Abs(_dlt.Übertragungspunkte[i].Lastwert);
        }


        var lastAuflösung = maxLastScreen / maxLastWert;

        for (var index = 1; index < _endIndex + 1; index++)
        {
            var lastPunkt = _dlt.Übertragungspunkte[index];
            // Punktlasten
            if (Math.Abs(lastPunkt.Punktlast.Sum()) > double.Epsilon)
            {
                if (lastPunkt.Typ != 1) continue;
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();

                const int lastPfeilGroesse = 10;
                var lastWert = lastPunkt.Punktlast[3];
                var endPoint = new Point(lastPunkt.Position * Auflösung, lastWert * lastAuflösung);
                pathFigure.StartPoint = endPoint;

                var startPoint = TransformPunkt(lastPunkt, Auflösung);
                pathFigure.Segments.Add(new LineSegment(startPoint, true));

                var vector = startPoint - endPoint;
                vector.Normalize();
                vector *= lastPfeilGroesse;
                vector = RotateVectorScreen(vector, 30);
                endPoint = new Point(startPoint.X - vector.X, startPoint.Y - vector.Y);
                pathFigure.Segments.Add(new LineSegment(endPoint, true));

                vector = RotateVectorScreen(vector, -60);
                endPoint = new Point(startPoint.X - vector.X, startPoint.Y - vector.Y);
                pathFigure.Segments.Add(new LineSegment(endPoint, false));
                pathFigure.Segments.Add(new LineSegment(startPoint, true));

                pathGeometry.Figures.Add(pathFigure);
                Shape path = new Path()
                {
                    Name = "Punktlast" + index,
                    Stroke = DarkRed,
                    StrokeThickness = 2,
                    Data = pathGeometry
                };

                // setz oben/links Position zum Zeichnen auf dem Canvas
                Canvas.SetLeft(path, PlazierungH);
                Canvas.SetTop(path, PlazierungV1);
                // zeichne Shape
                _visual.Children.Add(path);
            }

            // Gleichlast
            if (!(lastPunkt.Lastlänge > double.Epsilon)) continue;
            {
                const int lastPfeilGroesse = 6;

                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();
                var indexA = 0;
                var lastWert = -lastPunkt.Lastwert;
                // finde Übertragungspunkt am Anfang der Gleichlast
                for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
                {
                    if (Math.Abs(_dlt.Übertragungspunkte[i].Position -
                                 (lastPunkt.Position - lastPunkt.Lastlänge)) > double.Epsilon) continue;
                    indexA = i;
                    break;
                }
                var lastStart = _dlt.Übertragungspunkte[indexA];
                var startPunkt = new Point(lastStart.Position * Auflösung, 0);
                var endPunkt = new Point(lastPunkt.Position * Auflösung, 0);

                var vector = endPunkt - startPunkt;

                // Startpunkt und Lastpunkt am Anfang
                pathFigure.StartPoint = startPunkt;
                var lastVektor = RotateVectorScreen(vector, -90);
                lastVektor.Normalize();
                var vec = lastVektor * lastAuflösung * lastWert;
                var nextPunkt = new Point(startPunkt.X, startPunkt.Y - vec.Y);

                if (Math.Abs(vec.Length) > double.Epsilon)
                {
                    // Lastpfeil am Anfang
                    lastVektor *= lastPfeilGroesse;
                    lastVektor = RotateVectorScreen(lastVektor, -150);
                    var punkt = new Point(startPunkt.X - lastVektor.X, startPunkt.Y - lastVektor.Y);
                    pathFigure.Segments.Add(new LineSegment(punkt, true));
                    lastVektor = RotateVectorScreen(lastVektor, -60);
                    punkt = new Point(startPunkt.X - lastVektor.X, startPunkt.Y - lastVektor.Y);
                    pathFigure.Segments.Add(new LineSegment(punkt, false));
                    pathFigure.Segments.Add(new LineSegment(startPunkt, true));

                    // Linie vom Startpunkt zum Lastanfang
                    pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
                }

                //Linie zum Lastende
                lastVektor = RotateVectorScreen(vector, 90);
                lastVektor.Normalize();
                vec = lastVektor * lastAuflösung * lastWert;
                nextPunkt = new Point(endPunkt.X + vec.X, endPunkt.Y + vec.Y);
                pathFigure.Segments.Add(new LineSegment(nextPunkt, true));

                // Linie zum Endpunkt
                pathFigure.Segments.Add(new LineSegment(endPunkt, true));

                if (Math.Abs(vec.Length) > double.Epsilon)
                {
                    // Lastpfeil am Ende
                    lastVektor *= lastPfeilGroesse;
                    lastVektor = RotateVectorScreen(lastVektor, 30);
                    nextPunkt = new Point(endPunkt.X - lastVektor.X, endPunkt.Y - lastVektor.Y);
                    pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
                    lastVektor = RotateVectorScreen(lastVektor, -60);
                    nextPunkt = new Point(endPunkt.X - lastVektor.X, endPunkt.Y - lastVektor.Y);
                    pathFigure.Segments.Add(new LineSegment(nextPunkt, false));
                    pathFigure.Segments.Add(new LineSegment(endPunkt, true));
                }

                // schliess pathFigure zum Füllen
                //pathFigure.IsClosed = true;
                pathGeometry.Figures.Add(pathFigure);

                var myBrush = new SolidColorBrush(Colors.LightCoral);
                Shape path = new Path()
                {
                    Name = "Gleichlast" + index,
                    Stroke = Red,
                    Fill = myBrush,
                    StrokeThickness = 2,
                    Data = pathGeometry
                };

                // setz oben/links Position zum Zeichnen auf dem Canvas
                Canvas.SetLeft(path, PlazierungH);
                Canvas.SetTop(path, PlazierungV1);
                // zeichne Shape
                _visual.Children.Add(path);
            }
        }
    }

    public void Momentenverlauf()
    {
        var pathGeometry = new PathGeometry();
        var startPunkt = new Point(0, 0);
        var pathFigure = new PathFigure { StartPoint = startPunkt };
        var endPunkt = new Point(_dlt.Trägerlänge * Auflösung, 0);
        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape trägerPath = new Path()
        {
            Stroke = Black,
            StrokeThickness = 1,
            Data = pathGeometry
        };
        Canvas.SetLeft(trägerPath, PlazierungH);
        Canvas.SetTop(trägerPath, _plazierungV2);
        _visual.Children.Add(trägerPath);

        var maxMoment = Math.Abs(_dlt.Übertragungspunkte[0].Zr[2]);
        for (var i = 1; i < _dlt.Übertragungspunkte.Count; i++)
        {
            if (Math.Abs(_dlt.Übertragungspunkte[i].Zr[2]) > maxMoment)
            {
                maxMoment = Math.Abs(_dlt.Übertragungspunkte[i].Zr[2]);
            }

            if (!(Math.Abs(_dlt.Übertragungspunkte[i].Lastlänge) > 0)) continue;
            // maxMoment unter Gleichlast = ql^2/8
            var qll8 = _dlt.Übertragungspunkte[i].Lastwert *
                _dlt.Übertragungspunkte[i].Lastlänge * _dlt.Übertragungspunkte[i].Lastlänge / 8;
            if (qll8 > maxMoment) maxMoment = qll8;
        }
        // am Ende ist nur Zl definiert
        var ende = _dlt.Übertragungspunkte.Count - 1;
        if (Math.Abs(_dlt.Übertragungspunkte[ende].Zl[2]) > maxMoment)
        {
            maxMoment = Math.Abs(_dlt.Übertragungspunkte[ende].Zl[2]);
        }

        const int momentMaxScreen = 80;
        _momentenAuflösung = momentMaxScreen / maxMoment;

        pathFigure = new PathFigure { StartPoint = startPunkt };
        var nextPunkt = new Point(0, _dlt.Übertragungspunkte[0].Zr[2] * _momentenAuflösung);
        pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        _endIndex = _dlt.Übertragungspunkte.Count;
        for (var index = 1; index < _endIndex; index++)
        {
            var pi = _dlt.Übertragungspunkte[index];
            var pim1 = _dlt.Übertragungspunkte[index - 1];
            nextPunkt = new Point(pi.Position * Auflösung, pi.Zl[2] * _momentenAuflösung);

            if (pi.Lastlänge < double.Epsilon)
            {
                pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
            }
            else
            {
                // dM(x)/dx=0: Q-q*xMax=0  xMax = Q/q
                // var abstandMax = Math.Abs(pim1.Zr[3] / pi.Lastwert);
                const double anzahlProEinheit = 5;
                const double inkrement = 1 / anzahlProEinheit;
                var anzahl = (int)(pi.Lastlänge / inkrement);
                var polyLinePointArray = new Point[anzahl + 1];
                for (var i = 0; i <= anzahl; i++)
                {
                    // lokale x-Koordinate 0 <= x <= Lastlänge
                    var x = i * inkrement;
                    // M(x) = Ma + Qa*x - q*x*x/2
                    var m = pim1.Zr[2] + pim1.Zr[3] * x - pi.Lastwert * (i * inkrement) * (i * inkrement) / 2;
                    var mPoint = new Point((pim1.Position + x) * Auflösung, m * _momentenAuflösung);
                    polyLinePointArray[i] = mPoint;
                }
                var mSegment = new PolyLineSegment
                {
                    Points = new PointCollection(polyLinePointArray)
                };
                pathFigure.Segments.Add(mSegment);
            }
            pathGeometry.Figures.Add(pathFigure);
        }

        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape momentenPath = new Path()
        {
            Name = "Momentenlinie",
            Stroke = Black,
            StrokeThickness = 1,
            Data = pathGeometry
        };
        Canvas.SetLeft(momentenPath, PlazierungH);
        Canvas.SetTop(momentenPath, _plazierungV2);
        _visual.Children.Add(momentenPath);
    }

    public void Querkraftverlauf()
    {
        var pathGeometry = new PathGeometry();
        var startPunkt = new Point(0, 0);
        var pathFigure = new PathFigure { StartPoint = startPunkt };
        var endPunkt = new Point(_dlt.Trägerlänge * Auflösung, 0);
        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape trägerPath = new Path()
        {
            Stroke = Black,
            StrokeThickness = 1,
            Data = pathGeometry
        };
        Canvas.SetLeft(trägerPath, PlazierungH);
        Canvas.SetTop(trägerPath, _plazierungV3);
        _visual.Children.Add(trägerPath);

        var maxQuerkraft = Math.Abs(_dlt.Übertragungspunkte[0].Zr[3]);
        for (var i = 1; i < _dlt.Übertragungspunkte.Count - 1; i++)
        {
            if (Math.Abs(_dlt.Übertragungspunkte[i].Zr[3]) > maxQuerkraft)
            {
                maxQuerkraft = Math.Abs(_dlt.Übertragungspunkte[i].Zr[3]);
            }
            if (Math.Abs(_dlt.Übertragungspunkte[i].Zl[3]) > maxQuerkraft)
            {
                maxQuerkraft = Math.Abs(_dlt.Übertragungspunkte[i].Zl[3]);
            }
        }
        // am Ende ist nur Zl definiert
        var ende = _dlt.Übertragungspunkte.Count - 1;
        if (Math.Abs(_dlt.Übertragungspunkte[ende].Zl[3]) > maxQuerkraft)
        {
            maxQuerkraft = Math.Abs(_dlt.Übertragungspunkte[ende].Zl[3]);
        }

        const int querkraftMaxScreen = 50;
        _querkraftAuflösung = querkraftMaxScreen / maxQuerkraft;

        pathFigure = new PathFigure { StartPoint = startPunkt };
        var querkraftWert = _dlt.Übertragungspunkte[0].Zr[3];
        var nextPunkt = new Point(0, querkraftWert * _querkraftAuflösung);
        pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        _endIndex = _dlt.Übertragungspunkte.Count;
        for (var index = 1; index < _endIndex - 1; index++)
        {
            nextPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung,
                _dlt.Übertragungspunkte[index].Zl[3] * _querkraftAuflösung);
            pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
            pathGeometry.Figures.Add(pathFigure);

            nextPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung,
                _dlt.Übertragungspunkte[index].Zr[3] * _querkraftAuflösung);
            pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
            pathGeometry.Figures.Add(pathFigure);
        }

        nextPunkt = new Point(_dlt.Übertragungspunkte[_endIndex - 1].Position * Auflösung,
            _dlt.Übertragungspunkte[_endIndex - 1].Zl[3] * _querkraftAuflösung);
        pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape querkraftPath = new Path()
        {
            Stroke = Black,
            StrokeThickness = 1,
            Data = pathGeometry
        };
        Canvas.SetLeft(querkraftPath, PlazierungH);
        Canvas.SetTop(querkraftPath, _plazierungV3);
        _visual.Children.Add(querkraftPath);
    }

    public void MomentenTexteAnzeigen()
    {
        const int offset = 0;
        _endIndex = _dlt.Übertragungspunkte.Count - 1;

        // Momententexte
        var textPunkt = new Point(_dlt.Übertragungspunkte[0].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[0].Zr[2] * _momentenAuflösung + offset);
        var mText = _dlt.Übertragungspunkte[0].Zr[2];
        var schnittgrößenText = new TextBlock
        {
            Name = "Moment",
            FontSize = 12,
            Text = mText.ToString("F2"),
            Foreground = Red
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV2);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        MomentenTexte.Add(schnittgrößenText);

        for (var index = 1; index <= _endIndex; index++)
        {
            textPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung + offset,
                _dlt.Übertragungspunkte[index].Zl[2] * _momentenAuflösung + offset);
            mText = _dlt.Übertragungspunkte[index].Zl[2];
            schnittgrößenText = new TextBlock
            {
                Name = "Moment",
                FontSize = 12,
                Text = mText.ToString("F2"),
                Foreground = Red
            };
            Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV2);
            Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
            _visual.Children.Add(schnittgrößenText);
            MomentenTexte.Add(schnittgrößenText);
        }

        textPunkt = new Point(_dlt.Übertragungspunkte[_endIndex].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[_endIndex].Zl[2] * _momentenAuflösung + offset);
        mText = _dlt.Übertragungspunkte[_endIndex].Zl[2];
        schnittgrößenText = new TextBlock
        {
            Name = "Moment",
            FontSize = 12,
            Text = mText.ToString("F2"),
            Foreground = Red
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV2);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        MomentenTexte.Add(schnittgrößenText);
    }
    public void MomentenMaxTexteAnzeigen()
    {
        _endIndex = _dlt.Übertragungspunkte.Count - 1;

        // Momententexte
        for (var index = 1; index <= _endIndex; index++)
        {
            //  Text an Maximalmoment unter Gleichlast
            var pi = _dlt.Übertragungspunkte[index];
            var pim1 = _dlt.Übertragungspunkte[index - 1];
            if (!(pi.Lastlänge > double.Epsilon)) continue;
            var abstandMax = Math.Abs(pim1.Zr[3] / pi.Lastwert);
            if (abstandMax >= pi.Lastlänge) continue;
            var mMax = pim1.Zr[2] + pim1.Zr[3] * abstandMax - pi.Lastwert * abstandMax * abstandMax / 2;

            var textPunkt = new Point((pim1.Position + abstandMax) * Auflösung, mMax * _momentenAuflösung);
            var maxText = new TextBlock
            {
                FontSize = 12,
                Text = "max. Moment = " + mMax.ToString("F2"),
                Foreground = DarkRed
            };
            Canvas.SetTop(maxText, textPunkt.Y + _plazierungV2);
            Canvas.SetLeft(maxText, textPunkt.X + PlazierungH);
            _visual.Children.Add(maxText);
            MomentenMaxTexte.Add(maxText);
        }
    }
    public void QuerkraftTexteAnzeigen()
    {
        const int offset = 0;
        _endIndex = _dlt.Übertragungspunkte.Count - 1;


        // Querkrafttexte
        var textPunkt = new Point(_dlt.Übertragungspunkte[0].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[0].Zr[3] * _querkraftAuflösung + offset);
        var qText = _dlt.Übertragungspunkte[0].Zr[3];
        var schnittgrößenText = new TextBlock
        {
            FontSize = 12,
            Text = qText.ToString("F2"),
            Foreground = Blue
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        QuerkraftTexte.Add(schnittgrößenText);

        for (var index = 1; index < _endIndex; index++)
        {
            textPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung - offset,
                _dlt.Übertragungspunkte[index].Zl[3] * _querkraftAuflösung + offset);
            qText = _dlt.Übertragungspunkte[index].Zl[3];
            schnittgrößenText = new TextBlock
            {
                FontSize = 12,
                Text = qText.ToString("F2"),
                Foreground = Blue
            };
            Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
            Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
            _visual.Children.Add(schnittgrößenText);
            QuerkraftTexte.Add(schnittgrößenText);

            textPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung + offset,
                _dlt.Übertragungspunkte[index].Zr[3] * _querkraftAuflösung + offset);
            qText = _dlt.Übertragungspunkte[index].Zr[3];
            schnittgrößenText = new TextBlock
            {
                FontSize = 12,
                Text = qText.ToString("F2"),
                Foreground = Blue
            };
            Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
            Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
            _visual.Children.Add(schnittgrößenText);
            QuerkraftTexte.Add(schnittgrößenText);
        }

        textPunkt = new Point(_dlt.Übertragungspunkte[_endIndex].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[_endIndex].Zl[3] * _querkraftAuflösung + offset);
        qText = _dlt.Übertragungspunkte[_endIndex].Zl[3];
        schnittgrößenText = new TextBlock
        {
            FontSize = 12,
            Text = qText.ToString("F2"),
            Foreground = Blue
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        QuerkraftTexte.Add(schnittgrößenText);
    }

    public void MomentenTexteEntfernen()
    {
        foreach (var momentenText in MomentenTexte.Cast<TextBlock>())
        {
            _visual.Children.Remove(momentenText);
        }
    }
    public void MomentenMaxTexteEntfernen()
    {
        foreach (var momentenMaxText in MomentenMaxTexte.Cast<TextBlock>())
        {
            _visual.Children.Remove(momentenMaxText);
        }
    }
    public void QuerkraftTexteEntfernen()
    {
        foreach (var querkraftText in QuerkraftTexte.Cast<TextBlock>())
        {
            _visual.Children.Remove(querkraftText);
        }
    }

    public void ÜbertragungspunkteAnzeigen()
    {
        // Übertragungspunkte werden als EllipseGeometry hinzugefügt
        for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
        {
            var name = "Übertragungspunkt" + i;
            var punkt = new Point(_dlt.Übertragungspunkte[i].Position * Auflösung, 0);
            var übertragung = new EllipseGeometry(punkt, 5, 5);

            // Übertragungspunkte werden gezeichnet
            Shape üPunktSymbol = new Path()
            {
                Name = name,
                Stroke = Blue,
                StrokeThickness = 1,
                Data = übertragung
            };
            _visual.Children.Add(üPunktSymbol);
            Canvas.SetLeft(üPunktSymbol, PlazierungH);
            Canvas.SetTop(üPunktSymbol, PlazierungV1);
            ÜPunkte.Add(üPunktSymbol);

            var id = new TextBlock
            {
                Name = "Id",
                FontSize = 12,
                Text = i.ToString(),
                Foreground = Black
            };
            Canvas.SetTop(id, PlazierungV1);
            Canvas.SetLeft(id, _dlt.Übertragungspunkte[i].Position * Auflösung + PlazierungH + 5);
            _visual.Children.Add(id);
            KnotenIDs.Add(id);

            const int posOffset = 20;
            var pos = new TextBlock
            {
                Name = "Pos",
                FontSize = 12,
                Text = _dlt.Übertragungspunkte[i].Position.ToString(CultureInfo.CurrentCulture),
                Foreground = Black
            };
            Canvas.SetTop(pos, PlazierungV1 + posOffset);
            Canvas.SetLeft(pos, _dlt.Übertragungspunkte[i].Position * Auflösung + PlazierungH + 5);
            _visual.Children.Add(pos);
            KnotenIDs.Add(pos);
        }
    }
    public void ÜbertragungspunkteEntfernen()
    {
        foreach (var id in KnotenIDs.Cast<TextBlock>())
        {
            _visual.Children.Remove(id);
        }

        KnotenIDs.Clear();
        foreach (var üPunktSymbol in ÜPunkte.Cast<Shape>())
        {
            _visual.Children.Remove(üPunktSymbol);
        }

        ÜPunkte.Clear();
    }

    private static Point TransformPunkt(Übertragungspunkt knoten, double auflösung)
    {
        return new Point(knoten.Position * auflösung, 0);
    }
    private static Vector RotateVectorScreen(Vector vec, double winkel) // clockwise in degree
    {
        var vector = vec;
        var angle = winkel * Math.PI / 180;
        return new Vector(vector.X * Math.Cos(angle) - vector.Y * Math.Sin(angle),
            vector.X * Math.Sin(angle) + vector.Y * Math.Cos(angle));
    }
    public double[] TransformBildPunkt(Point point)
    {
        var koordinaten = new double[2];
        koordinaten[0] = (point.X - PlazierungH) / Auflösung;
        koordinaten[1] = (-point.Y + PlazierungV1) / Auflösung;
        return koordinaten;
    }

    //private static Vector OrthogonaleProjektion(Vector e1, Vector e2)
    //{
    //    var p = (e1.X * e2.X + e1.Y * e2.Y) / Math.Sqrt(e1.X * e1.X + e1.Y * e1.Y);
    //    e1.Normalize();
    //    return p * e1;
    //}
}