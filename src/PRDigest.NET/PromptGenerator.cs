using System.Runtime.CompilerServices;

namespace PRDigest.NET;

internal static class PromptGenerator
{
    private const int MaxFileCount = 30;

    public const string SystemPrompt = """
        あなたは.NET開発者向けのPull Request要約アシスタントです。
        以下の形式で要約を出力してください：

        =================================
        出力形式:
        
        #### 概要
        1行から5行ぐらいで簡潔に記述してください。
        またサンプルコードなどもあれば記載してください。
        
        #### 変更内容
        変更されたファイルと主な変更内容をリストアップしてください。
        
        #### パフォーマンスへの影響
        パフォーマンスに関連する変更があれば具体的に記載してください。（なければ"影響なし"）
        改善点や懸念点を明記してください。
        
        #### 関連Issue
        関連するIssueあれば記載してください。（なければ"なし"）
        
        #### その他
        それ以外に記載した方が良い特記事項があれば記載してください。（なければ"なし"）

        #### サンプルコードを記載時の注意点
        C#のコードブロックを使用してください：
        ```csharp
        // ソースコードを記載
        ```
        =================================

        .NET開発者にとって有益な情報を含める形で、最大1000文字までで要約してください。
        タイトルは不要です。markdown形式で出力してください。

        【追加の詳細ガイドライン】
        要約を作成する際は、以下の点に特に注意を払ってください：

        1. **コード変更の技術的影響**
           - 変更がランタイム、コンパイラ、ライブラリのどの部分に影響するか明記
           - API の変更がある場合は、公開APIか内部実装かを区別
           - 互換性への影響（破壊的変更、非推奨化など）を明確に記載

        2. **パフォーマンスに関する分析**
           - メモリ使用量、実行速度、スループットへの影響を具体的に記載
           - ベンチマーク結果や計測値がある場合は必ず含める
           - パフォーマンス改善の場合は、改善率や具体的な数値を記載

        3. **セキュリティとバグ修正**
           - セキュリティ上の脆弱性修正の場合は、その重要度を明記
           - バグ修正の場合、修正前の問題の再現条件と修正後の動作を対比
           - CVE番号などのセキュリティ識別子がある場合は記載
        """;

    public static string GeneratePrompt(PullRequestInfo info)
    {
        // pull reqeust info
        var body = info.PullRequest.Body;
        if (string.IsNullOrWhiteSpace(body))
        {
            body = "なし";
        }

        // file changes info
        var files = info.Files;
        var filesChanged = string.Join(Environment.NewLine, files
            .Take(MaxFileCount)
            .Select(f => $"- {f.FileName} (+{f.Additions}/-{f.Deletions}, total: {f.Changes})")
            .Append(files.Count > MaxFileCount ? $"- その他 {files.Count - MaxFileCount} files" : ""));

        // reviewer info
        var reviews = info.Reviews;
        var reviewersBuilder = new DefaultInterpolatedStringHandler(0, 0);
        for (int i = 0; i < reviews.Count; i++)
        {
            var pullRequestReview = reviews[i];
            reviewersBuilder.AppendLiteral(pullRequestReview.User.Login);
            if (i < reviews.Count - 1)
            {
                reviewersBuilder.AppendLiteral(", ");
            }
        }

        // latest copilot overview
        var copilotOverview = info.Reviews.Where(r => r.User.Login == "copilot-pull-request-reviewer[bot]")
            .OrderByDescending(r => r.SubmittedAt)
            .FirstOrDefault();
        var overviewText = copilotOverview != null ? copilotOverview.Body.Trim() : "";
        if (string.IsNullOrWhiteSpace(overviewText))
        {
            overviewText = "なし";
        }

        var prompt = $"""
以下のdotnet/runtimeのPull Requestを要約してください。
またできる限り、以下の情報以外の内容を推測して含めないようにしてください。

Pull Request:
- {info.PullRequest.Title} #{info.PullRequest.Number}
- 作成者: {info.PullRequest.User.Login}
- レビュワー: {reviewersBuilder.ToStringAndClear()}

作成者による概要:
{body}

Copilotによる概要:
{overviewText}

変更ファイル:
{filesChanged}

""";
        return prompt;
    }
}