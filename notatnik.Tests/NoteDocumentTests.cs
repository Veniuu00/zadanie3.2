using System;
using System.IO;
using System.Linq;
using System.Text;
using notatnik;

namespace notatnik.Tests;

public class NoteDocumentTests
{
    [Fact]
    public void SerializeAndParsePreservesFiveNotesInNewestFirstOrder()
    {
        var notes = Enumerable.Range(1, 5)
            .Select(index => new Note(
                index == 5 ? "Zażółć — finał" : $"Notatka {index}",
                index == 4 ? "Pierwsza linia\nDruga linia" : $"Treść {index}",
                DateTimeOffset.Now.AddMinutes(index)))
            .ToArray();

        var document = NoteDocument.Serialize(notes);
        var parsed = NoteDocument.Parse(document);

        Assert.Equal(5, parsed.Count);
        Assert.Equal(notes.OrderByDescending(note => note.CreatedAt).Select(note => note.Title), parsed.Select(note => note.Title));
        Assert.Contains("Zażółć — finał", document);
        Assert.Contains("Pierwsza linia\nDruga linia", document);
        Assert.Equal(document, NoteDocument.Serialize(parsed));
    }

    [Fact]
    public void ParseRejectsContentWithoutNoteHeaders()
    {
        Assert.Throws<FormatException>(() => NoteDocument.Parse("to nie jest wpis"));
    }

    [Fact]
    public void SerializeRejectsMultilineTitles()
    {
        var note = new Note("Pierwsza\nDruga", "Treść", DateTimeOffset.Now);

        Assert.Throws<ArgumentException>(() => NoteDocument.Serialize([note]));
    }

    [Fact]
    public void NoteStoreKeepsFiveUtf8NotesAfterEachSave()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"notatnik-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "notatki.md");
            var store = new NoteStore(path);
            var notes = new List<Note>();

            for (var index = 1; index <= 5; index++)
            {
                notes.Insert(0, new Note(
                    index == 5 ? "Zażółć — finał" : $"Notatka {index}",
                    index == 4 ? "Pierwsza linia\nDruga linia" : $"Treść {index}",
                    DateTimeOffset.Now.AddMinutes(index)));
                store.Save(notes);
            }

            var bytes = File.ReadAllBytes(path);
            var loadedNotes = store.Load();

            Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
            Assert.Equal(5, loadedNotes.Count);
            Assert.Equal(notes.Select(note => note.Title), loadedNotes.Select(note => note.Title));
            Assert.Contains("Zażółć — finał", Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
