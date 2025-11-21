using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EngineWindowsApplication1
{
    public partial class Form1 : Form
    {
        MapOperatorType mapOperatorType;
        public Form1()
        {
            InitializeComponent();

            SetupStatusStrip();

            axTOCControl1.SetBuddyControl(axMapControl1);

            //axMapControl2.AutoMouseWheel = false;
        }
        private void SetupStatusStrip()
        {
            // toolStripStatusLabel1 - 操作状态信息
            toolStripStatusLabel1.AutoSize = false;
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.Width = 200;
            toolStripStatusLabel1.Text = "就绪";

            // toolStripStatusLabel2 - 地图坐标显示
            toolStripStatusLabel2.AutoSize = false;
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel2.Width = 200;
            toolStripStatusLabel2.Text = "坐标: (0, 0)";

            // toolStripStatusLabel3 - 版权信息
            toolStripStatusLabel3.AutoSize = false;
            toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel3.Width = 300;
            toolStripStatusLabel3.Text = "开发人员：刘世坤，赵志阳，周桂添，2025.11.10";

            mapOperatorType = MapOperatorType.Default;
        }

        /// <summary>
        /// 地图操作枚举类型
        /// </summary>
        public enum MapOperatorType
        {
            /// <summary>
            /// 默认（无操作）
            /// </summary>
            Default,
            /// <summary>
            /// 画点
            /// </summary>
            DrawPoint,
            /// <summary>
            /// 画线
            /// </summary>
            DrawPolyline,
            /// <summary>
            /// 画多边形
            /// </summary>
            DrawPolygon,

            /// <summary>
            /// 画矩形
            /// </summary>
            DrawRectangle,

            /// <summary>
            /// 从地图上创建点要素
            /// </summary>
            CreatePoint,
            /// <summary>
            /// 从地图上创建线要素
            /// </summary>
            CreatePolyline,
            /// <summary>
            /// 从地图上创建面要素
            /// </summary>
            CreatePolygon,

            /// <summary>
            /// 标识/显示要素信息
            /// </summary>
            IdentifyFeature,

            /// <summary>
            /// 点选要素
            /// </summary>
            SelectFeatureByLocation,
            /// <summary>
            /// 线选要素
            /// </summary>
            SelectFeatureByPolyline,
            /// <summary>
            /// 多边形选择要素
            /// </summary>
            SelectFeatureByPolygon,
            /// <summary>
            /// 框选要素
            /// </summary>
            SelectFeatureByRectangle,

            /// <summary>
            /// 点选编辑要素
            /// </summary>
            EditFeatureByLocation,
            /// <summary>
            /// 框选编辑要素
            /// </summary>
            EditFeatureByRectangle,

            /// <summary>
            /// 点选删除要素
            /// </summary>
            DeleteFeatureByLocation,
            /// <summary>
            /// 框选删除要素
            /// </summary>
            DeleteFeatureByRectangle,
            /// <summary>
            /// 多边形选择删除要素
            /// </summary>
            DeleteFeatureByPolygon
        }

        private void menuFileOpen_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建打开文件对话框
                OpenFileDialog openFileDialog = new OpenFileDialog();

                // 设置支持的文件类型
                openFileDialog.Title = "打开地图文档";
                openFileDialog.Filter = "地图文档 (*.mxd)|*.mxd|图层文件 (*.lyr)|*.lyr|Shapefile (*.shp)|*.shp|所有文件 (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();

                    switch (fileExtension)
                    {
                        case ".mxd":
                            // 加载地图文档
                            OpenMapDocument(filePath);
                            break;
                        case ".lyr":
                            // 添加图层文件
                            AddLayerFile(filePath);
                            break;
                        case ".shp":
                            // 添加Shapefile
                            AddShapefile(filePath);
                            break;
                        default:
                            MessageBox.Show("不支持的文件格式！", "错误",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                    }

                    // 刷新地图显示
                    axMapControl1.Refresh();
                    MessageBox.Show($"成功打开文件：{System.IO.Path.GetFileName(filePath)}", "打开成功",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件时出错：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenMapDocument(string mxdPath)
        {
            try
            {
                // 检查地图文档是否存在
                if (!File.Exists(mxdPath))
                {
                    throw new FileNotFoundException("地图文档不存在！");
                }

                // 加载地图文档
                axMapControl1.LoadMxFile(mxdPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载地图文档失败：{ex.Message}");
            }
        }

        private void AddLayerFile(string lyrPath)
        {
            try
            {
                // 方法1：使用ILayerFile接口直接加载
                ILayerFile layerFile = new LayerFileClass();
                layerFile.Open(lyrPath);

                // 获取图层并添加到地图
                ILayer layer = layerFile.Layer;
                axMapControl1.AddLayer(layer);

                // 释放图层文件资源
                layerFile.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"加载图层文件失败：{ex.Message}");
            }
        }
        private void AddShapefile(string shpPath)
        {
            try
            {
                // 创建工作空间工厂
                IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
                IWorkspace workspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(shpPath), 0);

                // 打开要素类
                IFeatureClass featureClass = (workspace as IFeatureWorkspace).OpenFeatureClass(System.IO.Path.GetFileNameWithoutExtension(shpPath));

                // 创建要素图层
                IFeatureLayer featureLayer = new FeatureLayerClass();
                featureLayer.FeatureClass = featureClass;
                featureLayer.Name = featureClass.AliasName;

                // 添加到地图
                axMapControl1.AddLayer((ILayer)featureLayer);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载Shapefile失败：{ex.Message}");
            }
        }

        private void menuFileSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建保存文件对话框
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                // 设置文件类型
                saveFileDialog.Title = "保存地图文档";
                saveFileDialog.Filter = "地图文档 (*.mxd)|*.mxd|地图模板 (*.mxt)|*.mxt";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.DefaultExt = "mxd";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    // 检查文件是否已存在
                    if (File.Exists(filePath))
                    {
                        DialogResult result = MessageBox.Show("文件已存在，是否覆盖？", "确认覆盖",
                                                            MessageBoxButtons.YesNo,
                                                            MessageBoxIcon.Question);
                        if (result != DialogResult.Yes)
                            return;
                    }

                    // 使用 IMapDocument 保存地图文档
                    IMapDocument mapDocument = new MapDocumentClass();

                    try
                    {
                        // 如果文件已存在，先将其删除（因为MapDocument.SaveAs不能覆盖）
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }

                        // 设置地图文档的地图
                        mapDocument.New(filePath);
                        mapDocument.ReplaceContents(axMapControl1.Map as IMxdContents);

                        // 保存地图文档
                        mapDocument.Save(true, true);

                        MessageBox.Show($"地图文档已成功保存到：{filePath}", "保存成功",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    finally
                    {
                        // 释放地图文档资源
                        if (mapDocument != null)
                        {
                            mapDocument.Close();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(mapDocument);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件时出错：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void menuFileExit_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查地图是否有未保存的更改
                if (HasUnsavedChanges())
                {
                    DialogResult saveResult = MessageBox.Show("地图文档已修改，是否保存？",
                                                            "保存更改",
                                                            MessageBoxButtons.YesNoCancel,
                                                            MessageBoxIcon.Question);

                    if (saveResult == DialogResult.Yes)
                    {
                        menuFileSave_Click(sender, e);
                    }
                    else if (saveResult == DialogResult.Cancel)
                    {
                        return; // 取消退出
                    }
                }

                // 确认退出
                DialogResult result = MessageBox.Show("您确定要退出系统吗？",
                                                    "确认退出",
                                                    MessageBoxButtons.YesNo,
                                                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 清理ArcEngine资源
                    CleanupArcEngineResources();

                    // 退出应用程序
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"退出时出错：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool HasUnsavedChanges()
        {
            // 这里可以添加检查地图是否有未保存更改的逻辑
            // 例如检查地图控件是否有修改标志
            // return axMapControl1.get_IsModified();
            return false; // 暂时返回false，您需要根据实际情况实现
        }

        private void CleanupArcEngineResources()
        {
            try
            {
                // 清理地图控件
                if (axMapControl1 != null)
                {
                    axMapControl1.ClearLayers();
                    //axMapControl1.Reset();
                }

                // 释放其他ArcEngine资源
                // ...
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理资源时出错：{ex.Message}");
            }
        }

        #region 图层菜单功能

        // 全部数据 - 加载目录下所有SHP文件
        private void LoadAllShpFiles()
        {
            try
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "选择包含SHP文件的目录";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    string[] shpFiles = Directory.GetFiles(folderPath, "*.shp");

                    if (shpFiles.Length == 0)
                    {
                        MessageBox.Show("该目录下没有找到SHP文件！", "提示",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    int loadedCount = 0;
                    foreach (string shpFile in shpFiles)
                    {
                        try
                        {
                            AddShapefileToMap(shpFile);
                            loadedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"加载文件 {System.IO.Path.GetFileName(shpFile)} 失败: {ex.Message}");
                        }
                    }

                    axMapControl1.ActiveView.Refresh();
                    MessageBox.Show($"成功加载 {loadedCount} 个SHP文件", "加载完成",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 加载单个SHP文件到地图
        private void AddShapefileToMap(string shpPath)
        {
            try
            {
                string directory = System.IO.Path.GetDirectoryName(shpPath);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(shpPath);

                IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(directory, 0);
                IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(fileName);

                IFeatureLayer featureLayer = new FeatureLayerClass();
                featureLayer.FeatureClass = featureClass;
                featureLayer.Name = featureClass.AliasName;

                axMapControl1.AddLayer(featureLayer);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载SHP文件失败：{ex.Message}");
            }
        }

        // 移除选中图层
        private void RemoveSelectedLayer()
        {
            try
            {
                ILayer layer = GetSelectedLayer();
                if (layer != null)
                {
                    IMap map = axMapControl1.Map;

                    // 查找图层在地图中的索引
                    int layerIndex = -1;
                    for (int i = 0; i < map.LayerCount; i++)
                    {
                        if (map.get_Layer(i) == layer)
                        {
                            layerIndex = i;
                            break;
                        }
                    }

                    if (layerIndex >= 0)
                    {
                        // 使用图层索引删除图层
                        axMapControl1.DeleteLayer(layerIndex);
                        axMapControl1.ActiveView.Refresh();

                        MessageBox.Show($"已成功移除图层 [{layer.Name}]", "移除成功",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("未找到图层在地图中的位置！", "错误",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择一个图层！", "提示",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除图层失败：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // 设置选中图层为可选状态，其他为不可选
        private void SetSelectedLayerSelectable()
        {
            try
            {
                ILayer selectedLayer = GetSelectedLayer();
                if (selectedLayer == null)
                {
                    MessageBox.Show("请先选择一个图层！", "提示",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                IMap map = axMapControl1.Map;
                for (int i = 0; i < map.LayerCount; i++)
                {
                    ILayer layer = map.get_Layer(i);
                    if (layer is IFeatureLayer featureLayer)
                    {
                        featureLayer.Selectable = (layer == selectedLayer);
                    }
                }

                axMapControl1.ActiveView.Refresh();
                MessageBox.Show($"已将图层 [{selectedLayer.Name}] 设置为可选状态", "设置完成",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置可选状态失败：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 切换选中图层的显示/隐藏状态
        private void ToggleLayerVisibility()
        {
            try
            {
                ILayer layer = GetSelectedLayer();
                if (layer != null)
                {
                    layer.Visible = !layer.Visible;
                    axMapControl1.ActiveView.Refresh();

                    string status = layer.Visible ? "显示" : "隐藏";
                    MessageBox.Show($"已将图层 [{layer.Name}] 设置为{status}状态", "设置完成",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("请先选择一个图层！", "提示",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置显示状态失败：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 添加选中图层到鹰眼
        private void AddLayerToThumbnail()
        {
            try
            {
                ILayer layer = GetSelectedLayer();
                if (layer != null)
                {
                    // 假设鹰眼控件的编号为2，名称为axMapControl2
                    // 您需要根据实际的鹰眼控件名称修改
                    axMapControl2.ClearLayers();
                    axMapControl2.AddLayer(layer);
                    axMapControl2.ActiveView.Refresh();

                    MessageBox.Show($"已将图层 [{layer.Name}] 添加到鹰眼", "添加成功",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("请先选择一个图层！", "提示",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加到鹰眼失败：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 获取TOC控件中选中的图层
        private ILayer GetSelectedLayer()
        {
            try
            {
                // 这里假设您使用了TOC控件，根据实际情况调整获取选中图层的方法
                // 如果没有TOC控件，可以通过其他方式获取选中图层
                esriTOCControlItem item = esriTOCControlItem.esriTOCControlItemNone;
                IBasicMap basicMap = null;
                ILayer layer = null;
                object other = null;
                object index = null;

                // 注意：这里使用的是GetSelectedItem，它不需要鼠标坐标参数[citation:1]
                axTOCControl1.GetSelectedItem(ref item, ref basicMap, ref layer, ref other, ref index);

                return layer;
            }
            catch
            {
                // 如果通过TOC获取失败，尝试通过其他方式获取
                return GetFirstLayer(); // 返回第一个图层作为备选
            }
        }

        // 获取地图中的第一个图层（备选方法）
        private ILayer GetFirstLayer()
        {
            IMap map = axMapControl1.Map;
            if (map.LayerCount > 0)
            {
                return map.get_Layer(0);
            }
            return null;
        }

        #endregion

        #region 图层菜单事件

        private void menuLayerAllShp_Click(object sender, EventArgs e)
        {
            LoadAllShpFiles();
        }

        private void menuLayerAddShp_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "选择SHP文件";
                openFileDialog.Filter = "SHP文件 (*.shp)|*.shp";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        AddShapefileToMap(filePath);
                    }
                    axMapControl1.ActiveView.Refresh();
                    MessageBox.Show($"成功加载 {openFileDialog.FileNames.Length} 个SHP文件", "加载完成",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载SHP文件失败：{ex.Message}", "错误",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuLayerRemove_Click(object sender, EventArgs e)
        {
            RemoveSelectedLayer();
        }

        private void menuLayerSelectable_Click(object sender, EventArgs e)
        {
            SetSelectedLayerSelectable();
        }

        private void menuLayerVisible_Click(object sender, EventArgs e)
        {
            ToggleLayerVisibility();
        }

        private void menuLayerThum_Click(object sender, EventArgs e)
        {
            AddLayerToThumbnail();
        }

        #endregion

        #region 工具栏按钮事件

        private void tlbLayerAllShp_Click(object sender, EventArgs e)
        {
            LoadAllShpFiles();
        }

        private void tlbLayerAddShp_Click(object sender, EventArgs e)
        {
            menuLayerAddShp_Click(sender, e); // 直接调用菜单事件
        }

        private void tlbLayerRemove_Click(object sender, EventArgs e)
        {
            RemoveSelectedLayer();
        }

        private void tlbLayerSelectable_Click(object sender, EventArgs e)
        {
            SetSelectedLayerSelectable();
        }

        private void tlbLayerVisible_Click(object sender, EventArgs e)
        {
            ToggleLayerVisibility();
        }

        private void tlbLayerThum_Click(object sender, EventArgs e)
        {
            AddLayerToThumbnail();
        }

        #endregion

        private void axMapControl1_OnExtentUpdated(object sender, IMapControlEvents2_OnExtentUpdatedEvent e)
        {
            try
            {
                // 获取当前主地图的显示范围
                ESRI.ArcGIS.Geometry.IEnvelope ext = this.axMapControl1.Extent;

                // 检查范围是否有效
                if (ext == null || ext.IsEmpty)
                    return;

                // 确保第二个地图控件有活动视图
                if (this.axMapControl2.ActiveView == null)
                    return;

                // 清除之前的图形元素
                this.axMapControl2.ActiveView.GraphicsContainer.DeleteAllElements();

                // 创建红色边框符号
                ISimpleLineSymbol outlineSymbol = new SimpleLineSymbol();
                outlineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
                outlineSymbol.Width = 3.0;  // 设置明显的宽度

                IRgbColor lineColor = new RgbColor();
                lineColor.Red = 255;
                lineColor.Green = 0;
                lineColor.Blue = 0;
                lineColor.Transparency = 0;  // 不透明
                outlineSymbol.Color = lineColor;

                // 创建填充符号（完全透明）
                ISimpleFillSymbol fillSymbol = new SimpleFillSymbol();
                fillSymbol.Style = esriSimpleFillStyle.esriSFSNull;  // 无填充
                fillSymbol.Outline = outlineSymbol;

                // 创建矩形元素
                IRectangleElement rectangleElement = new RectangleElementClass();
                IElement element = (IElement)rectangleElement;
                element.Geometry = ext;

                // 设置元素符号
                IFillShapeElement fillShapeElement = (IFillShapeElement)element;
                fillShapeElement.Symbol = fillSymbol;

                // 添加到图形容器
                this.axMapControl2.ActiveView.GraphicsContainer.AddElement(element, 0);

                // 刷新显示
                this.axMapControl2.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            }
            catch (Exception ex)
            {
                
            }
        }

            private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            try
            {
                // 将屏幕坐标转换为地图坐标
                double mapX = e.mapX;
                double mapY = e.mapY;

                // 更新状态栏坐标显示，保留2位小数
                toolStripStatusLabel2.Text = $"坐标: ({mapX:F2}, {mapY:F2})";
            }
            catch (Exception ex)
            {
                toolStripStatusLabel2.Text = "坐标: (无法获取)";
            }
        }

        private void axMapControl2_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            try
            {
                double mapX = e.mapX;
                double mapY = e.mapY;
                toolStripStatusLabel2.Text = $"坐标: ({mapX:F2}, {mapY:F2})";
            }
            catch (Exception ex)
            {
                toolStripStatusLabel2.Text = "坐标: (无法获取)";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // 确保两个地图控件使用相同的坐标系
                if (axMapControl1.Map != null && axMapControl2.Map != null)
                {
                    ESRI.ArcGIS.Geometry.ISpatialReference spatialRef = axMapControl1.Map.SpatialReference;
                    axMapControl2.Map.SpatialReference = spatialRef;
                }

                // 初始绘制一次范围框
                DrawExtentRectangle();
            }
            catch (Exception ex)
            {
                //UpdateStatus($"初始化失败: {ex.Message}");
            }
        }
        // 独立的绘制方法，可以在其他地方调用
        private void DrawExtentRectangle()
        {
            try
            {
                ESRI.ArcGIS.Geometry.IEnvelope ext = this.axMapControl1.Extent;
                if (ext == null || ext.IsEmpty) return;

                // 清除之前的图形
                this.axMapControl2.ActiveView.GraphicsContainer.DeleteAllElements();

                // 创建图形元素
                IElement element = CreateExtentElement(ext);

                // 添加到图形容器
                this.axMapControl2.ActiveView.GraphicsContainer.AddElement(element, 0);

                // 刷新显示
                this.axMapControl2.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            }
            catch (Exception ex)
            {
                //UpdateStatus($"绘制范围框错误: {ex.Message}");
            }
        }

        // 创建范围元素的方法
        private IElement CreateExtentElement(ESRI.ArcGIS.Geometry.IEnvelope extent)
        {
            // 创建线条符号
            ISimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            lineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            lineSymbol.Width = 2.0;

            IRgbColor color = new RgbColor();
            color.Red = 255;
            color.Green = 0;
            color.Blue = 0;
            color.Transparency = 0;
            lineSymbol.Color = color;

            // 创建填充符号（透明）
            ISimpleFillSymbol fillSymbol = new SimpleFillSymbol();
            fillSymbol.Style = esriSimpleFillStyle.esriSFSNull;
            fillSymbol.Outline = lineSymbol;

            // 创建矩形元素
            IRectangleElement rectElement = new RectangleElementClass();
            IElement element = (IElement)rectElement;
            element.Geometry = extent;

            // 设置符号
            IFillShapeElement fillShapeElement = (IFillShapeElement)element;
            fillShapeElement.Symbol = fillSymbol;

            return element;
        }

        private void menuFeatureNew_Click(object sender, EventArgs e)
        {
            ILayer layer = GetSelectedLayer();
            if (layer == null) return;

            // 判断图层是否为要素图层
            IFeatureLayer featureLayer = layer as IFeatureLayer;
            if (featureLayer != null)
            {
                IFeatureClass featureClass = featureLayer.FeatureClass;
                if (featureClass != null)
                {
                    // 获取几何类型
                    esriGeometryType geometryType = featureClass.ShapeType;

                    switch (geometryType)
                    {
                        case esriGeometryType.esriGeometryPoint:
                        case esriGeometryType.esriGeometryMultipoint:
                            // 点图层操作
                            mapOperatorType = MapOperatorType.CreatePoint;
                            break;

                        case esriGeometryType.esriGeometryPolyline:
                        case esriGeometryType.esriGeometryLine:
                            // 线图层操作
                            mapOperatorType = MapOperatorType.CreatePolyline;
                            break;

                        case esriGeometryType.esriGeometryPolygon:
                            // 面图层操作
                            mapOperatorType = MapOperatorType.CreatePolygon;
                            break;

                        default:
                            // 其他几何类型
                            MessageBox.Show("不支持的几何类型");
                            break;
                    }
                }
            }
            else
            {
                // 如果不是要素图层（如图栅格图层等）
                MessageBox.Show("请选择要素图层");
            }
            FormNewFeatureClass formNewFeatureClass = new FormNewFeatureClass();
            formNewFeatureClass.Show();
        }

        private void menuFeatureEditByLocation_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.SelectFeatureByLocation;
        }

        private void menuFeatureEditByRectangle_Click(object sender, EventArgs e)
        {

        }

        private void menuFeatureDeleteByLocation_Click(object sender, EventArgs e)
        {

        }

        private void menuFeatureDeleteByRectangle_Click(object sender, EventArgs e)
        {

        }

        private void menuFeatureDeleteByPolygon_Click(object sender, EventArgs e)
        {

        }
        // 向图层添加点要素
        private void AddPointToLayer(ILayer layer, double x, double y)
        {
            try
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;
                if (featureLayer == null)
                {
                    MessageBox.Show("所选图层不是要素图层");
                    return;
                }

                IFeatureClass featureClass = featureLayer.FeatureClass;
                if (featureClass == null)
                {
                    MessageBox.Show("无法获取要素类");
                    return;
                }

                // 检查几何类型是否为点
                if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint &&
                    featureClass.ShapeType != esriGeometryType.esriGeometryMultipoint)
                {
                    MessageBox.Show("该图层不是点图层");
                    return;
                }

                // 开始编辑会话
                IWorkspaceEdit workspaceEdit = GetWorkspaceEdit(featureClass);
                if (workspaceEdit != null)
                {
                    workspaceEdit.StartEditing(false);
                    workspaceEdit.StartEditOperation();
                }

                // 创建新要素
                IFeature feature = featureClass.CreateFeature();

                // 创建点几何
                IPoint point = new PointClass();
                point.X = x;
                point.Y = y;

                // 设置空间参考（如果需要）
                IGeoDataset geoDataset = featureClass as IGeoDataset;
                if (geoDataset != null)
                {
                    point.SpatialReference = geoDataset.SpatialReference;
                }

                // 设置要素的几何
                feature.Shape = point;

                // 设置属性值（如果有属性字段）
                // feature.set_Value(featureClass.FindField("字段名"), "值");

                // 保存要素
                feature.Store();

                // 结束编辑会话
                if (workspaceEdit != null)
                {
                    workspaceEdit.StopEditOperation();
                    workspaceEdit.StopEditing(true);
                }

                // 刷新地图
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);

                MessageBox.Show("点要素添加成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加点要素时出错: " + ex.Message);
            }
        }

        // 获取工作空间编辑接口
        private IWorkspaceEdit GetWorkspaceEdit(IFeatureClass featureClass)
        {
            IDataset dataset = featureClass as IDataset;
            if (dataset != null)
            {
                IWorkspace workspace = dataset.Workspace;
                return workspace as IWorkspaceEdit;
            }
            return null;
        }
        /// <summary>
        /// 地图点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            ILayer layer = GetSelectedLayer();
            switch (mapOperatorType)
            {
                case MapOperatorType.Default:
                    break;
                case MapOperatorType.CreatePoint:
                    if (layer != null)
                    {
                        // 将屏幕坐标转换为地图坐标
                        IPoint mapPoint = axMapControl1.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
                        AddPointToLayer(layer, mapPoint.X, mapPoint.Y);
                    }
                    break;
                case MapOperatorType.CreatePolyline:
                    break;
                case MapOperatorType.CreatePolygon:
                    break;
                //点选要素
                case MapOperatorType.SelectFeatureByLocation:
                    IActiveView activeView = axMapControl1.ActiveView;
                    //创建一个点对象，用于存储鼠标点击的地图坐标
                    IPoint point = new PointClass();
                    point.PutCoords(e.mapX, e.mapY);
                    //将图层转换为该接口，该接口支持要素识别
                    IIdentify identifyLayer = (IIdentify)layer;
                    //在点击位置进行要素识别，返回识别结果
                    IArray array = identifyLayer.Identify(point);
                    //检查是否识别到要素
                    if(array != null && array.Count > 0)
                    {
                        //获取数组中的第一个元素
                        object obj = array.get_Element(0);
                        //将识别结果转换为要素识别的对象
                        IFeatureIdentifyObj fobj = obj as IFeatureIdentifyObj;
                        IRowIdentifyObject irow = fobj as IRowIdentifyObject;
                        //获取选中的要素
                        IFeature feature = irow.Row as IFeature;
                        //高亮显示选中的要素
                        this.axMapControl1.Map.SelectFeature(layer, feature);
                        //刷新地图显示
                        activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                        activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                        //打开要素编辑窗体
                        using (FormEditFeature editForm = new FormEditFeature(feature, layer as IFeatureLayer, activeView))
                        {
                            DialogResult result = editForm.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                this.axMapControl1.Map.ClearSelection();
                                activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                                activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                            }
                        }
                    }
                    break;
                default:
                    break;

            }

        }
    }
}