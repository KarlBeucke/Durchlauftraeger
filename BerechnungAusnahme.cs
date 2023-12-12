using System;

namespace Durchlauftraeger;

public class BerechnungAusnahme : Exception
{
    public BerechnungAusnahme(string message)
        : base(string.Format("Berechnung: Fehler in der Berechnung " + message)) { }

    public BerechnungAusnahme(string message, Exception innerException)
        : base(string.Format(message), innerException) { }
}