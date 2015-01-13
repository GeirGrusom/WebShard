using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WebShard
{
    public class Mimetypes
    {
        private readonly Dictionary<string, string> _extensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"atom", "application/atom+xml"},
            {"atomcat", "application/atomcat+xml"},
            {"atomsvc", "application/atomsvc+xml"},
            {"avi","video/x-msvideo"},
            {"css", "text/css"},
            {"csv", "text/csv"},
            {"dtd", "application/xml-dtd"},
            {"flv", "video/x-flv"},
            {"gif", "image/gif"},
            {"h261", "video/h261"},
            {"h263", "video/h263"},
            {"h264", "video/h265"},
            {"html", "text/html"},
            {"ico", "image/x-icon"},
            {"jar", "application/java-archive"},
            {"jpeg", "image/jpeg"},
            {"js","application/javascript"},
            {"json", "application/json" },
            {"mp3", "audio/mpeg3"},
            {"oga", "audio/ogg"},
            {"ogg", "application/ogg"},
            {"ogv", "video/ogg"},
            {"pdf", "application/pdf"},
            {"png", "image/png"},
            {"rss", "application/rss+xml"},
            {"svg", "image/svg+xml"},
            {"swf", "application/x-shockwave-flash"},
            {"torrent", "application/x-bittorrent"},
            {"ttf", "application/x-font-ttf"},
            {"txt", "text/plain"},
            {"wav", "audio/x-wav"},
            {"woff","application/x-font-woff"},
            {"xhtml", "application/xhtml+xml"},
            {"xml", "application/xml"},
            {"xslt", "application/xslt+xml"},
            {"yaml", "text/yaml"},
            {"zip", "application/zip"},
        };

        public string GetMimeType(string extension)
        {
            string result;
            if (_extensionMap.TryGetValue(extension.TrimStart('.'), out result))
                return result;
            return null;
        }

        public string GetExtensionMimetypeFromSystem(string extension)
        {
            using (var key = Registry.ClassesRoot.OpenSubKey(extension.StartsWith(".") ? extension : ("." + extension)))
            {
                if (key == null)
                    return null;
                return key.GetValue("Content Type", null) as string;
            }
        }
    }
}
