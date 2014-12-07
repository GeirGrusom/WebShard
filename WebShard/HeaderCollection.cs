using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebShard
{
    public sealed partial class HeaderCollection : IEnumerable<Header>
    {
        private readonly Dictionary<string, string> headers;

        public HeaderCollection()
        {
            headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public HeaderCollection(params Header[] headers)
        {
            this.headers = new Dictionary<string, string>(headers.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var item in headers)
            {
                this.headers.Add(item.Name, item.Value);
            }
        }

        public IEnumerator<Header> GetEnumerator()
        {
            return headers.Select(kv => new Header(kv.Key, kv.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public HeaderCollection(IEnumerable<Header> headers)
            : this(headers as Header[] ?? headers.ToArray())
        {
        }

        public int Count { get { return headers.Count; } }

        public void Add(string name, string value)
        {
            headers[name] = value;
        }

        public void AddRange(params Header[] headers)
        {
            foreach (var item in headers)
            {
                this.headers[item.Name] = item.Value;
            }
        }

        public void AddRange(IEnumerable<Header> headers)
        {
            AddRange(headers as Header[] ?? headers.ToArray());
        }

        public void Remove(string header)
        {
            headers.Remove(header);
        }

        

        public string this[string header]
        {
            get
            {
                string value;
                headers.TryGetValue(header, out value);
                return value;
            }
            set { headers[header] = value; }
        }
    }
}
