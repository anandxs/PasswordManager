using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PasswordManager.Api;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase("AppDb");
});
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
{
    s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Place to add JWT with Bearer",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    s.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
                Name = "Bearer",
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapIdentityApi<IdentityUser>();
app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.MapGet("/passwords", (ApplicationDbContext db, ClaimsPrincipal user) =>
{
    ClaimsIdentity claimsIdentity = (ClaimsIdentity)user.Identity!;
    var id = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var passwords = db.UserPasswords?.Where(p => p.UserId.Equals(id));
    return Results.Ok(passwords);
})
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/passwords", (ApplicationDbContext db, ClaimsPrincipal user, string encryptedPassword) =>
{
    ClaimsIdentity claimsIdentity = (ClaimsIdentity)user.Identity!;
    var id = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var entity = new UserPassword
    {
        EncrptedPassword = encryptedPassword,
        UserId = id!,
    };
    db.UserPasswords!.Add(entity);
    db.SaveChanges();

    return Results.NoContent();
})
.WithOpenApi()
.RequireAuthorization();

app.MapDelete("/passwords", (ApplicationDbContext db, ClaimsPrincipal user, int passwordId) =>
{
    ClaimsIdentity claimsIdentity = (ClaimsIdentity)user.Identity!;
    var id = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var entity = db.UserPasswords!.FirstOrDefault(x => x.Id == passwordId);
    if (entity is null) return Results.NoContent();
    db.UserPasswords!.Remove(entity);
    db.SaveChanges();
    return Results.NoContent();
})
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager, [FromBody] object empty) =>
{
    if (empty != null)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }
    return Results.Unauthorized();
})
.WithOpenApi()
.RequireAuthorization();

app.Run();
