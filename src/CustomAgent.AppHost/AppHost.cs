using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.CustomAgent_Server>("customagent-server");

var client = builder.AddViteApp("customagent-client", "../customagent.client", "dev")
    .WithHttpsEndpoint(port: 54221, env: "PORT")
    .WithReference(server);

builder.Build().Run();
