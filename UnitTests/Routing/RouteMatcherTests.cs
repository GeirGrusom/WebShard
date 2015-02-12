﻿using System.Collections.Generic;
using NUnit.Framework;
using WebShard.Routing;

namespace UnitTests.Routing
{
    [TestFixture]
    public class RouteMatcherTests
    {
        [Test]
        public void Match_OptionalMissing_Ok()
        {
            // Arrange
            var route = new Route(null, @"foo/{controller}/{action?}/{id:\d+}", new { action = "index" });
            var matcher = new RouteMatcher(route);

            // Act
            IDictionary<string, object> result;
            bool matched = matcher.Match("foo/bar/123", out result);

            // Assert
            Assert.That(matched, Is.True);
            Assert.That(result["action"], Is.EqualTo("index"));
        }

        [Test]
        public void Match_AddsMissingRouteValues()
        {
            // Arrange
            var route = new Route(null, "/", new {controller = "Home"});
            var matcher = new RouteMatcher(route);
            IDictionary<string, object> routeValues;
            // Act
            matcher.Match("/", out routeValues);

            // Assert
            Assert.That(routeValues.ContainsKey("controller"), Is.True);
        }

        [Test]
        public void Match_ReturnsValues()
        {
            var route = new Route(null, "foo/{controller}/{action}", new {controller = "Default", action = "foo"});
            var matcher = new RouteMatcher(route);

            IDictionary<string, object> result;
            bool matched = matcher.Match("foo/test/index", out result);

            // Assert
            Assert.That(matched, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result["controller"], Is.EqualTo("test"));
            Assert.That(result["action"], Is.EqualTo("index"));
        }

        [Test]
        public void Match_TrailingSlash()
        {
            // Arrange
            var route = new Route(null, "foo",new {});
            var matcher = new RouteMatcher(route);

            // Act
            IDictionary<string, object> care;
            Assert.That(matcher.Match("foo/", out care));

        }

        [Test]
        public void Constructor_AddsPrefixSlashIfMissing()
        {
            // Arrange
            var route = new Route(null, "foo", new {});

            // Act
            var matcher = new RouteMatcher(route);

            // Assert
            IDictionary<string, object> care;
            Assert.That(matcher.Match("/foo", out care));
        }

        [Test]
        public void Constructor_DoesNotAddPrefixSlashIfItIsAlreadyThere()
        {
            // Arrange
            var route = new Route(null, "/foo", new { });

            // Act
            var matcher = new RouteMatcher(route);

            // Assert
            IDictionary<string, object> care;
            Assert.That(matcher.Match("/foo", out care));
        }
    }
}
