using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace BolDeSangManager.Services;

public class PdfService
{
    // NotoSansSymbols (Symbols1) : ⚕ U+2695 — Miscellaneous Symbols
    // NotoSansSymbols2          : ✦ U+2726 (Dingbats) + ★ U+2605 (Misc Symbols)
    // Deux fontes embarquées → fonctionne identiquement sur Windows (dev) et Linux (Docker).
    private const string SymbolFont1 = "BDS-Symbols1";
    private const string SymbolFont2 = "BDS-Symbols2";

    static PdfService()
    {
        var asm = typeof(PdfService).Assembly;
        using var s1 = asm.GetManifestResourceStream("BolDeSangManager.Resources.NotoSansSymbols-Regular.ttf")!;
        FontManager.RegisterFontWithCustomName(SymbolFont1, s1);
        using var s2 = asm.GetManifestResourceStream("BolDeSangManager.Resources.NotoSansSymbols2-Regular.ttf")!;
        FontManager.RegisterFontWithCustomName(SymbolFont2, s2);
    }

    public byte[] GenererFeuilleEquipe(Team equipe, bool inclureDescriptionsCompetences,
        Match? matchProchain = null, string? urlExterne = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                // ── En-tête ─────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(equipe.Nom)
                                .Bold().FontSize(20).FontColor(Colors.Red.Darken2);
                            inner.Item().Text($"{equipe.TeamType?.Nom} — {equipe.TeamType?.Game?.Nom}")
                                .FontSize(11).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(160).Column(inner =>
                        {
                            inner.Item().Text($"Coach : {equipe.Coach?.PseudoCoach ?? equipe.Coach?.UserName}")
                                .FontSize(10);
                            inner.Item().Text($"Ligue : {equipe.League?.Nom}").FontSize(10);
                        });

                        // QR code compact en haut à droite si match à venir
                        if (matchProchain is not null && !string.IsNullOrWhiteSpace(urlExterne))
                        {
                            var url = $"{urlExterne.TrimEnd('/')}/matchs/{matchProchain.Id}/feuille";
                            var adversaire = matchProchain.EquipeDomicileId == equipe.Id
                                ? matchProchain.EquipeExterieur?.Nom ?? "Adversaire"
                                : matchProchain.EquipeDomicile?.Nom ?? "Adversaire";
                            var ronde = matchProchain.Ronde >= 100
                                ? $"Play-off T{matchProchain.Ronde - 99}"
                                : $"R{matchProchain.Ronde}";
                            var qrBytes = GenererQrCode(url);

                            row.ConstantItem(80).Border(0.5f).BorderColor(Colors.Red.Lighten2)
                                .Background(Colors.Red.Lighten5)
                                .Padding(4).Column(inner =>
                                {
                                    inner.Item().AlignCenter().Height(52).Image(qrBytes).FitArea();
                                    inner.Item().PaddingTop(2).AlignCenter()
                                        .Text($"vs {adversaire}")
                                        .FontSize(6).Bold().FontColor(Colors.Red.Darken2);
                                    inner.Item().AlignCenter()
                                        .Text(ronde)
                                        .FontSize(6).FontColor(Colors.Grey.Darken2);
                                });
                        }
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Red.Darken2);
                });

                page.Content().Column(col =>
                {
                    // ── Stats de l'équipe ────────────────────────────────
                    var vea = equipe.Joueurs.Where(j => !j.EstMort && !j.EstRetraite).Sum(j => j.ValeurActuelle)
                            + equipe.NombreRelances * (equipe.TeamType?.CoutRelance ?? 50_000)
                            + equipe.FansDevoues * 10_000
                            + equipe.NombreCoachsAssistants * 10_000
                            + equipe.NombreCheerleaders * 10_000
                            + (equipe.Apothicaire ? 50_000 : 0);

                    col.Item().PaddingVertical(8).Row(row =>
                    {
                        StatBox(row, "Trésorerie",        $"{equipe.Tresorerie / 1000}k po");
                        StatBox(row, "VEA",               $"{vea / 1000}k po");
                        StatBox(row, "Relances",           equipe.NombreRelances.ToString());
                        StatBox(row, "Fans Dévoués",       equipe.FansDevoues.ToString());
                        StatBox(row, "Coachs assist.",     equipe.NombreCoachsAssistants.ToString());
                        StatBox(row, "Cheerleaders",       equipe.NombreCheerleaders.ToString());
                        StatBox(row, "Apothicaire",        equipe.Apothicaire ? "Oui" : "Non");
                        StatBox(row, "Matchs joués",       equipe.NombreMatchsJoues.ToString());
                        StatBox(row, "Points ligue",       equipe.PointsLigue.ToString());
                    });

                    // ── Tableau des joueurs ──────────────────────────────
                    var joueurs = equipe.Joueurs
                        .Where(j => !j.EstMort && !j.EstRetraite)
                        .OrderBy(j => j.Numero)
                        .ToList();

                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22);   // #
                            cols.RelativeColumn(3);    // Nom
                            cols.RelativeColumn(3);    // Poste
                            cols.ConstantColumn(22);   // M
                            cols.ConstantColumn(22);   // F
                            cols.ConstantColumn(22);   // AG
                            cols.ConstantColumn(22);   // CP
                            cols.ConstantColumn(22);   // AR
                            cols.ConstantColumn(30);   // PSP
                            cols.ConstantColumn(38);   // Valeur
                            cols.RelativeColumn(4);    // Compétences
                        });

                        // En-tête
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "#", "Nom", "Poste", "M", "F", "AG", "CP", "AR", "PSP", "Valeur", "Compétences" })
                            {
                                header.Cell()
                                    .Background(Colors.Red.Darken2)
                                    .BorderBottom(2).BorderColor(Colors.Red.Darken4)
                                    .PaddingVertical(6).PaddingHorizontal(4)
                                    .Text(h).FontColor(Colors.White).Bold().FontSize(8);
                            }
                        });

                        // Lignes joueurs
                        bool pair = false;
                        foreach (var joueur in joueurs)
                        {
                            var bg  = pair ? Colors.Grey.Lighten3 : Colors.White;
                            var pos = joueur.PlayerPosition;

                            var competences = string.Join(", ",
                                (pos?.CompetencesDepart.Select(c => c.Skill?.Nom ?? "") ?? [])
                                .Concat(joueur.Competences.Where(c => !c.EstCompetenceDepart).Select(c => c.Skill?.Nom ?? ""))
                                .Where(s => s != ""));

                            bool hasSequel = joueur.Blessures.Any(b =>
                                b.Type == InjuryType.BlessurePersistante);

                            Cell(table, bg, joueur.Numero.ToString(), center: true);
                            CellNom(table, bg, joueur.Nom, joueur.ManqueSuivantMatch, hasSequel);
                            // Poste + mots-clés
                            {
                                var cell = table.Cell()
                                    .Background(bg)
                                    .BorderBottom(1f).BorderColor(Colors.Grey.Lighten1)
                                    .PaddingVertical(6).PaddingHorizontal(4);
                                cell.Column(col2 =>
                                {
                                    col2.Item().Text(pos?.Nom ?? "—").FontSize(8);
                                    var mc = pos?.MotsCles ?? "";
                                    if (!string.IsNullOrEmpty(mc))
                                        col2.Item().Text(mc).FontSize(6).FontColor(Colors.Grey.Darken1).Italic();
                                });
                            }
                            CellStat(table, bg, ((pos?.Mouvement ?? 0) + joueur.ModMouvement).ToString(), joueur.ModMouvement);
                            CellStat(table, bg, ((pos?.Force    ?? 0) + joueur.ModForce).ToString(),      joueur.ModForce);
                            CellStat(table, bg, EffStatStr(pos?.Agilite       ?? "—", joueur.ModAgilite,       false), joueur.ModAgilite);
                            CellStat(table, bg, EffStatStr(pos?.CapacitePasse ?? "—", joueur.ModCapacitePasse, false), joueur.ModCapacitePasse);
                            CellStat(table, bg, EffStatStr(pos?.Armure        ?? "—", joueur.ModArmure,        true),  joueur.ModArmure);
                            CellPsp(table, bg, joueur.PointsStarPlayer);
                            Cell(table, bg, $"{joueur.ValeurActuelle / 1000}k", center: true);
                            Cell(table, bg, competences);

                            pair = !pair;
                        }
                    });

                    // Légende
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.AutoItem().Text(t => IconLegend(t, "⚕", " Rate le prochain match", Colors.Grey.Darken2));
                        row.AutoItem().PaddingLeft(12).Text(t => IconLegend(t, "✦", " Séquelle persistante", Colors.Red.Darken2));
                        row.AutoItem().PaddingLeft(12).Text(t => IconLegend(t, "★", " ≥ 6 PSP : amélioration disponible", Colors.Orange.Darken3));
                        row.AutoItem().PaddingLeft(12)
                            .Background(Colors.Red.Lighten4)
                            .Padding(2)
                            .Text("Stat réduite par blessure").FontSize(7).FontColor(Colors.Red.Darken3);
                    });

                    // ── Rappel des compétences ───────────────────────────
                    if (inclureDescriptionsCompetences)
                    {
                        var toutesCompetences = equipe.Joueurs
                            .Where(j => !j.EstMort && !j.EstRetraite)
                            .SelectMany(j =>
                                (j.PlayerPosition?.CompetencesDepart
                                    .Select(pps => pps.Skill)
                                    .OfType<Skill>()
                                 ?? Enumerable.Empty<Skill>())
                                .Concat(j.Competences
                                    .Where(c => !c.EstCompetenceDepart)
                                    .Select(c => c.Skill)
                                    .OfType<Skill>()))
                            .DistinctBy(s => s.Id)
                            .OrderBy(s => s.Categorie)
                            .ThenBy(s => s.Nom)
                            .ToList();

                        if (toutesCompetences.Count > 0)
                        {
                            col.Item().PaddingTop(14).Text("Rappel des Compétences")
                                .Bold().FontSize(11).FontColor(Colors.Red.Darken2);
                            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Red.Lighten2);

                            string? lastCat = null;
                            col.Item().PaddingTop(6).Column(comp =>
                            {
                                foreach (var skill in toutesCompetences)
                                {
                                    var cat = skill.Categorie.ToString();
                                    if (cat != lastCat)
                                    {
                                        comp.Item().PaddingTop(lastCat is null ? 0 : 6)
                                            .Text(cat).Bold().FontSize(8)
                                            .FontColor(Colors.Red.Darken2);
                                        lastCat = cat;
                                    }
                                    comp.Item().PaddingVertical(2).PaddingLeft(8).Row(row =>
                                    {
                                        row.ConstantItem(110).Text(skill.Nom).Bold().FontSize(8);
                                        row.RelativeItem().Text(skill.Description).FontSize(8)
                                            .FontColor(Colors.Grey.Darken2);
                                    });
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(10).Text("Aucune compétence à décrire.")
                                .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                        }
                    }
                });

                // ── Pied de page ─────────────────────────────────────────
                page.Footer().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm} — BolDeSangManager")
                        .FontSize(7).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(60).AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" / ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static byte[] GenererQrCode(string url)
    {
        using var generator = new QRCodeGenerator();
        var qrData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(10);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void StatBox(RowDescriptor row, string label, string valeur)
    {
        row.RelativeItem()
            .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(5).Column(c =>
            {
                c.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
                c.Item().Text(valeur).Bold().FontSize(10);
            });
    }

    private static void Cell(TableDescriptor table, string bg, string texte, bool center = false)
    {
        var content = table.Cell()
            .Background(bg)
            .BorderBottom(1f).BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(10).PaddingHorizontal(4);

        if (center)
            content.AlignCenter().Text(texte).FontSize(8);
        else
            content.Text(texte).FontSize(8);
    }

    private static void CellNom(TableDescriptor table, string bg, string nom, bool mnm, bool sequel)
    {
        table.Cell()
            .Background(sequel ? Colors.Red.Lighten5 : bg)
            .BorderBottom(1f).BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(10).PaddingHorizontal(4)
            .Text(text =>
            {
                text.Span(nom).Bold().FontSize(8);
                if (mnm)
                    SymbolSpan(text, " ⚕", 7, Colors.Orange.Darken3, SymbolFont1);
                if (sequel)
                    SymbolSpan(text, " ✦", 7, Colors.Red.Darken2, SymbolFont2);
            });
    }

    private static void CellStat(TableDescriptor table, string bg, string texte, int mod)
    {
        string effectiveBg = mod < 0 ? Colors.Red.Lighten4 : bg;
        string textColor   = mod < 0 ? Colors.Red.Darken3  : Colors.Grey.Darken4;
        table.Cell()
            .Background(effectiveBg)
            .BorderBottom(1f).BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(10).PaddingHorizontal(4)
            .AlignCenter()
            .Text(texte).FontSize(8)
            .FontColor(textColor);
    }

    private static string EffStatStr(string baseVal, int mod, bool plusEstMieux)
    {
        if (baseVal == "—" || baseVal == "-" || mod == 0) return baseVal;
        if (!int.TryParse(baseVal.TrimEnd('+'), out var n)) return baseVal;
        var effective = plusEstMieux ? n + mod : n - mod;
        var hasSuffix = baseVal.EndsWith('+');
        return hasSuffix ? $"{effective}+" : effective.ToString();
    }

    private static void CellPsp(TableDescriptor table, string bg, int psp)
    {
        table.Cell()
            .Background(bg)
            .BorderBottom(1f).BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(10).PaddingHorizontal(4)
            .Text(text =>
            {
                text.AlignCenter();
                text.Span(psp.ToString()).FontSize(8);
                if (psp >= 6)
                    SymbolSpan(text, " ★", 7, Colors.Orange.Darken3, SymbolFont2);
            });
    }

    private static void SymbolSpan(TextDescriptor t, string symbol, float size, string color, string font)
    {
        t.Span(symbol).FontFamily(font).FontSize(size).FontColor(color);
    }

    // ⚕ U+2695 → Symbols1 ; ✦ U+2726 + ★ U+2605 → Symbols2
    private static string FontFor(string symbol) =>
        symbol.Contains('⚕') ? SymbolFont1 : SymbolFont2;

    private static void IconLegend(TextDescriptor t, string icon, string label, string color)
    {
        SymbolSpan(t, icon, 7, color, FontFor(icon));
        t.Span(label).FontSize(7).FontColor(color);
    }
}