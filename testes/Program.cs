using DragonShelterPTBR;
using System.Text.Json;

// This program is NOT the installer. All mutations are limited to a new test sandbox.
string project = Path.GetFullPath(args.Single());
if (!File.Exists(Path.Combine(project, "codigo", "DragonShelterPTBR.csproj")) ||
    !File.Exists(Path.Combine(project, "testes", "Testes.csproj"))) throw new Exception("Raiz de teste incorreta.");
string sandbox = Path.Combine(project, "testes", "execucoes", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);
byte[] original = File.ReadAllBytes(Path.Combine(project, "testes", "fontes", "catalog.original.json"));
byte[] bundle = File.ReadAllBytes(Path.Combine(project, "testes", "fontes", "bundle.original.bundle"));
byte[] payload = File.ReadAllBytes(Path.Combine(project, "payload", "localization-string-tables-english(en)_assets_all_PTBR_PATCH1.bundle"));
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
Test("Catálogo reproduz hash homologado; só CRC/tamanho mudam", () =>
{
    Check(Catalog.Hash(patched) == Catalog.PatchedCatalog && patched.Length == original.Length, "Catálogo divergente.");
    using var before = JsonDocument.Parse(original); using var after = JsonDocument.Parse(patched);
    foreach (var property in before.RootElement.EnumerateObject())
        if (property.Name != "m_ExtraDataString") Check(property.Value.GetRawText() == after.RootElement.GetProperty(property.Name).GetRawText(), "Outro campo mudou.");
    var a = Convert.FromBase64String(before.RootElement.GetProperty("m_ExtraDataString").GetString()!);
    var b = Convert.FromBase64String(after.RootElement.GetProperty("m_ExtraDataString").GetString()!);
    Check(a.AsSpan(0, 13643).SequenceEqual(b.AsSpan(0, 13643)) && a.AsSpan(13643 + 686).SequenceEqual(b.AsSpan(13643 + 686)), "Outro objeto mudou.");
    using var oa = JsonDocument.Parse(System.Text.Encoding.Unicode.GetString(a, 13643, 686));
    using var ob = JsonDocument.Parse(System.Text.Encoding.Unicode.GetString(b, 13643, 686));
    Check(oa.RootElement.EnumerateObject().Where(p => p.Value.GetRawText() != ob.RootElement.GetProperty(p.Name).GetRawText()).Select(p => p.Name).Order().SequenceEqual(new[] { "m_BundleSize", "m_Crc" }), "Campos inesperados.");
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
Test("Patch 1 recusa os arquivos pre-patch sem gravação", () =>
{
    var e = Fixture();
    File.Copy(Path.Combine(project, "testes", "fontes", "pre-patch1", "catalog.original.json"), e.CatalogPath, true);
    File.Copy(Path.Combine(project, "testes", "fontes", "pre-patch1", "bundle.original.bundle"), e.BundlePath, true);
    var s = Snapshot(e); MustFail(() => e.Install(payload)); Unchanged(e, s);
});
Test("Backup V1 coexistente permanece intacto ao instalar e restaurar Patch 1", () =>
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
    Check(e.BackupDirectory != legacy, "Backup do Patch 1 colidiu com V1.");
});
File.WriteAllLines(Path.Combine(sandbox, "RESULTADOS.txt"), results.Append($"Total: {results.Count} testes aprovados. Instalador GUI não executado. Apenas cópias de teste alteradas."));
Console.WriteLine($"Total: {results.Count} testes aprovados. Relatório: {sandbox}");
