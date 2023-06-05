using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MinimalAPI.Data;
using MinimalAPI.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

#region Configure Services

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API",
        Description = "Developed by Rafael Etiene",
        Contact = new OpenApiContact { Name = "Rafael Etiene", Email = "rafaellimaetiene@gmail.com" }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
    
builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("MinimalAPI")
));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor", policy => policy.RequireClaim("ExcluirFornecedor"));
});


var app = builder.Build();

#endregion

#region Configure pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();
app.UseHttpsRedirection();

app.MapPost("/registro", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
        .WithUserManager(userManager)
        .WithJwtSettings(appJwtSettings.Value)
        .WithEmail(user.Email)
        .WithJwtClaims()
        .WithUserClaims()
        .WithUserRoles()
        .BuildUserResponse();

        return Results.Ok(jwt);
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("RegistroUsuario")
    .WithTags("Usuario");


app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser loginUser
    ) =>
    {
        if (loginUser == null)
            return Results.BadRequest("Usuário não informado");
        if (!MiniValidator.TryValidate(loginUser, out var erros))
            return Results.ValidationProblem(erros);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut)
            return Results.BadRequest("Usuário bloqueada");
        if (!result.Succeeded)
            return Results.BadRequest("Usuário ou senha inválidos");

        var jwt = new JwtBuilder()
            .WithUserManager(userManager)
            .WithJwtSettings(appJwtSettings.Value)
            .WithEmail(loginUser.Email)
            .WithJwtClaims()
            .WithUserClaims()
            .WithUserRoles()
            .BuildUserResponse();

        return Results.Ok(jwt);
    });



app.MapGet("/fornecedor", [AllowAnonymous] async (
    MinimalContextDb context) =>
    await context.Fornecedores.ToListAsync())
.WithName("GetFornecedor")
.WithTags("Fornecedor");


app.MapGet("/fornecedor/{id}", [AllowAnonymous] async (
    Guid id,
    MinimalContextDb context) =>
    await context.Fornecedores.FindAsync(id)
    is Fornecedor fornecedor ? Results.Ok(fornecedor) : Results.NotFound()
)
.Produces<Fornecedor>(StatusCodes.Status200OK)
.Produces<Fornecedor>(StatusCodes.Status404NotFound)
.WithName("GetFornecedorPorId")
.WithTags("Fornecedor");


app.MapPost("fornecedor", [Authorize] async (MinimalContextDb context, Fornecedor fornecedor) =>
{
    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);


    context.Fornecedores.Add(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0 ? Results.Created($"/fornecedor/{fornecedor.Id}", fornecedor) : Results.BadRequest("Houve um erro ao salvar o registro");
})
.Produces<Fornecedor>(StatusCodes.Status201Created)
.Produces<Fornecedor>(StatusCodes.Status400BadRequest)
.WithName("PostFornecedor")
.WithTags("Fornecedor");


app.MapPut("/fornecedor/{id}", [Authorize] async (Guid id, Fornecedor fornecedor, MinimalContextDb context) =>
{
    var fornecedorBanco = await context.Fornecedores.FindAsync(id);
    if (fornecedorBanco == null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Update(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0 ? Results.NoContent() : Results.BadRequest("Houver um erro ao salvar o registro");
})
.Produces<Fornecedor>(StatusCodes.Status204NoContent)
.Produces<Fornecedor>(StatusCodes.Status400BadRequest)
.WithName("PutFornecedor")
.WithTags("Fornecedor");


app.MapDelete("fornecedor/{id}", [Authorize] async (MinimalContextDb context, Guid id) =>
{
    var fornecedor = await context.Fornecedores.FindAsync(id);
    if (fornecedor == null) return Results.NotFound();

    context.Fornecedores.Remove(fornecedor);
    var result = await context.SaveChangesAsync();

    return result > 0 ? Results.NoContent() : Results.BadRequest("Houve um erro ao deletar o registro");
})
.Produces<Fornecedor>(StatusCodes.Status204NoContent)
.Produces<Fornecedor>(StatusCodes.Status400BadRequest)
.RequireAuthorization("ExcluirFornecedor")
.WithName("DeleteFornecedor")
.WithTags("Fornecedor");

app.Run();

#endregion