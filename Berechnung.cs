using System.Collections.Generic;
using System.Windows.Controls;
using static Durchlauftraeger.MainWindow;

namespace Durchlauftraeger;

public class Berechnung
{
    private readonly Modell? _dlt;
    private static Canvas? _dltVisuell;
    private readonly Darstellung _darstellung;

    public Berechnung(Modell dlt, Darstellung darstellung, Canvas dltVisuell)
    {
        _dlt = dlt;
        _darstellung = darstellung;
        _dltVisuell = dltVisuell;
    }

    public void Neuberechnung()
    {
        double[] lk;
        double[] rs;

        // Sortierung der Übertragungspunkte nach aufsteigender Position,+ x-Koordinate
        IComparer<Übertragungspunkt> comparer = new OrdneAufsteigendeKoordinaten();
        _dlt!.Übertragungspunkte.Sort(comparer);

        // Aufteilung in Felder zwischen Auflagern, Anzahl Felder wird in Liste gesammelt
        // jede Liste enthält eine weitere Liste mit Übertragungspunkte des Feldes
        var felder = new List<List<int>>();
        var punkte = new List<int> { 0 };
        for (var i = 1; i < _dlt.Übertragungspunkte.Count; i++)
        {
            punkte.Add(i);
            if (_dlt.Übertragungspunkte[i].Typ != 3) continue;
            felder.Add(punkte);
            punkte = new List<int> { i };
        }

        if (KeineLast)
        {
            _dltVisuell!.Children.Clear();
            _darstellung.FestlegungAuflösung();
            _darstellung.TrägerDarstellen();
            return;
        }

        _dltVisuell!.Children.Clear();

        // (Vorwärts)-Übertragung mit Unbekannten im Zustandsvektor
        // beginnend mit dem ersten DLT-Feld
        for (var i = 0; i < felder.Count; i++)
        {
            // ein Feld hat mehrere Abschnitte
            for (var k = 1; k < felder[i].Count; k++)
            {
                double l;
                double[] lü;
                var pik = _dlt.Übertragungspunkte[felder[i][k]];
                var pikm1 = _dlt.Übertragungspunkte[felder[i][k - 1]];

                // zusätzliche neue Felder werden über eine Kopplungmatrix angeschlossen
                double[,] z;
                if (i > 0 && k == 1)
                {
                    var pi0 = _dlt.Übertragungspunkte[felder[i][0]];
                    double[,] anfang = { { 0, 0 }, { 1, 0 }, { 0, 0 }, { 0, 1 } };
                    z = Werkzeuge.MatrixMatrixMultiply(pi0.AnfangKopplung!, anfang);
                    lü = pi0.Lk!;
                }
                else
                {
                    z = pikm1.Z!;
                    lü = pikm1.LastÜ!;
                }

                switch (pik.Typ)
                {
                    // freier Abschnitt
                    case 0:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, z);
                        break;
                    // Abschnitt mit Last
                    case 1:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, z);
                        pik.LastÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, lü);
                        pik.LastÜ = Werkzeuge.VectorVectorAdd(pik.LastÜ, pik.Linienlast!);
                        pik.LastÜ = Werkzeuge.VectorVectorAdd(pik.LastÜ, pik.Punktlast!);
                        break;
                    // Abschnitt mit Lager am Ende, Übertragung des Zustandsvektors mit Federkopplung auf nächsten Abschnitt
                    case 3:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, z);
                        pik.LastÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, lü);
                        pik.LastÜ = Werkzeuge.VectorVectorAdd(pik.LastÜ, pik.Linienlast!);
                        break;
                }
            }

            if (felder.Count == 1)
            {
                Einfeldträger();
                _darstellung.TrägerDarstellen();
                _darstellung.Momentenverlauf();
                _darstellung.Querkraftverlauf();
                _darstellung.TexteAnzeigen();
                return;
            }

            // Mehrfeldträger, nicht im letzten Feld
            if (i >= felder.Count - 1) continue;
            // Koppelfedermatrix mit Lastterm lK
            var piE = _dlt.Übertragungspunkte[felder[i][felder[i].Count - 1]];
            var kk1 = Werkzeuge.SubMatrix(piE.Z!, 0, 1);
            var kk2 = Werkzeuge.SubMatrix(piE.Z!, 2, 3);
            var kk1Inv = Werkzeuge.Matrix2By2Inverse(kk1);
            var kk = Werkzeuge.MatrixMatrixMultiply(kk2, kk1Inv);

            var l1 = Werkzeuge.SubVektor(piE.LastÜ!, 0, 1);
            var l2 = Werkzeuge.SubVektor(piE.LastÜ!, 2, 3);
            lk = Werkzeuge.MatrixVectorMultiply(kk, l1);
            lk = Werkzeuge.VectorVectorMinus(l2, lk);

            // Anfangsvektor nächstes Feld
            var pip10 = _dlt.Übertragungspunkte[felder[i + 1][0]];
            pip10.kk1Inv = kk1Inv;
            pip10.AnfangKopplung = new[,]
                { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { kk[0, 0], kk[0, 1], 1, 0 }, { kk[1, 0], kk[1, 1], 0, 1 } };

            pip10.Lk![2] = lk[0];
            pip10.Lk![3] = lk[1];
        }

        // Am rechten Rand des DLTs Gleichungssystem aufstellen und lösen
        // letztes Feld der Übertragung
        var zGelenk = new double[4, 2];
        if (_dlt.EndeFest) // gilt für beide AnfangFest UND EndeFest und nur EndeFest
        {
            // eingespanntes Lager am Ende: we = phie = 0
            zGelenk[2, 0] = 1; zGelenk[3, 1] = 1;
            var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[^1].Z!, 0, 1);
            lk = _dlt.Übertragungspunkte[felder[^1][^1]].LastÜ!;

            // rs Zustandsgrößen am  am Anfang des letzten Feldes
            rs = Werkzeuge.SubVektor(lk, 0, 1);
            rs[0] = -rs[0];
            rs[1] = -rs[1];
            var gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
        }
        else
        {
            // gelenkiges Lager am Ende: we = Me = 0
            zGelenk[1, 0] = 1; zGelenk[3, 1] = 1;
            var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[^1].Z!, 0, 2);
            lk = _dlt.Übertragungspunkte[felder[^1][^1]].LastÜ!;

            // rs Zustandsgrößen am Anfang des letzten Feldes
            rs = Werkzeuge.SubVektor(lk, 0, 2);
            rs[0] = -rs[0];
            rs[1] = -rs[1];
            var gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
        }


        // (Rückwärts)-Übertragung mit bekannten Größen im Zustandsvektor
        // beginnend mit dem letzten DLT-Feld
        // Übertragung des Anfangsvektors über die Feder, die das vorhergehende Feld ersetzt
        // Anfangsvektor im letzten Feld
        var pE0 = _dlt.Übertragungspunkte[felder[^1][0]];
        pE0.Zr = Werkzeuge.MatrixVectorMultiply(zGelenk, rs);
        pE0.Zr = Werkzeuge.MatrixVectorMultiply(pE0.AnfangKopplung!, pE0.Zr);
        pE0.Zr = Werkzeuge.VectorVectorAdd(pE0.Zr, pE0.Lk!);

        // Zustandsvektoren im letzten Feld
        for (var index = 1; index < felder[^1].Count; index++)
        {
            var pkE = _dlt.Übertragungspunkte[felder[^1][index]];
            var pkEm1 = _dlt.Übertragungspunkte[felder[^1][index - 1]];
            pkE.Zl = Werkzeuge.MatrixVectorMultiply(pkE.A!, pkEm1.Zr!);
            pkE.Zl = Werkzeuge.VectorVectorAdd(pkE.Zl, pkE.Linienlast!);
            if (index == felder[^1].Count - 1) break;
            pkE.Zr = Werkzeuge.VectorVectorAdd(pkE.Zl, pkE.Punktlast!);
        }

        // (Rückwärts)-Übertragung vorhergehende Felder
        // der Anfangsvektor wird übertragen über alle Felder
        for (var i = felder.Count - 2; i >= 0; i--)
        {
            var pi0 = _dlt.Übertragungspunkte[felder[i][0]];
            var piE = _dlt.Übertragungspunkte[felder[i][^1]];
            var x = new double[2];
            x[0] = piE.Zr![0] - piE.LastÜ![0];
            x[1] = piE.Zr![1] - piE.LastÜ![1];
            x = Werkzeuge.MatrixVectorMultiply(piE.kk1Inv!, x);
            var z = new double[4];
            z[1] = x[0];
            z[3] = x[1];
            pi0.Zr = new double[4];

            // Anfangsvektor im Feld
            // im ersten Feld über Anfangszustand Z und bekannte Werte der Unbekannten 
            if (i == 0)
            {
                pi0.Zr = Werkzeuge.MatrixVectorMultiply(pi0.Z!, x);
            }
            // in weiteren Feldern über Anfangskopplung und bekannte Werte
            // der Verdrehung und Auflagerkraft am Feldanfang
            else
            {
                // Kopplung: w = 0, phi = z[3]
                pi0.Zr = Werkzeuge.MatrixVectorMultiply(pi0.AnfangKopplung!, z);
                pi0.Zr = Werkzeuge.VectorVectorAdd(pi0.Zr, pi0.Lk!);
            }

            // Zustandsvektoren an weiteren Punkten im Feld
            for (var index = 1; index < felder[i].Count; index++)
            {
                var piIm1 = _dlt.Übertragungspunkte[felder[i][index - 1]];
                var piI = _dlt.Übertragungspunkte[felder[i][index]];
                piI.Zl = Werkzeuge.MatrixVectorMultiply(piI.A!, piIm1.Zr!);
                piI.Zl = Werkzeuge.VectorVectorAdd(piI.Zl, piI.Linienlast!);
                // letzter Punkt im Feld hat nur Zl
                if (index == felder[i].Count - 1) break;
                piI.Zr = Werkzeuge.VectorVectorAdd(piI.Zl, piI.Punktlast!);
            }
        }

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
        switch (_dlt!.AnfangFest)
        {
            case true when _dlt.EndeFest:
                {
                    // Zustandsvektor zEnde{w,phi,M,Q}, fest w=0, phi=0
                    matrix = Werkzeuge.SubMatrix(piE?.Z!, 0, 1);
                    var rs = Werkzeuge.SubVektor(piE?.LastÜ!, 0, 1);
                    rs[0] = -rs[0];
                    rs[1] = -rs[1];

                    gaussSolver = new Gleichungslöser(matrix, rs);
                    if (gaussSolver.Decompose()) gaussSolver.Solve();
                    //zStart(Ma, Qa) = vektor
                    piA!.Zr![2] = rs[0];
                    piA.Zr![3] = rs[1];
                    break;
                }
            case true when !_dlt.EndeFest:
                {
                    // Zustandsvektor zEnde{w,phi,M,Q}, gelenkig w=0, M=0
                    matrix = Werkzeuge.SubMatrix(piE?.Z!, 0, 2);
                    var rs = Werkzeuge.SubVektor(piE?.LastÜ!, 0, 2);
                    rs[0] = -rs[0];
                    rs[1] = -rs[1];

                    gaussSolver = new Gleichungslöser(matrix, rs);
                    if (gaussSolver.Decompose()) gaussSolver.Solve();
                    //zStart(Ma, Qa) = vektor
                    piA!.Zr![2] = rs[0];
                    piA.Zr![3] = rs[1];
                    break;
                }
            default:
                {
                    if (_dlt.EndeFest && !_dlt.AnfangFest)
                    {
                        // Zustandsvektor zEnde{w,phi,M,Q}, fest w=0, phi=0
                        matrix = Werkzeuge.SubMatrix(piE?.Z!, 0, 1);
                        var rs = Werkzeuge.SubVektor(piE?.LastÜ!, 0, 1);
                        rs[0] = -rs[0];
                        rs[1] = -rs[1];

                        gaussSolver = new Gleichungslöser(matrix, rs);
                        if (gaussSolver.Decompose()) gaussSolver.Solve();
                        //zStart(phia, Qa) = vektor
                        piA!.Zr![1] = rs[0];
                        piA.Zr![3] = rs[1];
                    }

                    break;
                }
        }

        // erneute Uebertragung des Zustandsvektors
        for (var k = 1; k < _dlt?.Übertragungspunkte.Count; k++)
        {
            _dlt.Übertragungspunkte[k].Zl = Werkzeuge.MatrixVectorMultiply(
                _dlt.Übertragungspunkte[k].A!, _dlt.Übertragungspunkte[k - 1].Zr!);
            _dlt!.Übertragungspunkte[k].Zl = Werkzeuge.VectorVectorAdd(
                _dlt.Übertragungspunkte[k].Zl!, _dlt.Übertragungspunkte[k].Linienlast!);
            if (k == _dlt?.Übertragungspunkte.Count - 1) continue;
            _dlt!.Übertragungspunkte[k].Zr = Werkzeuge.VectorVectorAdd(
                _dlt.Übertragungspunkte[k].Zl!, _dlt.Übertragungspunkte[k].Punktlast!);
        }
    }
}