using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebShard.Routing
{
    public sealed class RouteMatcher
    {
        private readonly Route _prototype;
        private readonly Regex _regex;
        private readonly List<string> _segments;
        private readonly Dictionary<string, object> _defaults; 
        private static readonly Regex MatchRegex = new Regex(@"
{
(?<Name>[A-Za-z_][A-Za-z0-9_]*)
(?<Optional>[?])?
(:(?<Regex>[^}]+))?
}(?<Slash>[/])?", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
        private Regex CreateRegex()
        {
            string reg = MatchRegex.Replace(_prototype.Url,
                e =>
                {
                    _segments.Add(e.Groups["Name"].Value);
                    return "((?<" + e.Groups["Name"] + ">" +
                           (e.Groups["Regex"].Success ? e.Groups["Regex"].Value : "[^/]+") + (e.Groups["Slash"].Success ? ")/)" : "))") +
                           (e.Groups["Optional"].Success ? "?" : "");
                });
            return new Regex(reg, RegexOptions.Compiled | RegexOptions.Singleline);
        }

        public RouteMatcher(Route prototype)
        {
            _prototype = prototype;
            _segments = new List<string>();
            _defaults = CreateDictionary(prototype.Defaults);
            _regex = CreateRegex();
        }

        private Dictionary<string, object> CreateDictionary(object value)
        {
            var props = value.GetType().GetProperties();
            return props.ToDictionary(x => x.Name, x => x.GetValue(value));
        }

        public bool Match(string input, out IDictionary<string, object> routeValues)
        {
            var match = _regex.Match(input);
            if (!match.Success)
            {
                routeValues = null;
                return false;
            }

            routeValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var seg in _segments)
            {
                if (match.Groups[seg].Success)
                    routeValues[seg] = System.Net.WebUtility.UrlDecode(match.Groups[seg].Value);
                else
                {
                    object def;
                    if(_defaults.TryGetValue(seg, out def))
                        routeValues[seg] = def;
                }
            }
            foreach (var item in _defaults.Where(r => !_segments.Contains(r.Key)))
            {
                routeValues.Add(item.Key, item.Value);
            }

            return true;
        }
    }
}