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
        public FormFeatureBrowse(IFeatureLayer featureLayer)
        {
            InitializeComponent();
            LoadFeatureData(featureLayer);
        }

        // 加载要素数据到表格
        private void LoadFeatureData(IFeatureLayer featureLayer)
        {
            DataTable dt = new DataTable();
            IFeatureClass featureClass = featureLayer.FeatureClass;

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

            // 3. 绑定表格
            dgvFeatures.DataSource = dt;
            dgvFeatures.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
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


        // 关闭按钮事件
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
