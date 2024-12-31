using Microsoft.AspNetCore.Mvc;
using ModpackDownloadAPI;
using Modrinth;
using Modrinth.Exceptions;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ArchiveCreator>();
builder.Services.AddHttpClient<ModrinthClient>();
builder.Services.AddHttpClient<CurseForgeModpackParser>();
builder.Services.AddScoped(
    sp => new CurseForge.APIClient.ApiClient(builder.Configuration.GetValue<string>("CurseForgeApiKey")
    ));
builder.Services.AddSingleton(new ModrinthClientConfig());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/downloadmodpack/modrinth", async (HttpContext context, ArchiveCreator archiveCreator,
    ModrinthClient modrinthClient, [FromQuery, Required] string versionId) =>
{
    try
    {
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
        var dependencies = await Task.WhenAll(dependencyTasksArray);
        var archiveStream = await archiveCreator.CreateArchive(dependencies.Select(d => d.Files[0].Url));
        var archiveName = (await projectInfoTask).Title.Replace('.', '_') + ".zip";
        app.Logger.LogInformation("Content size: {}", archiveStream.Length);
        context.Response.ContentLength = archiveStream.Length;
        return Results.File(archiveStream, fileDownloadName: archiveName, contentType: "application/zip");
    }
    catch (ModrinthApiException e)
    {
        return Results.Problem(detail: e.Message);
    }
}).WithName("DownloadFromModrinth")
.WithOpenApi();

app.MapGet("/downloadmodpack/curseforge", async (HttpContext context, CurseForge.APIClient.ApiClient curseForgeClient, CurseForgeModpackParser modpackParser, ArchiveCreator archiveCreator,
    [FromQuery, Required] int projectId, [FromQuery, Required] int fileId) =>
{
    var modpackInfo = await curseForgeClient.GetModFileAsync(projectId, fileId);
    var fileDownloadUrls = await modpackParser.ParseModpack(modpackInfo.Data.DownloadUrl);
    var archive = fileDownloadUrls.Item2 == null || fileDownloadUrls.Item2.Length == 0 ? await archiveCreator.CreateArchive(fileDownloadUrls.Item1)
    : await archiveCreator.CreateArchiveWithReport(fileDownloadUrls.Item1, fileDownloadUrls.Item2);
    context.Response.ContentLength = archive.Length;
    return Results.File(archive, fileDownloadName: modpackInfo.Data.DisplayName + ".zip", contentType: "application/zip", enableRangeProcessing: true);
}).WithName("DownloadFromCurseForge")
.WithOpenApi();

app.Run();
