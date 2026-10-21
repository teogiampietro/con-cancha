using ConCancha.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "ConCancha API", Version = "v1" }));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ConCanchaDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// /health: la app está viva. /health/ready: además responde la base de datos.
app.MapHealthChecks("/health", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.Run();

public partial class Program;
