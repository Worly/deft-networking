using System;
using System.Collections.Generic;
using System.Text;

namespace Deft
{
    public partial class DeftRouter
    {
        // no body, no response
        public DeftRouter Add(string route, Action routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<DummyClass, DummyClass>(route, (from, req) =>
            {
                routeHandler();
                return new DummyClass();
            }, threadOptions);

            return this;
        }
        public DeftRouter Add(string route, Action<DeftConnectionOwner> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<DummyClass, DummyClass>(route, (from, req) =>
            {
                routeHandler(from);
                return new DummyClass();
            }, threadOptions);

            return this;
        }

        // only body
        public DeftRouter Add<TBody>(string route, Action<TBody> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, DummyClass>(route, (from, req) =>
            {
                routeHandler(req.Body);
                return new DummyClass();
            }, threadOptions);

            return this;
        }
        public DeftRouter Add<TBody>(string route, Action<DeftRequest<TBody>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, DummyClass>(route, (from, req) =>
            {
                routeHandler(req);
                return new DummyClass();
            }, threadOptions);

            return this;
        }
        public DeftRouter Add<TBody>(string route, Action<DeftConnectionOwner, TBody> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, DummyClass>(route, (from, req) =>
            {
                routeHandler(from, req.Body);
                return new DummyClass();
            }, threadOptions);

            return this;
        }
        public DeftRouter Add<TBody>(string route, Action<DeftConnectionOwner, DeftRequest<TBody>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, DummyClass>(route, (from, req) =>
            {
                routeHandler(from, req);
                return new DummyClass();
            }, threadOptions);

            return this;
        }

        // only response
        public DeftRouter Add<TResponse>(string route, Func<TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<DummyClass, TResponse>(route, (from, req) => routeHandler(), threadOptions);

            return this;
        }
        public DeftRouter Add<TResponse>(string route, Func<DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<DummyClass, TResponse>(route, (from, req) => routeHandler(), threadOptions);

            return this;
        }
        public DeftRouter Add<TResponse>(string route, Func<DeftConnectionOwner, TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<DummyClass, TResponse>(route, (from, req) => routeHandler(from), threadOptions);

            return this;
        }
        public DeftRouter Add<TResponse>(string route, Func<DeftConnectionOwner, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<DummyClass, TResponse>(route, (from, req) => routeHandler(from), threadOptions);

            return this;
        }

        // both body and response
        public DeftRouter Add<TBody, TResponse>(string route, Func<TBody, TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(req.Body), threadOptions);

            return this;
        }
        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftRequest<TBody>, TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(req), threadOptions);

            return this;
        }
        public DeftRouter Add<TBody, TResponse>(string route, Func<TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(req.Body), threadOptions);

            return this;
        }
        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(req), threadOptions);

            return this;
        }
        // both with DeftConnectionOwner
        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, TBody, TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);

            return this;
        }
        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, DeftRequest<TBody>, TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(from, req), threadOptions);

            return this;
        }
        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);

            return this;
        }
        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddInternal<TBody, TResponse>(route, (from, req) => routeHandler(from, req), threadOptions);

            return this;
        }


        // everything again but with concrete client type
        public DeftRouter AddFor<TClient>(string route, Action<TClient> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, DummyClass, DummyClass>(route, (from, req) =>
            {
                routeHandler(from);
                return new DummyClass();
            }, threadOptions);

            return this;
        }

        public DeftRouter AddFor<TClient, TBody>(string route, Action<TClient, TBody> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, TBody, DummyClass>(route, (from, req) =>
            {
                routeHandler(from, req.Body);
                return new DummyClass();
            }, threadOptions);

            return this;
        }
        public DeftRouter AddFor<TClient, TBody>(string route, Action<TClient, DeftRequest<TBody>> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, TBody, DummyClass>(route, (from, req) =>
            {
                routeHandler(from, req);
                return new DummyClass();
            }, threadOptions);

            return this;
        }

        public DeftRouter AddFor<TClient, TResponse>(string route, Func<TClient, TResponse> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, DummyClass, TResponse>(route, (from, req) => routeHandler(from), threadOptions);

            return this;
        }
        public DeftRouter AddFor<TClient, TResponse>(string route, Func<TClient, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, DummyClass, TResponse>(route, (from, req) => routeHandler(from), threadOptions);

            return this;
        }

        public DeftRouter AddFor<TClient, TBody, TResponse>(string route, Func<TClient, TBody, TResponse> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);

            return this;
        }
        public DeftRouter AddFor<TClient, TBody, TResponse>(string route, Func<TClient, DeftRequest<TBody>, TResponse> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req), threadOptions);

            return this;
        }
        public DeftRouter AddFor<TClient, TBody, TResponse>(string route, Func<TClient, TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);

            return this;
        }
        public DeftRouter AddFor<TClient, TBody, TResponse>(string route, Func<TClient, DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null) throw new ArgumentNullException("routeHandler");

            AddForInternal<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req), threadOptions);

            return this;
        }
    }

    internal class DummyClass
    {
    }
}
