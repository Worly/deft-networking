using System;
using System.Collections.Generic;

namespace Deft
{
    public class DeftResponse<TResponseType>
    {
        public ResponseStatusCode StatusCode { get; internal set; }
        public bool Ok { get => (int)StatusCode < 400; }
        public Dictionary<string, string> Headers { get; internal set; }
        public TResponseType Body { get; internal set; }

        internal DeftResponse() {}

        public static implicit operator DeftResponse<TResponseType>(TResponseType body)
        {
            return new DeftResponse<TResponseType>()
            {
                StatusCode = ResponseStatusCode.OK,
                Headers = null,
                Body = body,
            };
        }

        public static DeftResponse<TResponseType> From(TResponseType body, Dictionary<string, string> headers = null)
        {
            return new DeftResponse<TResponseType>()
            {
                StatusCode = ResponseStatusCode.OK,
                Headers = headers,
                Body = body
            };
        }

        public static DeftResponse<TResponseType> From(ResponseStatusCode code, TResponseType body = default)
        {
            return new DeftResponse<TResponseType>()
            {
                StatusCode = code,
                Body = body
            };
        }

        public DeftResponse<TResponseType> WithHeader(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (value == null)
                throw new ArgumentNullException("value");

            if (Headers == null)
                Headers = new Dictionary<string, string>();

            Headers[key] = value;
            return this;
        }

        public DeftResponse<TResponseType> WithStatusCode(ResponseStatusCode code)
        {
            StatusCode = code;
            return this;
        }
    }

    public enum ResponseStatusCode
    {
        OK                  = 200,
        BadRequest          = 400,
        Unauthorized        = 401,
        NotFound            = 404,
        NotReachable        = 405,
        InternalServerError = 500,
        Timeout             = 501
    }
}
