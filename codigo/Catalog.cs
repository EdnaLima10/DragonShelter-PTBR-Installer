using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DragonShelterPTBR;

public static class Catalog
{
    public const string BundleName = "localization-string-tables-english(en)_assets_all.bundle";
    public const string OriginalCatalog = "f610579475b4086c92a48b6844c838eb29fe51609f6baa156c7449c38cf555b4";
    public const string PatchedCatalog = "e5bf2827681cebec84fce28fad97eb8646cd2d9b5c2fb0ac194900703cf8b3d4";
    public const string OriginalBundle = "facfef68c357ac03274d3a79f4f8ad827b8176e836390806c0ec248d75252ec6";
    public const string TranslatedBundle = "8f2f33ebf439752ab3c41d80951a5ef6c422a9264e1c0c53025575104afd2e22";
    public const string Incompatible = "Esta versão ou combinação de arquivos ainda não é compatível. Nenhum arquivo foi alterado.";
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
    public static byte[] Read(string path, long max = 16 * 1024 * 1024)
    {
        Require(File.Exists(path), "Arquivo necessário não encontrado: " + path);
        Require(new FileInfo(path).Length <= max, "Arquivo maior que o limite desta edição: " + path);
        return File.ReadAllBytes(path);
    }
    public static void ValidatePayload(byte[] data) => Require(data.Length == 138206 && Hash(data) == TranslatedBundle,
        "O bundle PT-BR está corrompido ou não pertence a esta edição. Nenhum arquivo foi alterado.");

    public static byte[] Patch(byte[] original)
    {
        Require(Hash(original) == OriginalCatalog, Incompatible);
        var utf8 = new UTF8Encoding(false, true);
        var utf16 = new UnicodeEncoding(false, false, true);
        string text = utf8.GetString(original);
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var entries = Convert.FromBase64String(root.GetProperty("m_EntryDataString").GetString()!);
        var extra = Convert.FromBase64String(root.GetProperty("m_ExtraDataString").GetString()!);
        int Int(byte[] data, int at)
        {
            Require(at >= 0 && at <= data.Length - 4, "Estrutura de catálogo inválida.");
            return BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(at, 4));
        }
        int count = Int(entries, 0);
        Require(count > 0 && 4L + count * 28L == entries.Length, "Registros incompatíveis.");
        var ids = root.GetProperty("m_InternalIds");
        var providers = root.GetProperty("m_ProviderIds");
        var types = root.GetProperty("m_resourceTypes");
        string wanted = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneWindows64/" + BundleName;
        var matches = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int id = Int(entries, 4 + i * 28);
            Require(id >= 0 && id < ids.GetArrayLength(), "Índice de identificador inválido.");
            if (ids[id].GetString()!.Replace('\\', '/') == wanted) matches.Add(i);
        }
        Require(matches.Count == 1, "A entrada do bundle inglês não é única ou está ausente.");
        int record = 4 + matches[0] * 28;
        int provider = Int(entries, record + 4), type = Int(entries, record + 24);
        Require(provider >= 0 && provider < providers.GetArrayLength() &&
            providers[provider].GetString() == "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider", "Provider incompatível.");
        Require(type >= 0 && type < types.GetArrayLength() && types[type].GetProperty("m_ClassName").GetString() ==
            "UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource", "Tipo de recurso incompatível.");
        int offset = Int(entries, record + 16);
        Require(Enumerable.Range(0, count).Count(i => Int(entries, 4 + i * 28 + 16) == offset) == 1,
            "As opções são compartilhadas por outra entrada.");
        int p = offset;
        byte Next()
        {
            Require(p >= 0 && p < extra.Length, "Objeto de opções truncado.");
            return extra[p++];
        }
        string Name()
        {
            int n = Next();
            Require(p <= extra.Length - n, "Nome serializado truncado.");
            string s = Encoding.ASCII.GetString(extra, p, n); p += n; return s;
        }
        Require(Next() == 7, "Tipo de serialização incompatível.");
        Require(Name() == "Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Assembly incompatível.");
        Require(Name() == "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions", "Opções incompatíveis.");
        int length = Int(extra, p); p += 4;
        Require(length > 0 && length % 2 == 0 && p <= extra.Length - length, "Comprimento inválido.");
        string oldOptions = utf16.GetString(extra, p, length);
        using var options = JsonDocument.Parse(oldOptions);
        Require(options.RootElement.GetProperty("m_Crc").GetUInt32() == 1513908345 &&
            options.RootElement.GetProperty("m_BundleSize").GetInt64() == 135271, "Valores originais incompatíveis.");
        string ReplaceNumber(string source, string key, string oldValue, string newValue)
        {
            var regex = new Regex("(\"" + key + "\"\\s*:\\s*)" + oldValue + "\\b");
            Require(regex.Matches(source).Count == 1, "Campo ausente ou duplicado: " + key);
            return regex.Replace(source, m => m.Groups[1].Value + newValue);
        }
        string updated = ReplaceNumber(oldOptions, "m_Crc", "1513908345", "2562807805");
        updated = ReplaceNumber(updated, "m_BundleSize", "135271", "138206");
        byte[] segment = utf16.GetBytes(updated);
        int delta = checked(segment.Length - length);
        byte[] newExtra = new byte[checked(extra.Length + delta)];
        extra.AsSpan(0, p).CopyTo(newExtra);
        BinaryPrimitives.WriteInt32LittleEndian(newExtra.AsSpan(p - 4, 4), segment.Length);
        segment.CopyTo(newExtra, p);
        extra.AsSpan(p + length).CopyTo(newExtra.AsSpan(p + segment.Length));
        // Each extra-data object is referenced by its byte offset in an entry.
        // Growing the selected JSON object must move every later reference.
        for (int i = 0; i < count; i++)
        {
            int at = 4 + i * 28 + 16;
            int oldOffset = Int(entries, at);
            Require(oldOffset < offset || oldOffset == offset || oldOffset >= p + length,
                "Outra entrada aponta para o interior do objeto alterado.");
            if (oldOffset >= p + length)
                BinaryPrimitives.WriteInt32LittleEndian(entries.AsSpan(at, 4), checked(oldOffset + delta));
        }
        string ReplaceData(string source, string name, byte[] value)
        {
            var field = new Regex("(\"" + name + "\"\\s*:\\s*\")([^\"\\\\]*)(\")");
            Require(field.Matches(source).Count == 1, "Campo Base64 ausente ou duplicado.");
            Require(field.Match(source).Groups[2].Value == root.GetProperty(name).GetString(), "Base64 divergente.");
            return field.Replace(source, m => m.Groups[1].Value + Convert.ToBase64String(value) + m.Groups[3].Value);
        }
        string output = ReplaceData(text, "m_ExtraDataString", newExtra);
        output = ReplaceData(output, "m_EntryDataString", entries);
        byte[] result = utf8.GetBytes(output);
        Require(Hash(result) == PatchedCatalog,
            "O resultado não corresponde ao catálogo corrigido homologado.");
        return result;
    }
}
