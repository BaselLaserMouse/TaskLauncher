using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace TaskLauncher
{
    public partial class Form1 : Form
    {
        private Label? _leftLabel;
        private Label? _rightLabel;
        private PictureBox? _leftIcon;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Task Launcher";   // window title
            Load += Form1_Load;
        }

        // ---------------- JSON schema ----------------
        public class AppConfig
        {
            [JsonPropertyName("buttons")]
            public List<ButtonSpec> Buttons { get; set; } = new();

            [JsonPropertyName("header")]
            public HeaderSpec? Header { get; set; } = new HeaderSpec();
        }

        public class ButtonSpec
        {
            public string Text { get; set; } = "Launch";
            public string ExePath { get; set; } = "";

            // Users can provide either "args" (string) or "argsList" (array). Both optional.
            public string? Args { get; set; } = null;
            public List<string>? ArgsList { get; set; } = null;

            public bool RunAsAdmin { get; set; } = false;

            // Layout (basic). X is ignored because we center buttons.
            public int X { get; set; } = 30;
            public int Y { get; set; } = -1;     // -1 = auto stack
            public int Width { get; set; } = 220;
            public int Height { get; set; } = 40;

            public string WorkingDirectory { get; set; } = "";
        }

        public class HeaderSpec
        {
            public string Left { get; set; } = "My BIG Left Title";
            public string Right { get; set; } = "small right text";
            public bool Show { get; set; } = true;

            // Header row placement
            public int Y { get; set; } = 30;       // vertical position of the header row
            public int MarginX { get; set; } = 20; // padding from left/right edges

            // Fonts: left is large (~36pt to match 64px icon); right is configurable
            public float LeftFontSize { get; set; } = 36f;
            public float RightFontSize { get; set; } = 9f;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string cfgPath = Path.Combine(baseDir, "config.json");

                // Set window/taskbar icon from the application icon
                Icon? appIcon = null;
                try
                {
                    appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    if (appIcon != null) this.Icon = appIcon;
                }
                catch { /* ignore */ }

                // Create a sample config if missing
                if (!File.Exists(cfgPath))
                {
                    File.WriteAllText(cfgPath, SampleConfigJson(baseDir));
                    MessageBox.Show(
                        "No config.json found. A starter file was created next to the EXE.\n" +
                        "Edit it and restart the app.",
                        "Starter config created",
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    WriteIndented = true
                };

                var json = File.ReadAllText(cfgPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();

                if (config.Buttons.Count == 0)
                {
                    MessageBox.Show("config.json has no buttons. Add at least one button entry.", "No buttons",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ---------------- Header (64x64 icon + big left text + small right text) ----------------
                int headerBottom = 0;
                if (config.Header?.Show == true)
                {
                    int marginX = Math.Max(0, config.Header.MarginX);
                    int rowTop = Math.Max(0, config.Header.Y);

                    // Helper: fetch a crisp 64×64 bitmap from the app icon
                    Bitmap GetAppIconBitmap64()
                    {
                        var ico = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                        if (ico != null)
                        {
                            using (var icon64 = new Icon(ico, new Size(64, 64)))
                            {
                                return icon64.ToBitmap();
                            }
                        }
                        return new Bitmap(64, 64);
                    }

                    const int iconSize = 64;

                    var leftFont = new Font(
                        FontFamily.GenericSansSerif,
                        config.Header.LeftFontSize > 0 ? config.Header.LeftFontSize : 36f,
                        FontStyle.Bold
                    );
                    var rightFont = new Font(
                        FontFamily.GenericSansSerif,
                        config.Header.RightFontSize > 0 ? config.Header.RightFontSize : 9f,
                        FontStyle.Regular
                    );

                    string leftText = config.Header.Left ?? string.Empty;
                    string rightText = config.Header.Right ?? string.Empty;

                    // Measure text
                    var leftSize = TextRenderer.MeasureText(leftText, leftFont);
                    var rightSize = TextRenderer.MeasureText(rightText, rightFont);

                    // Place LEFT LABEL first
                    int gap = 10;
                    int leftTextX = marginX + iconSize + gap;
                    int leftTextY = rowTop;
                    _leftLabel = new Label
                    {
                        AutoSize = true,
                        Text = leftText,
                        Font = leftFont,
                        Location = new Point(leftTextX, leftTextY)
                    };
                    Controls.Add(_leftLabel);

                    // Place ICON centered to left label
                    int iconY = _leftLabel.Top + (_leftLabel.Height - iconSize) / 2;
                    _leftIcon = new PictureBox
                    {
                        Image = GetAppIconBitmap64(),
                        SizeMode = PictureBoxSizeMode.CenterImage,
                        Size = new Size(iconSize, iconSize),
                        Location = new Point(marginX, iconY)
                    };
                    Controls.Add(_leftIcon);

                    // Place RIGHT LABEL pinned to right
                    int rowHeight = Math.Max(iconSize, _leftLabel.Height);
                    int rightTextY = rowTop + (rowHeight - rightSize.Height) / 2;
                    _rightLabel = new Label
                    {
                        AutoSize = true,
                        Text = rightText,
                        Font = rightFont,
                        Location = new Point(this.ClientSize.Width - rightSize.Width - marginX, rightTextY)
                    };
                    Controls.Add(_rightLabel);

                    int labelsBottom = Math.Max(_leftLabel.Bottom, _rightLabel.Bottom);
                    headerBottom = Math.Max(_leftIcon.Bottom, labelsBottom);

                    // Keep right label pinned and buttons centered on resize
                    this.Resize += (s, eArgs) =>
                    {
                        if (_rightLabel != null)
                            _rightLabel.Left = this.ClientSize.Width - _rightLabel.Width - marginX;

                        foreach (Control c in this.Controls)
                            if (c is Button b)
                                b.Left = (this.ClientSize.Width - b.Width) / 2;
                    };
                }

                // ---------------- Buttons centered below header ----------------
                int autoY = (headerBottom > 0 ? headerBottom + 15 : 90);

                foreach (var spec in config.Buttons)
                {
                    var btn = new Button
                    {
                        Text = string.IsNullOrWhiteSpace(spec.Text) ? "Launch" : spec.Text,
                        AutoSize = spec.Width <= 0 || spec.Height <= 0,
                        Width = spec.Width > 0 ? spec.Width : 220,
                        Height = spec.Height > 0 ? spec.Height : 40,
                        Location = new Point(0, spec.Y >= 0 ? spec.Y : autoY)
                    };

                    btn.Left = (this.ClientSize.Width - btn.Width) / 2;
                    btn.Click += (s, _) => RunExe(spec);
                    Controls.Add(btn);

                    autoY += (spec.Height > 0 ? spec.Height : btn.Height) + 12;
                }

                // ---------------- Resize window to fit all content ----------------
                int maxBottom = 0;
                foreach (Control c in this.Controls)
                    if (c.Bottom > maxBottom) maxBottom = c.Bottom;

                int padding = 30;
                this.ClientSize = new Size(this.ClientSize.Width, maxBottom + padding);
            }
            catch (JsonException jx)
            {
                MessageBox.Show("Invalid JSON in config.json:\n\n" + jx.Message,
                    "JSON Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load configuration:\n\n" + ex,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Build the final argument string from either ArgsList or Args
        private static string BuildArguments(ButtonSpec spec)
        {
            if (spec.ArgsList != null && spec.ArgsList.Count > 0)
            {
                IEnumerable<string> quoted = spec.ArgsList.Select(token =>
                {
                    if (string.IsNullOrEmpty(token)) return "\"\"";
                    bool needsQuotes = token.Any(ch => char.IsWhiteSpace(ch) || ch == '\"');
                    if (!needsQuotes) return token;
                    string escaped = token.Replace("\"", "\\\"");
                    return $"\"{escaped}\"";
                });
                return string.Join(" ", quoted);
            }
            return spec.Args?.Trim() ?? string.Empty;
        }

        private void RunExe(ButtonSpec spec)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(spec.ExePath) || !File.Exists(spec.ExePath))
                {
                    MessageBox.Show($"File not found:\n{spec.ExePath}", "Launch Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = spec.ExePath,
                    Arguments = BuildArguments(spec),
                    UseShellExecute = true,
                    WorkingDirectory = !string.IsNullOrWhiteSpace(spec.WorkingDirectory)
                        ? spec.WorkingDirectory
                        : (Path.GetDirectoryName(spec.ExePath) ?? Environment.CurrentDirectory)
                };

                if (spec.RunAsAdmin) psi.Verb = "runas";

                var p = Process.Start(psi);
                if (p != null)
                {
                    // Exit launcher after successful start
                    this.Close();
                    Application.Exit();
                }
            }
            catch (System.ComponentModel.Win32Exception w32) when (w32.NativeErrorCode == 1223)
            {
                // User canceled UAC → keep launcher open
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch:\n{spec.ExePath}\n\n{ex.Message}",
                    "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Creates a starter config.json if missing
        private static string SampleConfigJson(string baseDir) =>
$@"{{
  // Edit this file without recompiling. Restart the app to see changes.
  ""header"": {{
    ""show"": true,
    ""left"": ""My BIG Left Title"",
    ""right"": ""small right text"",
    ""y"": 30,
    ""marginX"": 20,
    ""leftFontSize"": 36,
    ""rightFontSize"": 9
  }},
  ""buttons"": [
    {{
      ""text"": ""App 1 (no args)"",
      ""exePath"": ""C:\\\\Path\\\\To\\\\App1\\\\app1.exe"",
      ""y"": -1, ""width"": 220, ""height"": 40
    }},
    {{
      ""text"": ""App 2 (string args)"",
      ""exePath"": ""C:\\\\Path\\\\To\\\\App2\\\\app2.exe"",
      ""args"": ""--flag1 value --toggle"",
      ""y"": -1, ""width"": 220, ""height"": 40
    }},
    {{
      ""text"": ""App 3 (args list)"",
      ""exePath"": ""C:\\\\Path\\\\To\\\\App3\\\\app3.exe"",
      ""argsList"": [""--port"", ""8080"", ""--mode"", ""safe value with spaces""],
      ""runAsAdmin"": false,
      ""y"": -1, ""width"": 220, ""height"": 40
    }}
  ]
}}";
    }
}
