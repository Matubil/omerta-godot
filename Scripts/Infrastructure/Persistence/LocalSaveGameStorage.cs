using System;
using System.IO;

namespace Omerta.Infrastructure.Persistence;

public sealed class LocalSaveGameStorage
{
    public const string DefaultSlotFileName = "save_0.json";

    public LocalSaveGameStorage(string rootDirectory, string slotFileName = DefaultSlotFileName)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A save root directory is required.", nameof(rootDirectory));
        }

        if (string.IsNullOrWhiteSpace(slotFileName))
        {
            throw new ArgumentException("A save slot file name is required.", nameof(slotFileName));
        }

        FilePath = Path.Combine(rootDirectory, slotFileName);
        TemporaryFilePath = FilePath + ".tmp";
    }

    public string FilePath { get; }

    public string TemporaryFilePath { get; }

    public bool Exists => File.Exists(FilePath);

    public long FileSize => Exists ? new FileInfo(FilePath).Length : 0L;

    public void Write(string contents)
    {
        if (string.IsNullOrWhiteSpace(contents))
        {
            throw new InvalidDataException("Cannot write an empty save file.");
        }

        string? directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            File.WriteAllText(TemporaryFilePath, contents);
            File.Move(TemporaryFilePath, FilePath, true);
            if (!Exists || FileSize == 0L)
            {
                throw new IOException($"The save file was not created correctly at '{FilePath}'.");
            }
        }
        finally
        {
            if (File.Exists(TemporaryFilePath))
            {
                File.Delete(TemporaryFilePath);
            }
        }
    }

    public string Read()
    {
        if (!Exists)
        {
            throw new FileNotFoundException("No manual save exists for slot 0.", FilePath);
        }

        if (FileSize == 0L)
        {
            throw new InvalidDataException($"The save file at '{FilePath}' is empty.");
        }

        return File.ReadAllText(FilePath);
    }
}
