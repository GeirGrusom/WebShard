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
            {"json", "application/json" },
            {"html", "text/html"},
            {"xhtml", "application/xhtml+xml"},
            {"xml", "application/xml"},
            {"xslt", "application/xslt+xml"},
            {"js","application/javascript"},
            {"css", "text/css"},
            {"txt", "text/plain"},
            {"ttf", "application/x-font-ttf"},
            {"jar", "application/java-archive"},
            {"png", "image/png"},
            {"jpeg", "image/jpeg"},
            {"gif", "image/gif"},
            {"svg", "image/svg+xml"},
            {"ogg", "application/ogg"},
            {"oga", "audio/ogg"},
            {"ogv", "video/ogg"},
            {"mp3", "audio/mpeg3"},
            {"h261", "video/h261"},
            {"h263", "video/h263"},
            {"h264", "video/h265"},
            {"avi","video/x-msvideo"},
            {"wav", "audio/x-wav"},
            {"woff","application/x-font-woff"},
            {"ico", "image/x-icon"},
            {"zip", "application/zip"},
            {"yaml", "text/yaml"},
            {"rss", "application/rss+xml"},
            {"atom", "application/atom+xml"},
            {"atomcat", "application/atomcat+xml"},
            {"atomsvc", "application/atomsvc+xml"},
            {"pdf", "application/pdf"},
            {"swf", "application/x-shockwave-flash"},
            {"flv", "video/x-flv"},
            {"torrent", "application/x-bittorrent"},
            {"csv", "text/csv"},
            {"dtd", "application/xml-dtd"}
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
