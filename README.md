WebShard
========

Small web server for standalone applications

This web server started out when the ridiculousness of HttpContext.Current
was made apparent for the millionth time. It is intended to be a slim, 
no-nonsense web server that supplies you with everything you need to be 
able to set up simple applications that can be used for configuration
of services or other data front ends.

It has built-in dependency injection, url routing, and a controller model. 
Generally there is no need for inheritence. Most processes can be done
either by implementing (simple) interfaces, dependency injection or 
attributes.

It supports:

* HTTP
* HTTPS (using TLS 1.2 currently, and the certificate path is hard-coded)
* IPv4
* IPv6
* Dependency Injection
* Controllers and routing
* Request filtering
 
Using WebShard
==============

Setup is relatively simple. You need a `HttpApplication` and a `WebServer`.
The WebServer acts as the entry point, and sets up all the listeners. The
`HttpApplication` handles all the work when the request has been 
de-serialized.

In order to render anything you need to provide a controller and a route. If
nothing else is said, the action performed will default to the http method
provided by the request.

Routing
-------

Routing is fairly similar to MVC or Web API, and is of the following pattern:
`{RouteValueName?:RegularExpression}`

* `RouteValueName` is a identifier which must be valid in C# as a identifier.
* `?` is *optional* and specified that the identifier is optional.
* `RegularExpression` is a regular expression that must match the expression
  for the route to be considered a match.

Route example
-------------
```csharp
httpApplication.RouteTable.Add(`/{controller}/{action?}/{id?\d+}`, action = "Index"); 
```

Controllers
-----------
Controllers are classes with one or several methods that return a `IResponse`.
There are no base class to inherit from, or interfaces to implement, but
they have to be registered before they will be matched, end with 'Controller' and have to have at
least one public constructor.

Controller example
------------------

```csharp
public sealed class HelloWorldExample
{
  public IResponse Get()
  {
    return new ContentResponse("Hello World!");
  }
}
```

Complete example
----------------
```csharp
public sealed class HelloWorldController
{
  public IResponse Get()
  {
    return new ContentResponse("Hello World!");
  }
}

static class Program
{
  static void Main()
  {
    var app = new HttpApplication();
    app.ControllerRegistry.Register<HelloWorldController>();
    app.RouteTable.Add("/", new { controller = "HelloWorld" });
    
    var webServer = new WebServer(app);
    webServer.Start();
  }
}
```

Known issues
============

* POST data is not deserialized, so currently POST doesn't work unless the controller does
  all the hard work.
