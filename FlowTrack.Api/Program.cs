using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FlowTrack.Application.WorkItems;
using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Users;
using FlowTrack.Application.Tasks;
using FlowTrack.Infrastructure;
using FlowTrack.Infrastructure.Persistence;
using FlowTrack.Api.Middleware;
using FlowTrack.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must contain at least 32 UTF-8 bytes.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddScoped<WorkItemService>();
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<CreateTaskUseCase>();
builder.Services.AddScoped<UpdateTaskUseCase>();
builder.Services.AddScoped<DeleteTaskUseCase>();
builder.Services.AddScoped<GetTasksUseCase>();
builder.Services.AddScoped<GetTaskByIdUseCase>();
builder.Services.AddSingleton<IWorkItemRepository, InMemoryWorkItemRepository>();
builder.Services.AddInfrastructure(builder.Configuration);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FlowTrack API",
        Version = "v1",
        Description = "Authentication and task management API for FlowTrack.",
        Contact = new OpenApiContact { Name = "FlowTrack Engineering" }
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "FlowTrack.Api.xml"), includeControllerXmlComments: true);
    options.SchemaFilter<SwaggerExamplesSchemaFilter>();
    options.OperationFilter<AuthorizeOperationFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT token."
    });

});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FlowTrack API v1");
        options.DocumentTitle = "FlowTrack API Documentation";
        options.DisplayOperationId();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Demo:Enabled"))
{
    await SeedDemoDataAsync(app);
}

app.Run();

static async Task SeedDemoDataAsync(WebApplication app)
{
    var email = app.Configuration["Demo:Email"]
        ?? throw new InvalidOperationException("Demo:Email must be configured when demo seeding is enabled.");
    var userName = app.Configuration["Demo:UserName"]
        ?? throw new InvalidOperationException("Demo:UserName must be configured when demo seeding is enabled.");
    var password = app.Configuration["Demo:Password"]
        ?? throw new InvalidOperationException("Demo:Password must be configured when demo seeding is enabled.");

    await using var scope = app.Services.CreateAsyncScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var existingUser = await userRepository.GetByEmailAsync(email);
    if (existingUser is null)
    {
        var registration = await scope.ServiceProvider
            .GetRequiredService<RegisterUserUseCase>()
            .ExecuteAsync(new RegisterUserRequest(userName, email, password));
        if (!registration.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to seed the configured demo user: {string.Join("; ", registration.Errors.Select(error => error.Message))}");
        }

        existingUser = registration.User!;
    }

    var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
    var existingTasks = await taskRepository.SearchAsync(
        new FlowTrack.Application.Tasks.TaskQuery(PageSize: 1, Search: "Welcome to FlowTrack"),
        existingUser.Id);
    if (existingTasks.TotalCount == 0)
    {
        var task = await scope.ServiceProvider
            .GetRequiredService<CreateTaskUseCase>()
            .ExecuteAsync(new FlowTrack.Application.Tasks.CreateTaskRequest(
                "Welcome to FlowTrack",
                "This sample task is stored in MongoDB and belongs to the demo account.",
                DateTimeOffset.UtcNow.AddDays(30),
                existingUser.Id));
        if (!task.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to seed the demo task: {string.Join("; ", task.Errors.Select(error => error.Message))}");
        }
    }
}

public partial class Program { }
