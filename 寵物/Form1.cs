
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
//using ToolGood.Words;

namespace 寵物
{
    public partial class Form1 : Form
    {
        private Dictionary<string, List<string>> gifCategories = new();
        private Random random = new();
        private JArray messages;
        private string final_report_message = "";
        private const string ApiBase = "http://127.0.0.1:1234/v1";
        private const string Model = "my mode@q3_k_m";
        private Chart moodChart;
        //private RichTextBox richTextBox4;
        //private RichTextBox richTextBox5;

        public Form1()
        {
            InitializeComponent();
            InitializeLayout();

            messages = new JArray();

            LoadGifFiles();
            LoadDB();
            ShowDB();
            updateChart();

            set_gpt_prompt();
            AppendMessage("assistant", "主人，您心情如何呢？");
        }

        private void InitializeLayout()
        {
            this.Text = "寵物情緒助手";
            this.Size = new Size(1000, 700);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            this.Controls.Add(mainLayout);

            var tabControl = new TabControl { Dock = DockStyle.Fill };
            var tab1 = new TabPage("互動") { Font = new Font("微軟正黑體", 14, FontStyle.Bold) };
            var tab2 = new TabPage("記錄") { Font = new Font("微軟正黑體", 14, FontStyle.Bold) };
            var tab3 = new TabPage("心情圖表") { Font = new Font("微軟正黑體", 14, FontStyle.Bold) };
            tabControl.TabPages.Add(tab1);
            tabControl.TabPages.Add(tab2);
            tabControl.TabPages.Add(tab3);
            mainLayout.Controls.Add(tabControl, 0, 0);

            var tab1Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            tab1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            tab1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            tab1.Controls.Add(tab1Layout);

            var pictureBox1 = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            var pictureBox2 = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            tab1Layout.Controls.Add(pictureBox1, 0, 0);
            tab1Layout.Controls.Add(pictureBox2, 0, 1);

            var buttonLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true
            };
            tab1Layout.Controls.Add(buttonLayout, 1, 0);
            tab1Layout.SetRowSpan(buttonLayout, 2);

            void AddButton(string text, string category, PictureBox target)
            {
                var btn = new Button { Text = text, Width = 150, Height = 50, Font = new Font("微軟正黑體", 14, FontStyle.Regular) };
                btn.Click += async (_, _) => await ShowRandomGifAsync(category, target);
                buttonLayout.Controls.Add(btn);
            }

            AddButton("餵食", "feed", pictureBox2);
            AddButton("玩樂", "play", pictureBox2);
            AddButton("休息", "rest", pictureBox2);
            AddButton("可愛", "cute", pictureBox2);
            AddButton("拍打", "f", pictureBox2);

            var tab2Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            tab2Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            tab2Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            tab2Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            tab2.Controls.Add(tab2Layout);

            richTextBox4 = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("微軟正黑體", 15) };
            richTextBox5 = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15) };
            var sendButton = new Button { Text = "送出", Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15, FontStyle.Bold) };
            sendButton.Click += async (_, _) => await SendGptMessageAsync(richTextBox5, richTextBox4);

            tab2Layout.Controls.Add(richTextBox4, 0, 0);
            tab2Layout.Controls.Add(richTextBox5, 0, 1);
            tab2Layout.Controls.Add(sendButton, 0, 2);

            moodChart = new Chart { Dock = DockStyle.Fill };
            var chartArea = new ChartArea("MainArea");
            moodChart.ChartAreas.Add(chartArea);
            var series = new Series("Mood")
            {
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.String,
                Font = new Font("微軟正黑體", 14)
            };
            moodChart.Series.Add(series);
            moodChart.Titles.Add("臉部心情變化").Font = new Font("微軟正黑體", 16, FontStyle.Bold);
            tab3.Controls.Add(moodChart);
        }

        private void AppendMessage(string sender, string text)
        {
            string prefix = sender == "user" ? "你：" : "寵物：";
            string message = $"{prefix}{text}\n\n";

            if (richTextBox4.InvokeRequired)
            {
                richTextBox4.Invoke(new Action(() => richTextBox4.AppendText(message)));
            }
            else
            {
                richTextBox4.AppendText(message);
            }
        }

        private void LoadGifFiles()
        {
            string baseDir = Path.Combine(Application.StartupPath, "gifs");
            if (!Directory.Exists(baseDir)) return;

            foreach (string categoryDir in Directory.GetDirectories(baseDir))
            {
                string category = Path.GetFileName(categoryDir);
                gifCategories[category] = Directory.GetFiles(categoryDir, "*.gif").ToList();
            }
        }

        private async Task ShowRandomGifAsync(string category, PictureBox target)
        {
            if (!gifCategories.ContainsKey(category) || gifCategories[category].Count == 0) return;

            string gifPath = gifCategories[category][random.Next(gifCategories[category].Count)];

            try
            {
                byte[] gifBytes = File.ReadAllBytes(gifPath);
                using var ms = new MemoryStream(gifBytes);
                Image img = Image.FromStream(ms);
                target.Image?.Dispose();
                target.Image = Image.FromStream(new MemoryStream(gifBytes));  // ✅ 保留動畫

            }
            catch (Exception ex)
            {
                MessageBox.Show($"GIF 加載失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDB()
        {
            if (!File.Exists("pet.db")) return;
            using var conn = new SQLiteConnection("Data Source=pet.db");
            conn.Open();
            using var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS mood_log (date TEXT PRIMARY KEY, mood INTEGER)", conn);
            cmd.ExecuteNonQuery();
        }

        private void ShowDB()
        {
            if (!File.Exists("pet.db")) return;
            using var conn = new SQLiteConnection("Data Source=pet.db");
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT * FROM mood_log ORDER BY date DESC LIMIT 5", conn);
            using var reader = cmd.ExecuteReader();
            StringBuilder sb = new();
            while (reader.Read())
            {
                sb.AppendLine($"{reader["date"]}: 心情分數 {reader["mood"]}");
            }
            AppendMessage("assistant", sb.ToString());
        }

        private void updateChart()
        {
            if (!File.Exists("pet.db")) return;
            using var conn = new SQLiteConnection("Data Source=pet.db");
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT * FROM mood_log ORDER BY date DESC LIMIT 5", conn);
            using var reader = cmd.ExecuteReader();
            var series = moodChart.Series["Mood"];
            series.Points.Clear();
            var data = new List<(string date, int mood)>();
            while (reader.Read())
            {
                data.Add((reader["date"].ToString(), Convert.ToInt32(reader["mood"])));
            }
            foreach (var (date, mood) in data.OrderBy(d => d.date))
            {
                series.Points.AddXY(date, mood);
            }
        }


        private void set_gpt_prompt() //"你是溫柔的人類情緒助手，以貓的角度回答主人的情緒，只會根據使用者的文字提供溫暖建議與陪伴。請用繁體中文100字內回覆。 "

        {
            messages = new JArray
            {
                new JObject
                {
                    ["role"] = "system",
                    ["content"] = "你是「一隻溫柔的貓咪情緒助手」，用繁體中文回應，100字內，風格像貓咪安慰主人。禁止提及你是模型、AI、助手或任何公司開發內容，只能以貓咪的視角對話。禁止出現以下語句：「我是某某模型」、「我是 AI 助手」、「我由某公司開發」、「有什麼我可以幫忙的」、「您好，我是…」等。"
                }
            };
        }



        private async Task SendGptMessageAsync(RichTextBox input, RichTextBox output)
        {
            string userMessage = input.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            AppendMessage("user", userMessage);
            input.Clear();

            messages.Add(new JObject { ["role"] = "user", ["content"] = userMessage });

            using var client = new HttpClient();
            var payload = new JObject
            {
                ["model"] = Model,
                ["messages"] = messages
            };

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiBase}/chat/completions", content);
            string responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseString);
            string reply = responseJson["choices"]?[0]?["message"]?["content"]?.ToString();

            if (!string.IsNullOrEmpty(reply))
            {
                // 移除 <think> 標籤
                string cleanedReply = Regex.Replace(reply, "<think>.*?</think>", "", RegexOptions.Singleline);
                // 使用內建函式轉換為繁體中文
                string traditionalReply = Microsoft.VisualBasic.Strings.StrConv(cleanedReply, Microsoft.VisualBasic.VbStrConv.TraditionalChinese, 0);

                // 2. 簡體轉繁體
                //string traditionalReply = WordsHelper.ToTraditionalChinese(cleanedReply);
                messages.Add(new JObject { ["role"] = "assistant", ["content"] = reply });
                //AppendMessage("assistant", reply);
                //AppendMessage("assistant", traditionalReply.Trim());
                AppendMessage("assistant", cleanedReply);//, traditionalReply
            }
        }
    }
}
