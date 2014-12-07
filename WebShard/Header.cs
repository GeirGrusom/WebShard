using System;

namespace WebShard
{
    public struct Header : IEquatable<Header>
    {
        private readonly string name;
        private readonly string value;

        public string Name { get { return name; }}
        public string Value { get { return value; }}

        public Header(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public bool Equals(Header other)
        {
            return string.Equals(name, other.Name, StringComparison.OrdinalIgnoreCase) && value == other.Value;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", name, value);
        }
    }
}