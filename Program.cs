using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Validations;
using ModpackDownloadAPI;
using Modrinth;
using Modrinth.Exceptions;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ArchiveCreator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/downloadmodpack/modrinth", async (HttpContext context, ArchiveCreator archiveCreator, [FromQuery] string versionId) =>
{
    try
    {
        var modrinthClient = new ModrinthClient();
        var version = await modrinthClient.Version.GetAsync(versionId);
        var projectInfoTask = modrinthClient.Project.GetAsync(version.ProjectId);
        if (version.Dependencies == null) return Results.Problem(detail: "Requested modpack contains no dependencies.");
        List<Task<Modrinth.Models.Version>> dependencyRequestsList = new();
        foreach (var dependency in version.Dependencies)
        {
            if (dependency.VersionId == null) continue;
            dependencyRequestsList.Add(modrinthClient.Version.GetAsync(dependency.VersionId));
        }
        var dependencyTasksArray = dependencyRequestsList.ToArray();
        await Task.WhenAll(dependencyTasksArray);
        var archive = await archiveCreator.CreateArchive(dependencyTasksArray.Select(t => t.Result.Files[0].Url));
        var archiveName = (await projectInfoTask).Title.Replace('.', '_') + ".zip";
        app.Logger.LogInformation("Content size: {}", archive.Length);
        context.Response.ContentLength = archive.Length;
        return Results.File(archive, fileDownloadName: archiveName, contentType: "application/zip");
    }
    catch (ModrinthApiException e)
    {
        return Results.Problem(detail: e.Message);
    }
}).WithName("DownloadFromModrinth")
.WithOpenApi();

app.Run();
