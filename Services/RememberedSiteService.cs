using System;
using System.Collections.Generic;
using System.Linq;
using OpenWithTool.Models;

namespace OpenWithTool.Services;

public interface IRememberedSiteService
{
    List<SiteMatchOption> BuildMatchOptions(string url);
    RememberedSiteRule? FindMatchingRule(IEnumerable<RememberedSiteRule> rules, string url);
}

public class RememberedSiteService : IRememberedSiteService
{
    public List<SiteMatchOption> BuildMatchOptions(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return new List<SiteMatchOption> { new() { MatchType = SiteMatchType.ExactUrl, DisplayName = "Exact URL", Pattern = url } };

        var root = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        var options = new List<SiteMatchOption>
        {
            new() { MatchType = SiteMatchType.ExactUrl, DisplayName = "Exact URL", Pattern = uri.AbsoluteUri },
            new() { MatchType = SiteMatchType.Domain, DisplayName = "All under domain", Pattern = $"{root}/*" }
        };

        var firstSegment = uri.Segments.Skip(1).FirstOrDefault()?.Trim('/');
        if (!string.IsNullOrWhiteSpace(firstSegment))
        {
            options.Add(new SiteMatchOption
            {
                MatchType = SiteMatchType.Path,
                DisplayName = "All under path",
                Pattern = $"{root}/{firstSegment}/*"
            });
        }

        return options;
    }

    public RememberedSiteRule? FindMatchingRule(IEnumerable<RememberedSiteRule> rules, string url)
    {
        var orderedRules = rules.OrderByDescending(r => r.MatchType == SiteMatchType.ExactUrl ? 3 : r.MatchType == SiteMatchType.Path ? 2 : 1);
        return orderedRules.FirstOrDefault(rule => IsMatch(rule, url));
    }

    private static bool IsMatch(RememberedSiteRule rule, string url)
    {
        if (rule.MatchType == SiteMatchType.ExactUrl || !rule.Pattern.EndsWith("*", StringComparison.Ordinal))
            return string.Equals(rule.Pattern, url, StringComparison.OrdinalIgnoreCase);

        var prefix = rule.Pattern[..^1];
        return url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
