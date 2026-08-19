using BolDeSangManager.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using MimeKit;

namespace BolDeSangManager.Services;

public class GmailEmailSender(SettingsService settings, ILogger<GmailEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        await SendAsync(email, "Confirmation de votre compte — BolDeSang Manager",
            BuildEmail("Confirmer votre adresse email",
                "Merci de vous être inscrit sur BolDeSang Manager. Cliquez sur le bouton ci-dessous pour confirmer votre adresse email.",
                confirmationLink, "Confirmer mon email"));

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        await SendAsync(email, "Réinitialisation de votre mot de passe — BolDeSang Manager",
            BuildEmail("Réinitialisation du mot de passe",
                "Vous avez demandé la réinitialisation de votre mot de passe. Cliquez sur le bouton ci-dessous. Ce lien est valable 1 heure.",
                resetLink, "Réinitialiser mon mot de passe",
                footer: "Si vous n'avez pas fait cette demande, ignorez cet email — votre mot de passe restera inchangé."));

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        await SendAsync(email, "Code de réinitialisation — BolDeSang Manager",
            BuildCodeEmail(resetCode));

    public Task EnvoyerNotificationMatchAsync(
        string to, string subject, string titre, string texte, string lien, string bouton,
        string? footer = null) =>
        SendAsync(to, subject, BuildEmail(titre, texte, lien, bouton, footer));

    /// <summary>
    /// Indique si le compte d'envoi Gmail est configuré (expéditeur + mot de passe d'application).
    /// À appeler AVANT d'annoncer un envoi à l'utilisateur : sans configuration, <see cref="SendAsync"/>
    /// ignore le message silencieusement, ce qui produirait un faux succès.
    /// </summary>
    public async Task<bool> EstConfigureAsync()
    {
        var senderEmail = await settings.GetAsync(SettingsService.CleEmailExpediteur);
        var password    = await settings.GetAsync(SettingsService.CleEmailMotDePasse);
        return !string.IsNullOrWhiteSpace(senderEmail) && !string.IsNullOrWhiteSpace(password);
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        var senderEmail = await settings.GetAsync(SettingsService.CleEmailExpediteur);
        var senderName  = await settings.GetAsync(SettingsService.CleEmailNomExpediteur) ?? "BolDeSang Manager";
        var password    = await settings.GetAsync(SettingsService.CleEmailMotDePasse);

        if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Email non configuré — message vers {To} ignoré. Configurez le compte Gmail dans Admin > Paramètres.", to);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email envoyé à {To} : {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'envoi d'email vers {To}", to);
            throw;
        }
    }

    private static string BuildEmail(string titre, string texte, string lien, string bouton, string? footer = null) => $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:0;background:#121212;font-family:Arial,Helvetica,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr><td align="center" style="padding:40px 16px">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%">
                <tr>
                  <td style="background:#c62828;padding:24px 32px;border-radius:8px 8px 0 0">
                    <span style="color:#fff;font-size:1.4rem;font-weight:bold">⚔ BolDeSang Manager</span>
                  </td>
                </tr>
                <tr>
                  <td style="background:#1e1e1e;padding:32px;border-radius:0 0 8px 8px">
                    <h2 style="color:#ef9a9a;margin-top:0">{titre}</h2>
                    <p style="color:#e0e0e0;line-height:1.6">{texte}</p>
                    <a href="{lien}"
                       style="display:inline-block;background:#c62828;color:#fff;padding:13px 28px;border-radius:4px;text-decoration:none;font-weight:bold;font-size:1rem;margin:8px 0 24px">
                      {bouton}
                    </a>
                    {(footer is not null ? $"<p style=\"color:#9e9e9e;font-size:0.85rem\">{footer}</p>" : "")}
                    <hr style="border:none;border-top:1px solid rgba(255,255,255,0.08);margin:24px 0" />
                    <p style="color:#757575;font-size:0.75rem;margin:0">
                      Blood Bowl &amp; Dungeon Bowl — Gestion de ligues associatives
                    </p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private static string BuildCodeEmail(string code) => $"""
        <!DOCTYPE html>
        <html>
        <body style="margin:0;padding:0;background:#121212;font-family:Arial,Helvetica,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr><td align="center" style="padding:40px 16px">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%">
                <tr>
                  <td style="background:#c62828;padding:24px 32px;border-radius:8px 8px 0 0">
                    <span style="color:#fff;font-size:1.4rem;font-weight:bold">⚔ BolDeSang Manager</span>
                  </td>
                </tr>
                <tr>
                  <td style="background:#1e1e1e;padding:32px;border-radius:0 0 8px 8px">
                    <h2 style="color:#ef9a9a;margin-top:0">Code de réinitialisation</h2>
                    <p style="color:#e0e0e0">Votre code de réinitialisation de mot de passe :</p>
                    <div style="font-size:2rem;font-weight:bold;letter-spacing:0.4em;color:#ef9a9a;margin:16px 0;padding:16px;background:rgba(198,40,40,0.1);border-radius:4px;text-align:center">
                      {code}
                    </div>
                    <p style="color:#9e9e9e;font-size:0.85rem">Si vous n'avez pas fait cette demande, ignorez cet email.</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
