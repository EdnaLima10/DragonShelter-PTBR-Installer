using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DragonShelterPTBR;

public enum InstallState { Original, Installed, RecoveryRequired }

public sealed class Installer
{
    public string Root { get; }
    public string CatalogPath { get; }
    public string BundlePath { get; }
    public string BackupDirectory { get; }
    string ReceiptPath => Path.Combine(BackupDirectory, "registro.json");
    string CatalogBackup => Path.Combine(BackupDirectory, "catalog.original.json");
    string BundleBackup => Path.Combine(BackupDirectory, "bundle.original.bundle");
    // Used only by the separate internal test executable; the GUI never sets this.
    internal Action<string>? Checkpoint { get; set; }
    sealed record Receipt(string Product, string Root, string State);

    public Installer(string root)
    {
        Root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        Catalog.Require(Directory.Exists(Root), "Selecione a pasta raiz do jogo.");
        string aa = Path.Combine(Root, "Dragon Shelter_Data", "StreamingAssets", "aa");
        CatalogPath = Path.Combine(aa, "catalog.json");
        BundlePath = Path.Combine(aa, "StandaloneWindows64", Catalog.BundleName);
        BackupDirectory = Path.Combine(Root, ".dragon-shelter-ptbr-v1");
        ValidatePaths();
    }

    void ValidatePaths()
    {
        foreach (string path in new[] { CatalogPath, BundlePath, ReceiptPath, CatalogBackup, BundleBackup })
        {
            Catalog.Require(Path.GetFullPath(path).StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Destino fora da instalação selecionada.");
            // Also reject reparse points in ancestors, including the selected root.
            for (string? current = path; current != null; current = Path.GetDirectoryName(current))
                if (File.Exists(current) || Directory.Exists(current))
                    Catalog.Require((File.GetAttributes(current) & FileAttributes.ReparsePoint) == 0,
                        "Esta V1 não aceita links ou junções no caminho selecionado.");
        }
    }

    (string catalog, string bundle) Hashes()
    {
        ValidatePaths();
        return (Catalog.Hash(Catalog.Read(CatalogPath)), Catalog.Hash(Catalog.Read(BundlePath)));
    }

    public InstallState Verify(byte[] payload)
    {
        Catalog.ValidatePayload(payload);
        var pair = Hashes();
        if (pair == (Catalog.OriginalCatalog, Catalog.OriginalBundle)) return InstallState.Original;
        if (pair == (Catalog.PatchedCatalog, Catalog.TranslatedBundle)) return InstallState.Installed;
        if (Known(pair) && File.Exists(ReceiptPath))
        {
            ValidateBackups();
            return InstallState.RecoveryRequired;
        }
        throw new InvalidDataException(Catalog.Incompatible);
    }

    static bool Known((string catalog, string bundle) p) =>
        (p.catalog == Catalog.OriginalCatalog || p.catalog == Catalog.PatchedCatalog) &&
        (p.bundle == Catalog.OriginalBundle || p.bundle == Catalog.TranslatedBundle);

    static void GameClosed()
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
                Catalog.Require(!process.ProcessName.Equals("Dragon Shelter", StringComparison.OrdinalIgnoreCase),
                    "Feche Dragon Shelter antes de instalar ou restaurar.");
        }
    }

    Mutex Acquire()
    {
        var mutex = new Mutex(false, "Local\\DragonShelterPTBR-" + Catalog.Hash(Encoding.UTF8.GetBytes(Root.ToUpperInvariant())));
        bool acquired;
        try { acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) { mutex.Dispose(); throw new IOException("Outra operação do instalador está em andamento nesta pasta."); }
        return mutex;
    }

    static void WriteNew(string path, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        stream.Flush(true);
    }

    // Temp is on the destination volume. Never falls back to truncation/copy-over.
    static void AtomicWrite(string path, byte[] bytes)
    {
        string temp = path + ".ptbr-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            WriteNew(temp, bytes);
            Catalog.Require(Catalog.Hash(Catalog.Read(temp)) == Catalog.Hash(bytes), "Falha ao verificar arquivo temporário.");
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    void SaveReceipt(string state) => AtomicWrite(ReceiptPath,
        JsonSerializer.SerializeToUtf8Bytes(new Receipt("DragonShelterPTBR/V1", Root, state)));

    void ValidateBackups()
    {
        ValidatePaths();
        var receipt = JsonSerializer.Deserialize<Receipt>(Catalog.Read(ReceiptPath, 8192));
        Catalog.Require(receipt != null && receipt.Product == "DragonShelterPTBR/V1" &&
            string.Equals(receipt.Root, Root, StringComparison.OrdinalIgnoreCase),
            "O registro de backup não pertence a esta instalação.");
        Catalog.Require(Catalog.Hash(Catalog.Read(CatalogBackup)) == Catalog.OriginalCatalog &&
            Catalog.Hash(Catalog.Read(BundleBackup)) == Catalog.OriginalBundle,
            "Backup ausente ou corrompido. A restauração automática não é segura.");
    }

    void PrepareBackup(byte[] catalog, byte[] bundle)
    {
        if (Directory.Exists(BackupDirectory))
        {
            ValidateBackups();
            return;
        }
        // Build a complete backup privately; publish its directory only after validation.
        string pending = BackupDirectory + ".preparando-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(pending);
        try
        {
            WriteNew(Path.Combine(pending, "catalog.original.json"), catalog);
            WriteNew(Path.Combine(pending, "bundle.original.bundle"), bundle);
            WriteNew(Path.Combine(pending, "registro.json"), JsonSerializer.SerializeToUtf8Bytes(new Receipt("DragonShelterPTBR/V1", Root, "backup-pronto")));
            Catalog.Require(Catalog.Hash(Catalog.Read(Path.Combine(pending, "catalog.original.json"))) == Catalog.OriginalCatalog &&
                Catalog.Hash(Catalog.Read(Path.Combine(pending, "bundle.original.bundle"))) == Catalog.OriginalBundle, "Falha na cópia de backup.");
            Directory.Move(pending, BackupDirectory);
        }
        finally
        {
            // Delete only explicitly created staging files; never recursively delete a game directory.
            if (Directory.Exists(pending))
            {
                foreach (string name in new[] { "catalog.original.json", "bundle.original.bundle", "registro.json" })
                    if (File.Exists(Path.Combine(pending, name))) File.Delete(Path.Combine(pending, name));
                Directory.Delete(pending, false);
            }
        }
        ValidateBackups();
    }

    void RestorePair()
    {
        ValidateBackups();
        Catalog.Require(Known(Hashes()), "Arquivos externos foram alterados. Os backups foram preservados; recuperação automática interrompida.");
        // Restore each independently so one access failure does not suppress the other attempt.
        var errors = new List<Exception>();
        foreach (var item in new[] { (BundlePath, BundleBackup), (CatalogPath, CatalogBackup) })
        {
            try { AtomicWrite(item.Item1, Catalog.Read(item.Item2)); }
            catch (Exception ex) { errors.Add(ex); }
        }
        if (errors.Count > 0) throw new AggregateException("Não foi possível restaurar todos os arquivos. Preserve os backups e tente Restaurar original novamente.", errors);
        Catalog.Require(Hashes() == (Catalog.OriginalCatalog, Catalog.OriginalBundle), "A verificação final da restauração falhou.");
        SaveReceipt("restaurado");
    }

    public string Install(byte[] payload)
    {
        using var mutex = Acquire();
        try
        {
            InstallState state = Verify(payload);
            if (state == InstallState.Installed) return "A tradução PT-BR já está instalada. Nenhum arquivo foi alterado.";
            GameClosed();
            if (state == InstallState.RecoveryRequired)
            {
                RestorePair();
                return "Instalação interrompida recuperada: os originais foram restaurados. Verifique a compatibilidade antes de instalar novamente.";
            }
            byte[] original = Catalog.Read(CatalogPath), bundle = Catalog.Read(BundlePath);
            Catalog.Require(Catalog.Hash(bundle) == Catalog.OriginalBundle, Catalog.Incompatible);
            byte[] patched = Catalog.Patch(original);
            Checkpoint?.Invoke("validated");
            PrepareBackup(original, bundle);
            Checkpoint?.Invoke("backup-ready");
            Catalog.Require(Hashes() == (Catalog.OriginalCatalog, Catalog.OriginalBundle), "Os arquivos mudaram durante a preparação. Instalação cancelada.");
            SaveReceipt("instalando");
            bool replacing = false;
            try
            {
                Checkpoint?.Invoke("before-replace");
                GameClosed();
                replacing = true;
                AtomicWrite(BundlePath, payload);
                Checkpoint?.Invoke("bundle-replaced");
                Catalog.Require(Hashes() == (Catalog.OriginalCatalog, Catalog.TranslatedBundle), "Mudança inesperada durante a instalação.");
                AtomicWrite(CatalogPath, patched);
                Checkpoint?.Invoke("catalog-replaced");
                Catalog.Require(Hashes() == (Catalog.PatchedCatalog, Catalog.TranslatedBundle), "A verificação final falhou.");
                SaveReceipt("instalado");
                Checkpoint?.Invoke("committed");
                return "Tradução PT-BR instalada. Selecione English no jogo. Os backups foram preservados para Restaurar original.";
            }
            catch (Exception failure)
            {
                if (!replacing) throw;
                try { RestorePair(); }
                catch (Exception rollback)
                {
                    throw new IOException("Instalação falhou e a recuperação ficou pendente. Não abra o jogo. Use Restaurar original. Backups: " +
                        BackupDirectory + "\nFalha: " + failure.Message + "\nRecuperação: " + rollback.Message, rollback);
                }
                throw new IOException("Instalação cancelada. Catálogo e bundle originais foram restaurados e verificados.\nMotivo: " + failure.Message, failure);
            }
        }
        finally { mutex.ReleaseMutex(); }
    }

    public string Restore()
    {
        using var mutex = Acquire();
        try
        {
            GameClosed();
            ValidateBackups();
            Catalog.Require(Known(Hashes()), Catalog.Incompatible);
            SaveReceipt("restaurando");
            RestorePair();
            return "Catálogo e bundle originais restaurados e verificados. Os backups foram mantidos.";
        }
        finally { mutex.ReleaseMutex(); }
    }
}
