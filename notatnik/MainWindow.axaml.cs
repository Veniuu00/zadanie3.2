using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace notatnik;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Note> _notes = [];
    private readonly string _notesPath = Path.Combine(Directory.GetCurrentDirectory(), "notatki.md");
    private readonly NoteStore _noteStore;
    private bool _canSave = true;

    public MainWindow()
    {
        InitializeComponent();
        _noteStore = new NoteStore(_notesPath);
        NotesList.ItemsSource = _notes;
        LoadNotes();
    }

    private void LoadNotes()
    {
        try
        {
            foreach (var note in _noteStore.Load())
            {
                _notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            _canSave = false;
            SaveButton.IsEnabled = false;
            StatusText.Text = $"Nie udało się odczytać notatek: {ex.Message}";
        }
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_canSave)
        {
            return;
        }

        var title = TitleInput.Text?.Trim() ?? string.Empty;
        var content = ContentInput.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            StatusText.Text = "Podaj tytuł notatki.";
            return;
        }

        var note = new Note(title, content, DateTimeOffset.Now);
        var updatedNotes = _notes.Prepend(note).ToArray();

        try
        {
            _noteStore.Save(updatedNotes);
            _notes.Insert(0, note);
            TitleInput.Text = string.Empty;
            ContentInput.Text = string.Empty;
            StatusText.Text = "Zapisano.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Nie udało się zapisać: {ex.Message}";
        }
    }
}
