using System;

namespace Ginet.Logging
{
    internal static class GinetOut
    {
        public static IAppender Appender { get; set; } = new ActionAppender(Console.WriteLine);
    }
}
