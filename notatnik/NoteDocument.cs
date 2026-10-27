using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace notatnik;

public sealed record Note(string Title, string Content, DateTimeOffset CreatedAt)
{
    public string Header =>
        $"## {CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} — {Title}";
}

public static class NoteDocument
{
    private static readonly Regex HeaderPattern = new(
        @"^## (?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}) — (?<title>.*)$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public static string Serialize(IEnumerable<Note> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);

        return string.Join(
            "\n\n",
            notes.OrderByDescending(note => note.CreatedAt)
                .Select(note =>
                {
                    if (note.Title.Contains('\r') || note.Title.Contains('\n'))
                    {
                        throw new ArgumentException("Tytuł notatki musi mieścić się w jednym wierszu.", nameof(notes));
                    }

                    var timestamp = note.CreatedAt.ToLocalTime()
                        .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                    var content = note.Content.TrimEnd('\r', '\n');
                    return $"## {timestamp} — {note.Title}\n{content}";
                }));
    }

    public static IReadOnlyList<Note> Parse(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var normalized = document.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var headers = HeaderPattern.Matches(normalized);
        if (headers.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return [];
            }

            throw new FormatException("Plik notatek nie zawiera wpisów w oczekiwanym formacie.");
        }

        if (headers[0].Index != 0)
        {
            throw new FormatException("Plik notatek zawiera tekst przed pierwszym wpisem.");
        }

        var notes = new List<Note>(headers.Count);
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            if (!DateTimeOffset.TryParseExact(
                    header.Groups["timestamp"].Value,
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var createdAt))
            {
                throw new FormatException($"Nieprawidłowa data w nagłówku notatki: {header.Value}");
            }

            var contentStart = header.Index + header.Length;
            if (contentStart < normalized.Length && normalized[contentStart] == '\n')
            {
                contentStart++;
            }

            var contentEnd = index + 1 < headers.Count ? headers[index + 1].Index : normalized.Length;
            var content = normalized[contentStart..contentEnd].TrimEnd('\n');
            notes.Add(new Note(header.Groups["title"].Value, content, createdAt));
        }

        return notes;
    }
}
