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
    public partial class FormNewFeatureClass : Form
    {
        public FormNewFeatureClass()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "请选择数据目录";
                folderDialog.ShowNewFolderButton = true; // 允许创建新文件夹

                // 如果dataDir中已有路径，设置为初始目录
                if (!string.IsNullOrEmpty(dataDir.Text))
                {
                    folderDialog.SelectedPath = dataDir.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    dataDir.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void dataDir_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnOK_Click(object sender, EventArgs e)
        {

        }

        private void btnClose_Click(object sender, EventArgs e)
        {

        }

        private void btnDel_Click(object sender, EventArgs e)
        {

        }

        private void spitalCoord_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void geometryType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
