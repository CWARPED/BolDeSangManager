using BolDeSangManager.Data;
using BolDeSangManager.Data.Enums;
using BolDeSangManager.Data.Seeding;
using BolDeSangManager.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BolDeSangManager.Tests;

public class DbSeederTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public void SkillSeedData_ContientAuMoins85SkillsParJeu()
    {
        var skillsBB = SkillSeedData.GetSkills(1, GameType.BloodBowl).ToList();
        var skillsDB = SkillSeedData.GetSkills(2, GameType.DungeonBowl).ToList();
        Assert.True(skillsBB.Count >= 85, $"BB : Attendu ≥ 85 skills, obtenu {skillsBB.Count}");
        Assert.True(skillsDB.Count >= 85, $"DB : Attendu ≥ 85 skills, obtenu {skillsDB.Count}");
    }

    [Fact]
    public void SkillSeedData_ContientLesQuatreSkillsDungeonBowl()
    {
        var skills = SkillSeedData.GetSkills(2, GameType.DungeonBowl).ToList();
        Assert.Contains(skills, s => s.Nom == "Navigateur de Portail");
        Assert.Contains(skills, s => s.Nom == "Transmission dans la Course");
        Assert.Contains(skills, s => s.Nom == "Passe par un Portail");
        Assert.Contains(skills, s => s.Nom == "Lancer contre un Mur");
    }

    [Fact]
    public void SkillSeedData_BloodBowlNeContientPasLesSkillsDungeonBowl()
    {
        var skills = SkillSeedData.GetSkills(1, GameType.BloodBowl).ToList();
        Assert.DoesNotContain(skills, s => s.Nom == "Navigateur de Portail");
        Assert.DoesNotContain(skills, s => s.Nom == "Transmission dans la Course");
        Assert.DoesNotContain(skills, s => s.Nom == "Passe par un Portail");
        Assert.DoesNotContain(skills, s => s.Nom == "Lancer contre un Mur");
    }

    [Fact]
    public void BloodBowlTeamSeedData_Contient30Equipes()
    {
        var teams = BloodBowlTeamSeedData.GetTeams(1, 1).ToList();
        Assert.Equal(30, teams.Count);
    }

    [Fact]
    public void DungeonBowlTeamSeedData_ContientHuitColleges()
    {
        var colleges = DungeonBowlTeamSeedData.GetColleges(1, 1).ToList();
        Assert.Equal(8, colleges.Count);
    }

    [Fact]
    public void BloodBowlTeamSeedData_ToutesLesEquipesOntAuMoinsUnPoste()
    {
        var teams = BloodBowlTeamSeedData.GetTeams(1, 1).ToList();
        Assert.All(teams, t => Assert.NotEmpty(t.Positions));
    }

    [Fact]
    public void DungeonBowlTeamSeedData_ToutesLesEquipesOntCoutRelance50k()
    {
        var colleges = DungeonBowlTeamSeedData.GetColleges(1, 1).ToList();
        Assert.All(colleges, t => Assert.Equal(50_000, t.Type.CoutRelance));
    }

    [Fact]
    public void BloodBowlTeamSeedData_ChaqueEquipeAUneCategorie()
    {
        var teams = BloodBowlTeamSeedData.GetTeams(1, 1).ToList();
        Assert.All(teams, t => Assert.True(Enum.IsDefined(typeof(TeamCategory), t.Type.Categorie)));
    }
}
