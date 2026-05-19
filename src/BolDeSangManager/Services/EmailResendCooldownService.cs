namespace BolDeSangManager.Services;

// Singleton — garde en mémoire la dernière date d'envoi par email pour appliquer le cooldown.
public class EmailResendCooldownService
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, DateTime> _lastSent = new(StringComparer.OrdinalIgnoreCase);

    public bool PeutEnvoyer(string email) =>
        !_lastSent.TryGetValue(email, out var last) || DateTime.UtcNow - last >= Cooldown;

    public TimeSpan? TempsRestant(string email) =>
        _lastSent.TryGetValue(email, out var last) && DateTime.UtcNow - last < Cooldown
            ? Cooldown - (DateTime.UtcNow - last)
            : null;

    public void Enregistrer(string email) =>
        _lastSent[email] = DateTime.UtcNow;
}
