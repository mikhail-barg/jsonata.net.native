using Jsonata.Net.Native;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JsonataExerciser
{
    public sealed partial class Form1 : Form
    {
        private JToken? m_datasetJson = null;
        private JObject? m_bindingsJson = null;
        private JsonataQuery? m_query = null;
        private bool m_ignoreTextChanges = false;

        public Form1()
        {
            InitializeComponent();

            sampleComboBox.BeginUpdate();
            foreach (Sample sample in Sample.GetDefaultSamples())
            {
                sampleComboBox.Items.Add(sample);
            };
            sampleComboBox.EndUpdate();

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
                this.DatasetFctb.Text = this.m_datasetJson.ToString(Formatting.Indented);
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
            };
            TryApplyQuery();
        }

        private void TryParseBindings()
        {
            try
            {
                this.m_bindingsJson = JObject.Parse(this.BindingsFctb.Text);
            }
            catch (Exception ex)
            {
                this.m_bindingsJson = null;
                this.ResultFctb.Text = "Failed to parse bindings JSON: " + ex.Message;
                this.ResultFctb.WordWrap = true;
            };
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
            };
            TryApplyQuery();
        }

        private void TryApplyQuery()
        {
            if (this.m_datasetJson == null || this.m_query == null)
            {
                return;
            };

            try
            {
                JToken result = this.m_query.Eval(this.m_datasetJson, bindings: this.m_bindingsJson);
                this.ResultFctb.WordWrap = false;
                this.ResultFctb.Text = result.ToString(Formatting.Indented);
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

       
    }
}
