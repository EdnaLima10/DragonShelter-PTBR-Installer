using System.Reflection;

namespace DragonShelterPTBR;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainWindow());
    }
}

sealed class MainWindow : Form
{
    readonly TextBox folder = new() { ReadOnly = true, Dock = DockStyle.Fill };
    readonly TextBox output = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    readonly Button select = new() { Text = "Selecionar pasta do jogo", AutoSize = true };
    readonly Button verify = new() { Text = "Verificar compatibilidade", AutoSize = true };
    readonly Button install = new() { Text = "Instalar tradução PT-BR", AutoSize = true };
    readonly Button restore = new() { Text = "Restaurar original", AutoSize = true };
    bool busy;

    public MainWindow()
    {
        Text = "Dragon Shelter — Tradução PT-BR — Patch 1";
        ClientSize = new Size(760, 410);
        MinimumSize = new Size(680, 400);
        StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 5, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.Controls.Add(new Label { Text = "Patch 1 — versão homologada. Feche o jogo e aguarde o término de atualizações da Steam.\nA tradução substitui as tabelas em inglês; selecione English no jogo.", Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(folder, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
        actions.Controls.AddRange([select, verify, install, restore]);
        layout.Controls.Add(actions, 0, 2);
        layout.Controls.Add(output, 0, 3);
        layout.Controls.Add(new Label { Text = "Backups: pasta .dragon-shelter-ptbr-patch1 dentro da instalação. Não a exclua.\nSe o Windows negar acesso, feche e abra o instalador como administrador.", Dock = DockStyle.Fill }, 0, 4);
        Controls.Add(layout);
        select.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta raiz Dragon Shelter", UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) { folder.Text = dialog.SelectedPath; output.Text = "Pasta selecionada. Clique em Verificar compatibilidade."; }
        };
        verify.Click += async (_, _) => await Run(engine => engine.Verify(Payload()) switch
        {
            InstallState.Original => "Compatível: catálogo e bundle originais conferem. O payload PT-BR também foi validado. Pronto para instalar.",
            InstallState.Installed => "A tradução PT-BR já está instalada. Nenhum arquivo foi alterado. A restauração exige backups íntegros deste instalador.",
            _ => "Instalação interrompida detectada. Use Restaurar original; Instalar também recupera os originais antes de uma nova tentativa."
        });
        install.Click += async (_, _) => await Run(engine => engine.Install(Payload()));
        restore.Click += async (_, _) => await Run(engine => engine.Restore());
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; MessageBox.Show(this, "Aguarde o término da operação para fechar."); } };
    }

    static byte[] Payload()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("PTBR.bundle")
            ?? throw new InvalidDataException("Payload incorporado não encontrado.");
        using var memory = new MemoryStream(); resource.CopyTo(memory); return memory.ToArray();
    }

    async Task Run(Func<Installer, string> operation)
    {
        if (string.IsNullOrWhiteSpace(folder.Text)) { output.Text = "Selecione a pasta raiz do jogo primeiro."; return; }
        busy = true;
        foreach (var button in new[] { select, verify, install, restore }) button.Enabled = false;
        string selected = folder.Text;
        output.Text = "Verificando arquivos. Aguarde...";
        try { output.Text = await Task.Run(() => operation(new Installer(selected))); }
        catch (Exception ex) { output.Text = ex.Message; }
        finally
        {
            busy = false;
            foreach (var button in new[] { select, verify, install, restore }) button.Enabled = true;
        }
    }
}
