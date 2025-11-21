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
    public partial class FormEditFeature : Form
    {
        private IFeature _feature;
        private IFeatureLayer _layer;
        private IActiveView _activeView;
        public FormEditFeature(IFeature feature, IFeatureLayer layer, IActiveView activeView)
        {
            InitializeComponent();
            this._feature = feature;
            this._layer = layer;
            this._activeView = activeView;

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

                int j = 0;
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
                    j++;
                }

                if(j == 0)
                {
                    MessageBox.Show("该要素没有可编辑属性字段。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.btnOK.Enabled = false;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"异常：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// 确定按钮点击事件 - 保存DataGridView中对要素字段的修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                //检查是否处于编辑会话中
                IDataset dataset = _feature.Class as IDataset;
                IWorkspaceEdit workspaceEdit = dataset.Workspace as IWorkspaceEdit;

                bool intEditSession = workspaceEdit.IsBeingEdited();

                //如果不在编辑会话中，开始编辑
                if (!intEditSession)
                {
                    workspaceEdit.StartEditing(true);
                }

                //开始编辑操作
                workspaceEdit.StartEditOperation();

                int i;             //字段索引
                IField field;      //字段对象

                //遍历DataGridView中所有行
                foreach (DataGridViewRow row in this.dgvFields.Rows)
                {
                    // 跳过新行和空行
                    if (row.IsNewRow || row.Tag == null) 
                        continue;
                    //从行的tag中获取字段索引
                    i = (int)row.Tag;
                    //根据索引获取对应字段
                    field = _feature.Fields.Field[i];

                    //获取用户输入的值
                    object cellValue = row.Cells[1].Value;
                    if (cellValue == null)
                        continue;

                    string stringValue = cellValue.ToString();

                    //根据字段类型进行数据类型转换和赋值
                    if (field.Type == esriFieldType.esriFieldTypeString)
                        _feature.Value[i] = row.Cells[1].Value.ToString();
                    else if (field.Type == esriFieldType.esriFieldTypeInteger
                         || field.Type == esriFieldType.esriFieldTypeSmallInteger
                         )
                        _feature.Value[i] = int.Parse(row.Cells[1].Value.ToString());
                    else if (field.Type == esriFieldType.esriFieldTypeSingle
                          || field.Type == esriFieldType.esriFieldTypeDouble)
                        _feature.Value[i] = Single.Parse(row.Cells[1].Value.ToString());
                    else
                        _feature.Value[i] = row.Cells[1].Value;
                }

                //结束要素编辑
                workspaceEdit.StopEditOperation();
                //如果之前不在编辑，停止编辑并保存
                if (!intEditSession)
                {
                    workspaceEdit.StopEditing(true);
                }

                //保存要素修改
                _feature.Store();

                ////结束要素编辑
                //workspaceEdit.StopEditOperation();
                ////如果之前不在编辑，停止编辑并保存
                //if (!intEditSession)
                //{
                //    workspaceEdit.StopEditing(true);
                //}

                //刷新地图显示
                if (_activeView != null)
                {
                    _activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
                MessageBox.Show("要素修改成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("[异常]" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        /// <summary>
        /// 处理DataGridView的数据验证
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgvFields_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if(e.ColumnIndex == 1)
            {
                int fieldIndex = (int)dgvFields.Rows[e.RowIndex].Tag;
                IField field = _feature.Fields.Field[fieldIndex];
                string newValue = e.FormattedValue.ToString();

                //检查空值
                if(string.IsNullOrEmpty(newValue) && !field.IsNullable)
                {
                    dgvFields.Rows[e.RowIndex].ErrorText = "此字段为必填字段";
                    e.Cancel = true;
                }
                else
                {
                    dgvFields.Rows[e.RowIndex].ErrorText = "";
                }
            }
        }
    }
}
