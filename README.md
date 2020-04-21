# **Cloud.Core.Extensions.Configuration** 
[![Build status](https://dev.azure.com/cloudcoreproject/CloudCore/_apis/build/status/Cloud.Core%20Packages/Cloud.Core.Extensions.Configuration_Package)](https://dev.azure.com/cloudcoreproject/CloudCore/_build/latest?definitionId=6) ![Code Coverage](https://cloud1core.blob.core.windows.net/codecoveragebadges/Cloud.Core.Extensions.Configuration-LineCoverage.png) [![Cloud.Core.Extensions.Configuration package in Cloud.Core feed in Azure Artifacts](https://feeds.dev.azure.com/cloudcoreproject/dfc5e3d0-a562-46fe-8070-7901ac8e64a0/_apis/public/Packaging/Feeds/8949198b-5c74-42af-9d30-e8c462acada6/Packages/396a2077-073e-4795-b3f7-da67c254ce30/Badge)](https://dev.azure.com/cloudcoreproject/CloudCore/_packaging?_a=package&feed=8949198b-5c74-42af-9d30-e8c462acada6&package=396a2077-073e-4795-b3f7-da67c254ce30&preferRelease=true)



<div id="description">
Factory extensions to IConfiguration to enable configuration to use Kubernetes secrets files.  
</div>

## Usage

### Adding Sources - IConfigurationBuilder

You can explicitly add the Kubernetes secret configs as follows:

```csharp
builder
   .AddJsonFile("appSettings.json", true)
   .AddKubernetesSecrets()
   .Build();
```

Or you can just use the extension we've build, `UseDefaultConfigs`, as follows:

```csharp
builder.UseDefaultConfigs().Build();
```

Using this second method will add all configuration in the following order:

 - Kubernetes Secrets
 - Environment Variables
 - Json File (appsettings.json) [ignores if not found]
 - Command Line Arguments

NOTE: When using the DefaultConfigs method, it can optionally add json files (automatically) for different environments using environment variables.  The variable required
should be `ENVIRONMENT`, and it would typically be set to *dev* in development scenarios but can equally be set to *staging* for staging specific config.  Any config loaded
in this way will overwrite json file configs.

### Adding Values - IConfigurationBuilder

Extensions have been created to allow values to be added to the builder collection before being built.  Individually, as follows:

```csharp
builder.AddValue("MyKey", "MyValue");
```

Or as a collection of KeyValuePair's (`IEnumerable<KeyValuePair<string, string>>`), as follows:

```csharp
builder.AddValues(new List<KeyValuePair<string, string>> {
     new KeyValuePair<string, string>("testKey", "testVal")
});
```

### Reading Values - IConfigurationBuilder/IConfiguration

Extensions have been added to allow values to be read from the builder before it's been built.  Individually, as follows:

```csharp
var configVal = builder.GetValue<int>("MyIntExample");
```

Or gather all configured settings before being built:

```csharp
var allSettings = builder.GetAllSettingsAsString();
```

The equilivent for a build Configuration Builder (IConfiguration) would then be:

```csharp
var config = builder.Build();
var allSettings = config.GetAllSettings(); // as collection of values
 allSettingsString = config.GetAllSettingsAsString(); // as outputable string
```

An extension has been added to IConfiguration for attempting to get values:

```csharp
var config = builder.Build();
var success = config.TryGetValue<int>("MyIntKey", out var myInt);
```

### Binding Configuration

IConfiguration can only be bound to a type class when using the GetSection<T> call.  Meaning it has to be bound to a sub section of the config file, rather than the base section.  It's sometimes useful to bind the base section values to a typed class.  It can therefore be done as follows:

```csharp
var config = builder.Build();
var myTypedInstance = config.BindBaseSection<AppSettings>();
```

### IMemoryCache GetOrBuild

An extension method that allows a memory cache item to be gathered from in memory cache or, if it does not already exist, to build and store the item.

```csharp
IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

var cacheMe = "my cached piece of text";

// Will not exist already, therefore will build it.
var cachedItem = cache.GetOrBuild("key", () => cacheMe, TimeSpan.FromMinutes(30));
	
 Will just get the previously cached item.
 = cache.Get("key");
```

An additional `Contains` method has also been added for convenience when checking if an item already exists in cache.

## Test Coverage
A threshold will be added to this package to ensure the test coverage is above 80% for branches, functions and lines.  If it's not above the required threshold 
(threshold that will be implemented on ALL of the core repositories to gurantee a satisfactory level of testing), then the build will fail.

## Compatibility
This package has has been written in .net Standard and can be therefore be referenced from a .net Core or .net Framework application. The advantage of utilising from a .net Core application, 
is that it can be deployed and run on a number of host operating systems, such as Windows, Linux or OSX.  Unlike referencing from the a .net Framework application, which can only run on 
Windows (or Linux using Mono).
 
## Setup
This package is built using .net Standard 2.1 and requires the .net Core 3.1 SDK, it can be downloaded here: 
https://www.microsoft.com/net/download/dotnet-core/

IDE of Visual Studio or Visual Studio Code, can be downloaded here:
https://visualstudio.microsoft.com/downloads/

## How to access this package
All of the Cloud.Core.* packages are published to a internal NuGet feed.  To consume this on your local development machine, please add the following feed to your feed sources in Visual Studio:
https://pkgs.dev.azure.com/cloudcoreproject/CloudCore/_packaging/Cloud.Core/nuget/v3/index.json
 
For help setting up, follow this article: https://docs.microsoft.com/en-us/vsts/package/nuget/consume?view=vsts


<img src="https://cloud1core.blob.core.windows.net/icons/cloud_core_small.PNG" />
