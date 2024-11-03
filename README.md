# DomainSharp

CLI tool that, given a dictionary of words, checks which ones are legal domains and available.

# How it works

Initially, the tools figures out which ones of the words are legal domains. For example, "victim" would be a legal domain since it matches with "vict.im". <br>
Then, it filters the computed domains that are available for registration via an [NS DNS query](https://www.cloudflare.com/learning/dns/dns-records/dns-ns-record/) and outputs them to a file.

The tool implements transient error handling through [Polly](https://github.com/App-vNext/Polly), which makes it suitable for flaky internet connections and long runs with hundreds of thousands of words.

# Usage

```
head -n50 <(curl -s https://raw.githubusercontent.com/somegenericdev/DomainSharp/refs/heads/main/DomainSharp/words.txt) > words.txt
domainsharp -i words.txt -o output.txt
```