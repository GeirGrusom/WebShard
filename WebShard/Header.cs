using System;

namespace WebShard
{
    public struct Header : IEquatable<Header>
    {
        private readonly string _name;
        private readonly string _value;

        public string Name { get { return _name; }}
        public string Value { get { return _value; }}

        public Header(string name, string value)
        {
            _name = name;
            _value = value;
        }

        public bool Equals(Header other)
        {
            return string.Equals(_name, other.Name, StringComparison.OrdinalIgnoreCase) && _value == other.Value;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _name, _value);
        }
    }
}