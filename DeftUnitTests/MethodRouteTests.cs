using Deft;
using DeftUnitTests.ProjectClasses;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DeftUnitTests
{
    [TestClass]
    public class MethodRouteTests
    {
        [TestMethod]
        public void WhenAddingSameRouteTwice_ShouldThrowInvalidOperationException()
        {
            var route1 = "route1";
            var route2 = "/route2";
            var route3 = "route3/";
            var route4 = "route1/route4/";
            var route5 = "\\route5\\route5";

            var router = new Router();
            router.Add<TestArgs, TestResponse>(route1, (from, req) => null);
            router.Add<TestArgs, TestResponse>(route2, (from, req) => null);
            router.Add<TestArgs, TestResponse>(route3, (from, req) => null);
            router.Add<TestArgs, TestResponse>(route4, (from, req) => null);
            router.Add<TestArgs, TestResponse>(route5, (from, req) => null);

            Action action = () => router.Add<TestArgs, TestResponse>(route1, (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => router.Add<TestArgs, TestResponse>("route3", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => router.Add<TestArgs, TestResponse>("\\route1/route4", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");
        }

        [TestMethod]
        public void WhenAddingSameNestedRoutes_ShouldThrowInvalidOperationException()
        {
            var route1 = "route1";
            var route2 = "/route2";
            var route3 = "route3/";
            var route4 = "route1/route4/";
            var route5 = "\\route5\\route5";

            var nested = "nested";

            var routerParent = new Router();
            routerParent.Add<TestArgs, TestResponse>(route1, (from, req) => null);
            routerParent.Add<TestArgs, TestResponse>(route2, (from, req) => null);
            routerParent.Add<TestArgs, TestResponse>(route3, (from, req) => null);
            routerParent.Add<TestArgs, TestResponse>(route4, (from, req) => null);
            routerParent.Add<TestArgs, TestResponse>(route5, (from, req) => null);

            var routerNested = new Router();
            routerNested.Add<TestArgs, TestResponse>(route1, (from, req) => null);
            routerNested.Add<TestArgs, TestResponse>(route2, (from, req) => null);
            routerNested.Add<TestArgs, TestResponse>(route3, (from, req) => null);
            routerNested.Add<TestArgs, TestResponse>(route4, (from, req) => null);
            routerNested.Add<TestArgs, TestResponse>(route5, (from, req) => null);

            routerParent.Add(nested, routerNested);

            Action action = () => routerParent.Add<TestArgs, TestResponse>("nested/route1", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => routerParent.Add<TestArgs, TestResponse>("nested/route5\\route5", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            routerParent.Add<TestArgs, TestResponse>("nested/novi", (from, req) => null);

            action = () =>
            {
                routerNested.Add<TestArgs, TestResponse>("novi", (from, req) => null);
            };
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");
        }

        [TestMethod]
        public void WhenAddingSameMultiNestedRoutes_ShouldThrowInvalidOperationException()
        {
            var routerParent = new Router();
            routerParent.Add<TestArgs, TestResponse>("route", (from, req) => null);

            var routerNested = new Router();
            routerNested.Add<TestArgs, TestResponse>("route", (from, req) => null);

            var routerDeepNested = new Router();
            routerDeepNested.Add<TestArgs, TestResponse>("route", (from, req) => null);

            routerParent.Add("nested", routerNested);

            routerNested.Add("nested", routerDeepNested);

            routerParent.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/route",
                "/nested/route",
                "/nested/nested/route" },
                options => options.WithoutStrictOrdering());


            Action action = () => routerParent.Add<TestArgs, TestResponse>("nested/route", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => routerParent.Add<TestArgs, TestResponse>("nested/nested/route", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => routerNested.Add<TestArgs, TestResponse>("nested/route", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => routerParent.Add("novoo", routerNested);
            action.Should().Throw<InvalidOperationException>().WithMessage("*Cannot add router which is already added to some other router*");

            routerParent.Add<TestArgs, TestResponse>("nested/nested/novi", (from, req) => null);

            routerParent.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/route",
                "/nested/route",
                "/nested/nested/route",
                "/nested/nested/novi" },
                options => options.WithoutStrictOrdering());

            action = () => routerDeepNested.Add<TestArgs, TestResponse>("novi", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            action = () => routerNested.Add<TestArgs, TestResponse>("/nested/novi", (from, req) => null);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");


            routerParent.Add<TestArgs, TestResponse>("nested/nested/nested/nesto/nesto", (from, req) => null);

            routerParent.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/route",
                "/nested/route",
                "/nested/nested/route",
                "/nested/nested/novi",
                "/nested/nested/nested/nesto/nesto" },
                options => options.WithoutStrictOrdering());

            var routerNew = new Router();

            routerNew.Add<TestArgs, TestResponse>("nesto/nesto", (from, req) => null);

            action = () => routerDeepNested.Add("nested", routerNew);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");

            routerDeepNested.Add("nested/nested", routerNew);

            routerParent.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/route",
                "/nested/route",
                "/nested/nested/route",
                "/nested/nested/novi",
                "/nested/nested/nested/nesto/nesto",
                "/nested/nested/nested/nested/nesto/nesto" },
                options => options.WithoutStrictOrdering());

            var routerNewNew = new Router();
            var routerNewNewNew = new Router();

            routerNewNew.Add("nested", routerNewNewNew);

            routerNewNewNew.Add<TestArgs, TestResponse>("route", (from, req) => null);

            action = () => routerParent.Add("nested", routerNewNew);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");
        }

        [TestMethod]
        public void WhenRouterIsAddedTwice_ShouldThrowInvalidOperationException()
        {
            var router = new Router();

            var newRouter = new Router();

            router.Add("nesto", newRouter);

            Action action = () => router.Add("nestoDrugo", newRouter);
            action.Should().Throw<InvalidOperationException>().WithMessage("*cannot add route*");
        }

        [TestMethod]
        public void WhenArgumentsAreNull_ShouldThrowException()
        {
            var router = new Router();

            Action action = () => router.Add<TestArgs, TestResponse>(null, (from, req) => null);
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("route");

            action = () => router.Add<TestArgs, TestResponse>("route", null);
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("routeHandler");

            action = () => router.Add(null, new Router());
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("route");

            action = () => router.Add("route", null);
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("router");

            action = () => router.Add("", new Router());
            action.Should().Throw<InvalidOperationException>().WithMessage("*Cannot add router to empty route*");
        }

        [TestMethod]
        public void WhenRouteIsEmpty_ShouldWorkNormally()
        {
            var router = new Router();

            router.Add<TestArgs, TestResponse>("route", (from, req) => null);
            router.Add<TestArgs, TestResponse>("", (from, req) => null);
            router.Add<TestArgs, TestResponse>("route/nesto", (from, req) => null);

            router.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/",
                "/route",
                "/route/nesto"},
                options => options.WithoutStrictOrdering());

            var newRouter = new Router()
                .Add<TestArgs, TestResponse>("nesto", (from, req) => null);

            router.Add("nested", newRouter);

            router.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/",
                "/route",
                "/route/nesto",
                "/nested/nesto"},
                options => options.WithoutStrictOrdering());

            newRouter.Add<TestArgs, TestResponse>("/", (from, req) => null);

            router.GetHandledRoutes().Should().BeEquivalentTo(new string[] {
                "/",
                "/route",
                "/nested",
                "/route/nesto",
                "/nested/nesto"},
                options => options.WithoutStrictOrdering());
        }
    }
}
