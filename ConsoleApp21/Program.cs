// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using DnsClient;
using Polly;
using Polly.Extensions.Http;

// List<string> words = File.ReadAllText("/home/ubuntu/RiderProjects/ConsoleApp21/ConsoleApp21/commands.txt").Split("\n").ToList();
List<string> words = GetWords();
var tlds = GetTLDs();
var domains = words.SelectMany(w => GenerateDomains(w, tlds)).ToList();
Console.WriteLine(domains.Count());
var completedQueries = domains.Select(domain => { return CheckAvailability(domain); }).ToList();


File.WriteAllText("/home/ubuntu/RiderProjects/ConsoleApp21/ConsoleApp21/res.txt",
    string.Join("\n", completedQueries.Where(x => x.result.HasError).Select(x => x.domain).ToArray()));


List<string> GetWords()
{
    var wordsRaw = File.ReadAllText("/home/ubuntu/RiderProjects/ConsoleApp21/ConsoleApp21/360k.txt");

    // var rgx = new Regex("(?:[\\d]+[\\s]+)([a-z\\-]+)");
    // var matches= rgx.Matches(wordsRaw);
    // return matches.Select(x => x.Groups[1].Value).ToList();
    return wordsRaw.Split("\n").ToList();
}


List<string> GetTLDs()
{
    var policy = HttpPolicyExtensions
        .HandleTransientHttpError().Or<TaskCanceledException>().Or<TimeoutException>()
        .WaitAndRetry(50, (_) => TimeSpan.FromSeconds(5));
    var response = policy.Execute(() =>
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(100);
        var webRequest = new HttpRequestMessage(HttpMethod.Get, "http://data.iana.org/TLD/tlds-alpha-by-domain.txt");
        var response = client.Send(webRequest);
        return response;
    });

    using var reader = new StreamReader(response.Content.ReadAsStream());
    var splitted = reader.ReadToEnd();
    return splitted.Split("\n").Where(x => !string.IsNullOrEmpty(x)).Where(x => x[0] != '#').Where(x => x.Length < 5)
        .Select(x => x.ToLower())
        .ToList();
}


List<string> GenerateDomains(string word, List<string> TLDs)
{
    var TLDsPapabili = TLDs.Where(x => word.Length > x.Length).ToImmutableList();
    int lengthStart = 2;
    return GenerateDomainsRec(word, TLDsPapabili, lengthStart, word.Length - 1, new List<string>().ToImmutableList());
}


List<string> GenerateDomainsRec(string word, ImmutableList<string> TLDs, int length, int maxLength,
    ImmutableList<string> domainsAccumulator)
{
    if (length > maxLength)
    {
        return domainsAccumulator.ToList();
    }

    var currentTLDs = TLDs.Where(x => x.Length == length).ToImmutableList();
    var matchingTLDs = currentTLDs.Where(x => x == word.Substring(word.Length - length)).ToImmutableList();
    var domains = matchingTLDs.Select(tld => word.Substring(0, word.Length - length) + $".{tld}").ToImmutableList();
    return GenerateDomainsRec(word, TLDs, length + 1, maxLength,
        domainsAccumulator.AddRange(domains).ToImmutableList());
}


(IDnsQueryResponse result, string domain) CheckAvailability(string _domain)
{
    var policy = Policy<IDnsQueryResponse>.Handle<DnsResponseException>()
        .WaitAndRetryForever((_) => TimeSpan.FromSeconds(1));
    var result = policy.Execute( () =>
    {
        var lookup = new LookupClient();
        var _result = lookup.Query(_domain, QueryType.NS);
        return _result;
    });
    return (result, _domain);
}