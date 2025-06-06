var builder = DistributedApplication.CreateBuilder(args);

var sqldb = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("sqldb");

var products = builder.AddProject<Projects.Products>("products")
    .WithReference(sqldb)
    .WaitFor(sqldb);

var onlineresearcher = builder.AddProject<Projects.OnlineResearcher>("onlineresearcher")
    .WithExternalHttpEndpoints();

var parkinformationagent = builder.AddProject<Projects.ParkInformationAgent>("parkinformationagent")
    .WithExternalHttpEndpoints();

var weatheragent = builder.AddProject<Projects.WeatherAgent>("weatheragent")
    .WithExternalHttpEndpoints();

var eshopmcpserver = builder.AddProject<Projects.eShopMcpSseServer>("eshopmcpserver")
    .WithReference(onlineresearcher)
    .WaitFor(onlineresearcher)
    .WithReference(products)
    .WaitFor(products)
    .WithReference(weatheragent)
    .WaitFor(weatheragent)
    .WithReference(parkinformationagent)
    .WaitFor(parkinformationagent)
    .WithExternalHttpEndpoints();

var store = builder.AddProject<Projects.Store>("store")
    .WithReference(eshopmcpserver)
    .WaitFor(eshopmcpserver)
    .WithExternalHttpEndpoints();

// optional to check the SSE server
// builder.AddMCPInspector().WithSSE(eshopmcpserver);

if (builder.ExecutionContext.IsPublishMode)
{
    // production code uses Azure services, so we need to add them here
    var appInsights = builder.AddAzureApplicationInsights("appInsights");
    var chatDeploymentName = "gpt-4o-mini";
    var embeddingsDeploymentName = "text-embedding-ada-002";
    var aoai = builder.AddAzureOpenAI("openai")
        .AddDeployment(new AzureOpenAIDeployment(chatDeploymentName,
        "gpt-4o-mini",
        "2024-07-18",
        "GlobalStandard",
        10))
        .AddDeployment(new AzureOpenAIDeployment(embeddingsDeploymentName,
        "text-embedding-ada-002",
        "2"));

    products.WithReference(appInsights)
        .WithReference(aoai)
        .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName)
        .WithEnvironment("AI_embeddingsDeploymentName", embeddingsDeploymentName);

    eshopmcpserver.WithReference(appInsights)
        .WithReference(aoai)
        .WithEnvironment("AI_ChatDeploymentName", chatDeploymentName)
        .WithEnvironment("AI_embeddingsDeploymentName", embeddingsDeploymentName);

    store.WithReference(appInsights)
        .WithReference(aoai)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
