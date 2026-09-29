using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DragonShelterPTBR;

public static class Catalog
{
    public const string BundleName = "localization-string-tables-english(en)_assets_all.bundle";
    public const string OriginalCatalog = "d3840b0dcbafa591c087a39cafb27a64d4cff55c93543b83eca58706de95ec84";
    public const string PatchedCatalog = "e3baa58c3b271a8e587bc3401eac0eab4d961943e3b6e5b656c3672fdc46ddef";
    public const string OriginalBundle = "6fe36d8aa2c79f356e7634a95ca79f6e38883069174deeddc69f8343111a2b87";
    public const string TranslatedBundle = "374656a92913e98939216cf813e81ef925a3e5eee7cf2813a9f083f4583f9ffd";
    public const string Incompatible = "Esta versão ou combinação de arquivos ainda não é compatível. Nenhum arquivo foi alterado.";
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
    public static byte[] Read(string path, long max = 16 * 1024 * 1024)
    {
        Require(File.Exists(path), "Arquivo necessário não encontrado: " + path);
        Require(new FileInfo(path).Length <= max, "Arquivo maior que o limite da V1: " + path);
        return File.ReadAllBytes(path);
    }
    public static void ValidatePayload(byte[] data) => Require(data.Length == 132745 && Hash(data) == TranslatedBundle,
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
        Require(options.RootElement.GetProperty("m_Crc").GetUInt32() == 2252795409 &&
            options.RootElement.GetProperty("m_BundleSize").GetInt64() == 129866, "Valores originais incompatíveis.");
        string ReplaceNumber(string source, string key, string oldValue, string newValue)
        {
            var regex = new Regex("(\"" + key + "\"\\s*:\\s*)" + oldValue + "\\b");
            Require(regex.Matches(source).Count == 1, "Campo ausente ou duplicado: " + key);
            return regex.Replace(source, m => m.Groups[1].Value + newValue);
        }
        string updated = ReplaceNumber(oldOptions, "m_Crc", "2252795409", "1220118888");
        updated = ReplaceNumber(updated, "m_BundleSize", "129866", "132745");
        byte[] segment = utf16.GetBytes(updated);
        Require(segment.Length == length, "Esta V1 não suporta deslocar objetos do catálogo.");
        segment.CopyTo(extra, p);
        var field = new Regex("(\"m_ExtraDataString\"\\s*:\\s*\")([^\"\\\\]*)(\")");
        Require(field.Matches(text).Count == 1, "Campo Base64 ausente ou duplicado.");
        Require(field.Match(text).Groups[2].Value == root.GetProperty("m_ExtraDataString").GetString(), "Base64 divergente.");
        string output = field.Replace(text, m => m.Groups[1].Value + Convert.ToBase64String(extra) + m.Groups[3].Value);
        byte[] result = utf8.GetBytes(output);
        Require(result.Length == original.Length && Hash(result) == PatchedCatalog,
            "O resultado não corresponde ao catálogo corrigido homologado.");
        return result;
    }
}
