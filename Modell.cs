using System.Collections.Generic;

namespace Durchlauftraeger;

public class Modell
{
    public double Trägerlänge { get; set; }
    public bool AnfangFest { get; set; }
    public bool EndeFest { get; set; }

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
    public Übertragungspunkt ÜbertragungsPunkt;
}

public class Übertragungspunkt
{
    public double Position { get; set; }
    public short Typ { get; set; }
    public double Lastwert { get; set; }
    public double Lastlänge { get; set; }
    public double[]? Zl { get; set; }
    public double[,]? A { get; set; }
    public double[,]? Z { get; set; }
    public double[,]? AnfangKopplung { get; set; }
    public double[]? Zr { get; set; }
    public double[]? Last { get; set; }
    public double[]? LastÜ { get; set; }
    public double[]? Lk { get; set; }

    public Übertragungspunkt() { }

    public Übertragungspunkt(double position) { Position = position; }

    public Übertragungspunkt(double position, double[] last)
    {
        Position = position;
        Last = last;
    }
}