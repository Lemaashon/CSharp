using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;
using gFil = Lema._Utilities.File_Utils;

namespace Lema.Forms.Base
{
    public partial class BaseDropdown : Form
    {
        //Form properties
        private List<string> Keys;
        private List<object> Values;
        private int DefaultIndex;
        public BaseDropdown(List<string> keys, List<object> values, string title, string message, int defaultIndex = -1)
        {
            InitializeComponent();
            gFil.SetFormIcon(this);

            this.Keys = keys;
            this.Values = values;
            this.DefaultIndex = defaultIndex;
            this.Text = title;
            this.labelMessage.Text = message;

            this.DialogResult = DialogResult.Cancel;
            this.Tag = null;  
            
            PopulateComboBox();

        }

        private void PopulateComboBox()
        {
            this.comboBox.Items.Clear();

            foreach (var key in this.Keys)
            {
                this.comboBox.Items.Add(key);
            }

            if (this.DefaultIndex >= 0 && this.DefaultIndex < this.comboBox.Items.Count)
            {
                this.comboBox.SelectedIndex = this.DefaultIndex;
            }
            else
            {
                try
                {
                    this.comboBox.SelectedIndex = 0;    
                }
                catch
                {
                    this.comboBox.SelectedIndex = -1;
                }
            }

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (this.comboBox.SelectedIndex >= 0)
            {
                var selectedValue = this.Values[this.comboBox.SelectedIndex];
                this.Tag = selectedValue;
                this.DialogResult = DialogResult.OK;
            }

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();

        }
    }
}
