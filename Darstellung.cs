using System;
using System.Collections.Generic;
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
    private int _momentenAuflösung;
    private List<object> ÜPunkte { get; }
    private List<object> Texte { get; }
    private List<object> KnotenIDs { get; }

    public Darstellung(Modell dlt, Canvas visual)
    {
        _dlt = dlt;
        _visual = visual;
        ÜPunkte = new List<object>();
        Texte = new List<object>();
        KnotenIDs = new List<object>();
    }

    public void FestlegungAuflösung()
    {
        _screenH = _visual.ActualWidth;
        _screenV = _visual.ActualHeight;

        _maxX = _dlt.Trägerlänge;

        Auflösung = (_screenH - 2 * RandLinks) / _maxX;
        PlazierungH = RandLinks;

        PlazierungV1 = (int)(0.2 * _screenV);
        _plazierungV2 = (int)(0.5 * _screenV);
        _plazierungV3 = (int)(0.8 * _screenV);
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

        startPoint = new Point(endPoint.X + 5, endPoint.Y + 5);
        pathFigure.Segments.Add(new LineSegment(startPoint, false));
        endPoint = new Point(startPoint.X - 50, startPoint.Y);
        pathFigure.Segments.Add(new LineSegment(endPoint, true));

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
            if (Math.Abs(_dlt.Übertragungspunkte[i].Lastwert) > Math.Abs(maxLastWert))
                maxLastWert = Math.Abs(_dlt.Übertragungspunkte[i].Lastwert);
        }

        var lastAuflösung = (int)(maxLastScreen / maxLastWert);

        for (var index = 1; index < _endIndex + 1; index++)
        {
            var lastPunkt = _dlt.Übertragungspunkte[index];

            // Punktlasten
            if (lastPunkt.Lastlänge < double.Epsilon)
            {
                if (lastPunkt.Typ != 1) continue;
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();

                const int lastPfeilGroesse = 10;
                var lastWert = lastPunkt.Lastwert;
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
                    Stroke = Red,
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
            else
            {
                const int lastPfeilGroesse = 6;

                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();

                var lastWert = -lastPunkt.Lastwert;
                var lastStart = _dlt.Übertragungspunkte[index - 1];
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
                pathFigure.IsClosed = true;
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

        pathFigure = new PathFigure { StartPoint = startPunkt };
        _momentenAuflösung = 2;
        var nextPunkt = new Point(0, _dlt.Übertragungspunkte[0].Zr![2] * _momentenAuflösung);
        pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        _endIndex = _dlt.Übertragungspunkte.Count;
        for (var index = 1; index < _endIndex; index++)
        {
            var pi = _dlt.Übertragungspunkte[index];
            var pim1 = _dlt.Übertragungspunkte[index - 1];
            nextPunkt = new Point(pi.Position * Auflösung, pi.Zl![2] * _momentenAuflösung);

            if (pi.Lastlänge < double.Epsilon)
            {
                pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
            }
            else
            {
                var start = new Point(pim1.Position, pim1.Zr![2]);
                var ende = new Point(pi.Position, pi.Zl[2]);
                // dM(x)/dx=0: Q-q*xMax=0  xMax = Q/q
                var abstandMax = pim1.Zr![3] / pi.Lastwert;

                // Kontrollpunkt für quadratischen Bezier-Spline
                Point kontrollPunkt;
                var mMax = pim1.Zr![2] + pim1.Zr![3] * abstandMax - pi.Lastwert * abstandMax * abstandMax / 2;
                var maxPunkt = new Point(pim1.Position + abstandMax, mMax);
                const double überhöhung = 2;

                if (abstandMax > pi.Lastlänge)
                {
                    // MomentenMaxpunkt liegt hinter Übertragungspunkt
                    var proj = (Vector)maxPunkt + OrthogonaleProjektion(start-maxPunkt, ende-maxPunkt);
                    kontrollPunkt = proj + überhöhung*(ende - proj);
                }
                else
                {
                    // MomentenMaxpunkt liegt vor Übertragungspunkt
                    var proj = (Vector)ende + OrthogonaleProjektion(start-ende, maxPunkt-ende);
                    kontrollPunkt = proj + überhöhung*(maxPunkt - proj);
                }
                kontrollPunkt.X *= Auflösung;
                kontrollPunkt.Y *= _momentenAuflösung;
                pathFigure.Segments.Add(new QuadraticBezierSegment(kontrollPunkt, nextPunkt, true));
            }
            pathGeometry.Figures.Add(pathFigure);
        }

        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape momentenPath = new Path()
        {
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

        pathFigure = new PathFigure { StartPoint = startPunkt };
        var querkraftWert = _dlt.Übertragungspunkte[0].Zr![3];
        var querkraftAuflösung = 2;
        var nextPunkt = new Point(0, querkraftWert * querkraftAuflösung);
        pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        _endIndex = _dlt.Übertragungspunkte.Count;
        for (var index = 1; index < _endIndex - 1; index++)
        {
            nextPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung,
                _dlt.Übertragungspunkte[index].Zl![3] * querkraftAuflösung);
            pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
            pathGeometry.Figures.Add(pathFigure);

            nextPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung,
                _dlt.Übertragungspunkte[index].Zr![3] * querkraftAuflösung);
            pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
            pathGeometry.Figures.Add(pathFigure);
        }

        nextPunkt = new Point(_dlt.Übertragungspunkte[_endIndex - 1].Position * Auflösung,
            _dlt.Übertragungspunkte[_endIndex - 1].Zl![3] * querkraftAuflösung);
        pathFigure.Segments.Add(new LineSegment(nextPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        pathFigure.Segments.Add(new LineSegment(endPunkt, true));
        pathGeometry.Figures.Add(pathFigure);

        Shape momentenPath = new Path()
        {
            Stroke = Black,
            StrokeThickness = 1,
            Data = pathGeometry
        };
        Canvas.SetLeft(momentenPath, PlazierungH);
        Canvas.SetTop(momentenPath, _plazierungV3);
        _visual.Children.Add(momentenPath);
    }

    public void TexteAnzeigen()
    {
        const int offset = 0;
        _endIndex = _dlt.Übertragungspunkte.Count - 1;

        // Momententexte
        var textPunkt = new Point(_dlt.Übertragungspunkte[0].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[0].Zr![2] * _momentenAuflösung + offset);
        var mText = _dlt.Übertragungspunkte[0].Zr![2];
        var schnittgrößenText = new TextBlock
        {
            FontSize = 12,
            Text = mText.ToString("F2"),
            Foreground = Red
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV2);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        Texte.Add(schnittgrößenText);

        for (var index = 1; index <= _endIndex; index++)
        {
            textPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung + offset,
                _dlt.Übertragungspunkte[index].Zl![2] * _momentenAuflösung + offset);
            mText = _dlt.Übertragungspunkte[index].Zl![2];
            schnittgrößenText = new TextBlock
            {
                FontSize = 12,
                Text = mText.ToString("F2"),
                Foreground = Red
            };
            Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV2);
            Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
            _visual.Children.Add(schnittgrößenText);
            Texte.Add(schnittgrößenText);

            //  Text an Maximalmoment unter Gleichlast
            var pi = _dlt.Übertragungspunkte[index];
            var pim1 = _dlt.Übertragungspunkte[index - 1];
            if (!(pi.Lastlänge > double.Epsilon)) continue;
            var abstandMax = pim1.Zr![3] / pi.Lastwert;
            if (abstandMax >= pi.Lastlänge) continue;
            var mMax = pim1.Zr![2] + pim1.Zr![3] * abstandMax - pi.Lastwert * abstandMax * abstandMax / 2;

            textPunkt = new Point((pim1.Position + abstandMax) * Auflösung - 5, mMax * _momentenAuflösung);
            var maxText = new TextBlock
            {
                FontSize = 12,
                Text = mMax.ToString("F2"),
                Foreground = DarkRed
            };
            Canvas.SetTop(maxText, textPunkt.Y + _plazierungV2);
            Canvas.SetLeft(maxText, textPunkt.X + PlazierungH);
            _visual.Children.Add(maxText);
            Texte.Add(maxText);
        }

        textPunkt = new Point(_dlt.Übertragungspunkte[_endIndex].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[_endIndex].Zl![2] * _momentenAuflösung + offset);
        mText = _dlt.Übertragungspunkte[_endIndex].Zl![2];
        schnittgrößenText = new TextBlock
        {
            FontSize = 12,
            Text = mText.ToString("F2"),
            Foreground = Red
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV2);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        Texte.Add(schnittgrößenText);


        // Querkrafttexte
        textPunkt = new Point(_dlt.Übertragungspunkte[0].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[0].Zr![3] * _momentenAuflösung + offset);
        var qText = _dlt.Übertragungspunkte[0].Zr![3];
        schnittgrößenText = new TextBlock
        {
            FontSize = 12,
            Text = qText.ToString("F2"),
            Foreground = Blue
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        Texte.Add(schnittgrößenText);

        for (var index = 1; index < _endIndex; index++)
        {
            textPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung - offset,
                _dlt.Übertragungspunkte[index].Zl![3] * _momentenAuflösung + offset);
            qText = _dlt.Übertragungspunkte[index].Zl![3];
            schnittgrößenText = new TextBlock
            {
                FontSize = 12,
                Text = qText.ToString("F2"),
                Foreground = Blue
            };
            Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
            Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
            _visual.Children.Add(schnittgrößenText);
            Texte.Add(schnittgrößenText);

            textPunkt = new Point(_dlt.Übertragungspunkte[index].Position * Auflösung + offset,
                _dlt.Übertragungspunkte[index].Zr![3] * _momentenAuflösung + offset);
            qText = _dlt.Übertragungspunkte[index].Zr![3];
            schnittgrößenText = new TextBlock
            {
                FontSize = 12,
                Text = qText.ToString("F2"),
                Foreground = Blue
            };
            Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
            Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
            _visual.Children.Add(schnittgrößenText);
            Texte.Add(schnittgrößenText);
        }

        textPunkt = new Point(_dlt.Übertragungspunkte[_endIndex].Position * Auflösung + offset,
            _dlt.Übertragungspunkte[_endIndex].Zl![3] * _momentenAuflösung + offset);
        qText = _dlt.Übertragungspunkte[_endIndex].Zl![3];
        schnittgrößenText = new TextBlock
        {
            FontSize = 12,
            Text = qText.ToString("F2"),
            Foreground = Blue
        };
        Canvas.SetTop(schnittgrößenText, textPunkt.Y + _plazierungV3);
        Canvas.SetLeft(schnittgrößenText, textPunkt.X + PlazierungH);
        _visual.Children.Add(schnittgrößenText);
        Texte.Add(schnittgrößenText);
    }

    public void TexteEntfernen()
    {
        foreach (TextBlock schnittgrößenText in Texte.Cast<TextBlock>())
        {
            _visual.Children.Remove(schnittgrößenText);
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
                FontSize = 12,
                Text = i.ToString(),
                Foreground = Black
            };
            Canvas.SetTop(id, PlazierungV1);
            Canvas.SetLeft(id, _dlt.Übertragungspunkte[i].Position * Auflösung + PlazierungH + 5);
            _visual.Children.Add(id);
            KnotenIDs.Add(id);
        }
    }

    public void ÜbertragungspunkteEntfernen()
    {
        foreach (TextBlock id in KnotenIDs.Cast<TextBlock>())
        {
            _visual.Children.Remove(id);
        }

        KnotenIDs.Clear();
        foreach (Shape üPunktSymbol in ÜPunkte)
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

    private static Point OrthogonaleProjektion(Vector e1, Vector e2)
    {
        var p = (e1.X*e2.X+e1.Y*e2.Y)/Math.Sqrt(e1.X*e1.X+e1.Y*e1.Y);
        e1.Normalize();
        return (Point)(p * e1);
    }
}