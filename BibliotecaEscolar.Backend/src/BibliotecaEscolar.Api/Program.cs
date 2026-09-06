using BibliotecaEscolar.Api.Configuration;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.Middleware;
using BibliotecaEscolar.Api.Repositories;
using BibliotecaEscolar.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
// Console funciona no terminal, em contêineres e sem privilégios de Event Log no Windows.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var provider = builder.Configuration["Database:Provider"] ?? "Postgres";
var connectionString = builder.Configuration.GetConnectionString("Biblioteca");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:Biblioteca. Consulte docs/SUPABASE.md.");
if (provider == "Sqlite")
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("O banco de demonstração Sqlite só é permitido no ambiente Development.");
    builder.Services.AddDbContext<SqliteBibliotecaDbContext>(options => options.UseSqlite(connectionString));
    builder.Services.AddScoped<BibliotecaDbContext>(services => services.GetRequiredService<SqliteBibliotecaDbContext>());
}
else if (provider == "Postgres")
{
    builder.Services.AddDbContext<PostgresBibliotecaDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<BibliotecaDbContext>(services => services.GetRequiredService<PostgresBibliotecaDbContext>());
}
else throw new InvalidOperationException("Database:Provider deve ser Sqlite ou Postgres.");

builder.Services.AddBibliotecaAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<BibliotecaClock>();
builder.Services.AddScoped<LivroService>();
builder.Services.AddScoped<AlunoService>();
builder.Services.AddScoped<EmprestimoRepository>();
builder.Services.AddScoped<EmprestimoService>();
builder.Services.AddScoped<DashboardService>();
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Biblioteca Escolar API", Version = "v1", Description = "Base acadêmica: livros, alunos e empréstimos. Em Development, o operador é simulado." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Access token do Supabase Auth. Dispensado no modo de demonstração local."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    if (provider == "Sqlite")
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        await db.Database.MigrateAsync();
        if (app.Configuration.GetValue<bool>("Demo:SeedData"))
            await DemoData.SeedAsync(db, scope.ServiceProvider.GetRequiredService<BibliotecaClock>());
    }
    if (app.Configuration["Auth:Mode"] == "Development")
        app.Logger.LogWarning("DEMONSTRAÇÃO LOCAL: autenticação simulada. Utilize dados fictícios e acesso em localhost.");
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", async (BibliotecaDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503))
    .RequireAuthorization("Operador");
app.Run();

public partial class Program;
