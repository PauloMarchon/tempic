using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Tempic.BackgroundServices;
using Tempic.Data;
using Tempic.Services;
using Tempic.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string sqLiteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqLiteConnectionString));

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

builder.Services.AddScoped<IImageUploadService, ImageUploadService>();

builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

builder.Services.Configure<CleanupSettings>(
    builder.Configuration.GetSection(CleanupSettings.SectionName));

builder.Services.AddHostedService<ImageCleanupService>();

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
/*
 * TODO
 * try
            {
                bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs()
                    .WithBucket(_minioSettings.BucketName));
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "Error checking if bucket exists");
                throw new Exception("Error checking if bucket exists", ex);
            }
*/