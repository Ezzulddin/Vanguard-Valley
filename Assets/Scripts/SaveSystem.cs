using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>One saved character. Add appearance fields here as your creator grows.</summary>
[Serializable]
public class CharacterData
{
    public string id;
    public string displayName = "New Character";
    public string createdUtc;

    // --- placeholder appearance data, expand later ---
    public int bodyType;
    public int hairStyle;
    public float skinTone = 0.5f;
    public Color hairColor = Color.black;

    public static CharacterData CreateNew()
    {
        return new CharacterData
        {
            id = Guid.NewGuid().ToString("N"),
            createdUtc = DateTime.UtcNow.ToString("o")
        };
    }
}

/// <summary>Everything we write to disk: all characters plus which one is selected.</summary>
[Serializable]
public class CharacterDatabase
{
    public List<CharacterData> characters = new List<CharacterData>();
    public string activeCharacterId;

    public CharacterData Active => characters.Find(c => c.id == activeCharacterId);
}

/// <summary>
/// Static access to the saved characters. Nothing is ever overwritten unless you
/// pass an existing id, so creating a second character keeps the first one.
/// </summary>
public static class SaveSystem
{
    const string FileName = "characters.json";

    static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    static CharacterDatabase _db;

    public static CharacterDatabase Db
    {
        get
        {
            if (_db == null) _db = Load();
            return _db;
        }
    }

    public static bool HasAnyCharacter => Db.characters.Count > 0;

    static CharacterDatabase Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var db = JsonUtility.FromJson<CharacterDatabase>(json);
                if (db != null)
                {
                    if (db.characters == null) db.characters = new List<CharacterData>();
                    return db;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load save file: {e.Message}");
        }
        return new CharacterDatabase();
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Db, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to write save file: {e.Message}");
        }
    }

    /// <summary>Adds a new character, or updates one that already exists (matched by id).</summary>
    public static void AddOrUpdate(CharacterData character, bool makeActive = true)
    {
        if (character == null) return;
        if (string.IsNullOrEmpty(character.id)) character.id = Guid.NewGuid().ToString("N");

        int index = Db.characters.FindIndex(c => c.id == character.id);
        if (index >= 0) Db.characters[index] = character;
        else Db.characters.Add(character);

        if (makeActive || string.IsNullOrEmpty(Db.activeCharacterId))
            Db.activeCharacterId = character.id;

        Save();
    }

    public static void SetActive(string id)
    {
        if (Db.characters.Exists(c => c.id == id))
        {
            Db.activeCharacterId = id;
            Save();
        }
    }

    public static void Delete(string id)
    {
        Db.characters.RemoveAll(c => c.id == id);
        if (Db.activeCharacterId == id)
            Db.activeCharacterId = Db.characters.Count > 0 ? Db.characters[0].id : null;
        Save();
    }

    /// <summary>Call from the game scene to find out who the player is playing as.</summary>
    public static CharacterData GetActiveOrFirst()
    {
        var active = Db.Active;
        if (active != null) return active;
        return Db.characters.Count > 0 ? Db.characters[0] : null;
    }
}

/// <summary>Tiny bit of state that survives a scene load.</summary>
public static class CreationContext
{
    /// <summary>Null = creating a brand new character. Set = editing an existing one.</summary>
    public static string EditCharacterId;
}