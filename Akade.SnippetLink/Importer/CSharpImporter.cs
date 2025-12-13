using Akade.SnippetLink.Formatter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Akade.SnippetLink.Importer;

internal sealed class CSharpImporter(IFileSystem fileSystem) : SnippetImporter
{
    private readonly Dictionary<string, (string content, SyntaxTree tree)> _parseCacheBySourceFile = [];

    internal override string Name => "cs";

    internal override Type PreferredFormatter => typeof(CodeBlockFormatter);

    internal override Result CanImport(string sourceFile, string name, Options options)
    {
        if (!sourceFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return new Result.Failure($"'{sourceFile}' not end with .cs");

        if (!fileSystem.FileExists(sourceFile))
            return new Result.Failure($"Source file '{sourceFile}' not found.");

        return new Result.Success();
    }

    internal override async Task<Result<Snippet>> ImportSnippetAsync(string sourceFile, string name, Options options)
    {
        bool bodyOnly = options.Get("body-only", false);
        string sourceText;
        SyntaxTree tree;

        if (_parseCacheBySourceFile.TryGetValue(sourceFile, out var parseResult))
        {
            (sourceText, tree) = parseResult;
        }
        else
        {
            sourceText = await fileSystem.ReadAllTextAsync(sourceFile);
            tree = CSharpSyntaxTree.ParseText(sourceText);
            _parseCacheBySourceFile[sourceFile] = (sourceText, tree);
        }

        var root = tree.GetRoot();

        SnippetFinder finder = new(sourceText, name, bodyOnly);
        finder.Visit(root);

        return (finder.Kind, finder.Start, finder.End) switch
        {
            (_, >= 0, <= 0) => new Result.Failure($"Snippet '{name}' is missing its closing comment or region in '{sourceFile}'."),
            (_, >= 0, >= 0) => GetSuccessfulSnippet(),

            _ => new Result.Failure($"Snippet '{name}' not found in file '{sourceFile}'."),
        };

        Result<Snippet> GetSuccessfulSnippet()
        {
            var snippetSpan = TextSpan.FromBounds(finder.Start, finder.End);
            var snippetText = sourceText.AsSpan()[snippetSpan.Start..snippetSpan.End];

            // remove any leading or trailing newlines
            snippetText = snippetText.TrimStart(['\r', '\n']);
            snippetText = snippetText.TrimEnd(['\r', '\n']);

            StringBuilder content = new(snippetSpan.Length);

            foreach (ReadOnlySpan<char> rawLine in snippetText.EnumerateLines())
            {
                int currentIndentation = rawLine.IndexOfAnyExcept([' ', '\t']);
                int trim = currentIndentation > 0 ? Math.Min(finder.IndentationLevel, currentIndentation) : 0;
                content.Append(rawLine[trim..]);
                content.AppendLine();
            }
            content.Length -= Environment.NewLine.Length; // Remove last newline
            FileLinePositionSpan lineSpan = tree.GetLineSpan(snippetSpan);
            return new Snippet(
                SourceFile: sourceFile,
                Name: name,
                StartLine: lineSpan.StartLinePosition.Line,
                EndLine: lineSpan.EndLinePosition.Line,
                Content: content.ToString(),
                Language: "cs"
            );
        }
        ;
    }

    private class SnippetFinder(string sourceText, string snippetName, bool bodyOnly) : CSharpSyntaxWalker(SyntaxWalkerDepth.StructuredTrivia)
    {
        public int IndentationLevel { get; private set; } = -1;
        public int Start { get; private set; } = -1;
        public int End { get; private set; } = -1;
        public SnippetKind Kind { get; private set; } = SnippetKind.None;

        private int _nestingLevel = 0;

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            if (End != -1)
                return;

            ReadOnlySpan<char> text = sourceText.AsSpan()[trivia.SpanStart..trivia.Span.End];
            switch (trivia.Kind(), Kind)
            {
                case (SyntaxKind.SingleLineCommentTrivia, SnippetKind.None or SnippetKind.Comment)
                    when text.StartsWith("// begin-snippet: ", StringComparison.OrdinalIgnoreCase):
                    {
                        SetStart(trivia, SnippetKind.Comment, text["// begin-snippet: ".Length..]);
                    }
                    break;
                case (SyntaxKind.SingleLineCommentTrivia, SnippetKind.Comment)
                    when text.StartsWith("// end-snippet", StringComparison.OrdinalIgnoreCase):
                    {
                        SetEnd(trivia.Span);
                    }
                    break;
                case (SyntaxKind.RegionDirectiveTrivia, SnippetKind.None or SnippetKind.Region):
                    {
                        SetStart(trivia, SnippetKind.Region, text["#region ".Length..]);
                    }
                    break;
                case (SyntaxKind.EndRegionDirectiveTrivia, SnippetKind.Region):
                    {
                        SetEnd(trivia.Span);
                    }
                    break;

            }

            base.VisitTrivia(trivia);
        }

        public override void Visit(SyntaxNode? node)
        {
            SyntaxToken identifier = GetIdentifier(node);

            if (!identifier.IsKind(SyntaxKind.None))
            {
                Debug.Assert(node != null);
                ReadOnlySpan<char> nameSpan = sourceText.AsSpan()[identifier.SpanStart..identifier.Span.End];

                if (snippetName.EndsWith(nameSpan, StringComparison.OrdinalIgnoreCase)
                   && FullSymbolNameMatch(node, sourceText, snippetName))
                {
                    if (bodyOnly && TryGetBody(node, out SyntaxNode? startNode, out int start, out int end))
                    {
                        Start = start;
                        End = end;
                    }
                    else
                    {
                        TextSpan nodeSpan = node.FullSpan;
                        Start = nodeSpan.Start;
                        End = nodeSpan.End;
                        startNode = node;
                    }

                    IndentationLevel = startNode.SyntaxTree.GetLocation(startNode.Span).GetLineSpan().StartLinePosition.Character;
                    Kind = SnippetKind.Symbol;
                }
            }

            base.Visit(node);
        }

        private static bool TryGetBody(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? startNode, out int start, out int end)
        {
            IEnumerable<SyntaxNode> childNodes = node switch
            {
                BaseMethodDeclarationSyntax methodLike => methodLike.Body?.ChildNodes() ?? methodLike.ExpressionBody?.ChildNodes() ?? [],
                PropertyDeclarationSyntax propertyDecl => propertyDecl.AccessorList?.ChildNodes() ?? propertyDecl.ExpressionBody?.ChildNodes() ?? [],
                BaseTypeDeclarationSyntax baseTypeDecl => baseTypeDecl.ChildNodes(),
                _ => []
            };

            if (childNodes.Any())
            {
                startNode = childNodes.First();
                start = startNode.FullSpan.Start;
                end = childNodes.Last().FullSpan.End;
                return true;
            }

            startNode = null;
            start = 0;
            end = 0;
            return false;
        }

        private static SyntaxToken GetIdentifier(SyntaxNode? node)
        {
            return node switch
            {
                ConstructorDeclarationSyntax ctorDecl => ctorDecl.Identifier,
                MethodDeclarationSyntax methodDecl => methodDecl.Identifier,
                PropertyDeclarationSyntax propDecl => propDecl.Identifier,
                BaseTypeDeclarationSyntax typeDecl => typeDecl.Identifier,
                DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Identifier,
                _ => default
            };
        }

        private static bool FullSymbolNameMatch(SyntaxNode node, ReadOnlySpan<char> sourceText, ReadOnlySpan<char> snippetName)
        {
            int lastSegmentStart;
            ReadOnlySpan<char> remaining = snippetName;
            SyntaxNode? currentNode = node;
            while (!remaining.IsEmpty && currentNode != null)
            {
                lastSegmentStart = remaining.LastIndexOf('.');
                ReadOnlySpan<char> currentSegment = remaining[(lastSegmentStart + 1)..];
                remaining = lastSegmentStart > 0 ? remaining[..lastSegmentStart] : [];

                SyntaxToken identifier = GetIdentifier(currentNode);
                if (identifier.IsKind(SyntaxKind.None))
                    return false;

                ReadOnlySpan<char> nodeNameSpan = sourceText[identifier.SpanStart..identifier.Span.End];
                if (!currentSegment.Equals(nodeNameSpan, StringComparison.OrdinalIgnoreCase))
                    return false;

                currentNode = currentNode.Parent;
            }

            return remaining.IsEmpty;
        }

        private void SetEnd(TextSpan span)
        {
            _nestingLevel--;
            if (_nestingLevel == 0)
                End = span.Start;
        }

        private void SetStart(SyntaxTrivia trivia, SnippetKind snippetKind, ReadOnlySpan<char> name)
        {
            if (Kind == SnippetKind.None && name.StartsWith(snippetName, StringComparison.OrdinalIgnoreCase))
            {
                Start = trivia.Span.End;
                IndentationLevel = trivia.GetLocation().GetLineSpan().StartLinePosition.Character;
                Kind = snippetKind;
            }

            if (Kind != SnippetKind.None)
            {
                _nestingLevel++;
            }
        }
    }

    private enum SnippetKind
    {
        None,
        Region,
        Comment,
        Symbol
    }
}
