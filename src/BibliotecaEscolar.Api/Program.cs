using BibliotecaEscolar.Api.Configuration;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.Middleware;
using BibliotecaEscolar.Api.Queries;
using BibliotecaEscolar.Api.Security;
using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
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
builder.AddRequestSecurity();

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
builder.Services.AddScoped<EmprestimoQueries>();
builder.Services.AddScoped<EmprestimoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOperator, HttpCurrentOperator>();
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
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
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
app.UseMiddleware<RequestBodySizeMiddleware>();
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
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting(RequestSecurityOptions.ApiRateLimitPolicy);
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", GetReadinessAsync).AllowAnonymous();
app.Run();

static async Task<IResult> GetReadinessAsync(
    BibliotecaDbContext db,
    ILogger<Program> logger,
    CancellationToken cancellationToken)
{
    try
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ok" })
            : Results.Json(new { status = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "A verificação de prontidão não conseguiu acessar o banco de dados.");
        return Results.Json(
            new { status = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

public partial class Program;
