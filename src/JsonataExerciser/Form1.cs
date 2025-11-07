using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.JsonNet;
using Jsonata.Net.Native.SystemTextJson;
using System;
using System.Windows.Forms;

namespace JsonataExerciser
{
    public sealed partial class Form1 : Form
    {
        private readonly System.Text.Json.JsonSerializerOptions m_stJsonSerializerOptions = new System.Text.Json.JsonSerializerOptions() {
            WriteIndented = true
        };

        private JToken? m_datasetJson = null;
        private JObject? m_bindingsJson = null;
        private JsonataQuery? m_query = null;
        private bool m_ignoreTextChanges = false;
        private OutputProcessingMode m_outputProcessingMode = OutputProcessingMode.Default;

        public Form1()
        {
            InitializeComponent();

            sampleComboBox.BeginUpdate();
            foreach (Sample sample in Sample.GetDefaultSamples())
            {
                sampleComboBox.Items.Add(sample);
            }
            ;
            sampleComboBox.EndUpdate();

            outputModeComboBox.BeginUpdate();
            outputModeComboBox.Items.Add(new OutputProcessingItem(OutputProcessingMode.Default, "Jsonata.Net.Native"));
            outputModeComboBox.Items.Add(new OutputProcessingItem(OutputProcessingMode.Newtonsoft, "Json.Net (Newtonsoft.Json)"));
            outputModeComboBox.Items.Add(new OutputProcessingItem(OutputProcessingMode.SystemTextJson, "System.Text.Json"));
            outputModeComboBox.SelectedIndex = 0;
            outputModeComboBox.EndUpdate();

            TryParseDataset();
            TryParseBindings();
            TryParseQuery();
        }

        private void DatasetFctb_TextChangedDelayed(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            if (!this.m_ignoreTextChanges)
            {
                TryParseDataset();
            }
        }

        private void QueryFctb_TextChangedDelayed(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            if (!this.m_ignoreTextChanges)
            {
                TryParseQuery();
            }
        }

        private void BindingsFctb_TextChangedDelayed(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            if (!this.m_ignoreTextChanges)
            {
                TryParseBindings();
            }
        }

        private void formatDatasetJsonButton_Click(object sender, EventArgs e)
        {
            if (this.m_datasetJson != null)
            {
                this.DatasetFctb.Text = this.m_datasetJson.ToIndentedString();
            }
        }

        private void TryParseDataset()
        {
            try
            {
                this.m_datasetJson = JToken.Parse(this.DatasetFctb.Text);
            }
            catch (Exception ex)
            {
                this.m_datasetJson = null;
                this.ResultFctb.Text = "Failed to parse input JSON: " + ex.Message;
                this.ResultFctb.WordWrap = true;
            }
            ;
            TryApplyQuery();
        }

        private void TryParseBindings()
        {
            try
            {
                JToken token = JToken.Parse(this.BindingsFctb.Text);
                if (token.Type != JTokenType.Object)
                {
                    throw new Exception("bindings should be an Object");
                }
                this.m_bindingsJson = (JObject)token;
            }
            catch (Exception ex)
            {
                this.m_bindingsJson = null;
                this.ResultFctb.Text = "Failed to parse bindings JSON: " + ex.Message;
                this.ResultFctb.WordWrap = true;
            }
            ;
            TryApplyQuery();
        }

        private void TryParseQuery()
        {
            try
            {
                this.m_query = new JsonataQuery(this.QueryFctb.Text);
            }
            catch (Exception ex)
            {
                this.m_query = null;
                this.ResultFctb.Text = "Failed to parse query: " + ex.Message;
                this.ResultFctb.WordWrap = true;
            }
            TryApplyQuery();
        }

        private void TryApplyQuery()
        {
            if (this.m_datasetJson == null || this.m_query == null)
            {
                return;
            }

            try
            {
                JToken result = this.m_query.Eval(this.m_datasetJson, bindings: this.m_bindingsJson);
                string resultText;
                switch (this.m_outputProcessingMode)
                {
                case OutputProcessingMode.Default:
                    resultText = result.ToIndentedString();
                    break;
                case OutputProcessingMode.Newtonsoft:
                    {
                        Newtonsoft.Json.Linq.JToken newtonsoft = result.ToNewtonsoft();
                        resultText = Newtonsoft.Json.JsonConvert.SerializeObject(newtonsoft, Newtonsoft.Json.Formatting.Indented);
                    }
                    break;
                case OutputProcessingMode.SystemTextJson:
                    {
                        System.Text.Json.Nodes.JsonNode? stJson = result.ToSystemTextJsonNode();
                        if (stJson == null)
                        {
                            resultText = "null";
                        }
                        else if (stJson.GetValueKind() == System.Text.Json.JsonValueKind.Undefined)
                        {
                            resultText = "undefined";   //calling any other method would cause InvalidOperationException
                        }
                        else
                        {
                            resultText = stJson.ToJsonString(this.m_stJsonSerializerOptions);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException("Should not happen: " + this.m_outputProcessingMode);
                }
                this.ResultFctb.WordWrap = false;
                this.ResultFctb.Text = resultText;
            }
            catch (Exception ex)
            {
                this.ResultFctb.Text = "Failed to execute query: " + ex.Message;
                this.ResultFctb.WordWrap = true;
            }
        }

        private void sampleComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Sample? sample = (Sample?)this.sampleComboBox.SelectedItem;
            if (sample != null)
            {
                try
                {
                    this.m_ignoreTextChanges = true;
                    this.m_datasetJson = null;
                    this.m_bindingsJson = null;
                    this.m_query = null;

                    this.DatasetFctb.Text = sample.data;
                    this.BindingsFctb.Text = sample.bindings;
                    this.QueryFctb.Text = sample.query;

                    TryParseDataset();
                    TryParseBindings();
                    TryParseQuery();
                }
                finally
                {
                    this.m_ignoreTextChanges = false;
                }
            }
        }

        private void outputModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            OutputProcessingItem? item = (OutputProcessingItem?)this.outputModeComboBox.SelectedItem;
            if (item != null)
            {
                this.m_outputProcessingMode = item.mode;
                this.TryApplyQuery();
            }
        }

        private enum OutputProcessingMode
        {
            Default,
            Newtonsoft,
            SystemTextJson
        }

        private sealed class OutputProcessingItem
        {
            internal readonly OutputProcessingMode mode;
            private readonly string m_name;

            internal OutputProcessingItem(OutputProcessingMode mode, string name)
            {
                this.mode = mode;
                this.m_name = name;
            }

            public override string ToString()
            {
                return this.m_name;
            }
        }
    }
}
