using System;

namespace WebShard
{
    [Flags]
    public enum WebServerFlags
    {
        Undefined = 0,
        Http = 1,
        Https = 2,
        Ipv4 = 4,
        Ipv6 = 8,

        HttpOverIpv4 = Http | Ipv4,
        HttpOverIpv6 = Http | Ipv6,
        HttpsOverIpv4 = Https | Ipv4,
        HttpsOverIpv6 = Https | Ipv6,

        All = -1
    }
}