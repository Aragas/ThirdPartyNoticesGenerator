# thirdpartynoticesgenerator

[![NuGet](https://img.shields.io/nuget/v/ThirdPartyNoticesGenerator)](https://www.nuget.org/packages/ThirdPartyNoticesGenerator/)

## Installation

```
dotnet tool install -g ThirdPartyNoticesGenerator
```

## Get started

Go inside the project directory and run:

```
thirdpartynoticesgenerator
```

To change the name of the file that will be generated:

```
thirdpartynoticesgenerator --output-filename "third party notices.txt"
```

If your project is in a different directory:

```
thirdpartynoticesgenerator <project directory path>
thirdpartynoticesgenerator --output-filename "third party notices.txt" <project directory path>
```

Enabling unsafe resolver (will try to get the license from a githib.io link, considered unsafe because it might not lead to the desired repo):

```
thirdpartynoticesgenerator --use-unsafe-resolvers
```

For avoiding GitHub's API Rate Limit:

```
thirdpartynoticesgenerator --github-oauth CLIENT_ID:CLIENT_SECRET
```

## How it works
  
### 1. Resolve assemblies
  
It uses MSBuild to resolve assemblies that should land in the publish folder or release folder.  
  
For new SDK-style projects this is done using `ComputeFilesToPublish` target.  
  
For traditional .NET Framework projects this is done using `ResolveAssemblyReferences` target. Currently doesn't seem to work as expected.  
  
### 2. Try to find license based on the information from .nupkg
  
It tries to find `.nupkg` for those assemblies and attempts to crawl the license content either from the inner license file,
the license from the repository based on the commit, licenseUrl or projectUrl.  
  
Crawling from projectUrl currently works only with github.com and github.io projectUrls.  
  
Crawling from licenseUrl works with nuget.org, github.com, opensource.org and anything with `text/plain` Content-Type.
Any other content type will not be processed and will just yield the urls as the license.  
  
If the NuGet isn's found, will fail at getting the license.