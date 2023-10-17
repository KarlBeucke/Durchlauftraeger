using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Durchlauftraeger;

public partial class MainWindow
{
    private readonly Modell _dlt;
    public static Berechnung? Berechnung;
    private readonly Darstellung _darstellung;
    private DialogNeuerTräger? _träger;
    private DialogEinspannung? _einspannung;
    private DialogLager? _lager;
    private DialogPunktlast? _punktlast;
    private DialogGleichlast? _gleichlast;
    private bool _momentenTexteAn, _momentMaxTexteAn, _querkraftTexteAn, _üPunkteAn;

    private Point _mittelpunkt;
    private bool _isDragging;

    //alle gefundenen "Shapes" werden in dieser Liste gesammelt
    private List<Shape>? _hitList;
    private EllipseGeometry? _hitArea;
    //alle gefundenen "TextBlocks" werden in dieser Liste gesammelt
    private readonly List<TextBlock> _hitTextBlock = new();

    public MainWindow()
    {
        InitializeComponent();
        _dlt = new Modell();
        _darstellung = new Darstellung(_dlt, DltVisuell);
        Berechnung = new Berechnung(_dlt, _darstellung, DltVisuell);
        _momentenTexteAn = true;
        _querkraftTexteAn = true;
        _üPunkteAn = true;
    }

    private void NeuerTräger(object sender, RoutedEventArgs e)
    {
        DltVisuell.Children.Clear();
        _dlt.KeineLast = true;
        _träger = new DialogNeuerTräger(_dlt) { Topmost = true, Owner = (Window)Parent };
        _träger.ShowDialog();
        _darstellung.FestlegungAuflösung();
        Berechnung?.Neuberechnung();
    }

    private void EinspannungÄndern(object sender, RoutedEventArgs e)
    {
        DltVisuell.Children.Clear();
        _einspannung = new DialogEinspannung(_dlt)
        {
            Topmost = true,
            Owner = (Window)Parent,
        };
        if (_dlt.AnfangFest && _dlt.EndeFest)
        {
            _einspannung.EinspannungAnfang.IsChecked = true;
            _einspannung.EinspannungEnde.IsChecked = true;
        }
        else if (_dlt.AnfangFest) _einspannung.EinspannungAnfang.IsChecked = true;
        else if (_dlt.EndeFest) _einspannung.EinspannungEnde.IsChecked = true;
        _einspannung.ShowDialog();

        if (_dlt.AnfangFest)
        {
            // eingespannter Rand am Trägeranfang, wa = phia = 0
            var zStartFest = new double[4, 2];
            zStartFest[2, 0] = 1;
            zStartFest[3, 1] = 1;
            // zaL = zStartFest * (Ma, Qa)
            _dlt.Übertragungspunkte[0].Z = zStartFest;
        }
        else
        {
            // gelenkiger Rand am Trägeranfang, wa = Ma = 0
            var zStartGelenk = new double[4, 2];
            zStartGelenk[1, 0] = 1;
            zStartGelenk[3, 1] = 1;
            // zaL = zStartGelenk * (phia, Qa)
            _dlt.Übertragungspunkte[0].Z = zStartGelenk;
        }
        Berechnung?.Neuberechnung();
    }

    private void NeuesLager(object sender, RoutedEventArgs e)
    {
        DltVisuell.Children.Clear();
        _lager = new DialogLager(_dlt) { Topmost = true, Owner = (Window)Parent };
        _lager.ShowDialog();
        Berechnung?.Neuberechnung();
    }

    private void NeuePunktlast(object sender, RoutedEventArgs e)
    {
        DltVisuell.Children.Clear();
        _dlt.KeineLast = false;
        _punktlast = new DialogPunktlast(_dlt) { Topmost = true, Owner = (Window)Parent };
        _punktlast.ShowDialog();
        Berechnung?.Neuberechnung();
    }
    private void NeueGleichlast(object sender, RoutedEventArgs e)
    {
        DltVisuell.Children.Clear();
        _dlt.KeineLast = false;
        _gleichlast = new DialogGleichlast(_dlt) { Topmost = true, Owner = (Window)Parent };
        _gleichlast.ShowDialog();
        Berechnung?.Neuberechnung();
    }

    public class OrdneAufsteigendeKoordinaten : IComparer<Übertragungspunkt>
    {
        public int Compare(Übertragungspunkt? x, Übertragungspunkt? y)
        {
            var comparePosition = x!.Position.CompareTo(y!.Position);
            return comparePosition == 3 ? x.Position.CompareTo(y.Position) : comparePosition;
        }
    }

    private void NeueBerechnung(object sender, RoutedEventArgs e)
    {
        Berechnung?.Neuberechnung();
    }

    private void MomentenTexteAnzeigen(object sender, RoutedEventArgs e)
    {
        if (_momentenTexteAn)
        {
            _darstellung.MomentenTexteEntfernen();
            _momentenTexteAn = false;
        }
        else
        {
            _darstellung.MomentenTexteAnzeigen();
            _momentenTexteAn = true;
        }
    }

    private void QuerkraftTexteAnzeigen(object sender, RoutedEventArgs e)
    {
        if (_querkraftTexteAn)
        {
            _darstellung.QuerkraftTexteEntfernen();
            _querkraftTexteAn = false;
        }
        else
        {
            _darstellung.QuerkraftTexteAnzeigen();
            _querkraftTexteAn = true;
        }
    }

    private void ÜbertragungspunkteAnzeigen(object sender, RoutedEventArgs e)
    {
        if (_üPunkteAn)
        {
            _darstellung.ÜbertragungspunkteEntfernen();
            _üPunkteAn = false;
        }
        else
        {
            _darstellung.ÜbertragungspunkteAnzeigen();
            _üPunkteAn = true;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _hitList = new List<Shape>();
        _hitTextBlock.Clear();
        var hitPoint = e.GetPosition(DltVisuell);
        _hitArea = new EllipseGeometry(hitPoint, 10, 10);
        VisualTreeHelper.HitTest(DltVisuell, null, HitTestCallBack,
            new GeometryHitTestParameters(_hitArea));
  
        // click auf Übertragungspunkt ID --> Eigenschaften eines Übertragungspunktes werden dargestellt
        foreach (var item in _hitTextBlock)
        {
            if (_dlt == null || item.Name != "Id") continue;
            var index = int.Parse(item.Text);
            var sb = new StringBuilder();
            sb.Append("Id = " + item.Text + "\n");

            if (index == 0) // Punkt am Trägeranfang
            {
                var zr = _dlt.Übertragungspunkte[index].Zr;
                sb.Append("w rechts\t= " + zr[0].ToString("g3") + "\n");
                sb.Append("\u03c6 rechts\t= " + zr[1].ToString("g3") + "\n");
                sb.Append("M rechts\t= " + zr[2].ToString("g3") + "\n");
                sb.Append("Q rechts\t= " + zr[3].ToString("g3"));
            }
            else if (index < _dlt.Übertragungspunkte.Count - 1)
            {
                var zl = _dlt.Übertragungspunkte[index].Zl; // Punkt im Trägerspannbereich
                sb.Append("w links\t= " + zl[0].ToString("g3") + "\n");
                sb.Append("\u03c6 links\t= " + zl[1].ToString("g3") + "\n");
                sb.Append("M links\t= " + zl[2].ToString("g3") + "\n");
                sb.Append("Q links\t= " + zl[3].ToString("g3") + "\n" + "\n");

                var zr = _dlt.Übertragungspunkte[index].Zr;
                sb.Append("w rechts\t= " + zr[0].ToString("g3") + "\n");
                sb.Append("\u03c6 rechts\t= " + zr[1].ToString("g3") + "\n");
                sb.Append("M rechts\t= " + zr[2].ToString("g3") + "\n");
                sb.Append("Q rechts\t= " + zr[3].ToString("g3"));
            }

            else if (index == _dlt.Übertragungspunkte.Count - 1) // Punkt am Trägerende
            {
                var zl = _dlt.Übertragungspunkte[index].Zl;
                sb.Append("w links\t= " + zl[0].ToString("g3") + "\n");
                sb.Append("\u03c6 links\t= " + zl[1].ToString("g3") + "\n");
                sb.Append("M links\t= " + zl[2].ToString("g3") + "\n");
                sb.Append("Q links\t= " + zl[3].ToString("g3") + "\n" + "\n");
            }
            MyPopupText.Text = sb.ToString();
            MyPopup.IsOpen = true;
            return;
        }

        // click auf Shape Darstellungen

        // grafische Darstellung von Lasten, Lagern und Momentenlinien
        foreach (var item in _hitList.Where(item => !string.IsNullOrEmpty(item.Name)))
        {
            if (item.Name.Contains("Punktlast"))
            {
                //Übertragungspunkt
                var startIndex = "Punktlast".Length;
                var index = int.Parse(item.Name[startIndex..]);
                var punkt = _dlt!.Übertragungspunkte[index];
                Array.Clear(_dlt.Übertragungspunkte[index].Zl);
                Array.Clear(_dlt.Übertragungspunkte[index].Zr);
                _punktlast = new DialogPunktlast(_dlt, index, Berechnung, DltVisuell)
                {
                    //Topmost = true, Owner = (Window)Parent,
                    Position = { Text = punkt.Position.ToString("N2", CultureInfo.CurrentCulture) },
                    Lastwert = { Text = (-punkt.Punktlast[3]).ToString("N2", CultureInfo.CurrentCulture) } 
                };
                _mittelpunkt = new Point(punkt.Position * _darstellung.Auflösung + _darstellung.PlazierungH,
                    _darstellung.PlazierungV1);
                Canvas.SetLeft(Punkt, _mittelpunkt.X - Punkt.Width / 2);
                Canvas.SetTop(Punkt, _mittelpunkt.Y - Punkt.Height / 2);
                DltVisuell.Children.Add(Punkt);
                _punktlast.Show();
            }

            if (item.Name.Contains("Gleichlast"))
            {
                // Index des Übertragungspunktes am Ende der Gleichlast angehängt an "Gleichlast"
                var länge = "Gleichlast".Length;
                // Der Range Operator (..) wird benutzt als Abkürzung für den Zugriff auf arrays
                // binär(a..b): von-bis, unär(a..): von-Ende
                var endIndex = int.Parse(item.Name[länge..]);
                var anfangIndex = 0;
                // finde Übertragungspunkt am Anfang der Gleichlast
                var piE = _dlt!.Übertragungspunkte[endIndex];
                for (var i = 0; i < _dlt.Übertragungspunkte.Count; i++)
                {
                    if (Math.Abs(_dlt.Übertragungspunkte[i].Position -
                                 (piE.Position - piE.Lastlänge)) > double.Epsilon) continue;
                    anfangIndex = i;
                    break;
                }

                // Übertragungspunkt am Ende enthält Lastlänge und -wert
                DialogGleichlast gleichlast = new(_dlt, endIndex)
                {
                    Anfang = { Text = _dlt.Übertragungspunkte[anfangIndex].Position.ToString("G4") },
                    Länge = { Text = _dlt.Übertragungspunkte[endIndex].Lastlänge.ToString("G4") },
                    Lastwert = { Text = (_dlt.Übertragungspunkte[endIndex].Lastwert).ToString("G4") }
                };
                MyPopup.IsOpen = false;
                gleichlast.ShowDialog();
                DltVisuell.Children.Clear();
                Berechnung?.Neuberechnung();
            }

            if (item.Name.Contains("Lager"))
            {
                var startIndex = "Lager".Length;
                var index = int.Parse(item.Name[startIndex..]);

                //Übertragungspunkt
                if (_dlt?.Übertragungspunkte[index] == null) continue;
                var lager = new DialogLager(_dlt, index)
                {
                    LagerPosition = { Text = _dlt.Übertragungspunkte[index].Position.ToString("G4") },
                };
                MyPopup.IsOpen = false;
                lager.ShowDialog();
                DltVisuell.Children.Clear();
                Berechnung?.Neuberechnung();
            }

            if (!item.Name.Contains("Momentenlinie")) continue;
            {
                // click auf Momentenlinie --> Maximalmomente unter Gleichlast werden angezeigt
                if (_momentMaxTexteAn)
                {
                    _darstellung.MomentenMaxTexteEntfernen();
                    _momentMaxTexteAn = false;
                }
                else
                {
                    _darstellung.MomentenMaxTexteAnzeigen();
                    _momentMaxTexteAn = true;
                }
            }
        }
    }
    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        MyPopup.IsOpen = false;
    }
    private HitTestResultBehavior HitTestCallBack(HitTestResult result)
    {
        //_hitList=new List<Shape>();
        var intersectionDetail = ((GeometryHitTestResult)result).IntersectionDetail;

        switch (intersectionDetail)
        {
            case IntersectionDetail.Empty:
                return HitTestResultBehavior.Continue;
            case IntersectionDetail.FullyContains:
                switch (result.VisualHit)
                {
                    case Shape hit:
                        _hitList?.Add(hit);
                        break;
                }
                return HitTestResultBehavior.Continue;
            case IntersectionDetail.FullyInside:
                switch (result.VisualHit)
                {
                    case Shape hit:
                        _hitList?.Add(hit);
                        break;
                    case TextBlock hit:
                        _hitTextBlock.Add(hit);
                        break;
                }
                return HitTestResultBehavior.Continue;
            case IntersectionDetail.Intersects:
                switch (result.VisualHit)
                {
                    case Shape hit:
                        _hitList?.Add(hit);
                        break;
                }
                return HitTestResultBehavior.Continue;
            case IntersectionDetail.NotCalculated:
                return HitTestResultBehavior.Continue;
            default:
                return HitTestResultBehavior.Continue;
        }
    }

    private void Punkt_MouseEnter(object sender, MouseEventArgs e)
    {
        Punkt.CaptureMouse();
        _isDragging = true;
        MyPopup.IsOpen = false;
    }
    private void Punkt_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var canvPosToWindow = DltVisuell.TransformToAncestor(this).Transform(new Point(0, 0));

        if (sender is not Ellipse knoten) return;
        var upperlimit = canvPosToWindow.Y + knoten.Height / 2;
        var lowerlimit = canvPosToWindow.Y + DltVisuell.ActualHeight - knoten.Height / 2;

        var leftlimit = canvPosToWindow.X + knoten.Width / 2;
        var rightlimit = canvPosToWindow.X + DltVisuell.ActualWidth - knoten.Width / 2;


        var absmouseXpos = e.GetPosition(this).X;
        var absmouseYpos = e.GetPosition(this).Y;

        if (!(absmouseXpos > leftlimit) || !(absmouseXpos < rightlimit)
                                        || !(absmouseYpos > upperlimit) || !(absmouseYpos < lowerlimit)) return;

        var mittelpunkt = new Point(e.GetPosition(DltVisuell).X, e.GetPosition(DltVisuell).Y);

        Canvas.SetLeft(knoten, mittelpunkt.X - Punkt.Width / 2);
        Canvas.SetTop(knoten, mittelpunkt.Y - Punkt.Height / 2);

        var koordinate = _darstellung.TransformBildPunkt(mittelpunkt);
        _punktlast!.Position.Text = koordinate[0].ToString("N2", CultureInfo.CurrentCulture);
    }

    private void Punkt_RightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Punkt.ReleaseMouseCapture();
        _isDragging = false;
    }
}