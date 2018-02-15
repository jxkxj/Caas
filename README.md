# CaaS
We all need another "as-a-service" product.  Why not configurations?  Introducing a simple to use Configurations-as-a-Service system.

We all have applications whether the web, a desktop app, or a mobile app that need to get a config anonymously.  CaaS is designed to be quick, easy, and configurable to fit all these needs.  It's not designed to be a fancy system, it designed to pass configs.

Build Status: [![Build status](https://ci.appveyor.com/api/projects/status/9duu14is56ffo0dk?svg=true)](https://ci.appveyor.com/project/nwestfall/caas)

Client Nuget: Coming soon

## Platform Support
**Caas.Web**

Caas.Web is build on ASP.NET Core with Entity Framework and can run on Windows or Linux.  It also includes a "Dockerfile" for docker support (instructions below).

**Caas.Client**

No need to build a service to connect to CaaS.  We got you covered everywhere .NET Standard is.

|Platform|Version|
| ------------------- | :------------------: |
|Xamarin.iOS|10.0+|
|Xamarin.Mac|3.0+|
|Xamarin.Android|7.0+|
|Windows 10 UWP|10.0+|
|Windows|8.1+|
|.NET Core|1.0+|
|ASP.NET Core|1.0+|
|.NET|4.6+|
|Mono|4.6+|

## Quick Understanding

Here is a quick intro on the project and how the data is setup.

**Project**

There are two parts to this project.  Caas.Web and Caas.Client.  

Caas.Web is a ASP.NET Core application meant to run on Windows or Linux either standalone or in Docker.

Caas.Client is added to your application where you wish to read the configs from Caas.Web.  Requires 1 line to initialize and then you can easily start making calls.

**Data**

There are 4 data classes we use.

 * Client
 * ClientType
 * Config
 * ConfigAssociation

You can setup your data two ways.  One with just configs that are key-value based or with configs association with a client that are then key-value based.

Example #1: key-value (no client)

I will create 2 configs.
```c#
var config1 = new Config
{
    Key = "Key1",
    Value = "Value1"
};

var config2 = new Config
{
    Key = "Key2"
    Value = "Value2"
};
```
Now to read them from Caas.Client, I just need to call the following.
```c#
CaasManager.Init("http://localhost:5000");

var config1 = CaasManager.GetConfigAsync("Key1");
var config2 = CaasManager.GetConfigAsync("Key2");
```

Example #2: key-value (with client)

I will create 2 clients with configs.  One for a web app and one for a mobile app.
```c#
var webClient = new Client
{
    Identifier = "MyWebApp",
    ClientType = new ClientType
    {
        Name = "Web"
    }
};
var mobileClient = new Client
{
    Identifier = "012510329714913" //Android IMEI
    ClientType = new ClientType
    {
        Name = "Android"
    }
};

var config1 = new Config
{
    Key = "Key1",
    Value = "Value1"
};
var config2 = new Config
{
    Key = "Key1", //Notice how it has the same key
    Value = "Value2"
}

var configAssociation1 = new ConfigAsssociation
{
    Config = config1,
    Client = webClient
};
var configAssociation2 = new ConfigAssociation
{
    Config = config2,
    Client = mobileClient
};
```
Now to read them from Caas.Client, I just need to call the following.
```c#
CaasManager.Init("http://localhost:5000");

//To call on my web client
var config1 = CaasManager.GetConfigForClientAsync("MyWebApp", "Web", "Key1");
//To call on my android app
var config2 = CaasManager.GetConfigForClientAsync("012510329714913", "Android", "Key1");
```

Both ways are acceptable use cases and you can uses one or both in your implementation.

## Caas.Web

**Enviroment Variables**

Since it is designed to run in Docker, there are a few enviroment variables you can use to setup the service.

 * ```SQLSERVER_HOST``` (Default: ```localhost\\SQLEXPRESS```)
 * ```SQLSERVER_USER``` (Default: sa)
 * ```SQLSERVER_PASSWORD``` (Default: password)
 * ```INMEMORYCACHE_USE``` (Default: "true")
 * ```INMEMORYCACHE_TIMEOUT``` (Default: 60) [In Minutes]

**Running from command line**

From inside the ```Caas.Web``` folder, run the following

```dotnet restore && dotnet run```

It be default runs on port 5000 (http) and 5001 (https)

**Running from Docker**

From inside the ```Caas.Web``` folder, run the following

```cmd
-> dotnet publish -c Release -o out
-> docker build -t caas-web .
-> docker run -p 5000:5000 caas.web
or run with enviroment variables
-> docker run -p 5000:5000 -e INMEMORYCACHE_TIMEOUT=10 -e SQLSERVER_PASSWORD=Password123 caas-web
```

## Caas.Client
This library is meant for you to pull into any app that you want to read configs from CaaS from.  It is very simple to use.

**Setup**

Before you make any calls, just call the following line.

```c#
CaasManager.Init("http://localhost:5000");
```

or if you are using Xamarin and are using something like [ModernHttpClient](https://github.com/paulcbetts/ModernHttpClient)

```c#
CaasManager.Init("http://localhost:5000", new NativeMessageHandler());
```

And that's it!  You can now start making calls to get your configs.

**Available Calls**

Here is a quick reference of the calls you can make.  They currently match all the endpoints available in Caas.Web

```c#
Task<Config> GetConfigForClientAsync(string identifier, string type, string key);

Task<Config> GetConfigAsync(string key);

Task<IEnumerable<Config>> GetAllConfigsAsync();

Task<IEnumerable<Config>> GetAllConfigsForClientAsync(string identifier, string type);
```

## FAQ

Have a question?  Open up an issue.  Otherwise, here are a few.

**If I connect this to an existing SQL Database, will it work?**

Yes.  We only use 4 tables so as long as you don't have the same names it will be fine.  They are as follows
 * Client
 * ClientType
 * Config
 * ConfigAssociation

If you wish to rename any of the tables to avoid conflict, just open the ```Caas.Models``` project and add the following attribute to the class

```c#
[System.ComponentModel.DataAnnotations.Schema.Table("MyClientTable")]
public class Client
```

**How do I add new configs and clients?**

Currently, you will have to build a way to do this or use SQL.  We plan on coming out with a manager soon to help you do this.

## License
Under MIT (see license file)

## Issues
We'd love to hear!  Please submit an issue through github.
