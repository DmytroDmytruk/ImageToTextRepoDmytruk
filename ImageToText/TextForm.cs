using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageToText
{
    public partial class TextForm : Form
    {
        public TextForm(string text)
        {
            InitializeComponent();
            richTextBox1.Text = text ?? string.Empty;
        }

        private void TextForm_Load(object sender, EventArgs e)
        {

        }
    }
}
