using System;
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
        for (var i = 1; i < _dlt.Übertragungspunkte.Count; i++)
        {
            //if (_dlt.Übertragungspunkte[i].Typ == 1) keineLast = false;
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

        for (var i = 0; i < felder.Count; i++)
        {
            rs = new double[2];

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
                    z = _dlt.Übertragungspunkte[felder[i][0]].AnfangKopplung!;
                    lü = _dlt.Übertragungspunkte[felder[i][0]].Lk!;
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
                        // Punktlast
                        if (pik.Lastlänge < double.Epsilon)
                        {
                            pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, z);
                            var lkÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, lü);
                            pik.LastÜ = Werkzeuge.VectorVectorAdd(lkÜ, pik.Last!);
                        }
                        // Gleichlast
                        else
                        {
                            pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, z);
                            var lkÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, lü);
                            pik.LastÜ = Werkzeuge.VectorVectorAdd(lkÜ, pik.Last!);
                        }
                        break;
                    // Abschnitt mit Lager am Ende, Übertragung des Zustandsvektors mit Federkopplung auf nächsten Abschnitt
                    case 3:
                        l = pik.Position - pikm1.Position;
                        pik.A = Werkzeuge.Uebertragungsmatrix(l, 1);
                        pik.Z = Werkzeuge.MatrixMatrixMultiply(pik.A!, z);
                        pik.LastÜ = Werkzeuge.MatrixVectorMultiply(pik.A!, lü);
                        pik.LastÜ = Werkzeuge.VectorVectorAdd(pik.LastÜ, pik.Last!);
                        break;
                }
            }

            if (felder.Count == 1)
            {
                Einfeldträger();
                _darstellung!.TrägerDarstellen();
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
            kk1Inv = Werkzeuge.Matrix2By2Inverse(kk1);
            var kk = Werkzeuge.MatrixMatrixMultiply(kk2, kk1Inv);

            var l1 = Werkzeuge.SubVektor(piE.LastÜ!, 0, 1);
            var l2 = Werkzeuge.SubVektor(piE.LastÜ!, 2, 3);
            lk = Werkzeuge.MatrixVectorMultiply(kk, l1);
            lk = Werkzeuge.VectorVectorMinus(l2, lk);

            // Anfangsvektor nächstes Feld
            double[,] kopplung = { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { kk[0, 0], kk[0, 1], 1, 0 }, { kk[1, 0], kk[1, 1], 0, 1 } };
            double[,] anfang = { { 0, 0 }, { 1, 0 }, { 0, 0 }, { 0, 1 } };
            _dlt.Übertragungspunkte[felder[i + 1][0]].AnfangKopplung = Werkzeuge.MatrixMatrixMultiply(kopplung, anfang);

            _dlt.Übertragungspunkte[felder[i + 1][0]].Lk![2] = lk[0];
            _dlt.Übertragungspunkte[felder[i + 1][0]].Lk![3] = lk[1];

            if (i >= felder.Count - 2) continue;
            // gelenkiges Zwischenlager am Feldende: w = M = 0
            //var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[felder[i][2]].Z!, 0, 2);
            var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[felder[i][felder[i].Count - 1]].Z!, 0, 2);
            lk = _dlt.Übertragungspunkte[felder[i][2]].LastÜ!;

            rs = Werkzeuge.SubVektor(lk, 0, 2);
            rs[0] = -rs[0];
            rs[1] = -rs[1];
            var gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
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
            var matrix = Werkzeuge.SubMatrix(_dlt.Übertragungspunkte[^1].Z!, 0, 2);
            lk = _dlt.Übertragungspunkte[felder[^1][^1]].LastÜ!;

            rs = Werkzeuge.SubVektor(lk, 0, 2);
            rs[0] = -rs[0];
            rs[1] = -rs[1];
            var gaussSolver = new Gleichungslöser(matrix, rs);
            if (gaussSolver.Decompose()) gaussSolver.Solve();
        }

        // '^' unary_expression is called the "index from end operator"
        // Endvektor im letzten Feld, indexEnde = felder[^1].Count - 1;
        var pEe = _dlt.Übertragungspunkte[felder[^1][^1]];
        pEe.Zl = Werkzeuge.MatrixVectorMultiply(pEe.Z!, rs);
        pEe.Zl = Werkzeuge.VectorVectorAdd(pEe.Zl!, pEe.LastÜ!);

        // Anfangsvektor im letzten Feld
        var pE0 = _dlt.Übertragungspunkte[felder[^1][0]];
        var vektor4 = Werkzeuge.MatrixVectorMultiply(pE0.AnfangKopplung!, rs);
        pE0.Zr = Werkzeuge.VectorVectorAdd(vektor4, pE0.Lk!);

        // Zustandsvektor an Zwischenpunkten im letzten Feld
        for (var index = 1; index < felder[^1].Count - 1; index++)
        {
            var pkE = _dlt.Übertragungspunkte[felder[^1][index]];
            pkE.Zl = Werkzeuge.MatrixVectorMultiply(pkE.Z!, rs);
            pkE.Zl = Werkzeuge.VectorVectorAdd(pkE.Zl!, pkE.LastÜ!);
            // Gleichlast: kein neuer Lasteintrag am Ende
            pkE.Zr = !(pkE.Lastlänge > 0) ? Werkzeuge.VectorVectorAdd(pkE.Zl, pkE.Last!) : pkE.Zl;
        }

        // erneute Übertragung über alle vorhergehenden Felder
        for (var i = 0; i < felder.Count - 1; i++)
        {
            // Anfangsvektor im Feld
            var vektor2 = new double[2];
            // Kopplung: wc = 0, phic = z2L[3]
            var p00 = _dlt.Übertragungspunkte[felder[i][0]];
            p00.Zr = new double[4];
            vektor2[0] = _dlt.Übertragungspunkte[felder[i + 1][0]].Zr![0] -
                         _dlt.Übertragungspunkte[felder[i][^1]].LastÜ![0];
            vektor2[1] = _dlt.Übertragungspunkte[felder[i + 1][0]].Zr![1] -
                         _dlt.Übertragungspunkte[felder[i][^1]].LastÜ![1];
            vektor2 = Werkzeuge.MatrixVectorMultiply(kk1Inv, vektor2);

            // AnfangFest und BeideFest: Anfang M=vektor2[0]
            if (_dlt.AnfangFest) p00.Zr![2] = vektor2[0];
            // nur EndeFest: Anfang             phi=vektor2[0]
            if (_dlt.EndeFest && !_dlt.AnfangFest) p00.Zr![1] = vektor2[0];
            // in allen Fällen: Anfang          Q=vektor2[1]
            p00.Zr![3] = vektor2[1];

            // Zustandsvektor an Zwischenpunkten im Feld
            for (var index = 1; index < felder[i].Count - 1; index++)
            {
                var piK = _dlt.Übertragungspunkte[felder[i][index]];
                var piKm1 = _dlt.Übertragungspunkte[felder[i][index - 1]];

                piK.Zl = Werkzeuge.MatrixVectorMultiply(piK.A!, piKm1.Zr!);
                piK.Zr = Werkzeuge.VectorVectorAdd(piK.Zl!, piK.Last!);
            }

            // Endvektor im Feld
            var pEm1 = _dlt.Übertragungspunkte[felder[i].Count - 1];
            vektor2 = Werkzeuge.SubVektor(_dlt.Übertragungspunkte[felder[i][0]].Zr!, 2, 3);
            pEm1.Zl = Werkzeuge.MatrixVectorMultiply(pEm1.Z!, vektor2);
            pEm1.Zl = Werkzeuge.VectorVectorAdd(pEm1.Zl!, pEm1.LastÜ!);
        }

        _darstellung!.TrägerDarstellen();
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
            piA!.Zr![2] = rs[0];
            piA.Zr![3] = rs[1];
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
            piA!.Zr![2] = rs[0];
            piA.Zr![3] = rs[1];
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
            piA!.Zr![1] = rs[0];
            piA.Zr![3] = rs[1];
        }

        // erneute Uebertragung des Zustandsvektors
        for (var k = 1; k < _dlt?.Übertragungspunkte.Count; k++)
        {
            _dlt.Übertragungspunkte[k].Zl = Werkzeuge.MatrixVectorMultiply(
                _dlt.Übertragungspunkte[k].A!, _dlt.Übertragungspunkte[k - 1].Zr!);
            if (k == _dlt?.Übertragungspunkte.Count - 1) continue;
            _dlt!.Übertragungspunkte[k].Zr = Werkzeuge.VectorVectorAdd(
                _dlt.Übertragungspunkte[k].Zl!, _dlt.Übertragungspunkte[k].Last!);
        }
    }
}