using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder
	.AddPostgres("postgres", password: postgresPassword)
	.WithDataVolume("tomatime-postgres-data");

var database = postgres.AddDatabase("tomatime");

builder
	.AddProject<Projects.Tomatime_Api>("tomatime-api")
	.WithReference(database)
	.WaitFor(database);

builder.Build().Run();
