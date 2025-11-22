using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EngineWindowsApplication1
{
    public partial class FormIdentifyFeature : Form
    {
        private IFeature _feature;
        public FormIdentifyFeature(IFeature feature)
        {
            InitializeComponent();
            this._feature = feature;

            DisplayFeatureClassInfo();
        }
        /// <summary>
        /// 显示要素类路径及字段列表
        /// </summary>
        public void DisplayFeatureClassInfo()
        {
            try
            {
                //显示要素类路径信息
                IDataset dataset = _feature.Class as IDataset;
                //清空现有行
                this.dgvFields.Rows.Clear();

                IFields fields = _feature.Fields;

                for(int i = 0;i < fields.FieldCount;i++)
                {
                    IField field = fields.Field[i];

                    //跳过OID和几何字段
                    if(field.Type == esriFieldType.esriFieldTypeOID || 
                        field.Type == esriFieldType.esriFieldTypeGeometry)
                    {
                        continue;
                    }

                    //获取字段值
                    object fieldValue = _feature.Value[i];
                    string displayValue = fieldValue != null ? fieldValue.ToString() : "";
                    //在表格中添加行，显示字段别名和值
                    int rowIndex = this.dgvFields.Rows.Add(field.AliasName, displayValue);

                    //在行的Tag属性中记录字段序号
                    this.dgvFields.Rows[rowIndex].Tag = i;
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show($"异常：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormEditFeature_Load(object sender, EventArgs e)
        {
            //设置焦点到第一个可编辑单元格
            if(dgvFields.Rows.Count > 0)
            {
                dgvFields.CurrentCell = dgvFields.Rows[0].Cells[1];
            }
        }
    }
}
