using System.Collections.Generic;

namespace Durchlauftraeger;

public class Modell
{
    public double Trägerlänge { get; set; }
    public bool AnfangFest { get; set; }
    public bool EndeFest { get; set; }
    public bool KeineLast { get; set; }

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
    public short Typ { get; init; }
    public double Lastwert { get; set; }
    public double Lastlänge { get; set; }
    public double[]? Zl { get; set; }
    public double[,]? A { get; set; }
    public double[,]? Z { get; set; }
    public double[,]? AnfangKopplung { get; set; }
    public double[,]? Kk1Inv { get; set; }
    public double[]? Zr { get; set; }
    public double[]? Punktlast { get; init; }
    public double[]? Linienlast { get; set; }
    public double[]? LastÜ { get; set; }
    public double[]? Lk { get; init; }

    public Übertragungspunkt() { }

    public Übertragungspunkt(double position) { Position = position; }

    public Übertragungspunkt(double position, double[] punktlast)
    {
        Position = position;
        Punktlast = punktlast;
    }
}