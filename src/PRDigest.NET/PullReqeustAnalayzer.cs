using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Runtime.InteropServices;

namespace PRDigest.NET;

internal static class PullReqeustAnalayzer
{
    public static AnalayzerResult Analayze(MarkdownDocument document)
    {
        var tableOfContents = false;
        var prCount = 0;
        HeadingBlock? nextPrNumber = null;
        HashSet<string>? prNumberTable = null;
        Dictionary<string, List<HeadingBlock>>? labelTable = new();

        foreach (var block in document)
        {
            if (tableOfContents && block is HeadingBlock headingBlock)
            {
                var link = headingBlock.Inline?.Descendants<LinkInline>().FirstOrDefault()?.FirstChild;
                if (prNumberTable!.TryGetValue(((link as LiteralInline)?.Content.ToString() ?? "Notfound"), out var prNumber))
                {
                    nextPrNumber = headingBlock;
                }
            }
            else if (block is ListBlock listBlock)
            {
                if (tableOfContents && nextPrNumber is not null)
                {
                    var labelBlock = listBlock.Descendants<ListItemBlock>().Skip(3).FirstOrDefault();
                    var labels = labelBlock?.Descendants<LiteralInline>()
                            .Where(l =>
                            {
                                var labelText = l.Content.ToString();
                                return !string.IsNullOrWhiteSpace(labelText) && !labelText.Contains("ラベル");
                            });

                    foreach (var label in labels ?? [])
                    {
                        ref var prList = ref CollectionsMarshal.GetValueRefOrAddDefault(labelTable, label.ToString(), out var _);
                        prList ??= new();
                        prList.Add(nextPrNumber);
                    }

                    nextPrNumber = null;
                }
                else if (!tableOfContents)
                {
                    foreach (var listItemBlock in listBlock.Descendants<ListItemBlock>())
                    {
                        prCount++;
                        var prNumber = listItemBlock.Descendants<LinkInline>().FirstOrDefault();
                        if (prNumber is not null)
                        {
                            prNumberTable ??= new HashSet<string>();
                            prNumberTable.Add(prNumber?.Url?.Trim() ?? "");
                        }
                    }
                    tableOfContents = true;
                }
            }
        }


        return new AnalayzerResult
        {
            PullRequestCount = prCount,
            LabelInfo = labelTable
        };
    }

    public ref struct AnalayzerResult
    {
        public int PullRequestCount;
        public Dictionary<string, List<HeadingBlock>>? LabelInfo;
        public readonly int LabelCount => LabelInfo?.Count ?? 0;
    }
}