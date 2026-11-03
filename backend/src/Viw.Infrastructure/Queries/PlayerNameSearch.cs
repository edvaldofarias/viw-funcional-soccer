using System.Globalization;
using System.Text;
using Fastenshtein;
using Microsoft.EntityFrameworkCore;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Queries;

public static class PlayerNameSearch
{
    public sealed record Candidate(Guid Id, string? Name, string? OriginalName = null, string? Nickname = null);
    public sealed record Match(Guid Id, string Name, int Score);

    public static async Task<Match[]> FindPlayersAsync(ViwDbContext db, IQueryable<Player> players, string search, CancellationToken ct)
    {
        var names = await players.Select(x => new Candidate(x.Id, x.DisplayName, null, null)).ToArrayAsync(ct);
        if (names.Length == 0) return [];
        var aliases = await (from player in players
            join link in db.PlayerIdentityLinkRecords.AsNoTracking() on player.Id equals link.PlayerId
            join source in db.SourcePlayerRecords.AsNoTracking() on link.SourcePlayerId equals source.Id
            where link.UnlinkedAt == null
            select new Candidate(player.Id, player.DisplayName, source.OriginalName, source.Nickname)).ToArrayAsync(ct);
        return Rank(names.Concat(aliases), search, ct);
    }

    public static Match[] Rank(IEnumerable<Candidate> candidates, string? search, CancellationToken ct = default)
    {
        var scorer = new NameScorer(search);
        var matches = new Dictionary<Guid, Match>();
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var score = Math.Max(scorer.Score(candidate.Name),
                Math.Max(scorer.Score(candidate.OriginalName), scorer.Score(candidate.Nickname)));
            if (score > 0 && (!matches.TryGetValue(candidate.Id, out var existing) || score > existing.Score))
                matches[candidate.Id] = new(candidate.Id, candidate.Name ?? "", score);
        }
        return matches.Values.OrderByDescending(x => x.Score)
            .ThenBy(x => Normalize(x.Name), StringComparer.Ordinal).ThenBy(x => x.Id).ToArray();
    }

    private static string Normalize(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark) continue;
            if (char.IsLetterOrDigit(character)) result.Append(char.ToUpperInvariant(character));
            else if (result.Length > 0 && result[^1] != ' ') result.Append(' ');
        }
        return result.ToString().TrimEnd();
    }

    private sealed class NameScorer
    {
        private readonly bool empty;
        private readonly string query;
        private readonly Term[] terms;
        private readonly Dictionary<string, int> scores = new(StringComparer.Ordinal);

        public NameScorer(string? search)
        {
            empty = string.IsNullOrWhiteSpace(search);
            query = Normalize(search ?? "");
            var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            var meaningful = tokens.Where(x => x is not ("DA" or "DE" or "DO" or "DAS" or "DOS")).ToArray();
            terms = (meaningful.Length == 0 ? tokens : meaningful).Select(x => new Term(x)).ToArray();
        }

        public int Score(string? name)
        {
            if (empty) return 1;
            if (string.IsNullOrEmpty(name) || terms.Length == 0) return 0;
            if (scores.TryGetValue(name, out var cached)) return cached;
            var normalized = Normalize(name);
            var score = ScoreNormalized(normalized);
            scores[name] = score;
            return score;
        }

        private int ScoreNormalized(string name)
        {
            if (name == query) return 10000;
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var exact = 0; var initials = 0; var fuzzy = 0; var penalty = 0;
            foreach (var term in terms)
            {
                if (term.Value.Length == 1)
                {
                    if (words.Any(x => x.StartsWith(term.Value, StringComparison.Ordinal))) initials++;
                }
                else if (name.Contains(term.Value, StringComparison.Ordinal)) exact++;
                else if (words.Any(x => x.Length == 1 && x[0] == term.Value[0])) initials++;
                else if (term.Distance is not null)
                {
                    var distance = term.MaximumDistance + 1;
                    foreach (var word in words)
                    {
                        if (word.Length < 3 || Math.Abs(word.Length - term.Value.Length) > term.MaximumDistance) continue;
                        distance = Math.Min(distance, term.Distance.DistanceFrom(word));
                        if (distance == 1) break;
                    }
                    if (distance <= term.MaximumDistance) { fuzzy++; penalty += distance; }
                }
            }
            var matched = exact + initials + fuzzy;
            if (matched == 0) return 0;
            if (exact == terms.Length) return 9000;
            var tier = exact > 0 ? 6000 : initials > 0 ? 4000 : 2000;
            return tier + matched * 100 + exact * 10 - penalty * 10 + (matched == terms.Length ? 500 : 0);
        }
    }

    private sealed class Term(string value)
    {
        public string Value { get; } = value;
        public int MaximumDistance { get; } = value.Length >= 6 ? 2 : 1;
        // Each request owns its comparers: DistanceFrom reuses a mutable buffer.
        public Levenshtein? Distance { get; } = value.Length >= 3 && value.All(char.IsLetter) ? new(value) : null;
    }
}
