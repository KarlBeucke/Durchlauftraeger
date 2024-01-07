﻿using System.Collections.Generic;

namespace Durchlauftraeger;

public class Modell
{
    public double Trägerlänge { get; set; }
    public bool AnfangFest { get; set; }
    public bool EndeFest { get; set; }
    public bool EndeFrei { get; set; }
    public bool KeineLast { get; set; }
    public double EI { get; set; }

    public Übertragungspunkt ÜbertragungsPunkt;
    public readonly List<Übertragungspunkt> Übertragungspunkte;

    public Modell()
    {
        ÜbertragungsPunkt = new Übertragungspunkt();
        Übertragungspunkte = new List<Übertragungspunkt>();
    }
    public Modell(Übertragungspunkt übertragungsPunkt)
    {
        ÜbertragungsPunkt = übertragungsPunkt;
        Übertragungspunkte = new List<Übertragungspunkt>();
    }
    public Modell(double trägerlänge, Übertragungspunkt übertragungsPunkt)
    {
        Trägerlänge = trägerlänge;
        ÜbertragungsPunkt = übertragungsPunkt;
        Übertragungspunkte = new List<Übertragungspunkt>();
    }
}

public class Übertragungspunkt
{
    public double Position { get; set; }
    public short Typ { get; set; }
    public double[,] A { get; set; }
    public double[,] Z { get; set; }
    public double[] Zl { get; set; }
    public double[] Zr { get; set; }
    public double[] Punktlast { get; set; }
    public double Q { get; set; }
    public double Qa { get; set; }
    public double Qb { get; set; }
    // Lastlänge ist die Gesamtlänge der verteilten Last
    // Anfangs- und Endpunkt sind Übertragungspunkte, aber nicht notwendigerweise sukzessive
    // Übertragung zwischen 2 Punkten erfordert Lastübertragung zwischen diesen Punkten
    // d.h. die Länge der Lastverteilung ist der Abstand zwischen beiden Punkten
    public double Lastlänge { get; set; }
    public double[] LastÜ { get; set; }
    public double[] Lk { get; init; }
    public double[,] AnfangKopplung { get; set; }
    public double[,] Kk1Inv { get; set; }

    public Übertragungspunkt()
    {
        A = new double[4, 4];
        Z = new double[4, 2];
        Zl = new double[4];
        Zr = new double[4];
        Punktlast = new double[4];
        LastÜ = new double[4];
        Lk = new double[4];
        AnfangKopplung = new double[,] { };
        Kk1Inv = new double[,] { };
    }

    public Übertragungspunkt(double position)
    {
        Position = position;
        A = new double[4, 4];
        Z = new double[4, 2];
        Zl = new double[4];
        Zr = new double[4];
        Punktlast = new double[4];
        LastÜ = new double[4];
        Lk = new double[4];
        AnfangKopplung = new double[,] { };
        Kk1Inv = new double[,] { };
    }

    public Übertragungspunkt(double position, double[] punktlast)
    {
        Position = position;
        Punktlast = punktlast;
        A = new double[4, 4];
        Z = new double[4, 2];
        Zl = new double[4];
        Zr = new double[4];
        Punktlast = new double[4];
        LastÜ = new double[4];
        Lk = new double[4];
        AnfangKopplung = new double[,] { };
        Kk1Inv = new double[,] { };
    }
}