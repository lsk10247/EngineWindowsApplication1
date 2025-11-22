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
    public partial class FormFeatureBrowse : Form
    {
        //当前页
        private int currentPage = 1;
        //总要素数
        private int totalFeatures = 0;
        //所有数据
        private DataTable dt;
        //当前图层
        private IFeatureLayer currentFeatureLayer;
        //当前每页行数
        private int pageSize = 100;

        public FormFeatureBrowse(IFeatureLayer featureLayer)
        {
            InitializeComponent();
            LoadFeatureData(featureLayer);
            currentFeatureLayer = featureLayer;
        }

        // 加载要素数据到表格
        private void LoadFeatureData(IFeatureLayer featureLayer)
        {
            dt = new DataTable();
            IFeatureClass featureClass = featureLayer.FeatureClass;

            //获取总要素数
            totalFeatures = featureClass.FeatureCount(null);

            // 1. 添加列（跳过OID和几何字段，避免冗余）
            for (int i = 0; i < featureClass.Fields.FieldCount; i++)
            {
                IField field = featureClass.Fields.Field[i];
                if (field.Type == esriFieldType.esriFieldTypeOID || field.Type == esriFieldType.esriFieldTypeGeometry)
                    continue;
                Type netType = EsriFieldTypeToSystemType(field.Type);
                dt.Columns.Add(field.AliasName ?? field.Name, netType);
            }

            // 2. 添加行数据
            IFeatureCursor cursor = featureClass.Search(null, false);
            IFeature feature = cursor.NextFeature();
            while (feature != null)
            {
                DataRow dr = dt.NewRow();
                for (int i = 0; i < featureClass.Fields.FieldCount; i++)
                {
                    IField field = featureClass.Fields.Field[i];
                    if (field.Type == esriFieldType.esriFieldTypeOID || field.Type == esriFieldType.esriFieldTypeGeometry)
                        continue;
                    dr[field.AliasName ?? field.Name] = feature.Value[i] ?? DBNull.Value;
                }
                dt.Rows.Add(dr);
                feature = cursor.NextFeature();
            }


            //状态栏显示要素数量
            toolStripStatusLabel1.Text = $"要素数量：{totalFeatures}";

            ShowCurrentPage();
            UpdatePaginationInfo();
        }


        /// <summary>
        /// 将 ArcGIS 的字段类型枚举转换为对应的 .NET 类型
        /// </summary>
        /// <param name="esriType">ArcGIS 字段类型</param>
        /// <returns>对应的 .NET 类型</returns>
        private static Type EsriFieldTypeToSystemType(esriFieldType esriType)
        {
            switch (esriType)
            {
                case esriFieldType.esriFieldTypeInteger:
                    return typeof(int);
                case esriFieldType.esriFieldTypeSmallInteger:
                    return typeof(short);
                case esriFieldType.esriFieldTypeDouble:
                    return typeof(double);
                case esriFieldType.esriFieldTypeSingle:
                    return typeof(float);
                case esriFieldType.esriFieldTypeString:
                    return typeof(string);
                case esriFieldType.esriFieldTypeDate:
                    return typeof(DateTime);
                case esriFieldType.esriFieldTypeOID:
                    return typeof(int); // 通常 OID 是整数类型
                case esriFieldType.esriFieldTypeGeometry:
                    return typeof(string); // 几何类型不直接显示，可用字符串表示其类型
                case esriFieldType.esriFieldTypeBlob:
                    return typeof(byte[]);
                case esriFieldType.esriFieldTypeRaster:
                    return typeof(string); //  raster 类型也用字符串表示
                case esriFieldType.esriFieldTypeGUID:
                    return typeof(Guid);
                case esriFieldType.esriFieldTypeGlobalID:
                    return typeof(string); // GlobalID 通常以字符串形式处理
                default:
                    // 对于未知类型，默认使用 string
                    return typeof(string);
            }
        }
        /// <summary>
        /// 显示当前页数据
        /// </summary>
        private void ShowCurrentPage()
        {
            if(dt == null)
            {
                return;
            }
            //计算当前页数据范围
            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, dt.Rows.Count);

            //创建当前页数据表
            DataTable currentDataTable = dt.Clone();

            for(int i = startIndex;i < endIndex;i++)
            {
                currentDataTable.ImportRow(dt.Rows[i]);
            }

            //绑定到DataGridView
            dgvFeatures.DataSource = currentDataTable;
            dgvFeatures.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ////设置最后一列填充剩余空间
            //if (dgvFeatures.Columns.Count > 0)
            //{
            //    dgvFeatures.Columns[dgvFeatures.Columns.Count - 1].AutoSizeMode =
            //        DataGridViewAutoSizeColumnMode.Fill;
            //}
        }
        /// <summary>
        /// 更新分页信息与按钮状态
        /// </summary>
        private void UpdatePaginationInfo()
        {

            int totalPages = (int)Math.Ceiling((double)totalFeatures / pageSize);
            if(totalPages == 0)
            {
                totalPages = 1;
            }
            textBox2.Text = totalPages.ToString();
            textBox1.Text = currentPage.ToString();

            //更新按钮状态
            btnFirst.Enabled = currentPage > 1;
            btnPrevious.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;
            btnLast.Enabled = currentPage < totalPages;
        }

        /// <summary>
        /// 窗体加载时初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormFeatureBrowse_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 1;
            UpdatePaginationInfo();
        }
        /// <summary>
        /// 当下拉框选项改变时，同步更改pageSize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pageSize = int.Parse(comboBox1.SelectedItem.ToString());
            currentPage = 1;
            ShowCurrentPage();
            UpdatePaginationInfo();
        }
        /// <summary>
        /// 首页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFirst_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            ShowCurrentPage();
            UpdatePaginationInfo();
        }
        /// <summary>
        /// 前一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if(currentPage > 1)
            {
                currentPage--;
                ShowCurrentPage();
                UpdatePaginationInfo();
            }
        }
        /// <summary>
        /// 后一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalFeatures / pageSize);
            if (currentPage < totalPages)
            {
                currentPage++;
                ShowCurrentPage();
                UpdatePaginationInfo();
            }
        }
        /// <summary>
        /// 末页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLast_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalFeatures / pageSize);
            currentPage = totalPages;
            ShowCurrentPage();
            UpdatePaginationInfo();
        }
    }
}
