﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Deft
{
    public class Router
    {
        private List<RouterEntry> entries = new List<RouterEntry>();
        private List<ExceptionHandler> exceptionHandlers = new List<ExceptionHandler>();

        internal DeftRoute myRoute = DeftRoute.FromString("/");
        internal Router parentRouter = null;

        internal void Handle(DeftConnectionOwner owner, uint methodIndex, DeftRoute route, string headersJSON, string bodyJSON)
        {
            foreach (var entry in entries)
            {
                if (entry.Router != null && entry.Route.MatchesPrefix(route) && entry.Router.IsRouteHandled(route.Pop(entry.Route)))
                {
                    entry.Router.Handle(owner, methodIndex, route.Pop(entry.Route), headersJSON, bodyJSON);
                    return;
                }
                else if (entry.RouteHandler != null && entry.Route.MatchesExactly(route))
                {
                    entry.RouteHandler.Handle(owner, methodIndex, headersJSON, bodyJSON);
                    return;
                }
            }

            Logger.LogWarning($"Route {route} not found, responding with {ResponseStatusCode.NotFound}");
            DeftMethods.Respond(owner.Connection, methodIndex, ResponseStatusCode.NotFound);
        }

        internal void HandleException(DeftConnectionOwner owner, uint methodIndex, Exception exception)
        {
            foreach (var exceptionHandler in exceptionHandlers)
            {
                if (exceptionHandler.Handles(exception))
                {
                    exceptionHandler.Handle(owner, methodIndex, exception);
                    return;
                }
            }

            if (parentRouter != null)
                parentRouter.HandleException(owner, methodIndex, exception);
            else
            {
                DeftMethods.Respond(owner.Connection, methodIndex, new DeftResponseDTO()
                {
                    StatusCode = ResponseStatusCode.InternalServerError,
                    HeadersJSON = JsonConvert.SerializeObject(new Dictionary<string, string>() {
                                { "exception-type", exception.GetType().Name },
                                { "exception-message", exception.Message },
                                { "exception-stacktrace", DeftConfig.RespondWithExceptionStackTrace ? exception.StackTrace : null }
                            })
                });
            }
        }

        internal bool IsRouteHandled(DeftRoute route)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Router != null && entries[i].Route.MatchesPrefix(route) && entries[i].Router.IsRouteHandled(route.Pop(entries[i].Route)))
                    return true;
                else if (entries[i].RouteHandler != null && entries[i].Route.MatchesExactly(route))
                    return true;
            }

            return false;
        }

        internal bool IsRouteHandledByParent(DeftRoute route)
        {
            if (parentRouter == null)
                return false;

            return parentRouter.IsRouteHandled(myRoute.Concat(route)) || parentRouter.IsRouteHandledByParent(myRoute.Concat(route));
        }

        internal IEnumerable<DeftRoute> GetHandledDeftRoutes()
        {
            foreach (var entry in entries)
            {
                if (entry.Router != null)
                {
                    foreach (var handled in entry.Router.GetHandledDeftRoutes())
                        yield return entry.Route.Concat(handled);
                }
                else
                    yield return entry.Route;
            }
        }

        public IEnumerable<string> GetHandledRoutes()
        {
            foreach (var route in GetHandledDeftRoutes())
                yield return route.ToString();
        }

        public Router Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions threadOptions = ThreadOptions.Default)
        {
            if (route == null)
                throw new ArgumentNullException("route");
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            var deftRoute = DeftRoute.FromString(route);

            if (IsRouteHandled(deftRoute) || IsRouteHandledByParent(deftRoute))
                throw new InvalidOperationException($"Cannot add route {route} because that route is already handled by another handler");

            entries.Add(new RouterEntry()
            {
                Route = deftRoute,
                RouteHandler = RouteHandler.From(myRoute.Concat(deftRoute), routeHandler, threadOptions, this)
            });

            return this;
        }

        public Router Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, TBody, TResponse> routeHandler, ThreadOptions threadOptions = ThreadOptions.Default)
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public Router Add<TBody, TResponse>(string route, Func<DeftConnectionOwner, TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions threadOptions = ThreadOptions.Default)
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public Router Add<TClient, TBody, TResponse>(string route, Func<TClient, DeftRequest<TBody>, DeftResponse<TResponse>> routeHandler, ThreadOptions threadOptions = ThreadOptions.Default) where TClient : DeftConnectionOwner
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

        public Router Add<TClient, TBody, TResponse>(string route, Func<TClient, TBody, TResponse> routeHandler, ThreadOptions threadOptions = ThreadOptions.Default) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public Router Add<TClient, TBody, TResponse>(string route, Func<TClient, TBody, DeftResponse<TResponse>> routeHandler, ThreadOptions threadOptions = ThreadOptions.Default) where TClient : DeftConnectionOwner
        {
            if (routeHandler == null)
                throw new ArgumentNullException("routeHandler");

            Add<TClient, TBody, TResponse>(route, (from, req) => routeHandler(from, req.Body), threadOptions);
            return this;
        }

        public Router AddExceptionHandler<TException, TResponse>(Func<DeftConnectionOwner, TException, DeftResponse<TResponse>> exceptionHandler, ThreadOptions threadOptions = ThreadOptions.Default) where TException : Exception
        {
            if (exceptionHandler == null)
                throw new ArgumentNullException("exceptionHandler");

            exceptionHandlers.Add(ExceptionHandler.From(exceptionHandler, threadOptions, this));
            return this;
        }

        public Router Add(string route, Router router)
        {
            if (route == null)
                throw new ArgumentNullException("route");
            if (router == null)
                throw new ArgumentNullException("router");

            if (router.parentRouter != null)
                throw new InvalidOperationException("Cannot add router which is already added to some other router");

            var deftRoute = DeftRoute.FromString(route);

            if (deftRoute.Routes.Count == 0)
                throw new InvalidOperationException("Cannot add router to empty route /");

            if (IsRouteHandled(deftRoute) || IsRouteHandledByParent(deftRoute))
                throw new InvalidOperationException($"Cannot add route {deftRoute} because that route is already handled by another handler");

            foreach (var handledRoute in router.GetHandledDeftRoutes())
            {
                if (IsRouteHandled(deftRoute.Concat(handledRoute)) || IsRouteHandledByParent(deftRoute.Concat(handledRoute)))
                    throw new InvalidOperationException($"Cannot add router to {deftRoute} because route {handledRoute} is already handled in this router");
            }

            router.myRoute = deftRoute;
            router.parentRouter = this;

            entries.Add(new RouterEntry()
            {
                Route = deftRoute,
                Router = router
            });

            return this;
        }

        private class RouterEntry
        {
            public DeftRoute Route { get; set; }
            public Router Router { get; set; }
            public RouteHandler RouteHandler { get; set; }
        }
    }
}
