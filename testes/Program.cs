using DragonShelterPTBR;
using System.Text.Json;

if (args.Length == 2 && args[0] == "--payload")
{
    var app = System.Reflection.Assembly.LoadFile(Path.GetFullPath(args[1]));
    if (app.GetName().Name != "DragonShelterPTBR_Patch3" || app.GetName().Version?.ToString() != "1.3.0.0")
        throw new Exception("Identidade do instalador incorreta");
    using var resource = app.GetManifestResourceStream("PTBR.bundle") ?? throw new Exception("Bundle incorporado ausente");
    using var memory = new MemoryStream(); resource.CopyTo(memory);
    Catalog.ValidatePayload(memory.ToArray());
    Console.WriteLine("PASS: assembly Release 1.3.0.0 incorpora exatamente o bundle PTBR Patch3: " + Catalog.Hash(memory.ToArray()));
    return;
}

// This program is NOT the installer. All mutations are limited to a new test sandbox.
string project = Path.GetFullPath(args.Single());
if (Path.GetFileName(project) != "INSTALADOR_PATCH3") throw new Exception("Raiz de teste incorreta.");
string sandbox = Path.Combine(project, "testes", "execucoes", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);
byte[] original = File.ReadAllBytes(Path.Combine(project, "testes", "fontes", "catalog.original.json"));
byte[] bundle = File.ReadAllBytes(Path.Combine(project, "testes", "fontes", "bundle.original.bundle"));
byte[] payload = File.ReadAllBytes(Path.Combine(project, "payload", "localization-string-tables-english(en)_assets_all_PTBR_PATCH3.bundle"));
var results = new List<string>();
int number = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
void MustFail(Action action)
{
    bool failed = false;
    try { action(); } catch { failed = true; }
    Check(failed, "A operação deveria recusar.");
}
Installer Fixture()
{
    string root = Path.Combine(sandbox, "Jogo cópia " + (++number));
    string aa = Path.Combine(root, "Dragon Shelter_Data", "StreamingAssets", "aa");
    Directory.CreateDirectory(Path.Combine(aa, "StandaloneWindows64"));
    File.WriteAllBytes(Path.Combine(aa, "catalog.json"), original);
    File.WriteAllBytes(Path.Combine(aa, "StandaloneWindows64", Catalog.BundleName), bundle);
    return new Installer(root);
}
Dictionary<string, string> Snapshot(Installer e) => Directory.GetFiles(e.Root, "*", SearchOption.AllDirectories)
    .ToDictionary(p => Path.GetRelativePath(e.Root, p), p => Catalog.Hash(File.ReadAllBytes(p)));
void Unchanged(Installer e, Dictionary<string, string> before)
{
    var after = Snapshot(e);
    Check(before.Count == after.Count && before.All(kv => after.TryGetValue(kv.Key, out var hash) && hash == kv.Value), "Houve gravação inesperada.");
}
void Originals(Installer e) => Check(Catalog.Hash(File.ReadAllBytes(e.CatalogPath)) == Catalog.OriginalCatalog &&
    Catalog.Hash(File.ReadAllBytes(e.BundlePath)) == Catalog.OriginalBundle, "Originais não restaurados.");
void Test(string name, Action action) { action(); results.Add("PASS: " + name); Console.WriteLine(results.Last()); }

Test("Fontes e payload correspondem aos hashes homologados", () =>
{
    Check(Catalog.Hash(original) == Catalog.OriginalCatalog && Catalog.Hash(bundle) == Catalog.OriginalBundle, "Fontes divergentes.");
    Catalog.ValidatePayload(payload);
});
byte[] patched = Catalog.Patch(original);
Test("Catálogo reproduz referência independente; CRC/tamanho e offsets corretos", () =>
{
    Check(Catalog.Hash(patched) == Catalog.PatchedCatalog, "Catálogo divergente.");
    Check(patched.SequenceEqual(File.ReadAllBytes(Path.Combine(project, "testes", "fontes", "catalog.ptbr.esperado.json"))), "Referência independente divergente.");
    using var before = JsonDocument.Parse(original); using var after = JsonDocument.Parse(patched);
    foreach (var property in before.RootElement.EnumerateObject())
        if (property.Name != "m_ExtraDataString" && property.Name != "m_EntryDataString") Check(property.Value.GetRawText() == after.RootElement.GetProperty(property.Name).GetRawText(), "Outro campo mudou.");
    var a = Convert.FromBase64String(before.RootElement.GetProperty("m_ExtraDataString").GetString()!);
    var b = Convert.FromBase64String(after.RootElement.GetProperty("m_ExtraDataString").GetString()!);
    var ea = Convert.FromBase64String(before.RootElement.GetProperty("m_EntryDataString").GetString()!);
    var eb = Convert.FromBase64String(after.RootElement.GetProperty("m_EntryDataString").GetString()!);
    int Int(byte[] data, int pos) => System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
    int count = Int(ea, 0);
    Check(ea.Length == eb.Length && Int(eb, 0) == count, "Índice mudou de tamanho.");
    int selected = Enumerable.Range(0, count).Single(i => before.RootElement.GetProperty("m_InternalIds")[Int(ea, 4 + i * 28)].GetString()!.Replace('\\', '/').EndsWith("/" + Catalog.BundleName, StringComparison.Ordinal));
    int offset = Int(ea, 4 + selected * 28 + 16);
    int p = offset + 1;
    for (int i = 0; i < 2; i++) p += a[p] + 1;
    int oldLength = Int(a, p), newLength = Int(b, p); p += 4;
    int shift = newLength - oldLength;
    Check(shift == 0 && b.Length == a.Length + shift, "Delta de tamanho inesperado.");
    Check(a.AsSpan(0, p - 4).SequenceEqual(b.AsSpan(0, p - 4)) && a.AsSpan(p + oldLength).SequenceEqual(b.AsSpan(p + newLength)), "Bytes de outros objetos mudaram.");
    int moved = 0;
    for (int i = 0; i < count; i++)
        for (int field = 0; field < 7; field++)
        {
            int at = 4 + i * 28 + field * 4, expected = Int(ea, at);
            if (field == 4 && expected >= p + oldLength) { expected += shift; moved++; }
            Check(Int(eb, at) == expected, "Campo/offset incorreto no índice.");
        }
    Check(moved == 8, "Quantidade de referências posteriores incorreta.");
    using var oa = JsonDocument.Parse(System.Text.Encoding.Unicode.GetString(a, p, oldLength));
    using var ob = JsonDocument.Parse(System.Text.Encoding.Unicode.GetString(b, p, newLength));
    Check(oa.RootElement.EnumerateObject().Where(p => p.Value.GetRawText() != ob.RootElement.GetProperty(p.Name).GetRawText()).Select(p => p.Name).Order().SequenceEqual(new[] { "m_BundleSize", "m_Crc" }), "Campos inesperados.");
});
Test("Patch 3 recusa Patch 1 original e traduzido sem gravação", () =>
{
    foreach (bool translated in new[] { false, true })
    {
        var e = Fixture();
        File.Copy(Path.Combine(project, "testes", "fontes", "patch1", "catalog.original.json"), e.CatalogPath, true);
        File.Copy(translated
            ? Path.Combine(project, "payload", "localization-string-tables-english(en)_assets_all_PTBR_PATCH1.bundle")
            : Path.Combine(project, "testes", "fontes", "patch1", "bundle.original.bundle"), e.BundlePath, true);
        var s = Snapshot(e); MustFail(() => e.Install(payload)); Unchanged(e, s);
    }
});
Test("Backup Patch 1 permanece intacto ao instalar e restaurar Patch 3", () =>
{
    var e = Fixture();
    string legacy = Path.Combine(e.Root, ".dragon-shelter-ptbr-patch1");
    Directory.CreateDirectory(legacy);
    foreach (string file in new[] { "catalog.original.json", "bundle.original.bundle" })
        File.Copy(Path.Combine(project, "testes", "fontes", "patch1", file), Path.Combine(legacy, file));
    File.WriteAllText(Path.Combine(legacy, "registro.json"), "registro Patch 1 preservado");
    var oldHashes = Directory.GetFiles(legacy).ToDictionary(p => p, p => Catalog.Hash(File.ReadAllBytes(p)));
    Check(e.BackupDirectory.EndsWith(".dragon-shelter-ptbr-patch3", StringComparison.Ordinal), "Namespace de backup incorreto.");
    e.Install(payload); Check(e.Verify(payload) == InstallState.Installed, "Não instalado.");
    e.Restore(); Originals(e);
    Check(oldHashes.All(kv => Catalog.Hash(File.ReadAllBytes(kv.Key)) == kv.Value), "Backup Patch 1 alterado.");
});
Test("Verificação não grava arquivos", () => { var e = Fixture(); var s = Snapshot(e); Check(e.Verify(payload) == InstallState.Original, "Estado errado."); Unchanged(e, s); });
Test("Instalação, reconhecimento, backups imutáveis e restauração", () =>
{
    var e = Fixture(); e.Install(payload); Check(e.Verify(payload) == InstallState.Installed, "Não instalado.");
    var s = Snapshot(e); e.Install(payload); Unchanged(e, s);
    e.Restore(); Originals(e); e.Install(payload); e.Restore(); Originals(e);
});
foreach (string kind in new[] { "catalog", "bundle", "payload", "missing" })
    Test("Recusa sem gravação: " + kind, () =>
    {
        var e = Fixture(); byte[] supplied = payload.ToArray();
        if (kind == "catalog") File.AppendAllText(e.CatalogPath, " ");
        if (kind == "bundle") File.WriteAllBytes(e.BundlePath, [1, 2, 3]);
        if (kind == "payload") supplied[0] ^= 1;
        if (kind == "missing") File.Delete(e.BundlePath);
        var s = Snapshot(e); MustFail(() => e.Install(supplied)); Unchanged(e, s);
    });
foreach (string point in new[] { "validated", "backup-ready", "before-replace", "bundle-replaced", "catalog-replaced", "committed" })
    Test("Falha simulada e originais preservados: " + point, () =>
    {
        var e = Fixture(); e.Checkpoint = p => { if (p == point) throw new IOException("Falha injetada: " + p); };
        MustFail(() => e.Install(payload)); Originals(e);
    });
Test("Instalação interrompida: recuperação do par misto", () =>
{
    var e = Fixture(); e.Install(payload); File.WriteAllBytes(e.CatalogPath, original);
    Check(e.Verify(payload) == InstallState.RecoveryRequired, "Interrupção não detectada.");
    e.Install(payload); Originals(e);
});
Test("Mistura sem backups: recusa sem gravação", () =>
{
    var e = Fixture(); File.WriteAllBytes(e.BundlePath, payload); var s = Snapshot(e); MustFail(() => e.Install(payload)); Unchanged(e, s);
});
Test("Backup corrompido: restauração recusada sem gravação", () =>
{
    var e = Fixture(); e.Install(payload); File.WriteAllBytes(Path.Combine(e.BackupDirectory, "bundle.original.bundle"), [0]);
    var s = Snapshot(e); MustFail(() => e.Restore()); Unchanged(e, s);
});
Test("Atualização externa: restauração recusada sem gravação", () =>
{
    var e = Fixture(); e.Install(payload); File.AppendAllText(e.CatalogPath, " "); var s = Snapshot(e); MustFail(() => e.Restore()); Unchanged(e, s);
});
Test("Instalação manual reconhecida; sem backup não restaura", () =>
{
    var e = Fixture(); File.WriteAllBytes(e.CatalogPath, patched); File.WriteAllBytes(e.BundlePath, payload);
    var s = Snapshot(e); Check(e.Verify(payload) == InstallState.Installed, "Estado errado."); e.Install(payload); MustFail(() => e.Restore()); Unchanged(e, s);
});
Test("Bloqueio real de catálogo: recuperação pendente e nova restauração", () =>
{
    var e = Fixture(); FileStream? hold = null;
    e.Checkpoint = p =>
    {
        if (p != "bundle-replaced") return;
        hold = new FileStream(e.CatalogPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    };
    try { MustFail(() => e.Install(payload)); }
    finally { hold?.Dispose(); }
    Check(Catalog.Hash(File.ReadAllBytes(e.BundlePath)) == Catalog.OriginalBundle, "Rollback do bundle foi omitido.");
    e.Restore(); Originals(e);
});
Test("Patch 3 recusa os arquivos pre-patch sem gravação", () =>
{
    var e = Fixture();
    File.Copy(Path.Combine(project, "testes", "fontes", "pre-patch1", "catalog.original.json"), e.CatalogPath, true);
    File.Copy(Path.Combine(project, "testes", "fontes", "pre-patch1", "bundle.original.bundle"), e.BundlePath, true);
    var s = Snapshot(e); MustFail(() => e.Install(payload)); Unchanged(e, s);
});
Test("Backup V1 coexistente permanece intacto ao instalar e restaurar Patch 3", () =>
{
    var e = Fixture();
    string legacy = Path.Combine(e.Root, ".dragon-shelter-ptbr-v1");
    Directory.CreateDirectory(legacy);
    File.Copy(Path.Combine(project, "testes", "fontes", "pre-patch1", "catalog.original.json"), Path.Combine(legacy, "catalog.original.json"));
    File.Copy(Path.Combine(project, "testes", "fontes", "pre-patch1", "bundle.original.bundle"), Path.Combine(legacy, "bundle.original.bundle"));
    File.WriteAllText(Path.Combine(legacy, "registro.json"), "registro V1 preservado");
    var oldHashes = Directory.GetFiles(legacy).ToDictionary(p => p, p => Catalog.Hash(File.ReadAllBytes(p)));
    e.Install(payload); Check(e.Verify(payload) == InstallState.Installed, "Não instalado."); e.Restore(); Originals(e);
    Check(oldHashes.All(kv => Catalog.Hash(File.ReadAllBytes(kv.Key)) == kv.Value), "Backup V1 foi alterado.");
    Check(e.BackupDirectory != legacy, "Backup do Patch 3 colidiu com V1.");
});

Test("Patch 3 recusa Patch 2 original e traduzido sem gravacao", () =>
{
    foreach (bool translated in new[] { false, true })
    {
        var e = Fixture();
        File.Copy(Path.Combine(project, "testes", "fontes", "patch2", "catalog.original.json"), e.CatalogPath, true);
        File.Copy(translated ? Path.Combine(project, "payload", "localization-string-tables-english(en)_assets_all_PTBR_PATCH2.bundle") : Path.Combine(project, "testes", "fontes", "patch2", "bundle.original.bundle"), e.BundlePath, true);
        var snapshot = Snapshot(e); MustFail(() => e.Install(payload)); Unchanged(e, snapshot);
    }
});
Test("Backup Patch 2 preservado durante instalacao/restauracao Patch 3", () =>
{
    var e = Fixture(); var legacy = Path.Combine(e.Root, ".dragon-shelter-ptbr-patch2");
    Directory.CreateDirectory(legacy);
    foreach (string file in new[] { "catalog.original.json", "bundle.original.bundle" })
        File.Copy(Path.Combine(project, "testes", "fontes", "patch2", file), Path.Combine(legacy, file));
    File.WriteAllText(Path.Combine(legacy, "registro.json"), "registro Patch 2 preservado");
    var hashes = Directory.GetFiles(legacy).ToDictionary(p => p, p => Catalog.Hash(File.ReadAllBytes(p)));
    e.Install(payload); e.Restore(); Originals(e);
    Check(hashes.All(kv => Catalog.Hash(File.ReadAllBytes(kv.Key)) == kv.Value), "Backup Patch 2 modificado");
});

File.WriteAllLines(Path.Combine(sandbox, "RESULTADOS.txt"), results.Append($"Total: {results.Count} testes aprovados. Instalador GUI não executado. Apenas cópias de teste alteradas."));
Console.WriteLine($"Total: {results.Count} testes aprovados. Relatório: {sandbox}");
