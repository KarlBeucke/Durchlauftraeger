using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Durchlauftraeger;

public partial class MainWindow
{
    private Modell? _dlt;
    private DialogNeuerTräger? _neuerTräger;
    private DialogEinspannung? _einspannung;
    private DialogLager? _neuesLager;
    private DialogPunktlast? _neuePunktlast;
    private DialogGleichlast? _neueGleichlast;
    private Darstellung? _darstellung;
    private bool _texteAn;

    //alle gefundenen "Shapes" werden in dieser Liste gesammelt
    private List<Shape>? _hitList;
    private EllipseGeometry? _hitArea;
    public MainWindow()
    {
        InitializeComponent();
        _neuerTräger = null;
        _einspannung = null;
        _neuesLager = null;
        _neuePunktlast = null;
        _neueGleichlast = null;
        _darstellung = null;
        _texteAn = true;
    }

    private void NeuerTräger(object sender, RoutedEventArgs e)
    {
        VisualErgebnisse.Children.Clear();
        _dlt = new Modell();
        _neuerTräger = new DialogNeuerTräger(_dlt) { Topmost = true, Owner = (Window)Parent };
        _neuerTräger.ShowDialog();
        Neuberechnung();
    }

    private void EinspannungÄndern(object sender, RoutedEventArgs e)
    {
        VisualErgebnisse.Children.Clear();
        _einspannung = new DialogEinspannung(_dlt)
        {
            Topmost = true,
            Owner = (Window)Parent,
        };
        if (_dlt!.AnfangFest && _dlt.EndeFest)
        {
            _einspannung.EinspannungAnfang.IsChecked = true;
            _einspannung.EinspannungEnde.IsChecked = true;
        }
        else if (_dlt.AnfangFest) _einspannung.EinspannungAnfang.IsChecked = true;
        else if (_dlt.EndeFest) _einspannung.EinspannungEnde.IsChecked = true;
        _einspannung.ShowDialog();

        if (_dlt!.AnfangFest)
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
        Neuberechnung();
    }

    private void NeuesLager(object sender, RoutedEventArgs e)
    {
        VisualErgebnisse.Children.Clear();
        _neuesLager = new DialogLager(_dlt) { Topmost = true, Owner = (Window)Parent };
        _neuesLager.ShowDialog();
        Neuberechnung();
    }

    private void NeuePunktlast(object sender, RoutedEventArgs e)
    {
        VisualErgebnisse.Children.Clear();
        _neuePunktlast = new DialogPunktlast(_dlt) { Topmost = true, Owner = (Window)Parent };
        _neuePunktlast.ShowDialog();
        Neuberechnung();
    }
    private void NeueGleichlast(object sender, RoutedEventArgs e)
    {
        VisualErgebnisse.Children.Clear();
        _neueGleichlast = new DialogGleichlast(_dlt) { Topmost = true, Owner = (Window)Parent };
        _neueGleichlast.ShowDialog();
        Neuberechnung();
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
        Neuberechnung();
    }

    private void Neuberechnung()
    {
        var kk1Inv = new double[2, 2];
        double[] lk;
        var rs = Array.Empty<double>();

        // Sortierung der Übertragungspunkte nach aufsteigender Position,+ x-Koordinate
        IComparer<Übertragungspunkt> comparer = new OrdneAufsteigendeKoordinaten();
        _dlt!.Übertragungspunkte.Sort(comparer);

        // Aufteilung in Felder zwischen Auflagern
        // Anzahl Felder wird in Liste gesammelt,
        // jede Liste enthält eine weitere Liste mit Übertragungspunkte des Feldes
        var felder = new List<List<int>>();
        var punkte = new List<int> { 0 };
        var keineLast = true;
        for (var i = 1; i < _dlt.Übertragungspunkte.Count; i++)
        {
            if (_dlt.Übertragungspunkte[i].Typ == 1) keineLast = false;
            punkte.Add(i);
            if (_dlt.Übertragungspunkte[i].Typ != 3) continue;
            felder.Add(punkte);
            punkte = new List<int> { i };
        }

        if (keineLast)
        {
            var nurTräger = new Darstellung(_dlt, VisualErgebnisse);
            nurTräger.FestlegungAuflösung();

            nurTräger.TrägerDarstellen();
            return;
        }

        VisualErgebnisse.Children.Clear();

        for (var i = 0; i < felder.Count; i++)
        {
            double[] Lk;
            rs = new double[2];

            // ein Feld hat mehrere Abschnitte
            for (var k = 1; k < felder[i].Count; k++)
            {
                double l;
                var pik = _dlt.Übertragungspunkte[felder[i][k]];
                var pikm1 = _dlt.Übertragungspunkte[felder[i][k - 1]];

                // zusätzliche neue Felder werden über eine Kopplungmatrix angeschlossen
                double[,] Z;
                if (i > 0 && k == 1)
                {
                    Z = _dlt.Übertragungspunkte[felder[i][0]].AnfangKopplung!;
                    Lk = _dlt.Übertragungspunkte[felder[i][0]].Lk!;
                }
                else
                {
                    Z = pikm1.Z!;
                    Lk = pikm1.LastÜ!;
                }

                switch (pik.Typ)
                {
                    // freier Abschnitt
                    case 0:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, Z);
                        break;
                    // Abschnitt mit Last
                    case 1:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        // Punktlast
                        if (pik.Lastlänge < double.Epsilon)
                        {
                            pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, Z);
                            var lkÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, Lk);
                            pik.LastÜ = Werkzeuge.VectorVectorAdd(lkÜ, pik.Last!);
                        }
                        // Gleichlast
                        else
                        {
                            pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, Z);
                            var lkÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, Lk);
                            pik.LastÜ = Werkzeuge.VectorVectorAdd(lkÜ, pik.Last!);
                        }
                        break;
                    // Abschnitt mit Lager am Ende, Übertragung des Zustandsvektors mit Federkopplung auf nächsten Abschnitt
                    case 3:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, Z);
                        pik.LastÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, Lk);
                        break;
                }
            }

            if (felder.Count == 1)
            {
                Einfeldträger();
                _darstellung = new Darstellung(_dlt!, VisualErgebnisse);
                _darstellung.FestlegungAuflösung();
                _darstellung.TrägerDarstellen();
                _darstellung.Momentenverlauf();
                _darstellung.Querkraftverlauf();
                _darstellung.TexteAnzeigen();
                return;
            }

            // Mehrfeldträger, nicht im letzten Feld
            if (i >= felder.Count - 1) continue;
            // Koppelfedermatrix mit Lastterm lK
            var kk1 = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[felder[i].Count - 1].Z!, 0, 1);
            var kk2 = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[felder[i].Count - 1].Z!, 2, 3);
            kk1Inv = Werkzeuge.Matrix2By2Inverse(kk1);

            var kk = Werkzeuge.MatrixMatrixMultiply(kk2, kk1Inv);
            var l1 = Werkzeuge.SubVektor(_dlt.Übertragungspunkte[felder[i].Count - 1].LastÜ!, 0, 1);
            var l2 = Werkzeuge.SubVektor(_dlt.Übertragungspunkte[felder[i].Count - 1].LastÜ!, 2, 3);
            lk = Werkzeuge.MatrixVectorMultiply(kk, l1);
            lk = Werkzeuge.VectorVectorMinus(l2, lk);

            // Anfangsvektor nächstes Feld
            double[,] kopplung = { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { kk[0, 0], kk[0, 1], 1, 0 }, { kk[1, 0], kk[1, 1], 0, 1 } };
            double[,] anfang = { { 0, 0 }, { 1, 0 }, { 0, 0 }, { 0, 1 } };
            _dlt.Übertragungspunkte[felder[i + 1][0]].AnfangKopplung = Werkzeuge.MatrixMatrixMultiply(kopplung, anfang);

            _dlt.Übertragungspunkte[felder[i + 1][0]].Lk![2] = lk[0];
            _dlt.Übertragungspunkte[felder[i + 1][0]].Lk![3] = lk[1];
        }

        // letztes Feld der Übertragung
        if (_dlt.EndeFest)  // gilt fuer beide AnfangFest UND EndeFest und nur EndeFest
        {
            // eingespanntes Lager am Ende: we = phie = 0
            var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[^1].Z!,
                0, 1);
            lk = _dlt.Übertragungspunkte[felder[^1][^1]].LastÜ!;

            rs = Werkzeuge.SubVektor(lk, 0, 1);
            rs[0] = -rs[0];
            rs[1] = -rs[1];
            var gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
        }
        else if (_dlt.AnfangFest)
        {
            // gelenkiges Lager am Ende: we = Me = 0
            var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[^1].Z!,
                0, 2);
            lk = _dlt.Übertragungspunkte[felder[^1][^1]].LastÜ!;

            rs = Werkzeuge.SubVektor(lk, 0, 2);
            rs[0] = -rs[0];
            rs[1] = -rs[1];
            var gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
        }

        // Endvektor im letzten Feld, indexEnde = felder[^1].Count - 1;
        var pEe = _dlt.Übertragungspunkte[felder[^1][^1]];
        pEe.ZL = Werkzeuge.MatrixVectorMultiply(pEe.Z!, rs);
        pEe.ZL = Werkzeuge.VectorVectorAdd(pEe.ZL!, pEe.LastÜ!);

        // Anfangsvektor im letzten Feld
        var pE0 = _dlt.Übertragungspunkte[felder[^1][0]];
        var vektor4 = Werkzeuge.MatrixVectorMultiply(pE0.AnfangKopplung!, rs);
        pE0.ZR = Werkzeuge.VectorVectorAdd(vektor4, pE0.Lk!);

        // Zustandsvektor an Zwischenpunkten im letzten Feld
        for (var index = 1; index < felder[^1].Count - 1; index++)
        {
            var pkE = _dlt.Übertragungspunkte[felder[^1][index]];
            pkE.ZL = Werkzeuge.MatrixVectorMultiply(pkE.Z!, rs);
            pkE.ZL = Werkzeuge.VectorVectorAdd(pkE.ZL!, pkE.LastÜ!);
            // Gleichlast: kein neuer Lasteintrag am Ende
            pkE.ZR = !(pkE.Lastlänge > 0) ? Werkzeuge.VectorVectorAdd(pkE.ZL, pkE.Last!) : pkE.ZL;
        }

        // erneute Übertragung über alle vorhergehenden Felder
        for (var i = 0; i < felder.Count - 1; i++)
        {
            // Anfangsvektor im Feld
            var vektor2 = new double[2];
            // Kopplung: wc = 0, phic = z2L[3]
            var p00 = _dlt.Übertragungspunkte[felder[i][0]];
            p00.ZR = new double[4];
            vektor2[0] = _dlt.Übertragungspunkte[felder[i + 1][0]].ZR![0] -
                         _dlt.Übertragungspunkte[felder[i][^1]].LastÜ![0];
            vektor2[1] = _dlt.Übertragungspunkte[felder[i + 1][0]].ZR![1] -
                         _dlt.Übertragungspunkte[felder[i][^1]].LastÜ![1];
            vektor2 = Werkzeuge.MatrixVectorMultiply(kk1Inv, vektor2);

            // AnfangFest und BeideFest: Anfang M=vektor2[0]
            if (_dlt.AnfangFest) p00.ZR![2] = vektor2[0];
            // nur EndeFest: Anfang             phi=vektor2[0]
            if (_dlt.EndeFest && !_dlt.AnfangFest) p00.ZR![1] = vektor2[0];
            // in allen Fällen: Anfang          Q=vektor2[1]
            p00.ZR![3] = vektor2[1];

            // Zustandsvektor an Zwischenpunkten im Feld
            for (var index = 1; index < felder[i].Count - 1; index++)
            {
                var piK = _dlt.Übertragungspunkte[felder[i][index]];
                var piKm1 = _dlt.Übertragungspunkte[felder[i][index - 1]];

                piK.ZL = Werkzeuge.MatrixVectorMultiply(piK.A!, piKm1.ZR!);
                piK.ZR = Werkzeuge.VectorVectorAdd(piK.ZL!, piK.Last!);
            }

            // Endvektor im Feld
            var pEm1 = _dlt.Übertragungspunkte[felder[i].Count - 1];
            vektor2 = Werkzeuge.SubVektor(_dlt.Übertragungspunkte[felder[i][0]].ZR!, 2, 3);
            pEm1.ZL = Werkzeuge.MatrixVectorMultiply(pEm1.Z!, vektor2);
            pEm1.ZL = Werkzeuge.VectorVectorAdd(pEm1.ZL!, pEm1.LastÜ!);
        }

        _darstellung = new Darstellung(_dlt!, VisualErgebnisse);
        bbbbbb
        _darstellung.FestlegungAuflösung();
        _darstellung.TrägerDarstellen();
        _darstellung.Momentenverlauf();
        _darstellung.Querkraftverlauf();
        _darstellung.TexteAnzeigen();
    }

    private void Einfeldträger()
    {
        var piA = _dlt?.Übertragungspunkte[0];
        var piE = _dlt?.Übertragungspunkte[^1];

        double[,] matrix;
        Gleichungslöser gaussSolver;
        if (_dlt!.AnfangFest && _dlt.EndeFest)
        {
            // Zustandsvektor zEnde{w,phi,M,Q}, fest w=0, phi=0
            matrix = Werkzeuge.SubMatrix(piE?.Z!, 0, 1);
            var rs = Werkzeuge.SubVektor(piE?.LastÜ!, 0, 1);
            rs[0] = -rs[0];
            rs[1] = -rs[1];

            gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
            //zStart(Ma, Qa) = vektor
            piA!.ZR![2] = rs[0];
            piA.ZR![3] = rs[1];
        }
        else if (_dlt!.AnfangFest && !_dlt.EndeFest)
        {
            // Zustandsvektor zEnde{w,phi,M,Q}, gelenkig w=0, M=0
            matrix = Werkzeuge.SubMatrix(piE?.Z!, 0, 2);
            var rs = Werkzeuge.SubVektor(piE?.LastÜ!, 0, 2);
            rs[0] = -rs[0];
            rs[1] = -rs[1];

            gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
            //zStart(Ma, Qa) = vektor
            piA!.ZR![2] = rs[0];
            piA.ZR![3] = rs[1];
        }
        else if (_dlt.EndeFest && !_dlt.AnfangFest)
        {
            // Zustandsvektor zEnde{w,phi,M,Q}, fest w=0, phi=0
            matrix = Werkzeuge.SubMatrix(piE?.Z!, 0, 1);
            var rs = Werkzeuge.SubVektor(piE?.LastÜ!, 0, 1);
            rs[0] = -rs[0];
            rs[1] = -rs[1];

            gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
            //zStart(phia, Qa) = vektor
            piA!.ZR![1] = rs[0];
            piA.ZR![3] = rs[1];
        }

        // erneute Uebertragung des Zustandsvektors
        for (var k = 1; k < _dlt?.Übertragungspunkte.Count; k++)
        {
            _dlt.Übertragungspunkte[k].ZL = Werkzeuge.MatrixVectorMultiply(
                _dlt.Übertragungspunkte[k].A!, _dlt.Übertragungspunkte[k - 1].ZR!);
            if (k == _dlt?.Übertragungspunkte.Count - 1) continue;
            _dlt!.Übertragungspunkte[k].ZR = Werkzeuge.VectorVectorAdd(
                _dlt.Übertragungspunkte[k].ZL!, _dlt.Übertragungspunkte[k].Last!);
        }
    }

    private void TexteAnzeigen(object sender, RoutedEventArgs e)
    {
        if (_texteAn)
        {
            _darstellung!.TexteEntfernen();
            _texteAn = false;
        }
        else
        {
            _darstellung!.TexteAnzeigen();
            _texteAn = true;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        //MyPopup.IsOpen = false;
        _hitList = new List<Shape>();
        var hitPoint = e.GetPosition(VisualErgebnisse);
        _hitArea = new EllipseGeometry(hitPoint, 2.0, 2.0);
        VisualTreeHelper.HitTest(VisualErgebnisse, null, HitTestCallBack,
            new GeometryHitTestParameters(_hitArea));

        var sb = new StringBuilder();
        MyPopup.IsOpen = true;
        // click auf Shape Darstellungen
        foreach (var item in _hitList.Where(item => !string.IsNullOrEmpty(item.Name)))
        {
            var startIndex = "Übertragungspunkt".Length;
            if (!item.Name.Contains("Übertragungspunkt")) continue;
            // index des zugehörigen Übertragungspunktes angehängt an "Übertragungspunkt"
            if (startIndex < item.Name.Length)
            {
                var index = int.Parse(item.Name[startIndex..]);

                if (_dlt?.Übertragungspunkte[index] == null) continue;
                MyPopup.IsOpen = true;
                sb.Append(item.Name + "\n");

                if (index == 0) // Punkt am Trägeranfang
                {
                    var zr = _dlt.Übertragungspunkte[index].ZR;
                    if (zr != null)
                    {
                        sb.Append("w rechts\t= " + zr[0].ToString("g3") + "\n");
                        sb.Append("\u03c6 rechts\t= " + zr[1].ToString("g3") + "\n");
                        sb.Append("M rechts\t= " + zr[2].ToString("g3") + "\n");
                        sb.Append("Q rechts\t= " + zr[3].ToString("g3"));
                    }

                    continue;
                }

                if (index == _dlt.Übertragungspunkte.Count - 1) // Punkt am Trägerende
                {
                    var zl = _dlt.Übertragungspunkte[index].ZL;
                    if (zl != null)
                    {
                        sb.Append("w links\t= " + zl[0].ToString("g3") + "\n");
                        sb.Append("\u03c6 links\t= " + zl[1].ToString("g3") + "\n");
                        sb.Append("M links\t= " + zl[2].ToString("g3") + "\n");
                        sb.Append("Q links\t= " + zl[3].ToString("g3") + "\n" + "\n");
                    }

                    continue;
                }
                else
                {
                    var zl = _dlt.Übertragungspunkte[index].ZL; // Punkt im Trägerspannbereich
                    if (zl != null)
                    {
                        sb.Append("w links\t= " + zl[0].ToString("g3") + "\n");
                        sb.Append("\u03c6 links\t= " + zl[1].ToString("g3") + "\n");
                        sb.Append("M links\t= " + zl[2].ToString("g3") + "\n");
                        sb.Append("Q links\t= " + zl[3].ToString("g3") + "\n" + "\n");
                    }

                    var zr = _dlt.Übertragungspunkte[index].ZR;
                    if (zr != null)
                    {
                        sb.Append("w rechts\t= " + zr[0].ToString("g3") + "\n");
                        sb.Append("\u03c6 rechts\t= " + zr[1].ToString("g3") + "\n");
                        sb.Append("M rechts\t= " + zr[2].ToString("g3") + "\n");
                        sb.Append("Q rechts\t= " + zr[3].ToString("g3"));
                    }
                }
            }
            MyPopupText.Text = sb.ToString();
            return;
        }

        foreach (var item in _hitList.Where(item => !string.IsNullOrEmpty(item.Name)))
        {
            if (item.Name.Contains("Punktlast"))
            {
                var startIndex = "Punktlast".Length;
                var index = int.Parse(item.Name[startIndex..]);

                //Übertragungspunkt
                if (_dlt?.Übertragungspunkte[index] == null) continue;
                Array.Clear(_dlt.Übertragungspunkte[index].ZL!);
                Array.Clear(_dlt.Übertragungspunkte[index].ZR!);
                var punktlast = new DialogPunktlast(_dlt, index)
                {
                    Position = { Text = _dlt.Übertragungspunkte[index].Position.ToString("G4") },
                    Lastwert = { Text = (-_dlt.Übertragungspunkte[index].Lastwert).ToString("G4") }
                };
                MyPopup.IsOpen = false;
                punktlast.ShowDialog();
                VisualErgebnisse.Children.Clear();
                Neuberechnung();
            }

            if (item.Name.Contains("Gleichlast"))
            {
                var startIndex = "Gleichlast".Length;
                var index = int.Parse(item.Name[startIndex..]);

                //Übertragungspunkte
                if (_dlt?.Übertragungspunkte[index] == null) continue;
                var gleichlast = new DialogGleichlast(_dlt, index)
                {
                    Anfang = { Text = _dlt.Übertragungspunkte[index].Position.ToString("G4") },
                    Länge = { Text = _dlt.Übertragungspunkte[index].Lastlänge.ToString("G4") },
                    Lastwert = { Text = (_dlt.Übertragungspunkte[index].Lastwert).ToString("G4") }
                };
                MyPopup.IsOpen = false;
                gleichlast.ShowDialog();
                VisualErgebnisse.Children.Clear();
                Neuberechnung();
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
                VisualErgebnisse.Children.Clear();
                Neuberechnung();
            }
        }
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        MyPopup.IsOpen = false;
    }

    private HitTestResultBehavior HitTestCallBack(HitTestResult result)
    {
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
                return HitTestResultBehavior.Stop;
        }
    }
}