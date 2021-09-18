﻿using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Deft
{
    public class DeftRouter
    {
        private List<MiddlewareHandler> middlewares = new List<MiddlewareHandler>();
        private List<RouterEntry> entries = new List<RouterEntry>();
        private ThreadOptions threadOptions = ThreadOptions.Default;

        internal async Task<DeftResponse> Handle(DeftRequest request)
        {
            var middlewares = new List<MiddlewareHandler>(this.middlewares);
            return await HandleMiddleware(request, middlewares, 0);
        }

        private async Task<DeftResponse> HandleMiddleware(DeftRequest request, List<MiddlewareHandler> middlewares, int index)
        {
            if (index >= middlewares.Count)
                return await HandleRoute(request);

            return await middlewares[index](request, async () =>
            {
                return await HandleMiddleware(request, middlewares, index + 1);
            });
        }

        private async Task<DeftResponse> HandleRoute(DeftRequest request)
        {
            foreach (var entry in entries)
            {
                if (entry.RouterType != null && entry.Route.MatchesPrefix(request.Route))
                    return await this.HandleWithRouter(request, entry);
                else if (entry.RouteHandler != null && entry.Route.MatchesExactly(request.Route))
                    return await entry.RouteHandler.Handle(request);
            }

            Logger.LogWarning($"Route {request.FullRoute} not found, responding with {ResponseStatusCode.NotFound}");
            return new DeftResponse()
            {
                StatusCode = ResponseStatusCode.NotFound,
                Headers = new Dictionary<string, string>(),
                BodyJSON = null
            };
        }

        private async Task<DeftResponse> HandleWithRouter(DeftRequest request, RouterEntry entry)
        {
            var newRequest = new DeftRequest()
            {
                InjectionScope = request.InjectionScope,
                MethodIndex = request.MethodIndex,
                Owner = request.Owner,
                BodyJSON = request.BodyJSON,
                Headers = request.Headers,
                FullRoute = request.FullRoute,
                Route = request.Route.Pop(entry.Route)
            };

            DeftRouter router;
            try
            {
                router = request.InjectionScope.GetInstance(entry.RouterType) as DeftRouter;
            }
            catch (Exception e)
            {
                Logger.LogError($"Router of type {entry.RouterType} for route {request.FullRoute} could not be created, see exception: " + e.ToString());
                return new DeftResponse()
                {
                    StatusCode = ResponseStatusCode.InternalServerError
                };
            }

            return await router.Handle(newRequest);
        }

        public DeftRouter OnThread(ThreadOptions threadOptions)
        {
            if (this.entries.Count > 0)
                throw new InvalidOperationException("ThreadOptions should be set before adding any of the route handlers");

            this.threadOptions = threadOptions;

            return this;
        }

        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (route == null)
                throw new ArgumentNullException("route");
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            var deftRoute = DeftRoute.FromString(route);

            entries.Add(new RouterEntry()
            {
                Route = deftRoute,
                RouteHandler = RouteHandler.From(routeHandler, threadOptions ?? this.threadOptions)
            });

            return this;
        }

        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, TBody, TResponse> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public DeftRouter Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null)
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public DeftRouter Add<TClient, TBody, TResponse>(string route, Func<TClient, DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TBody, TResponse>(route, (from, req) =>
            {
                var fromCasted = from as TClient;
                if (fromCasted == null)
                    return DeftResponse<TResponse>.From(default(TResponse)).WithStatusCode(ResponseStatusCode.Unauthorized);

                return routeHandler(fromCasted, req);
            }, threadOptions);

            return this;
        }

        public DeftRouter Add<TClient, TBody, TResponse>(string route, Func<TClient, TBody, TResponse> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public DeftRouter Add<TClient, TBody, TResponse>(string route, Func<TClient, TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions? threadOptions = null) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public DeftRouter UseMiddleware(MiddlewareHandler middlewareHandler)
        {
            if (middlewareHandler == null)
                throw new ArgumentNullException("middlwareHandler");

            this.middlewares.Add(middlewareHandler);

            return this;
        }

        public DeftRouter Add<TRouter>(string route) where TRouter : DeftRouter
        {
            if (route == null)
                throw new ArgumentNullException("route");

            var deftRoute = DeftRoute.FromString(route);

            if (deftRoute.Routes.Count == 0)
                throw new InvalidOperationException("Cannot add router to empty route /");

            entries.Add(new RouterEntry()
            {
                Route = deftRoute,
                RouterType = typeof(TRouter)
            });

            return this;
        }

        private class RouterEntry
        {
            public DeftRoute Route { get; set; }
            public Type RouterType { get; set; }
            public RouteHandler RouteHandler { get; set; }
        }

        public delegate Task<DeftResponse> MiddlewareHandler(DeftRequest request, Func<Task<DeftResponse>> next);
    }
}
