using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace notatnik;

public sealed class NoteStore
{
    private readonly string _path;

    public NoteStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public IReadOnlyList<Note> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        return NoteDocument.Parse(File.ReadAllText(_path, new UTF8Encoding(false, true)));
    }

    public void Save(IEnumerable<Note> notes)
    {
        var temporaryPath = Path.Combine(
            Path.GetDirectoryName(_path)!,
            $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, NoteDocument.Serialize(notes), new UTF8Encoding(false));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
