using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace GT7CreditEditor
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--self-test")
            {
                return SaveCodec.SelfTest();
            }
            if (args.Length > 1 && args[0] == "--check")
            {
                try
                {
                    byte[] file = File.ReadAllBytes(args[1]);
                    int offset;
                    uint credits = SaveCodec.ReadCredits(file, out offset);
                    Console.WriteLine("OK CREDITS=" + credits + " OFFSET=0x" + offset.ToString("X"));
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("ERROR " + ex.Message);
                    return 1;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new EditorForm());
            return 0;
        }
    }

    internal static class SaveCodec
    {
        public const uint MaximumCredits = 100000000;
        private const int MoneyRelativeOffset = 0x172;
        private static readonly byte[] Progress = Encoding.ASCII.GetBytes("PROGRESS");
        private static readonly byte[] Quest = Encoding.ASCII.GetBytes("QUEST");

        private static int Find(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { match = false; break; }
                }
                if (match) { return i; }
            }
            return -1;
        }

        public static uint ReadCredits(byte[] file, out int offset)
        {
            if (file == null || file.Length < 512 || file.Length > 16777216)
                throw new InvalidDataException("Tamanho de save nao reconhecido.");

            int start = Find(file, Progress);
            offset = start + MoneyRelativeOffset;
            if (start < 0 || offset + 4 > file.Length)
                throw new InvalidDataException("Secao PROGRESS nao encontrada.");

            // Verified against two RyGT7 saves before and after a 14,300 Cr purchase.
            if (file[start + 32] != 0x9c || file[start + 33] != 0x2b ||
                file[start + 34] != 0x4a || file[start + 35] != 0xa9)
                throw new InvalidDataException("Versao do save nao reconhecida. Nenhuma alteracao feita.");

            int next = Find(file, Quest);
            if (next <= offset || Find(file, Encoding.ASCII.GetBytes("CARCOLIV")) < 0)
                throw new InvalidDataException("Estrutura interna do save nao reconhecida.");

            uint amount = ((uint)file[offset] << 24) |
                          ((uint)file[offset + 1] << 16) |
                          ((uint)file[offset + 2] << 8) | file[offset + 3];
            if (amount > MaximumCredits)
                throw new InvalidDataException("Saldo fora da faixa validada (0 a 100 milhoes).");
            return amount;
        }

        public static byte[] ChangeCredits(byte[] original, uint newAmount)
        {
            if (newAmount > MaximumCredits)
                throw new ArgumentOutOfRangeException("newAmount");
            int offset;
            ReadCredits(original, out offset);
            byte[] modified = (byte[])original.Clone();
            modified[offset] = (byte)(newAmount >> 24);
            modified[offset + 1] = (byte)(newAmount >> 16);
            modified[offset + 2] = (byte)(newAmount >> 8);
            modified[offset + 3] = (byte)newAmount;
            int checkOffset;
            if (ReadCredits(modified, out checkOffset) != newAmount || checkOffset != offset)
                throw new InvalidDataException("Validacao da edicao falhou.");
            return modified;
        }

        public static string Hash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }

        public static int SelfTest()
        {
            try
            {
                byte[] sample = new byte[1024];
                Buffer.BlockCopy(Encoding.ASCII.GetBytes("CARCOLIV"), 0, sample, 8, 8);
                int start = 96;
                Buffer.BlockCopy(Progress, 0, sample, start, Progress.Length);
                sample[start + 32] = 0x9c;
                sample[start + 33] = 0x2b;
                sample[start + 34] = 0x4a;
                sample[start + 35] = 0xa9;
                Buffer.BlockCopy(Quest, 0, sample, 600, Quest.Length);
                int money = start + MoneyRelativeOffset;
                sample[money + 2] = 0x14;
                sample[money + 3] = 0xe6;
                int readOffset;
                if (ReadCredits(sample, out readOffset) != 5350 || readOffset != money)
                    return 1;
                byte[] changed = ChangeCredits(sample, 100000);
                if (ReadCredits(changed, out readOffset) != 100000)
                    return 2;
                for (int i = 0; i < sample.Length; i++)
                    if ((i < money || i >= money + 4) && sample[i] != changed[i])
                        return 3;
                Console.WriteLine("SELFTEST_OK 5350 -> 100000; only credit bytes changed");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 9;
            }
        }
    }

    internal sealed class EditorForm : Form
    {
        private readonly Color bg = Color.FromArgb(15, 20, 28);
        private readonly Color panel = Color.FromArgb(27, 35, 46);
        private readonly Color muted = Color.FromArgb(159, 173, 189);
        private readonly Color cyan = Color.FromArgb(36, 207, 230);
        private readonly Color green = Color.FromArgb(94, 220, 145);
        private TextBox pathBox;
        private Label balanceLabel;
        private Label statusLabel;
        private NumericUpDown amountBox;
        private string loadedHash;
        private uint loadedCredits;
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public EditorForm()
        {
            Text = "GT7 | Editor de Creditos";
            ClientSize = new Size(525, 352);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = bg;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Label title = MakeLabel("GT7  /  CREDIT EDITOR", 24, 18, 470, 27, 16F, cyan, true);
            Controls.Add(title);
            Controls.Add(MakeLabel("RyGT7  |  Editor offline com backup automatico", 25, 49, 475, 22, 9F, muted, false));

            Controls.Add(MakeLabel("ARQUIVO DO SAVE", 25, 88, 350, 19, 9F, muted, true));
            pathBox = new TextBox();
            pathBox.SetBounds(25, 111, 390, 30);
            pathBox.ReadOnly = true;
            pathBox.BackColor = panel;
            pathBox.ForeColor = Color.White;
            pathBox.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(pathBox);
            Button browse = MakeButton("...", 425, 110, 76, 30, panel, Color.White);
            browse.Click += BrowseClick;
            Controls.Add(browse);

            Controls.Add(MakeLabel("SALDO ATUAL", 25, 159, 180, 19, 9F, muted, true));
            balanceLabel = MakeLabel("-- Cr", 25, 180, 230, 40, 22F, green, true);
            Controls.Add(balanceLabel);
            Button reload = MakeButton("RECARREGAR", 320, 181, 181, 33, panel, Color.White);
            reload.Click += delegate { Reload(); };
            Controls.Add(reload);

            Controls.Add(MakeLabel("NOVO SALDO (0 A 100 MILHOES)", 25, 232, 355, 20, 9F, muted, true));
            amountBox = new NumericUpDown();
            amountBox.SetBounds(25, 257, 235, 37);
            amountBox.Minimum = 0;
            amountBox.Maximum = SaveCodec.MaximumCredits;
            amountBox.Value = 100000;
            amountBox.Increment = 1000;
            amountBox.ThousandsSeparator = true;
            amountBox.BackColor = panel;
            amountBox.ForeColor = Color.White;
            amountBox.BorderStyle = BorderStyle.FixedSingle;
            amountBox.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            Controls.Add(amountBox);
            Button save = MakeButton("SALVAR CREDITOS", 272, 257, 229, 37, cyan, Color.FromArgb(8, 18, 24));
            save.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            save.Click += SaveClick;
            Controls.Add(save);

            statusLabel = MakeLabel("Selecione um save para comecar.", 25, 312, 474, 26, 9F, muted, false);
            Controls.Add(statusLabel);

            pathBox.Text = DetectSave();
            Reload();
        }

        private Label MakeLabel(string value, int x, int y, int w, int h, float size, Color color, bool bold)
        {
            Label label = new Label();
            label.Text = value;
            label.SetBounds(x, y, w, h);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            label.AutoEllipsis = true;
            return label;
        }

        private Button MakeButton(string label, int x, int y, int w, int h, Color color, Color text)
        {
            Button button = new Button();
            button.Text = label;
            button.SetBounds(x, y, w, h);
            button.BackColor = color;
            button.ForeColor = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static string DetectSave()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo parent = Directory.GetParent(baseDir.TrimEnd(Path.DirectorySeparatorChar));
            string[] candidates = {
                parent == null ? "" : Path.Combine(parent.FullName, "_sd", "gt7offline", "gt7save.bin"),
                Path.Combine(baseDir, "_sd", "gt7offline", "gt7save.bin"),
                @"C:\GAMES\GT7\_sd\gt7offline\gt7save.bin"
            };
            foreach (string path in candidates)
                if (File.Exists(path)) return path;
            return candidates[0];
        }

        private void BrowseClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Selecionar gt7save.bin";
                dialog.Filter = "Save GT7 (gt7save.bin)|gt7save.bin|Arquivos BIN (*.bin)|*.bin";
                dialog.FileName = "gt7save.bin";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    pathBox.Text = dialog.FileName;
                    Reload();
                }
            }
        }

        private void Reload()
        {
            loadedHash = null;
            balanceLabel.Text = "-- Cr";
            try
            {
                if (!String.Equals(Path.GetFileName(pathBox.Text), "gt7save.bin", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Escolha um arquivo chamado gt7save.bin.");
                byte[] data = File.ReadAllBytes(pathBox.Text);
                int offset;
                loadedCredits = SaveCodec.ReadCredits(data, out offset);
                loadedHash = SaveCodec.Hash(data);
                balanceLabel.Text = loadedCredits.ToString("N0", PtBr) + " Cr";
                amountBox.Value = loadedCredits;
                statusLabel.ForeColor = muted;
                statusLabel.Text = "Save validado. O jogo deve estar fechado para salvar.";
            }
            catch (Exception ex)
            {
                statusLabel.ForeColor = Color.FromArgb(241, 134, 134);
                statusLabel.Text = ex.Message;
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string sourceFile in Directory.GetFiles(source))
                File.Copy(sourceFile, Path.Combine(destination, Path.GetFileName(sourceFile)), false);
            foreach (string subdir in Directory.GetDirectories(source))
                CopyDirectory(subdir, Path.Combine(destination, Path.GetFileName(subdir)));
        }

        private static bool IsGameRunning()
        {
            return Process.GetProcessesByName("GT7").Length > 0 ||
                   Process.GetProcessesByName("RyGT7").Length > 0;
        }

        private void SaveClick(object sender, EventArgs e)
        {
            string stage = null;
            try
            {
                if (IsGameRunning())
                    throw new InvalidOperationException("Feche o GT7 antes de editar o save.");
                if (loadedHash == null)
                    throw new InvalidOperationException("Carregue um save valido primeiro.");

                string target = pathBox.Text;
                byte[] before = File.ReadAllBytes(target);
                if (!String.Equals(SaveCodec.Hash(before), loadedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("O save mudou. Clique em RECARREGAR primeiro.");
                int offset;
                uint actual = SaveCodec.ReadCredits(before, out offset);
                if (actual != loadedCredits)
                    throw new InvalidOperationException("Saldo mudou. Recarregue o save.");
                uint wanted = decimal.ToUInt32(amountBox.Value);
                if (wanted == actual)
                    throw new InvalidOperationException("O saldo ja tem esse valor.");

                string prompt = "Saldo atual: " + actual.ToString("N0", PtBr) + " Cr\n" +
                    "Novo saldo: " + wanted.ToString("N0", PtBr) + " Cr\n\n" +
                    "Criar backup e salvar agora?";
                if (MessageBox.Show(this, prompt, "Confirmar edicao", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes) return;

                if (IsGameRunning())
                    throw new InvalidOperationException("GT7 foi iniciado. Operacao cancelada.");
                string saveDir = Path.GetDirectoryName(target);
                DirectoryInfo parent = Directory.GetParent(saveDir);
                if (parent == null || parent.Parent == null)
                    throw new InvalidOperationException("Nao foi possivel definir a pasta de backup.");
                string backupRoot = Path.Combine(parent.Parent.FullName, "_SAVE_BACKUPS");
                Directory.CreateDirectory(backupRoot);
                string backupDir = Path.Combine(backupRoot,
                    "GT7CreditEditor_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
                CopyDirectory(saveDir, backupDir);
                byte[] backed = File.ReadAllBytes(Path.Combine(backupDir, Path.GetFileName(target)));
                if (SaveCodec.Hash(backed) != loadedHash)
                    throw new IOException("Backup diferente do original. Edicao cancelada.");

                byte[] after = SaveCodec.ChangeCredits(before, wanted);
                stage = Path.Combine(saveDir, ".gt7credits_" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllBytes(stage, after);
                if (SaveCodec.Hash(File.ReadAllBytes(target)) != loadedHash || IsGameRunning())
                    throw new InvalidOperationException("O jogo/save mudou durante a edicao. Cancelado.");

                string atomicBackup = Path.Combine(backupDir, "gt7save_before_atomic.bak");
                File.Replace(stage, target, atomicBackup, true);
                stage = null;

                byte[] verified = File.ReadAllBytes(target);
                int newOffset;
                if (SaveCodec.ReadCredits(verified, out newOffset) != wanted ||
                    newOffset != offset || verified.Length != before.Length)
                    throw new IOException("Falha na verificacao final. Restaure o backup.");
                for (int i = 0; i < before.Length; i++)
                {
                    if ((i < offset || i >= offset + 4) && verified[i] != before[i])
                        throw new IOException("Outro dado mudou inesperadamente. Restaure o backup.");
                }

                loadedHash = SaveCodec.Hash(verified);
                loadedCredits = wanted;
                balanceLabel.Text = wanted.ToString("N0", PtBr) + " Cr";
                statusLabel.ForeColor = green;
                statusLabel.Text = "Salvo! Backup: " + Path.GetFileName(backupDir);
                MessageBox.Show(this, "Saldo gravado: " + wanted.ToString("N0", PtBr) +
                    " Cr\n\nBackup completo:\n" + backupDir +
                    "\n\nAbra o GT7 para confirmar o saldo.", "Sucesso", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                statusLabel.ForeColor = Color.FromArgb(241, 134, 134);
                statusLabel.Text = "Erro: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Edicao nao concluida", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (stage != null && File.Exists(stage))
                {
                    try { File.Delete(stage); } catch { }
                }
            }
        }
    }
}
