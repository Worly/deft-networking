using System;
using System.Collections.Generic;
using System.Linq;

namespace Deft
{
    public class DeftRoute
    {
        internal List<string> Routes { get; private set; } = new List<string>();

        internal static DeftRoute FromString(string route)
        {
            return new DeftRoute()
            {
                Routes = route.ToLower().Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
        }

        public bool MatchesPrefix(DeftRoute other)
        {
            if (Routes.Count > other.Routes.Count)
                return false;

            for (int i = 0; i < Routes.Count; i++)
            {
                if (Routes[i] != other.Routes[i])
                    return false;
            }

            return true;
        }

        public bool MatchesExactly(DeftRoute other)
        {
            if (Routes.Count != other.Routes.Count)
                return false;

            for (int i = 0; i < Routes.Count; i++)
            {
                if (Routes[i] != other.Routes[i])
                    return false;
            }

            return true;
        }

        public DeftRoute Pop(DeftRoute other)
        {
            var newDeftRoute = new DeftRoute();

            for (int i = other.Routes.Count; i < Routes.Count; i++)
                newDeftRoute.Routes.Add(Routes[i]);

            return newDeftRoute;
        }

        public DeftRoute Concat(DeftRoute other)
        {
            var newDeftRoute = new DeftRoute();

            for (int i = 0; i < Routes.Count; i++)
                newDeftRoute.Routes.Add(Routes[i]);

            for (int i = 0; i < other.Routes.Count; i++)
                newDeftRoute.Routes.Add(other.Routes[i]);

            return newDeftRoute;
        }

        public override string ToString()
        {
            if (Routes.Count == 0)
                return "/";
            else
                return "/" + Routes.Aggregate((f, s) => f + "/" + s);
        }
    }
}
