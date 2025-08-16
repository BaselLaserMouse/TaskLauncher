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
    public partial class GUI : Form
    {
        private Label? _leftLabel;
        private Label? _rightLabel;
        private PictureBox? _leftIcon;

        public GUI()
        {
            InitializeComponent();
            this.Text = "Task Launcher"; // window title
            Load += GUI_Load;
        }

        // ---------- JSON schema ----------
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

            // Users can supply either args (string) OR argsList (array). Both optional.
            public string? Args { get; set; } = null;
            public List<string>? ArgsList { get; set; } = null;

            public bool RunAsAdmin { get; set; } = false;

            // Layout (basic)
            public int X { get; set; } = 30;     // ignored when centering
            public int Y { get; set; } = -1;     // -1 = auto stack
            public int Width { get; set; } = 180;
            public int Height { get; set; } = 35;

            public string WorkingDirectory { get; set; } = "";
        }

        public class HeaderSpec
        {
            public string Left { get; set; } = "My Bold Title";
            public string Right { get; set; } = "small note";
            public bool LeftBold { get; set; } = true;
            public float LeftFontSize { get; set; } = 16f;
            public float RightFontSize { get; set; } = 9f;

            // Positioning for the header row
            public int Y { get; set; } = 30;          // vertical position of the header row
            public int MarginX { get; set; } = 20;    // side padding from left/right edges
            public bool Show { get; set; } = true;

            // Icon drawn to the LEFT of the left text
            public int IconSize { get; set; } = 124;  // you asked for 124x124
            public int IconTextGap { get; set; } = 10; // space between icon and left text
        }

        private void GUI_Load(object? sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string cfgPath = Path.Combine(baseDir, "config.json");

                // Set the form/taskbar icon from the application icon
                Icon? appIcon = null;
                try
                {
                    appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    if (appIcon != null) this.Icon = appIcon;
                }
                catch { /* ignore */ }

                // Create sample config if missing
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

                // ---------------- Header row (icon + left bold text + right small text) ----------------
                int headerBottom = 0; // we'll compute the bottom of the header to place buttons underneath

                if (config.Header?.Show == true)
                {
                    int marginX = Math.Max(0, config.Header.MarginX);
                    int rowTop = Math.Max(0, config.Header.Y);

                    // Prepare fonts
                    var leftFont = new Font(FontFamily.GenericSansSerif,
                                             config.Header.LeftFontSize > 0 ? config.Header.LeftFontSize : 16f,
                                             config.Header.LeftBold ? FontStyle.Bold : FontStyle.Regular);
                    var rightFont = new Font(FontFamily.GenericSansSerif,
                                             config.Header.RightFontSize > 0 ? config.Header.RightFontSize : 9f,
                                             FontStyle.Regular);

                    // Measure text (for vertical centering)
                    var leftText = config.Header.Left ?? string.Empty;
                    var rightText = config.Header.Right ?? string.Empty;

                    var leftSize = TextRenderer.MeasureText(leftText, leftFont);
                    var rightSize = TextRenderer.MeasureText(rightText, rightFont);

                    int iconSize = Math.Max(0, config.Header.IconSize);
                    int rowHeight = Math.Max(iconSize, Math.Max(leftSize.Height, rightSize.Height));
                    if (rowHeight == 0) rowHeight = Math.Max(leftSize.Height, rightSize.Height);

                    // Left icon (optional; only if we have an app icon)
                    if (appIcon != null && iconSize > 0)
                    {
                        _leftIcon = new PictureBox
                        {
                            Image = appIcon.ToBitmap(),
                            SizeMode = PictureBoxSizeMode.StretchImage,
                            Size = new Size(iconSize, iconSize),
                            Location = new Point(marginX, rowTop)
                        };
                        Controls.Add(_leftIcon);
                    }

                    // Left label: to the right of the icon (or at marginX if no icon), vertically centered
                    int leftTextX = marginX + ((appIcon != null && iconSize > 0) ? (iconSize + config.Header.IconTextGap) : 0);
                    int leftTextY = rowTop + (rowHeight - leftSize.Height) / 2;

                    _leftLabel = new Label
                    {
                        AutoSize = true,
                        Text = leftText,
                        Font = leftFont,
                        Location = new Point(leftTextX, leftTextY)
                    };
                    Controls.Add(_leftLabel);

                    // Right label: align to right edge, vertically centered to the same row
                    int rightTextY = rowTop + (rowHeight - rightSize.Height) / 2;

                    _rightLabel = new Label
                    {
                        AutoSize = true,
                        Text = rightText,
                        Font = rightFont
                    };
                    _rightLabel.Location = new Point(this.ClientSize.Width - _rightLabel.Width - marginX, rightTextY);
                    Controls.Add(_rightLabel);

                    // Compute bottom of the header row so we can place buttons underneath
                    int iconBottom = _leftIcon?.Bottom ?? 0;
                    int labelsBottom = Math.Max(_leftLabel.Bottom, _rightLabel.Bottom);
                    headerBottom = Math.Max(iconBottom, labelsBottom);

                    // Keep right label stuck to right edge on resize
                    this.Resize += (s, eArgs) =>
                    {
                        if (_rightLabel != null)
                            _rightLabel.Left = this.ClientSize.Width - _rightLabel.Width - marginX;

                        // Keep buttons centered when window resizes
                        foreach (Control c in this.Controls)
                        {
                            if (c is Button b)
                                b.Left = (this.ClientSize.Width - b.Width) / 2;
                        }
                    };
                }

                // ---------------- Buttons (centered horizontally, stacked vertically) ----------------
                // Start below the header (or at a safe default if header is hidden)
                int startY = headerBottom > 0 ? headerBottom + 15 : 90;
                int autoY = startY;

                foreach (var spec in config.Buttons)
                {
                    var btn = new Button
                    {
                        Text = string.IsNullOrWhiteSpace(spec.Text) ? "Launch" : spec.Text,
                        AutoSize = spec.Width <= 0 || spec.Height <= 0,
                        Width = spec.Width > 0 ? spec.Width : 180,
                        Height = spec.Height > 0 ? spec.Height : 35,
                        Location = new Point(0, spec.Y >= 0 ? spec.Y : autoY)
                    };

                    // Center horizontally in the current client area
                    btn.Left = (this.ClientSize.Width - btn.Width) / 2;

                    btn.Click += (s, _) => RunExe(spec);
                    Controls.Add(btn);

                    // Advance Y for next button
                    autoY += (spec.Height > 0 ? spec.Height : btn.Height) + 12;
                }
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

                if (spec.RunAsAdmin) psi.Verb = "runas"; // UAC prompt

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


        // Sample config created next to the EXE if missing
        private static string SampleConfigJson(string baseDir) =>
$@"{{
  // Edit this file without recompiling. Restart the app to see changes.
  ""header"": {{
    ""show"": true,
    ""left"": ""My Bold Title"",
    ""right"": ""small note"",
    ""leftBold"": true,
    ""leftFontSize"": 16,
    ""rightFontSize"": 9,
    ""y"": 30,
    ""marginX"": 20,
    ""iconSize"": 124,
    ""iconTextGap"": 10
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
