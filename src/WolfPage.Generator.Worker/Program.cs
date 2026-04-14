using Microsoft.EntityFrameworkCore;
using WolfPage.Generator.Application.Persistence;
using WolfPage.Generator.Application.Rendering;
using WolfPage.Generator.Application.Services;
using WolfPage.Generator.Infrastructure.Options;
using WolfPage.Generator.Infrastructure.Persistence;
using WolfPage.Generator.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPageGenerationRepository, PageGenerationRepository>();
builder.Services.AddScoped<IPageGenerationService, PageGenerationService>();
builder.Services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();

builder.Services.AddHostedService<RabbitMqPageConsumerService>();

var host = builder.Build();
await host.RunAsync();