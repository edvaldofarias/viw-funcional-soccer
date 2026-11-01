using Microsoft.EntityFrameworkCore;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence;

public sealed class ViwDbContext(DbContextOptions<ViwDbContext> options) : DbContext(options)
{
    public DbSet<Competition> CompetitionRecords => Set<Competition>();
    public DbSet<ChampionshipEdition> ChampionshipEditionRecords => Set<ChampionshipEdition>();
    public DbSet<EditionSource> EditionSourceRecords => Set<EditionSource>();
    public DbSet<RuleSet> RuleSetRecords => Set<RuleSet>();
    public DbSet<CompetitionPhase> CompetitionPhaseRecords => Set<CompetitionPhase>();
    public DbSet<CompetitionRound> CompetitionRoundRecords => Set<CompetitionRound>();
    public DbSet<TeamEntry> TeamEntryRecords => Set<TeamEntry>();
    public DbSet<Player> PlayerRecords => Set<Player>();
    public DbSet<SourcePlayer> SourcePlayerRecords => Set<SourcePlayer>();
    public DbSet<PlayerIdentityLink> PlayerIdentityLinkRecords => Set<PlayerIdentityLink>();
    public DbSet<PlayerRegistration> PlayerRegistrationRecords => Set<PlayerRegistration>();
    public DbSet<Match> MatchRecords => Set<Match>();
    public DbSet<PlayerMatchStatistics> PlayerMatchStatisticsRecords => Set<PlayerMatchStatistics>();
    public DbSet<MatchSourceEvent> MatchSourceEventRecords => Set<MatchSourceEvent>();
    public DbSet<Coach> CoachRecords => Set<Coach>();
    public DbSet<CoachAssignment> CoachAssignmentRecords => Set<CoachAssignment>();
    public DbSet<AwardDefinition> AwardDefinitionRecords => Set<AwardDefinition>();
    public DbSet<AwardGrant> AwardGrantRecords => Set<AwardGrant>();
    public DbSet<MediaAsset> MediaAssetRecords => Set<MediaAsset>();
    public DbSet<ImportRun> ImportRunRecords => Set<ImportRun>();
    public DbSet<SourceSnapshot> SourceSnapshotRecords => Set<SourceSnapshot>();
    public DbSet<ImportCheckpoint> ImportCheckpointRecords => Set<ImportCheckpoint>();
    public DbSet<ImportLease> ImportLeaseRecords => Set<ImportLease>();
    public DbSet<AuditEntry> AuditEntryRecords => Set<AuditEntry>();
    public DbSet<OutboxMessage> OutboxMessageRecords => Set<OutboxMessage>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Textos curtos usam Unicode; as configurações individuais podem definir exceções.
        configurationBuilder.Properties<string>().AreUnicode().HaveMaxLength(100);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        // Preserva instante e offset; normalização UTC continua nos casos de uso.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("datetimeoffset");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("viw");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ViwDbContext).Assembly);
    }
}
