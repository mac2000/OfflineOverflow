using System.Diagnostics;
using System.Xml.Serialization;
using Elasticsearch.Net;
using Nest;
using SharpCompress.Archives.SevenZip;

var tags = new[] {"c#", "sql-server", "sql", ".net", "kubernetes", "powershell", "nuget", "tsql", ".net-core", "docker", "go", "bash", "cert-manager", "elasticsearch", "nuget-package", "nginx", "asp.net-core", "kubectl", "curl", "linux", "shell", "asp.net", "sqlite", "entity-framework", "entity-framework-core", "stored-procedures", "kubernetes-helm", "kubernetes-ingress", "lets-encrypt", "unix", "spring", "node.js", "regex", "terminal", "apple-m1", "azure-aks", "ado.net", "clean-architecture", "docker-compose" };
// var path = "apple.stackexchange.com.7z";
var path = "Stackoverflow.com-Posts.7z";
var index = "posts";
var questions = 0;
var answers = 0;
var errors = 0;
var ids = new List<int>();
var client = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).BasicAuthentication("elastic", "S3cr3tPAssw0rd").ServerCertificateValidationCallback(CertificateValidations.AllowAll).RequestTimeout(TimeSpan.FromMinutes(10)));

client.Indices.Delete(index);
client.Indices.Create(index, i => i.Settings(s => s.NumberOfShards(3).NumberOfReplicas(0).RefreshInterval("-1")));

var timer = Stopwatch.StartNew();
Parallel.ForEach(Reader.Read(path, tags).Where(row => row.PostTypeId == 1).Chunk(1000), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount}, batch =>
{
    var res = client.Bulk(s => s.UpdateMany(batch, (b, row) => b
        .Id(row.Id)
        .Doc(row)
        .Index(index)
        .DocAsUpsert()));
    if (res.Errors)
    {
        errors += 1;
    }
    questions += batch.Length;
    ids.AddRange(batch.Select(row => row.Id));
});
Console.WriteLine($"done indexing {questions} questions with {errors} errors in {timer.Elapsed}");

timer = Stopwatch.StartNew();
Parallel.ForEach(Reader.Read(path, tags).Where(row => row.PostTypeId == 2 && ids.Contains(row.ParentId)).Chunk(1000), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount}, batch =>
{
    var res = client.Bulk(s => s.UpdateMany(batch, (b, row) => b
        .Id(row.Id)
        .Doc(row)
        .Index(index)
        .DocAsUpsert()));
    if (res.Errors)
    {
        errors += 1;
    }
    answers += batch.Length;
    ids.AddRange(batch.Select(row => row.Id));
});
Console.WriteLine($"done indexing {answers} answers with {errors} errors in {timer.Elapsed}");

client.Indices.UpdateSettings(index, u => u.IndexSettings(i => i.RefreshInterval("30s")));


internal static class Reader
{
    public static IEnumerable<Row> Read(string path, string[] tags)
    {
        var serializer = new XmlSerializer(typeof(Row), new XmlRootAttribute
        {
            ElementName = "row",
            IsNullable = true
        });
        using var archive = SevenZipArchive.Open(path);
        var entry = archive.Entries.First(e => e.Key == "Posts.xml");
        using var reader = System.Xml.XmlReader.Create(entry.OpenEntryStream());
        reader.ReadToDescendant("posts");
        reader.ReadToDescendant("row");
        do
        {
            var deserialized = serializer.Deserialize(reader);
            if (deserialized is Row row)
            {
                if (row.Score < 0)
                {
                    continue;
                }
                
                if (row.PostTypeId != 1 && row.PostTypeId != 2)
                {
                    continue;
                }

                if (row.PostTypeId == 1 && row.AnswerCount == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(row.Tags))
                {
                    row.Tags = row.Tags.Replace("><", ", ").TrimStart('<').TrimEnd('>');
                }
                
                if (row.PostTypeId == 1 && !string.IsNullOrEmpty(row.Tags) && !tags.Any(t => row.Tags.Contains(t)))
                {
                    continue;
                }
                
                yield return row;
            }
        } while (reader.ReadToNextSibling("row"));
    }
}


[Serializable]
public record Row
{
    [XmlAttribute] public int Id { get; set; }
    [XmlAttribute] public int PostTypeId { get; set; }
    [XmlAttribute] public int AcceptedAnswerId { get; set; }
    [XmlAttribute] public DateTime CreationDate { get; set; }
    [XmlAttribute] public int Score { get; set; }
    [XmlAttribute] public int ViewCount { get; set; }
    [XmlAttribute] public string? Body { get; set; }
    [XmlAttribute] public int OwnerUserId { get; set; }
    [XmlAttribute] public int LastEditorUserId { get; set; }
    [XmlAttribute] public DateTime LastEditDate { get; set; }
    [XmlAttribute] public DateTime LastActivityDate { get; set; }
    [XmlAttribute] public string? Title { get; set; }
    [XmlAttribute] public string? Tags { get; set; }
    [XmlAttribute] public int AnswerCount { get; set; }
    [XmlAttribute] public int CommentCount { get; set; }
    [XmlAttribute] public string? ContentLicense { get; set; }
    [XmlAttribute] public int ParentId { get; set; }
}
