using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SaveManager
{
    private static readonly string SaveFileName = "save.dat";

    private static readonly string EncryptionPassphrase = "jId9D1+b_&R|XY*T.!#9I(yl0;2GmPRR";
    private static readonly byte[] EncryptionSalt = new byte[] {
            0x4D, 0x23, 0xA7, 0x19,
            0xC2, 0x5D, 0xEB, 0x70
        };

    private static readonly int KeyIterations = 10000;

    public static bool IsLoadingSave { get; set; } = false;

    public static MasterSaveData LoadedData { get; set; }

    public static void SaveGame(MasterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveLoadManager] Cannot save: data is null.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data);

            byte[] encryptedBytes = EncryptStringToBytes_Aes(json, EncryptionPassphrase, EncryptionSalt);

            string fullPath = GetSaveFilePath();
            File.WriteAllBytes(fullPath, encryptedBytes);

            Debug.Log($"[SaveLoadManager] Save successful. Wrote {encryptedBytes.Length} bytes to:\n{fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoadManager] Save failed: {ex}");
        }
    }

    public static MasterSaveData LoadGame()
    {
        string fullPath = GetSaveFilePath();
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[SaveLoadManager] No save file found at:\n{fullPath}");
            LoadedData = null;
            return null;
        }

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(fullPath);
            string json = DecryptStringFromBytes_Aes(encryptedBytes, EncryptionPassphrase, EncryptionSalt);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[SaveLoadManager] Decryption produced an empty string.");
                LoadedData = null;
                return null;
            }

            MasterSaveData data = JsonUtility.FromJson<MasterSaveData>(json);
            if (data == null)
            {
                Debug.LogError("[SaveLoadManager] Failed to deserialize JSON into MasterSaveData.");
                LoadedData = null;
                IsLoadingSave = false;
                return null;
            }

            LoadedData = data;
            IsLoadingSave = true;
            Debug.Log("[SaveLoadManager] Load successful.");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoadManager] Load failed: {ex}");
            LoadedData = null;
            IsLoadingSave = false;
            return null;
        }
    }

    public static void DeleteSave()
    {
        string fullPath = GetSaveFilePath();
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                LoadedData = null;
                IsLoadingSave = false;
                Debug.Log($"[SaveLoadManager] Deleted save file at:\n{fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadManager] Failed to delete save file: {ex}");
            }
        }
        else
        {
            Debug.Log($"[SaveLoadManager] No save file to delete at:\n{fullPath}");
        }
    }

    private static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private static byte[] EncryptStringToBytes_Aes(string plainText, string passphrase, byte[] salt)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        using Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(passphrase, salt, KeyIterations);
        byte[] key = keyDerivation.GetBytes(32);
        byte[] iv = keyDerivation.GetBytes(16);

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msEncrypt = new MemoryStream();
        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
        {
            swEncrypt.Write(plainText);
        }

        return msEncrypt.ToArray();
    }

    private static string DecryptStringFromBytes_Aes(byte[] cipherTextBytes, string passphrase, byte[] salt)
    {
        if (cipherTextBytes == null || cipherTextBytes.Length == 0)
        {
            throw new ArgumentNullException(nameof(cipherTextBytes));
        }

        using Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(passphrase, salt, KeyIterations);
        byte[] key = keyDerivation.GetBytes(32);
        byte[] iv = keyDerivation.GetBytes(16);

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        using ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msDecrypt = new MemoryStream(cipherTextBytes);
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);

        string plaintext = srDecrypt.ReadToEnd();
        return plaintext;
    }
}