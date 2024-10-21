using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace VerthashManager
{
    public partial class XmlEditorForm : Form
    {
        private string configFileName;
        private FastColoredTextBox fastColoredTextBox;
        public XmlEditorForm(string fileName)
        {
            InitializeComponent();
            
            configFileName = fileName;
            fastColoredTextBox = new FastColoredTextBox();
            fastColoredTextBox.Language = Language.XML;
            fastColoredTextBox.Dock = DockStyle.Fill;

            fastColoredTextBox.TextChanged += FastColoredTextBox_TextChanged;
            fastColoredTextBox.Text = File.ReadAllText(fileName);
            saveToolStripButton.Enabled = false;

            this.Controls.Add(fastColoredTextBox);
            this.Controls.SetChildIndex(fastColoredTextBox, 0);
        }

        private void FastColoredTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            saveToolStripButton.Enabled = true;
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            File.WriteAllText(configFileName, fastColoredTextBox.Text);
            saveToolStripButton.Enabled = false;
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            fastColoredTextBox.Cut();
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            fastColoredTextBox.Copy();
        }

        private void pastToolStripButton_Click(object sender, EventArgs e)
        {
            fastColoredTextBox.Paste();
        }
    }
}
