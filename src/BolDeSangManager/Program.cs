using BolDeSangManager.Components;
using BolDeSangManager.Components.Account;
using BolDeSangManager.Data;
using BolDeSangManager.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// MudBlazor
builder.Services.AddMudServices();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Durée pendant laquelle l'état d'un circuit déconnecté est conservé côté
        // serveur. Les navigateurs mobiles (Safari iOS surtout) gèlent l'onglet en
        // arrière-plan : sans cette marge, un coach qui prend un appel pendant la
        // saisie d'une feuille de match retrouve un circuit refusé et perd sa saisie.
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
        options.DisconnectedCircuitMaxRetained = 200;
    })
    .AddHubOptions(options =>
    {
        // Doit rester cohérent avec withServerTimeout/withKeepAliveInterval défini
        // dans wwwroot/reconnect.js. Règle : timeout >= 2 x keep-alive.
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

// Auth
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// SplitQuery : quand une requête charge plusieurs collections à la fois
// (les équipes ET les matchs d'une ligue, par exemple), EF produit par défaut
// une seule jointure — donc le PRODUIT CROISÉ des deux listes. Sur une ligue
// de 16 équipes et 120 matchs, SQLite renvoyait 1920 lignes pour 136 lignes
// utiles, et EF recomposait le tout en mémoire à chaque affichage.
// En mode Split, EF émet une requête par collection : le volume redevient une
// addition au lieu d'une multiplication.
//
// Contrepartie assumée : les requêtes séparées ne sont pas dans une transaction
// unique, donc une écriture concurrente pourrait théoriquement produire un
// ensemble incohérent. Sur ce projet les lectures d'affichage tolèrent ce
// risque, et SQLite ne sert qu'un processus.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString,
        sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Compression des réponses HTML. Mesuré : « Mes matchs » fait 96 Ko de HTML
// brut, 12 Ko compressé (-88 %). Traefik compresse déjà les CSS/JS mais PAS
// le text/html : la page elle-même partait en clair à chaque navigation.
// Le gain se voit surtout en 4G et sur les écrans à grosse ligue.
//
// Activé ici plutôt que dans Traefik pour que le comportement suive
// l'application, quel que soit l'hébergement.
builder.Services.AddResponseCompression(options =>
{
    // Blazor Server sert aussi le HTML en HTTPS : sans ce drapeau la
    // compression serait ignorée sur exactement les pages qui comptent.
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
    [
        "text/html",
        "application/octet-stream"   // flux du circuit Blazor
    ]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
    o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o =>
    o.Level = System.IO.Compression.CompressionLevel.Fastest);

// Permet la lecture des headers X-Forwarded-For / X-Forwarded-Proto envoyés par un reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();   // accepter tout réseau (Docker bridge)
    options.KnownProxies.Clear();
});

// Clés DataProtection persistées dans le volume /data (évite la perte de sessions au redémarrage)
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(dpKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<GmailEmailSender>();
builder.Services.AddScoped<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<GmailEmailSender>());
builder.Services.AddSingleton<EmailResendCooldownService>();

// Services métier
builder.Services.AddScoped<LeagueService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<MarkdownService>();
builder.Services.AddScoped<CalendrierService>();
builder.Services.AddScoped<LeagueExportService>();
builder.Services.AddScoped<GameDataExportService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<DataEditService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<BolDeSangManager.Services.IAuthorizationService, BolDeSangManager.Services.AuthorizationService>();

// QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// Configurer le pipeline
// UseForwardedHeaders DOIT être le premier middleware : il réécrit Request.Scheme /
// Request.Host à partir des X-Forwarded-* envoyés par le reverse proxy (Traefik).
// Tout middleware placé avant lui (HSTS, gestion d'erreurs, redirections) verrait
// encore http:// et générerait des URLs absolues en clair.
app.UseForwardedHeaders();

// Compression : juste après UseForwardedHeaders, donc avant tout middleware qui
// écrit dans la réponse. Placée plus bas, une partie des réponses partirait déjà
// en clair.
app.UseResponseCompression();

// Auth déclarée EXPLICITEMENT ici : sans ces deux appels, WebApplication insère
// automatiquement UseAuthentication/UseAuthorization en TÊTE de pipeline, donc AVANT
// UseForwardedHeaders — la redirection vers /Account/Login serait alors construite
// avec le scheme http:// même quand Traefik annonce X-Forwarded-Proto: https.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// En container (DOTNET_RUNNING_IN_CONTAINER=true, défini par les images .NET officielles),
// le reverse proxy gère HTTPS — ne pas rediriger pour éviter le warning de port introuvable
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();

// Seeder au démarrage
await DbSeeder.SeedAsync(app.Services);

app.Run();
