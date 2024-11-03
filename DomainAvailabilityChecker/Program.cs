using System.Collections.Immutable;
using CommandLine;
using DomainAvailabilityChecker;
using DnsClient;
using Polly;
using Polly.Extensions.Http;


Parser.Default.ParseArguments<CommandLineOptions>(args)
    .WithParsed<CommandLineOptions>(o =>
    {
        var inputPath = o.InputPath;
        var outputPath = o.OutputPath;
        List<string> words = GetWords(inputPath);
        var tlds = GetTLDs();
        var domains = words.SelectMany(w => GenerateDomains(w, tlds)).ToList();
        var completedQueries = domains.Select(domain => CheckAvailability(domain)).ToList();
        File.WriteAllText(outputPath, completedQueries.Where(x => x.result.HasError).Select(x => x.domain).Implode("\n"));
    });


List<string> GetWords(string path)
{
    var wordsRaw = File.ReadAllText(path);
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


(IDnsQueryResponse result, string domain) CheckAvailability(string domain)
{
    var policy = Policy<IDnsQueryResponse>.Handle<DnsResponseException>()
        .WaitAndRetryForever((_) => TimeSpan.FromSeconds(1));
    var result = policy.Execute( () =>
    {
        var lookup = new LookupClient();
        var _result = lookup.Query(domain, QueryType.NS);
        return _result;
    });
    return (result, domain);
}