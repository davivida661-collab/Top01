using System.Runtime.InteropServices;

static class Native
{
    [DllImport("Syntax.dll", CallingConvention = CallingConvention.StdCall)] public static extern bool Initialize();
    [DllImport("Syntax.dll", CallingConvention = CallingConvention.StdCall)] public static extern uint FindRobloxProcess();
    [DllImport("Syntax.dll", CallingConvention = CallingConvention.StdCall)] public static extern bool Connect(uint pid);
    [DllImport("Syntax.dll", CallingConvention = CallingConvention.StdCall)] public static extern void Disconnect();
    [DllImport("Syntax.dll", CallingConvention = CallingConvention.StdCall)] public static extern int ExecuteScript(string src, int len);
    [DllImport("Syntax.dll", CallingConvention = CallingConvention.StdCall)] public static extern int GetLastExecError(System.Text.StringBuilder buf, int len);
}

class SyntaxForm : Form
{
    TextBox editor = new() { Multiline = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 10), Dock = DockStyle.Fill };
    Label status = new() { Text = "Status: Desconectado", Dock = DockStyle.Bottom, Height = 24, ForeColor = Color.OrangeRed };
    Button btnAttach = new() { Text = "ATTACH", Width = 110, Height = 36, BackColor = Color.MediumPurple, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    Button btnExec = new() { Text = "EXECUTE", Width = 110, Height = 36, BackColor = Color.LimeGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

    public SyntaxForm()
    {
        Text = "Syntax Executor v1.1";
        Size = new Size(800, 550);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 22);

        var top = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(28, 28, 32) };
        var lbl = new Label { Text = "  SYNTAX", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(8, 10) };
        btnAttach.Location = new Point(550, 4); btnExec.Location = new Point(670, 4);
        btnAttach.Click += OnAttach; btnExec.Click += OnExec;
        top.Controls.AddRange([lbl, btnAttach, btnExec]);

        editor.Text = "print(\"Syntax pronto!\")\r\ngame.Players.LocalPlayer.Character.Humanoid.WalkSpeed = 50";
        Controls.Add(editor); Controls.Add(top); Controls.Add(status);

        try { Native.Initialize(); } catch { status.Text = "Status: Syntax.dll não encontrada (coloque ao lado do .exe)"; }
    }

    void OnAttach(object? _, EventArgs __)
    {
        try
        {
            uint pid = Native.FindRobloxProcess();
            if (pid == 0) { status.Text = "Status: Roblox não encontrado"; return; }
            status.Text = $"Status: Conectando PID {pid}...";
            if (Native.Connect(pid)) { status.Text = $"Status: Conectado! PID {pid}"; status.ForeColor = Color.Lime; }
            else status.Text = "Status: Falha ao conectar";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }
    void OnExec(object? _, EventArgs __)
    {
        try
        {
            int r = Native.ExecuteScript(editor.Text, editor.Text.Length);
            if (r != 0) { var sb = new System.Text.StringBuilder(1024); Native.GetLastExecError(sb, sb.Capacity); MessageBox.Show($"Erro {r}: {sb}"); }
            else status.Text = "Status: Script executado!";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }
}

static class Program
{
    [STAThread] static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new SyntaxForm()); }
}
