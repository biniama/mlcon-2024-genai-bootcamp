# Talk To Your App

A sample application that shows how you can use Semantic Kernel with Open AI or Azure Open AI to automate your Blazor web application.

## Getting started

In the `Client` project, add your Open AI or Azure Open AI configuration (ApiKey, Model or Azure Deployment name, Base url) to the `wwwroot/appsettings.json` file.

Then launch the `Server` project, as this will also start up the required API enpoints.

### Configure your OpenAI services and API Keys

Depending on whether you use OpenAI directly or Azure OpenAI, you need to provide your API keys. Since they should be secret, please add them to your user secrets and not in the `appsettings.json` file.

The Blazor application communicates through a local YARP proxy to the (Azure) OpenAI service, and so we configure the api keys as header transformations in YARP. So please add your API keys to one or both of the following settings: 

```pwsh
cd Server

dotnet user-secrets set ReverseProxy:Routes:openai:Transforms:1:Set "Bearer YOUR_OPENAI_API_KEY"
dotnet user-secrets set ReverseProxy:Routes:azureopenai:Transforms:1:Set YOUR_AZURE_OPENAI_API_KEY
```

## Plugins

* ApplicationNavigation  
  Uses reflection to find all the pages in the application, allows the AI to navigate to any page in the application, it should also be able to use parameters to initialize the page correctly.
* FormCompletion  
  Uses reflection to find all the forms in the application, allows the AI to fill in the form fields.
