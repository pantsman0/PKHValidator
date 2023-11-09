using System.DirectoryServices.ActiveDirectory;
using System.Text.Json;
using System.Text.Json.Serialization;
using PKHeX.Core;

namespace PokeHomeValidator
{
    public partial class PokeHomeWindow : Form
    { 
        public PokeHomeWindow()
        {
            InitializeComponent();
        }

        private void PokeHomeWindow_Load(object sender, EventArgs e)
        {

        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            SearchOption options = SearchOption.TopDirectoryOnly;
            if (Directory.Exists(textBox1.Text))
            {
                if (checkBox1.Checked) { options = SearchOption.AllDirectories;}

                List<String> pokemonFiles = Directory.EnumerateFiles(textBox1.Text, "*.ph3", options).ToList();

                richTextBox1.AppendText($"Found {pokemonFiles.Count} PH3 files.\nProcessing...\n");
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();

                var valid = new List<(string,string)>();
                var invalid = new List<(string, string)>();

                foreach (var pokeFile in  pokemonFiles)
                {
                    var (validity, message) = getValidityReport(pokeFile);
                    if (validity)
                    {
                        valid.Add((pokeFile, message));
                    } else
                    {
                        invalid.Add((pokeFile, message));
                    }
                }

                richTextBox1.AppendText($"Validity report:\n\n Valid Pokemon Count: {valid.Count}\nInvalid Pokemon Count: {invalid.Count}\n\nInvalid Pokemon:\n");
                foreach (var poke in invalid) { richTextBox1.AppendText($"{poke.Item1}:\r\nMessage:\r\n{poke.Item2}\r\n\r\n"); }
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();


            }
            else
            {
                MessageBox.Show("Unable to process directory: not found.");
            }
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            };
        }

        private (bool, string) getValidityReport(string fileName)
        {

            try
            {
                PKH pk = new(File.ReadAllBytes(fileName));
                PKM pkm; SaveFile savegame;
                if (PersonalTable.SV.IsPresentInGame(pk.Species, pk.Form))
                {
                    savegame = new SAV9SV();
                    pkm = pk.ConvertToPK9();

                }
                else if (PersonalTable.SWSH.IsPresentInGame(pk.Species, pk.Form))
                {
                    savegame = new SAV8SWSH();
                    pkm = pk.ConvertToPK8();
                }
                else if (PersonalTable.BDSP.IsPresentInGame(pk.Species, pk.Form))
                {
                    savegame = new SAV8BS();
                    pkm = pk.ConvertToPB8();
                }
                else if (PersonalTable.LA.IsPresentInGame(pk.Species, pk.Form))
                {
                    savegame = new SAV8LA();
                    pkm = pk.ConvertToPA8();
                }
                else if (PersonalTable.GG.IsPresentInGame(pk.Species, pk.Form))
                {
                    savegame = new SAV7b();
                    pkm = pk.ConvertToPB7();
                }
                else { return (false, $"GO pokemon: {pk.Met_Location == Locations.GO7 || pk.Met_Location == Locations.GO8}\r\nOT: {pk.OT_Name}\r\nError: no valid switch game found for pokemon species {pk.Species}"); }



                savegame.AdaptPKM(pkm);
                if (pkm == null)
                {
                    return (false, "GO pokemon: {pk.Met_Location == Locations.GO7 || pk.Met_Location == Locations.GO8}\r\nOT: {pk.OT_Name}\r\nError: unable to adapt pokemon to target game.");
                }
                var legality = new LegalityAnalysis(pkm);
                if (legality.Valid) { return (true, "GO pokemon: {pk.Met_Location == Locations.GO7 || pk.Met_Location == Locations.GO8}\r\nOT: {pk.OT_Name}\r\nValid!"); }
                return (false, $"GO pokemon: {pk.Met_Location == Locations.GO7 || pk.Met_Location == Locations.GO8}\r\nOT: {pk.OT_Name}\r\nError: Invalid Pokemon.\n Report: {legality.Report(false)}");
            } catch (Exception ex) { return (false, $"Error: unable to process file.\nFileName: {fileName}\nError Message: {ex.Message}"); }
        }
    }
}