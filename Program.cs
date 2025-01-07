using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using ModpackDownloadAPI;
using Modrinth;
using Modrinth.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ArchiveCreator>();
builder.Services.AddHttpClient<ModrinthClient>();
builder.Services.AddSingleton(new CurseForge.APIClient.ApiClient(
    builder.Configuration.GetValue<string>("CurseForgeApiKey")));
builder.Services.AddScoped<CurseForgeModpackParser>();
builder.Services.AddSingleton(new ModrinthClientConfig());
builder.Services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));
builder.Services.AddSingleton<CurseForgeErrorReportFabric>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();

app.MapGet("/downloadmodpack/modrinth", async (HttpContext context, ArchiveCreator archiveCreator,
    ModrinthClient modrinthClient, [FromQuery, Required] string versionId) =>
{
    try
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var version = await modrinthClient.Version.GetAsync(versionId);
        var projectInfoTask = modrinthClient.Project.GetAsync(version.ProjectId);
        if (Path.GetExtension(version.Files[0].FileName) != ".mrpack") return Results.BadRequest("Requested resource is not modpack.");
        var archiveStream = await archiveCreator.CreateMrpackArchive(version.Files[0].Url);
        var projectInfo = await projectInfoTask;
        app.Logger.LogInformation("Content size: {}, Elapsed Time: {}", archiveStream.Length, stopwatch.Elapsed.TotalSeconds);
        context.Response.ContentLength = archiveStream.Length;
        return Results.File(archiveStream, fileDownloadName: $"{projectInfo.Title} {version.Name}.zip", 
            contentType: "application/zip");
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
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    var modpackInfo = await curseForgeClient.GetModAsync(projectId);
    if (modpackInfo.Error != null) return modpackInfo.Error.CreateAPIResponse();
    if (modpackInfo.Data.ClassId != 4471) return Results.BadRequest("Requested resource is not modpack.");
    var modpackFileInfo = await curseForgeClient.GetModFileAsync(projectId, fileId);
    if (modpackFileInfo.Error != null) return modpackFileInfo.Error.CreateAPIResponse();
    var archive = await archiveCreator.CreateCurseForgeArchive(modpackFileInfo.Data.DownloadUrl);
    context.Response.ContentLength = archive.Length;
    app.Logger.LogWarning("Execution completed in: {} seconds.", stopwatch.Elapsed.TotalSeconds);
    return Results.File(archive, fileDownloadName: modpackFileInfo.Data.DisplayName + ".zip", contentType: "application/zip", enableRangeProcessing: true);
}).WithName("DownloadFromCurseForge")
.WithOpenApi();

app.Run();
