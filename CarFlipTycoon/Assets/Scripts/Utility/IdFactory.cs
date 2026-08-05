using System;
using System.Globalization;

namespace CarFlipTycoon.Utility
{
    public static class IdFactory
    {
        public static string NewId() => Guid.NewGuid().ToString("N");

        public static string NowUtcIso() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }
}
