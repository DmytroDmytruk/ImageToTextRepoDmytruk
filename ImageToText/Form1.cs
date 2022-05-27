using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace ImageToText
{
    public partial class Form1 : Form
    {
        Bitmap loadedImage;
        List<Bitmap> loadedImageList = new List<Bitmap>();
        List<string> textList = new List<string>();
        List<string> textNames = new List<string>();

        int maxQuantizationLevel;
        int currentQuantizationLevel = 16;

        public enum Directions { Horizontal = 'H', Vertical = 'V' };
        public enum ColorScheme { Red = 'R', Green = 'G', Blue = 'B', Monochrome = 'M'};
        public struct Dimensions
        {
            public int Width { get; set; }
            public int Height { get; set; }
        };
        public struct FileNameParsedData
        {
            public string Name { get; set; }
            public Directions Direction { get; set; }
            public ColorScheme ColorScheme { get; set; }
            public Dimensions Dimensions { get; set; }
            public int QuantizationLevels { get; set; }
        };

        string text;

        bool multiple;

        public Form1()
        {
            InitializeComponent();

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            CalculateMaxQuantizationLevel();
        }
        private void loadImage(string filename)
        {
            loadedImage = new Bitmap(filename);

            pictureBox1.Image = loadedImage;
            enableBuildTextButton();
            label6.Text = Path.GetFileNameWithoutExtension(filename);
        }
        private FileNameParsedData parseFileName(string filename)
        {
            // FileName_H/V_WxH.txt
            // FileName_H/V_R/G/B/M_WxH.txt
            string pattern = @"(?<name>\w+)_(?<direction>H|V)_(?<color>[RGBM]{1})_(?<bits>\d+)_(?<width>\d+)x{1}(?<height>\d+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(filename);

            FileNameParsedData parsedData = new FileNameParsedData();
            parsedData.Name = match.Groups["name"].Value;
            parsedData.Direction = (Directions)Convert.ToChar(match.Groups["direction"].Value);
            parsedData.ColorScheme = (ColorScheme)Convert.ToChar(match.Groups["color"].Value);

            Dimensions parsedDimensions = new Dimensions();
            parsedDimensions.Width = Convert.ToInt32(match.Groups["width"].Value);
            parsedDimensions.Height = Convert.ToInt32(match.Groups["height"].Value);
            parsedData.Dimensions = parsedDimensions;
            parsedData.QuantizationLevels = Convert.ToInt32(Math.Log(Convert.ToDouble(match.Groups["bits"].Value), 2));

            return parsedData;
        }
        private void loadText(string filename)
        {
            FileNameParsedData fileNameData = parseFileName(filename);

            int[,] pixelValues = new int[fileNameData.Dimensions.Width, fileNameData.Dimensions.Height];
            string[] allLines = File.ReadAllLines(filename);
            string[] imageDataLines = new string[1];

            for (int i = 0; i < allLines.Length; i++)
            {
                imageDataLines[0] += allLines[i];
            }

            int[] pixelValuesFromFile = Array.ConvertAll(imageDataLines[0].Split(' '), str => Convert.ToInt32(str));
            int counter = 0;

            for (int i = 0; i < fileNameData.Dimensions.Width; i++)
            {
                for (int j = 0; j < fileNameData.Dimensions.Height; j++)
                {
                    pixelValues[i, j] = pixelValuesFromFile[counter++];
                }
            }

            Bitmap bitmap = new Bitmap(fileNameData.Dimensions.Width, fileNameData.Dimensions.Height);

            for (int x = 0; x < fileNameData.Dimensions.Width; x++)
            {
                for (int y = 0; y < fileNameData.Dimensions.Height; y++)
                {
                    Color color;
                    int value = 
                        (int)Math.Floor((double)(pixelValues[x, y] * maxQuantizationLevel / currentQuantizationLevel)) % 255;

                    switch (fileNameData.ColorScheme)
                    {
                        case ColorScheme.Red:
                            color = Color.FromArgb(value, 0, 0);
                            break;
                        case ColorScheme.Green:
                            color = Color.FromArgb(0, value, 0);
                            break;
                        case ColorScheme.Blue:
                            color = Color.FromArgb(0, 0, value);
                            break;
                        case ColorScheme.Monochrome:
                            color = Color.FromArgb(value, value, value);
                            break;
                        default:
                            color = Color.FromArgb(value, value, value);
                            break;
                    }

                    bitmap.SetPixel(x, y, color);
                }
            }

            pictureBox1.Image = bitmap;

            int colorSchemeIndex = 0;
            switch (fileNameData.ColorScheme)
            {
                case ColorScheme.Red:
                    colorSchemeIndex = 0;
                    break;
                case ColorScheme.Green:
                    colorSchemeIndex = 1;
                    break;
                case ColorScheme.Blue:
                    colorSchemeIndex = 2;
                    break;
                case ColorScheme.Monochrome:
                    colorSchemeIndex = 3;
                    break;
            }
            comboBox1.SelectedIndex = colorSchemeIndex; 
            comboBox2.SelectedIndex = fileNameData.Direction == Directions.Horizontal ? 0 : 1;
            numericUpDown1.Value = fileNameData.QuantizationLevels;
            label6.Text = filename;
        }

        private int[] parsePixelRange(string rangeString)
        {
            string withoutWhitespaces = String.Concat(rangeString.Where(c => !Char.IsWhiteSpace(c)));
            List<int> rangeList = new List<int>();
            string[] ranges = withoutWhitespaces.Split(',');
            string rangePattern = @"^(\d+)-(\d+)$";
            string singularPattern = @"^\d+$";
            Regex rangeRegex = new Regex(rangePattern);
            Regex singularRegex = new Regex(singularPattern);

            foreach (string rangeStr in ranges)
            {
                if (rangeRegex.IsMatch(rangeStr))
                {
                    string[] boundaries = rangeStr.Split('-');
                    int start = Convert.ToInt32(boundaries[0]);
                    int finish = Convert.ToInt32(boundaries[1]);

                    for (int i = start; i <= finish; i++)
                    {
                        rangeList.Add(i - 1);
                    }
                } 
                else if (singularRegex.IsMatch(rangeStr))
                {
                    rangeList.Add(Convert.ToInt32(rangeStr) - 1);
                }
            }

            int[] resArr = rangeList.ToArray();
            Array.Sort(resArr);

            return resArr;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            ClearData();
            multiple = false;
            string imageFilesFilter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = imageFilesFilter;
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (openFileDialog.FileName != "")
                    {
                            loadImage(openFileDialog.FileName);
                    }
                }
            }

            groupBox1.Enabled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateMaxQuantizationLevel();
            CalculateCurrentQuantizationLevel();
        }

        private void CalculateMaxQuantizationLevel()
        {
            if (comboBox1.SelectedIndex == 1)
                maxQuantizationLevel = 256;
            else
            {
                maxQuantizationLevel = 256;
            }
        }

        private void CalculateCurrentQuantizationLevel()
        {
            int quantization_amount;
            if (int.TryParse(numericUpDown1.Value.ToString(), out quantization_amount))
            {
                currentQuantizationLevel = (int)Math.Round(Math.Pow(2, quantization_amount));
                if (currentQuantizationLevel > maxQuantizationLevel) currentQuantizationLevel = maxQuantizationLevel;
                label3.Text = "Max quantization value = " + currentQuantizationLevel.ToString();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            CalculateCurrentQuantizationLevel();
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            text = String.Join(" ", ReadImage(loadedImage));
            enableTextOptions();
        }

        string[] ReadImage(Bitmap loadedImage)
        {
            bool isHorizontal = comboBox2.SelectedIndex == 0;
            bool isSelectiveMode = radioButton2.Checked;
            int ii = loadedImage.Width;
            int jj = loadedImage.Height;
            int size = ii * jj;
            var char_text = new string[size];
            int counter = 0;

            if (isSelectiveMode && textBox2.Text.Length > 0)
            {
                int[] indexes = parsePixelRange(textBox2.Text);

                if (isHorizontal)
                {
                    size = indexes.Length * jj;
                    Array.Resize(ref char_text, size);

                    for (int i = 0; i < indexes.Length; i++)
                    {
                        for (int j = 0; j < jj; j++)
                        {
                            Color color = loadedImage.GetPixel(indexes[i], j);

                            char_text[counter++] = GetPixel(color.R, color.G, color.B, comboBox1.SelectedIndex);
                        }
                    }
                }
                else
                {
                    size = ii * indexes.Length;
                    Array.Resize(ref char_text, size);

                    for (int j = 0; j < indexes.Length; j++)
                    {
                        for (int i = 0; i < ii; i++)
                        {
                            Color color = loadedImage.GetPixel(i, indexes[j]);

                            char_text[counter++] = GetPixel(color.R, color.G, color.B, comboBox1.SelectedIndex);
                        }
                    }
                }
            } 
            else
            {
                size = ii * jj;
                Array.Resize(ref char_text, size);

                if (isHorizontal)
                {
                    for (int i = 0; i < ii; i++)
                    {
                        for (int j = 0; j < jj; j++)
                        {
                            Color color = loadedImage.GetPixel(i, j);

                            char_text[counter++] = GetPixel(color.R, color.G, color.B, comboBox1.SelectedIndex);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < jj; j++)
                    {
                        for (int i = 0; i < ii; i++)
                        {
                            Color color = loadedImage.GetPixel(i, j);

                            char_text[counter++] = GetPixel(color.R, color.G, color.B, comboBox1.SelectedIndex);
                        }
                    }
                }
            }

            return char_text;
        }

        private string GetPixel(byte r, byte g, byte b, int value)
        {
            double pixel_value = 0d;
            switch(value)
            {
                case 0:
                    {
                        pixel_value = r;
                        break;
                    }
                case 1:
                    {
                        pixel_value = g;
                        break;
                    }
                case 2:
                    {
                        pixel_value = b;
                        break;
                    }
                case 3:
                    {
                        pixel_value = r * 0.2126d + g * 0.7152d + b * 0.0722d;
                        break;
                    }
            }
            int quantized_value = (int)Math.Floor(pixel_value * currentQuantizationLevel / maxQuantizationLevel);

            return quantized_value.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            TextForm text_form = new TextForm(text);
            text_form.Show();
        }
        private string colorSchemeComboBoxParser(int activeIndex)
        {
            string colorSchema = "";

            switch (activeIndex)
            {
                case 0:
                    colorSchema = "R";
                    break;
                case 1:
                    colorSchema = "G";
                    break;
                case 2:
                    colorSchema = "B";
                    break;
                default:
                    colorSchema = "M";
                    break;
            }

            return colorSchema;
        }
        private string buildFileName(string filename)
        {
            // FileName_H/V_WxH.txt
            // FileName_H/V_R/G/B/M_bit_WxH.txt
            bool isHorizontal = comboBox2.SelectedIndex == 0;
            bool isSelectiveMode = radioButton2.Checked;
            int[] ranges = parsePixelRange(textBox2.Text);
            int width = isSelectiveMode && isHorizontal ? ranges.Length : loadedImage.Width;
            int height = isSelectiveMode && !isHorizontal ? ranges.Length : loadedImage.Height;
            int bits = (int)Math.Pow(2, Convert.ToInt32(numericUpDown1.Value));
            string dimensionsString = isHorizontal ? $"{width}x{height}" : $"{height}x{width}";
            string directionString = isHorizontal ? "H" : "V";
            string colorSchema = colorSchemeComboBoxParser(comboBox1.SelectedIndex);
            string selectiveModeSuffix = isSelectiveMode ? "_({ textBox2.Text})" : "";

            return $"{filename}_{directionString}_{colorSchema}_{bits}_{dimensionsString}{selectiveModeSuffix}.txt";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!multiple)
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Text|*.txt";
                    dialog.Title = "Save an Text File";
                    dialog.FileName = buildFileName(label6.Text);
                    dialog.ShowDialog();

                    if (dialog.FileName != "")
                    {
                        System.IO.StreamWriter file = new System.IO.StreamWriter(dialog.FileName);
                        file.WriteLine(text);

                        file.Close();
                    }
                }
            }
            else
            {
                using (FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog())
                {
                    int i = 0;
                    if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                    {
                        var folder = folderBrowserDialog1.SelectedPath;
                        foreach (string current_text in textList)
                        {
                            System.IO.StreamWriter file = new System.IO.StreamWriter(folder + "/" + textNames[i] + ".txt");
                            file.WriteLine(current_text);

                            file.Close();
                            i++;
                        }

                    }
                }
            }


        }

        void ClearData()
        {
            loadedImageList.Clear();
            loadedImage = null;
            pictureBox1.Image = null;
            text = "";
            textList.Clear();
            textNames.Clear();
            groupBox1.Enabled = false;
            textBox2.Text = "";
            radioButton1.Checked = true;
            radioButton2.Checked = false;
            button5.Enabled = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            disableTextOptions();

            ClearData();
            multiple = false;
            string textFilesFilter = "Text Files(*.TXT)|*.TXT";

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = textFilesFilter;
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (openFileDialog.FileName != "")
                    {
                        loadText(openFileDialog.FileName);
                    }
                }
            }
            button5.Enabled = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = true;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = false;
        }

        private void enableBuildTextButton()
        {
            button2.Enabled = true;
        }

        private void enableTextOptions()
        {
            button3.Enabled = true;
            button4.Enabled = true;
        }
        private void disableTextOptions()
        {
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            CalculateCurrentQuantizationLevel();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
                dialog.Title = "Save an Image File";
                dialog.FileName = label6.Text.Replace(".txt", "");
                dialog.ShowDialog();

                if (dialog.FileName != "")
                {
                    pictureBox1.Image.Save(dialog.FileName);
                }
            }
        }
    }
}
