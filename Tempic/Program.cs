using FluentValidation;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Tempic.BackgroundServices;
using Tempic.Data;
using Tempic.DTOs;
using Tempic.Repository;
using Tempic.Services;
using Tempic.Settings;
using Tempic.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string sqLiteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqLiteConnectionString)
    .EnableSensitiveDataLogging(true)
    .EnableDetailedErrors(true)
    .LogTo(
        message => Console.WriteLine(message),
        new[] { DbLoggerCategory.Database.Command.Name },
        LogLevel.Information
        )
    );

builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection("MinioSettings"));
builder.Services.AddScoped<MinioSettings>(sp => sp.GetRequiredService<IOptions<MinioSettings>>().Value);

builder.Services.AddSingleton<IMinioClient>(mc => 
{
    var minioSettings = mc.GetRequiredService<IOptions<MinioSettings>>().Value;

    return new MinioClient()
        .WithEndpoint(minioSettings.Endpoint)
        .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
        .Build();
});

builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<IImageMetadataRepository, ImageMetadataRepository>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<IShortenedUrlRepository, ShortenedUrlRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IValidator<UploadImageRequest>, UploadImageRequestValidator>();
builder.Services.AddScoped<ShortCodeGenerator>();
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

 //builder.Services.Configure<CleanupSettings>(
 //builder.Configuration.GetSection(CleanupSettings.SectionName));
 //builder.Services.AddHostedService<ImageCleanupService>();
 
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
