using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace FileSyncApp
{
    public partial class SyncForm : Form
    {
        private string _source = "";
        private string _target = "";
        private bool _useXml = true;

        public SyncForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Синхронизация файлов";
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 5 };

            var sourceLabel = new Label { Text = "Источник:" };
            var targetLabel = new Label { Text = "Цель:" };
            var formatLabel = new Label { Text = "Формат лога:" };

            var txtSource = new TextBox { ReadOnly = true, Width = 200 };
            var txtTarget = new TextBox { ReadOnly = true, Width = 200 };

            var btnSource = new Button { Text = "Выбрать" };
            var btnTarget = new Button { Text = "Выбрать" };
            var btnSync = new Button { Text = "Синхронизировать", Dock = DockStyle.Top };

            var rbXml = new RadioButton { Text = "XML", Checked = true };
            var rbJson = new RadioButton { Text = "JSON" };

            layout.Controls.Add(sourceLabel, 0, 0);
            layout.Controls.Add(txtSource, 1, 0);
            layout.Controls.Add(btnSource, 2, 0);

            layout.Controls.Add(targetLabel, 0, 1);
            layout.Controls.Add(txtTarget, 1, 1);
            layout.Controls.Add(btnTarget, 2, 1);

            layout.Controls.Add(formatLabel, 0, 2);
            layout.Controls.Add(rbXml, 1, 2);
            layout.Controls.Add(rbJson, 2, 2);

            layout.Controls.Add(btnSync, 1, 4);

            this.Controls.Add(layout);
            this.Width = 500;
            this.Height = 200;

            FolderBrowserDialog fbd = new FolderBrowserDialog();

            btnSource.Click += (s, e) =>
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtSource.Text = fbd.SelectedPath;
                    _source = txtSource.Text;
                }
            };

            btnTarget.Click += (s, e) =>
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtTarget.Text = fbd.SelectedPath;
                    _target = txtTarget.Text;
                }
            };

            btnSync.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_source) || string.IsNullOrEmpty(_target))
                {
                    MessageBox.Show("Выберите обе папки.");
                    return;
                }

                _useXml = rbXml.Checked;
                SyncFolders(_source, _target, _useXml);
                MessageBox.Show("Синхронизация завершена.");
            };
        }

        private void SyncFolders(string source, string target, bool useXml)
        {
            var log = new List<FileLogEntry>();

            if (!Directory.Exists(target)) Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
            {
                string name = Path.GetFileName(file);
                string destFile = Path.Combine(target, name);

                if (!File.Exists(destFile))
                {
                    File.Copy(file, destFile);
                    log.Add(new FileLogEntry { FilePath = name, Action = "Created", Timestamp = DateTime.Now });
                }
                else if (new FileInfo(file).LastWriteTime > new FileInfo(destFile).LastWriteTime)
                {
                    File.Copy(file, destFile, true);
                    log.Add(new FileLogEntry { FilePath = name, Action = "Modified", Timestamp = DateTime.Now });
                }
            }

            foreach (var file in Directory.GetFiles(target))
            {
                string name = Path.GetFileName(file);
                string srcFile = Path.Combine(source, name);
                if (!File.Exists(srcFile))
                {
                    File.Delete(file);
                    log.Add(new FileLogEntry { FilePath = name, Action = "Deleted", Timestamp = DateTime.Now });
                }
            }

            if (useXml)
                SaveXml(log);
            else
                SaveJson(log);
        }

        private void SaveXml(List<FileLogEntry> log)
        {
            var xs = new XmlSerializer(typeof(List<FileLogEntry>));
            using (var writer = new StreamWriter("sync_log.xml"))
                xs.Serialize(writer, log);
        }

        private void SaveJson(List<FileLogEntry> log)
        {
            string json = JsonConvert.SerializeObject(log, Formatting.Indented);
            File.WriteAllText("sync_log.json", json);
        }

        [Serializable]
        public class FileLogEntry
        {
            public string FilePath { get; set; }
            public string Action { get; set; }
            public DateTime Timestamp { get; set; }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new SyncForm());
        }
    }
}
