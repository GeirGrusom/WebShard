using System;

namespace WebShard
{
    public struct Status : IEquatable<Status>
    {
        private readonly int _statusCode;
        private readonly string _description;

        public int Code { get { return _statusCode; } }
        public string Description { get { return _description; } }

        public override int GetHashCode()
        {
            return _statusCode;
        }

        public Status(int code, string description)
        {
            _statusCode = code;
            _description = description;
        }

        public bool Equals(Status other)
        {
            return _statusCode == other._statusCode;
        }

        public override string ToString()
        {
            return Code + " " + Description;
        }

        public static readonly Status Ok = new Status(200, "Ok");
        public static readonly Status NotModified = new Status(304, "Not modified");
        public static readonly Status BadRequest = new Status(400, "Bad request");
        public static readonly Status Unauthorized = new Status(401, "Unauthorized");
        public static readonly Status Forbidden = new Status(403, "Forbidden");
        public static readonly Status NotFound = new Status(404, "Not found");
        public static readonly Status InternalServerError = new Status(500, "Internal server error");
        
    }
}