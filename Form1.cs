using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EngineWindowsApplication1
{
    public partial class Form1 : Form
    {
        MapOperatorType mapOperatorType;

        // 用于存储折线绘制过程中的临时点集合
        private List<IPoint> polylinePoints = new List<IPoint>();
        // 用于存储多边形绘制过程中的临时点集合
        private List<IPoint> polygonPoints = new List<IPoint>();

        //用于多义线缓冲区查询
        private MapOperatorType m_currentOperator = MapOperatorType.Default;
        private IGraphicsContainer m_graphicsContainer;
        private IElement m_bufferElement;
        public Form1()
        {
            InitializeComponent();

            SetupStatusStrip();

            axTOCControl1.SetBuddyControl(axMapControl1);

            axMapControl2.AutoMouseWheel = false;
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
            DeleteFeatureByPolygon,
            /// <summary>
            /// 多义线缓冲区查询
            /// </summary>
            QueryByPolylineBuffer,
            /// <summary>
            /// 获取点击位置高程
            /// </summary>
            GetClickPointElevation,
            /// <summary>
            /// 面要素点击查询
            /// </summary>
            PolygonClickQuery,            
            /// <summary>
            /// 面要素点击查询
            /// </summary>
            PolylineClickQuery,
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
            //FormNewFeatureClass formNewFeatureClass = new FormNewFeatureClass();
            //formNewFeatureClass.Show();
        }
        /// <summary>
        /// 点选编辑按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFeatureEditByLocation_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.SelectFeatureByLocation;
        }
        /// <summary>
        /// 框选编辑按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFeatureEditByRectangle_Click(object sender, EventArgs e)
        {
            //设置当前操作类型为框选
            mapOperatorType = MapOperatorType.SelectFeatureByRectangle;
            //改变鼠标光标样式提示用户
            axMapControl1.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
        }

        private void menuFeatureDeleteByLocation_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.DeleteFeatureByLocation;
        }

        private void menuFeatureDeleteByRectangle_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.DeleteFeatureByRectangle;
        }

        private void menuFeatureDeleteByPolygon_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.DeleteFeatureByPolygon;
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

        /// <summary>
        /// 高亮显示选中要素的通用方法
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="feature"></param>
        /// <param name="activeView"></param>
        //private void HighLightFeature(ILayer layer, IFeature feature, IActiveView activeView)
        //{
        //    axMapControl1.Map.ClearSelection(); // 清除之前的选择
        //    axMapControl1.Map.SelectFeature(layer, feature); // 高亮当前要素
        //    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null); // 刷新选择高亮
        //}

        /// <summary>
        /// 点选编辑要素主函数
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void SelectFeatureByLocation_Func(IMapControlEvents2_OnMouseDownEvent e, ILayer layer, IActiveView activeView)
        {
            //创建一个点对象，用于存储鼠标点击的地图坐标
            IPoint point = new PointClass();
            point.PutCoords(e.mapX, e.mapY);
            //将图层转换为该接口，该接口支持要素识别
            IIdentify identifyLayer = (IIdentify)layer;
            //在点击位置进行要素识别，返回识别结果
            IArray array = identifyLayer.Identify(point);
            //检查是否识别到要素
            if (array != null && array.Count > 0)
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
                    this.axMapControl1.Map.ClearSelection();
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
            }
        }
        /// <summary>
        /// 框选编辑要素主函数
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private void SelectFeatureByRectangle_Func(IMapControlEvents2_OnMouseDownEvent e, ILayer layer, IActiveView activeView)
        {
            try
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;

                //1. 让用户在地图上绘制矩形
                IEnvelope envelope = axMapControl1.TrackRectangle();
                if (envelope == null || envelope.IsEmpty)
                {
                    MessageBox.Show("未绘制有效的选择范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                // 2. 创建空间过滤器
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = envelope;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                // 3. 搜索要素
                IFeatureCursor featureCursor = featureLayer.FeatureClass.Search(spatialFilter, true);
                IFeature feature = featureCursor.NextFeature();

                //高亮显示选中的要素
                this.axMapControl1.Map.SelectFeature(layer, feature);
                //刷新地图显示
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                //打开要素编辑窗体
                using (FormEditFeature editForm = new FormEditFeature(feature, layer as IFeatureLayer, activeView))
                {
                    DialogResult result = editForm.ShowDialog();
                    this.axMapControl1.Map.ClearSelection();
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"框选编辑要素失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 点选删除要素函数
        /// </summary>
        /// <param name="e"></param>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void DeleteFeatureByLocation_Func(IMapControlEvents2_OnMouseDownEvent e, ILayer layer, IActiveView activeView)
        {
            //创建一个点对象，用于存储鼠标点击的地图坐标
            IPoint point = new PointClass();
            point.PutCoords(e.mapX, e.mapY);
            //将图层转换为该接口，该接口支持要素识别
            IIdentify identifyLayer = (IIdentify)layer;
            //在点击位置进行要素识别，返回识别结果
            IArray array = identifyLayer.Identify(point);
            //检查是否识别到要素
            if (array != null && array.Count > 0)
            {
                //获取数组中的第一个元素
                object obj = array.get_Element(0);
                //将识别结果转换为要素识别的对象
                IFeatureIdentifyObj fobj = obj as IFeatureIdentifyObj;
                IRowIdentifyObject irow = fobj as IRowIdentifyObject;
                //获取选中的要素
                IFeature feature = irow.Row as IFeature;
                //高亮显示选中的要素
                HighlightSelectedFeatures(new List<IFeature> { feature }, layer, activeView);
                //删除要素
                DeleteSelectedFeatures(layer, activeView);
            }
        }
        /// <summary>
        /// 框选删除要素函数
        /// </summary>
        /// <param name="e"></param>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void DeleteFeatureByRectangle_Func(IMapControlEvents2_OnMouseDownEvent e, ILayer layer, IActiveView activeView)
        {
            try
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;
                //1. 让用户在地图上绘制矩形
                IEnvelope envelope = axMapControl1.TrackRectangle();
                if (envelope == null || envelope.IsEmpty)
                {
                    MessageBox.Show("未绘制有效的选择范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //2. 创建空间过滤器
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = envelope;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                //3. 搜索要素
                //此处改为false，禁用回收
                IFeatureCursor featureCursor = featureLayer.FeatureClass.Search(spatialFilter, false);
                IFeature feature = featureCursor.NextFeature();
                //4. 创建要素集合
                List<IFeature> features = new List<IFeature>();
                //5. 将要素放入要素集合中
                while (feature != null)
                {
                    features.Add(feature);
                    feature = featureCursor.NextFeature();
                }

                if (features.Count > 0)
                {
                    MessageBox.Show($"选中{features.Count}个要素！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //高亮显示选中的要素
                HighlightSelectedFeatures(features, layer, activeView);
                //HighlightSelectedFeatures(new List<IFeature> { feature }, layer, activeView);
                //删除要素
                DeleteSelectedFeatures(layer, activeView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"框选删除要素失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 多边形选择删除要素
        /// </summary>
        /// <param name="e"></param>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void DeleteFeatureByPolygon_Func(IMapControlEvents2_OnMouseDownEvent e, ILayer layer, IActiveView activeView)
        {
            try
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;
                //1. 让用户在地图上绘制多边形
                IPolygon polygon = (IPolygon)axMapControl1.TrackPolygon();
                if (polygon == null || polygon.IsEmpty)
                {
                    MessageBox.Show("未绘制有效的选择范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //2. 创建空间过滤器
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = polygon;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                //3. 搜索要素
                IFeatureCursor featureCursor = featureLayer.FeatureClass.Search(spatialFilter, false);
                IFeature feature = featureCursor.NextFeature();
                //4. 创建要素集合
                List<IFeature> features = new List<IFeature>();
                //5. 将要素放入要素集合中
                while (feature != null)
                {
                    features.Add(feature);
                    feature = featureCursor.NextFeature();
                }

                if (features.Count > 0)
                {
                    MessageBox.Show($"选中{features.Count}个要素！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //高亮显示选中的要素
                HighlightSelectedFeatures(features, layer, activeView);
                //删除要素
                DeleteSelectedFeatures(layer, activeView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"多边形选择删除要素失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 高亮选中的要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void HighlightSelectedFeatures(List<IFeature> features, ILayer layer, IActiveView activeView)
        {
            try
            {
                //清除之前的选择
                axMapControl1.Map.ClearSelection();
                //高亮所有选中的要素
                foreach (IFeature feature in features)
                {
                    axMapControl1.Map.SelectFeature(layer, feature);
                }
                //刷新
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"高亮选中要素失败:{ex.Message}！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 批量删除选中要素
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void DeleteSelectedFeatures(ILayer layer, IActiveView activeView)
        {
            try
            {
                IMap map = axMapControl1.Map;
                //检查是否有选中的要素
                if(map.SelectionCount == 0)
                {
                    MessageBox.Show("没有选中要素！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //确认删除
                if(MessageBox.Show($"确定要删除选中的{map.SelectionCount}个要素吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    axMapControl1.Map.ClearSelection();
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                    return;
                }

                IWorkspaceEdit workspaceEdit = null;
                bool intEditSession = false;

                //获取工作空间编辑接口
                IFeatureLayer featureLayer = layer as IFeatureLayer;
                IDataset dataset = featureLayer.FeatureClass as IDataset;
                workspaceEdit = dataset.Workspace as IWorkspaceEdit;

                if(workspaceEdit == null)
                {
                    MessageBox.Show("工作空间不支持编辑！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                //检查编辑会话状态
                intEditSession = workspaceEdit.IsBeingEdited();
                if(!intEditSession)
                {
                    workspaceEdit.StartEditing(true);
                }
                //开始编辑操作
                workspaceEdit.StartEditOperation();

                //获取选中的要素并删除
                IEnumFeature enumFeature = map.FeatureSelection as IEnumFeature;
                enumFeature.Reset();
                IFeature feature = enumFeature.Next();
                int deleteCount = 0;
                while(feature != null)
                {
                    feature.Delete();
                    feature = enumFeature.Next();
                    deleteCount++;
                }
                //停止编辑操作
                workspaceEdit.StopEditOperation();
                //如果之前不在编辑会话，停止编辑并保存
                if(!intEditSession)
                {
                    workspaceEdit.StopEditing(true);
                }
                //清除选择并更新地图
                map.ClearSelection();
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                activeView.Refresh();
                MessageBox.Show($"成功删除{deleteCount}个要素！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除选中要素失败:{ex.Message}！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 标识并显示要素信息
        /// </summary>
        /// <param name="e"></param>
        /// <param name="layer"></param>
        /// <param name="activeView"></param>
        private void IdentifyFeature_Func(IMapControlEvents2_OnMouseDownEvent e, ILayer layer, IActiveView activeView)
        {
            //创建一个点对象，用于存储鼠标点击的地图坐标
            IPoint point = new PointClass();
            point.PutCoords(e.mapX, e.mapY);
            //将图层转换为该接口，该接口支持要素识别
            IIdentify identifyLayer = (IIdentify)layer;
            //在点击位置进行要素识别，返回识别结果
            IArray array = identifyLayer.Identify(point);
            //检查是否识别到要素
            if (array != null && array.Count > 0)
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
                //打开标识要素窗体
                using (FormIdentifyFeature identifyForm = new FormIdentifyFeature(feature))
                {
                    identifyForm.ShowDialog();
                    this.axMapControl1.Map.ClearSelection();
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                    activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
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
            IActiveView activeView = axMapControl1.ActiveView;
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
                    if (layer != null && e.button == 1) // 左键开始绘制
                    {
                        // 使用 TrackLine 绘制折线
                        IPolyline polyline = axMapControl1.TrackLine() as IPolyline;
                        if (polyline != null)
                        {
                            AddPolylineToLayer(layer, polyline);
                        }
                    }
                    break;
                case MapOperatorType.CreatePolygon:
                    if (layer != null && e.button == 1) // 左键开始绘制
                    {
                        // 使用 TrackPolygon 绘制多边形
                        IPolygon polygon = axMapControl1.TrackPolygon() as IPolygon;
                        if (polygon != null)
                        {
                            AddPolygonToLayer(layer, polygon);
                        }
                    }
                    break;
                //点选编辑要素
                case MapOperatorType.SelectFeatureByLocation:
                    SelectFeatureByLocation_Func(e, layer, activeView);
                    break;
                //框选编辑要素
                case MapOperatorType.SelectFeatureByRectangle:
                    SelectFeatureByRectangle_Func(e, layer, activeView);
                    break;
                //点选删除要素
                case MapOperatorType.DeleteFeatureByLocation:
                    DeleteFeatureByLocation_Func(e, layer, activeView);
                    break;
                //框选删除要素
                case MapOperatorType.DeleteFeatureByRectangle:
                    DeleteFeatureByRectangle_Func(e, layer, activeView);
                    break;
                //多边形选择删除要素
                case MapOperatorType.DeleteFeatureByPolygon:
                    DeleteFeatureByPolygon_Func(e, layer, activeView);
                    break;
                case MapOperatorType.IdentifyFeature:
                    IdentifyFeature_Func(e, layer, activeView);
                    break;
                case MapOperatorType.QueryByPolylineBuffer:
                    HandlePolylineBufferMouseDown(e);
                    break;
                case MapOperatorType.GetClickPointElevation:
                    HandleGetElevationMouseDown(e);
                    break;
                case MapOperatorType.PolygonClickQuery:
                    IdentifyPolygonAtLocation(e.x, e.y);
                    break;                
                case MapOperatorType.PolylineClickQuery:
                    IdentifyPolygonAtLocation(e.x, e.y);
                    break;
                default:
                    break;

            }

        }
        /// <summary>
        /// 在指定位置查询面要素
        /// </summary>
        /// <param name="x">鼠标X坐标</param>
        /// <param name="y">鼠标Y坐标</param>
        private void IdentifyPolygonAtLocation(int x, int y)
        {
            try
            {
                // 1. 获取当前选中图层
                var layer = GetSelectedLayer();
                if (layer == null)
                {
                    MessageBox.Show("请先选择一个图层", "提示");
                    return;
                }

                // 2. 检查是否为面图层
                if (!(layer is IFeatureLayer featureLayer))
                {
                    return;
                }

                // 3. 将屏幕坐标转换为地图坐标点
                var screenPoint = new tagPOINT { x = x, y = y };
                IPoint clickPoint = axMapControl1.ToMapPoint(screenPoint.x, screenPoint.y);

                // 4. 执行要素查询
                IFeature hitFeature = FindFeatureAtPoint(featureLayer, clickPoint);

                // 5. 打印查询结果
                PrintPolygonInfo(hitFeature, featureLayer);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询面要素时发生错误: {ex.Message}", "错误");
            }
        }
        private IFeature FindFeatureAtPoint(IFeatureLayer featureLayer, IPoint point, double tolerance = 10)
        {
            if (featureLayer == null || point == null)
                return null;

            try
            {
                // 获取要素类几何类型
                IFeatureClass featureClass = featureLayer.FeatureClass;
                esriGeometryType geometryType = featureClass.ShapeType;

                // 根据几何类型选择查询策略
                switch (geometryType)
                {
                    case esriGeometryType.esriGeometryPolygon:
                        return FindPolygonAtPoint(featureLayer, point);

                    case esriGeometryType.esriGeometryPolyline:
                        return FindLineAtPoint(featureLayer, point, tolerance);

                    //case esriGeometryType.esriGeometryPoint:
                    //    return FindPointAtPoint(featureLayer, point, tolerance);

                    default:
                        System.Diagnostics.Debug.WriteLine($"不支持的几何类型: {geometryType}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查询要素时出错: {ex.Message}");
                return null;
            }
        }
        private IFeature FindLineAtPoint(IFeatureLayer featureLayer, IPoint point, double tolerance = 10)
        {
            if (featureLayer == null || point == null)
                return null;

            IFeatureCursor featureCursor = null;
            IFeature closestFeature = null;

            try
            {
                IFeatureClass featureClass = featureLayer.FeatureClass;
                IGeoDataset featureGeoDataset = (IGeoDataset)featureClass;
                ISpatialReference featureSpatialRef = featureGeoDataset.SpatialReference;

                // 修复1：使用更安全的空间参考检查方法
                IPoint searchPoint = EnsureSpatialReference(point, featureSpatialRef);
                if (searchPoint == null)
                {
                    System.Diagnostics.Debug.WriteLine("无法统一空间参考系统");
                    return null;
                }

                // 创建缓冲区
                ITopologicalOperator topoOp = searchPoint as ITopologicalOperator;
                if (topoOp == null) return null;

                IPolygon searchGeometry = topoOp.Buffer(tolerance) as IPolygon;

                // 创建空间过滤器
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = searchGeometry;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.WhereClause = "";

                // 修复2：移除有问题的设置（如果会导致错误）
                // spatialFilter.OutputSpatialReference = featureSpatialRef;

                double minDistance = double.MaxValue;

                try
                {
                    featureCursor = featureLayer.Search(spatialFilter, false);
                    IFeature feature = featureCursor.NextFeature();

                    while (feature != null)
                    {
                        try
                        {
                            if (IsValidLineGeometry(feature.Shape))
                            {
                                IProximityOperator proxOp = feature.Shape as IProximityOperator;
                                if (proxOp != null)
                                {
                                    double distance = proxOp.ReturnDistance(searchPoint);
                                    if (distance < minDistance && distance <= tolerance)
                                    {
                                        minDistance = distance;
                                        closestFeature = feature;
                                    }
                                }
                            }
                        }
                        catch (Exception featureEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"处理要素 {feature.OID} 时出错: {featureEx.Message}");
                        }

                        feature = featureCursor.NextFeature();
                    }

                    return closestFeature;
                }
                finally
                {
                    if (featureCursor != null)
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查询线要素时出错: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 验证线几何是否有效
        /// </summary>
        private bool IsValidLineGeometry(IGeometry geometry)
        {
            try
            {
                if (geometry == null || geometry.IsEmpty)
                    return false;

                // 检查是否是线要素
                IPolyline line = geometry as IPolyline;
                if (line == null)
                    return false;

                // 检查线是否至少有两个点
                IPointCollection pointCollection = line as IPointCollection;
                if (pointCollection == null || pointCollection.PointCount < 2)
                    return false;

                // 检查线的长度是否大于0
                ICurve curve = line as ICurve;
                if (curve != null && curve.Length <= 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全的空间参考处理方法
        /// </summary>
        private IPoint EnsureSpatialReference(IPoint point, ISpatialReference targetSpatialRef)
        {
            try
            {
                if (point == null || targetSpatialRef == null)
                    return null;

                // 检查点的空间参考是否存在
                if (point.SpatialReference == null)
                {
                    // 如果点的空间参考为空，直接分配目标空间参考
                    point.SpatialReference = targetSpatialRef;
                    return point;
                }

                // 修复3：使用更安全的空间参考比较方法
                if (!IsSpatialReferenceEqual(point.SpatialReference, targetSpatialRef))
                {
                    try
                    {
                        // 尝试投影转换
                        point.Project(targetSpatialRef);
                        System.Diagnostics.Debug.WriteLine("点的空间参考已投影转换");
                    }
                    catch (Exception projEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"投影转换失败: {projEx.Message}");
                        // 创建新点并复制坐标
                        IPoint newPoint = new PointClass();
                        newPoint.X = point.X;
                        newPoint.Y = point.Y;
                        newPoint.SpatialReference = targetSpatialRef;
                        return newPoint;
                    }
                }

                return point;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"统一空间参考时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 安全的空间参考比较方法
        /// </summary>
        private bool IsSpatialReferenceEqual(ISpatialReference sr1, ISpatialReference sr2)
        {
                // 方法2：比较工厂代码作为备选
            try
            {
                IProjectedCoordinateSystem pcs1 = sr1 as IProjectedCoordinateSystem;
                IProjectedCoordinateSystem pcs2 = sr2 as IProjectedCoordinateSystem;
                if (pcs1 != null && pcs2 != null)
                {
                    return pcs1.FactoryCode == pcs2.FactoryCode;
                }

                IGeographicCoordinateSystem gcs1 = sr1 as IGeographicCoordinateSystem;
                IGeographicCoordinateSystem gcs2 = sr2 as IGeographicCoordinateSystem;
                if (gcs1 != null && gcs2 != null)
                {
                    return gcs1.FactoryCode == gcs2.FactoryCode;
                }
            }
            catch
            {
                // 忽略比较异常
            }

            return false;
        }

        /// <summary>
        /// 在指定点查找面要素
        /// </summary>
        /// <param name="featureLayer">面图层</param>
        /// <param name="point">查询点</param>
        /// <returns>命中的面要素，未找到返回null</returns>
        private IFeature FindPolygonAtPoint(IFeatureLayer featureLayer, IPoint point)
        {
            // 检查输入参数
            if (featureLayer == null || point == null)
                return null;

            try
            {

                // 创建空间过滤器
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = point;

                // 修正空间关系：点被面包含
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelWithin;

                // 或者使用相交关系，更宽松
                // spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                spatialFilter.WhereClause = "";

                // 设置搜索容差（重要！）
                ISpatialFilter spatialFilter2 = spatialFilter as ISpatialFilter;
                if (spatialFilter2 != null)
                {
                    spatialFilter2.SearchOrder = esriSearchOrder.esriSearchOrderSpatial;
                }

                IFeatureCursor featureCursor = null;
                try
                {
                    featureCursor = featureLayer.Search(spatialFilter, false);
                    IFeature feature = featureCursor.NextFeature();

                    if (feature != null)
                    {
                        // 直接返回找到的要素，不需要重新获取
                        return feature;
                    }
                }
                finally
                {
                    if (featureCursor != null)
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
                }
            }
            catch (Exception ex)
            {
                // 记录异常信息
                System.Diagnostics.Debug.WriteLine($"查询要素时出错: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 打印面要素信息
        /// </summary>
        /// <param name="feature">面要素</param>
        /// <param name="featureLayer">要素图层</param>
        private void PrintPolygonInfo(IFeature feature, IFeatureLayer featureLayer)
        {
            if (feature == null)
            {
                Console.WriteLine("未找到要素");
                MessageBox.Show("未找到要素", "查询结果");
                return;
            }

            // 获取要素基本信息
            int featureID = feature.OID;
            string featureName = GetPolygonName(feature);

            // 获取几何信息
            string geometryInfo = GetGeometryInfo(feature);

            // 构建输出信息
            StringBuilder info = new System.Text.StringBuilder();
            info.AppendLine($"=== 要素查询结果 ===");
            info.AppendLine($"图层名称: {featureLayer.Name}");
            info.AppendLine($"要素ID: {featureID}");
            info.AppendLine($"要素名称: {featureName}");
            info.AppendLine($"几何信息: {geometryInfo}");
            info.AppendLine($"------------------------");

            // 打印到控制台
            Console.WriteLine(info.ToString());

            // 显示消息框
            MessageBox.Show(info.ToString(), "要素查询结果");
        }

        /// <summary>
        /// 获取面要素的名称
        /// </summary>
        /// <param name="feature">面要素</param>
        /// <returns>要素名称</returns>
        private string GetPolygonName(IFeature feature)
        {
            // 面要素常见的名称字段
            string[] polygonNameFields = {
        "NAME", "名称", "MC", "地名", "BUILDING_NAME", "AREA_NAME",
        "POLYGON_NAME", "FEATURE_NAME", "LABEL", "DESCRIPTION"
    };

            IFields fields = feature.Fields;
            for (int i = 0; i < fields.FieldCount; i++)
            {
                IField field = fields.Field[i];
                string fieldName = field.Name.ToUpper();

                if (polygonNameFields.Contains(fieldName))
                {
                    object value = feature.get_Value(i);
                    if (value != null && !Convert.IsDBNull(value))
                    {
                        return value.ToString();
                    }
                }
            }

            return "未命名";
        }

        private string GetGeometryInfo(IFeature feature)
        {
            if (feature?.Shape == null || feature.Shape.IsEmpty)
                return "无几何信息";

            try
            {
                IGeometry geometry = feature.Shape;
                string geometryType = GetGeometryType(geometry);

                switch (geometry.GeometryType)
                {
                    case esriGeometryType.esriGeometryPolygon:
                        return GetPolygonInfo(geometry);
                    case esriGeometryType.esriGeometryPolyline:
                        return GetPolylineInfo(geometry);
                    case esriGeometryType.esriGeometryPoint:
                        return GetPointInfo(geometry);
                    default:
                        return $"不支持的地理类型: {geometryType}";
                }
            }
            catch (Exception ex)
            {
                return $"几何信息获取失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取几何类型描述
        /// </summary>
        private string GetGeometryType(IGeometry geometry)
        {
            if (geometry == null) return "未知";

            switch (geometry.GeometryType)
            {
                case esriGeometryType.esriGeometryPolygon: return "面";
                case esriGeometryType.esriGeometryPolyline: return "线";
                case esriGeometryType.esriGeometryPoint: return "点";
                case esriGeometryType.esriGeometryMultiPatch: return "多面片";
                case esriGeometryType.esriGeometryEnvelope: return "包络矩形";
                default: return geometry.GeometryType.ToString();
            }
        }

        /// <summary>
        /// 获取面要素信息
        /// </summary>
        private string GetPolygonInfo(IGeometry geometry)
        {
            IPolygon polygon = geometry as IPolygon;
            if (polygon == null) return "面几何转换失败";

            IArea area = polygon as IArea;
            ICurve curve = polygon as ICurve;
            IPointCollection pointCollection = polygon as IPointCollection;

            double polygonArea = area?.Area ?? 0;
            double polygonLength = curve?.Length ?? 0;
            int pointCount = pointCollection?.PointCount ?? 0;
            int partCount = GetPartCount(polygon);

            // 获取外包矩形信息
            IEnvelope envelope = polygon.Envelope;
            double width = envelope.Width;
            double height = envelope.Height;
            double centerX = (envelope.XMin + envelope.XMax) / 2;
            double centerY = (envelope.YMin + envelope.YMax) / 2;

            return $"面要素 - " +
                   $"面积: {polygonArea:F2}, " +
                   $"周长: {polygonLength:F2}, " +
                   $"范围: {width:F2} × {height:F2}, " +
                   $"中心: ({centerX:F2}, {centerY:F2}), " +
                   $"顶点数: {pointCount}, " +
                   $"环数: {partCount}";
        }

        /// <summary>
        /// 获取线要素信息
        /// </summary>
        private string GetPolylineInfo(IGeometry geometry)
        {
            IPolyline polyline = geometry as IPolyline;
            if (polyline == null) return "线几何转换失败";

            ICurve curve = polyline as ICurve;
            IPointCollection pointCollection = polyline as IPointCollection;

            double length = curve?.Length ?? 0;
            int pointCount = pointCollection?.PointCount ?? 0;
            int partCount = GetPartCount(polyline);

            // 获取起点和终点
            IPoint fromPoint = null;
            IPoint toPoint = null;
            try
            {
                fromPoint = curve.FromPoint;
                toPoint = curve.ToPoint;
            }
            catch
            {
                // 忽略起点终点获取失败
            }

            // 获取外包矩形信息
            IEnvelope envelope = polyline.Envelope;
            double width = envelope.Width;
            double height = envelope.Height;
            double centerX = (envelope.XMin + envelope.XMax) / 2;
            double centerY = (envelope.YMin + envelope.YMax) / 2;

            string fromToInfo = (fromPoint != null && toPoint != null) ?
                $", 起点: ({fromPoint.X:F2}, {fromPoint.Y:F2}), 终点: ({toPoint.X:F2}, {toPoint.Y:F2})" : "";

            return $"线要素 - " +
                   $"长度: {length:F2}, " +
                   $"范围: {width:F2} × {height:F2}, " +
                   $"中心: ({centerX:F2}, {centerY:F2}), " +
                   $"顶点数: {pointCount}, " +
                   $"部分数: {partCount}" +
                   fromToInfo;
        }

        /// <summary>
        /// 获取点要素信息
        /// </summary>
        private string GetPointInfo(IGeometry geometry)
        {
            IPoint point = geometry as IPoint;
            if (point == null) return "点几何转换失败";

            return $"点要素 - " +
                   $"坐标: ({point.X:F2}, {point.Y:F2})";
        }

        /// <summary>
        /// 获取多点要素信息
        /// </summary>
        private string GetMultipointInfo(IGeometry geometry)
        {
            IMultipoint multipoint = geometry as IMultipoint;
            if (multipoint == null) return "多点几何转换失败";

            IPointCollection pointCollection = multipoint as IPointCollection;
            int pointCount = pointCollection?.PointCount ?? 0;

            // 获取外包矩形信息
            IEnvelope envelope = multipoint.Envelope;
            double width = envelope.Width;
            double height = envelope.Height;
            double centerX = (envelope.XMin + envelope.XMax) / 2;
            double centerY = (envelope.YMin + envelope.YMax) / 2;

            return $"多点要素 - " +
                   $"点数: {pointCount}, " +
                   $"范围: {width:F2} × {height:F2}, " +
                   $"中心: ({centerX:F2}, {centerY:F2})";
        }

        /// <summary>
        /// 获取几何部分数量（环数或线段数）
        /// </summary>
        private int GetPartCount(IGeometry geometry)
        {
            try
            {
                IGeometryCollection geometryCollection = geometry as IGeometryCollection;
                return geometryCollection?.GeometryCount ?? 1;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// 简化的通用方法（如果只需要基本信息）
        /// </summary>
        private string GetSimpleGeometryInfo(IFeature feature)
        {
            if (feature?.Shape == null || feature.Shape.IsEmpty)
                return "无几何信息";

            try
            {
                IGeometry geometry = feature.Shape;
                string geometryType = GetGeometryType(geometry);

                // 通用信息
                IEnvelope envelope = geometry.Envelope;
                double width = envelope.Width;
                double height = envelope.Height;
                double centerX = (envelope.XMin + envelope.XMax) / 2;
                double centerY = (envelope.YMin + envelope.YMax) / 2;

                string info = $"{geometryType}要素 - 范围: {width:F2} × {height:F2}, 中心: ({centerX:F2}, {centerY:F2})";

                // 类型特定信息
                if (geometry.GeometryType == esriGeometryType.esriGeometryPolygon)
                {
                    IArea area = geometry as IArea;
                    if (area != null)
                        info += $", 面积: {area.Area:F2}";
                }

                if (geometry.GeometryType == esriGeometryType.esriGeometryPolyline ||
                    geometry.GeometryType == esriGeometryType.esriGeometryPolygon)
                {
                    ICurve curve = geometry as ICurve;
                    if (curve != null)
                        info += $", 长度: {curve.Length:F2}";
                }

                return info;
            }
            catch (Exception ex)
            {
                return $"几何信息获取失败: {ex.Message}";
            }
        }
        private void AddPolylineToLayer(ILayer layer, IPolyline polyline)
        {
            try
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;
                if (featureLayer == null) return;

                IFeatureClass featureClass = featureLayer.FeatureClass;
                if (featureClass.ShapeType != esriGeometryType.esriGeometryPolyline)
                {
                    MessageBox.Show("当前图层不是线图层！");
                    return;
                }

                // 使用与多边形相同的兼容性处理方法
                IPolyline finalPolyline = EnsureZValueCompatibility(polyline, featureClass);
                IPolyline compatiblePolyline = CreateCompatibleGeometry(featureClass, finalPolyline) as IPolyline;
                if (compatiblePolyline == null) return;

                // 确保几何体有效
                ITopologicalOperator topoOp = compatiblePolyline as ITopologicalOperator;
                if (topoOp != null)
                {
                    topoOp.Simplify();
                }

                // 添加要素
                IFeature feature = featureClass.CreateFeature();
                feature.Shape = compatiblePolyline;
                feature.Store();

                // 刷新地图
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);

                MessageBox.Show("折线添加成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加折线失败：" + ex.Message);
            }
            mapOperatorType = MapOperatorType.Default;
        }

        private IPolyline EnsureZValueCompatibility(IPolyline polyline, IFeatureClass featureClass)
        {
            try
            {
                // 方法1：检查要素类是否需要 Z 值
                bool requiresZ = CheckIfFeatureClassRequiresZ(featureClass);

                if (requiresZ)
                {
                    // 如果要素类需要 Z 值，确保几何体有 Z 值
                    //IZAware zAware = polyline as IZAware;
                    //if (zAware != null && !zAware.ZAware)
                    //{
                    //    zAware.ZAware = true;
                    //}
                    //// 确保所有点都有 Z 值
                    //IPointCollection pointCollection = polyline as IPointCollection;
                    //for (int i = 0; i < pointCollection.PointCount; i++)
                    //{
                    //    IPoint point = pointCollection.get_Point(i);
                    //    IZAware pointZAware = point as IZAware;
                    //    if (pointZAware != null && !pointZAware.ZAware)
                    //    {
                    //        pointZAware.ZAware = true;
                    //        if (double.IsNaN(point.Z))
                    //        {
                    //            point.Z = 0.0; // 设置默认 Z 值
                    //        }
                    //    }
                    //}
                    return CreateNewPolylineWithZ(polyline, featureClass);
                }

                return polyline;
            }
            catch (Exception ex)
            {
                // 如果失败，尝试创建新的几何体
                return CreateNewPolylineWithZ(polyline, featureClass);
            }
        }

        private IPolyline CreateNewPolylineWithZ(IPolyline sourcePolyline, IFeatureClass featureClass)
        {
            try
            {
                // 创建新的折线
                IPolyline newPolyline = new PolylineClass();

                // 设置为 Z aware
                IZAware zAware = newPolyline as IZAware;
                if (zAware != null)
                {
                    zAware.ZAware = true;
                }

                // 复制点
                IPointCollection sourcePoints = sourcePolyline as IPointCollection;
                IPointCollection targetPoints = newPolyline as IPointCollection;

                for (int i = 0; i < sourcePoints.PointCount; i++)
                {
                    IPoint sourcePoint = sourcePoints.get_Point(i);
                    IPoint newPoint = new PointClass();
                    newPoint.X = sourcePoint.X;
                    newPoint.Y = sourcePoint.Y;
                    newPoint.Z = 0.0; // 设置默认 Z 值

                    // 设置为 Z aware
                    IZAware pointZAware = newPoint as IZAware;
                    if (pointZAware != null)
                    {
                        pointZAware.ZAware = true;
                    }

                    targetPoints.AddPoint(newPoint);
                }

                return newPolyline;
            }
            catch (Exception ex)
            {
                throw new Exception("创建带 Z 值的折线失败: " + ex.Message);
            }
        }

        private bool CheckIfFeatureClassRequiresZ(IFeatureClass featureClass)
        {
            string shapeFieldName = featureClass.ShapeFieldName;
            if (featureClass.Fields.get_Field(featureClass.FindField(shapeFieldName)).GeometryDef.HasZ)
            {
                return true;
            }
            return false;
        }

        private void AddPolygonToLayer(ILayer layer, IPolygon polygon)
        {
            try
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;
                if (featureLayer == null) return;

                IFeatureClass featureClass = featureLayer.FeatureClass;
                if (featureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                {
                    MessageBox.Show("当前图层不是面图层！");
                    return;
                }

                // 确保几何体与要素类兼容
                IPolygon compatiblePolygon = CreateCompatibleGeometry(featureClass, polygon) as IPolygon;
                if (compatiblePolygon == null) return;

                // 确保多边形闭合
                ITopologicalOperator topoOp = compatiblePolygon as ITopologicalOperator;
                if (topoOp != null)
                {
                    topoOp.Simplify();
                }

                // 添加要素
                IFeature feature = featureClass.CreateFeature();
                feature.Shape = compatiblePolygon;
                feature.Store();

                // 刷新地图
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);

                MessageBox.Show("多边形添加成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加多边形失败：" + ex.Message);
            }
            mapOperatorType = MapOperatorType.Default;
        }

        private IGeometry CreateCompatibleGeometry(IFeatureClass featureClass, IGeometry geometry)
        {
            try
            {
                // 方法1：使用要素模板创建兼容几何体
                IFeature templateFeature = featureClass.CreateFeature();
                IGeometry templateGeometry = templateFeature.ShapeCopy;

                if (templateGeometry != null)
                {
                    IClone clone = templateGeometry as IClone;
                    IGeometry compatibleGeometry = clone.Clone() as IGeometry;

                    // 复制坐标
                    IPointCollection sourcePoints = geometry as IPointCollection;
                    IPointCollection targetPoints = compatibleGeometry as IPointCollection;

                    if (sourcePoints != null && targetPoints != null)
                    {
                        targetPoints.RemovePoints(0, targetPoints.PointCount);
                        for (int i = 0; i < sourcePoints.PointCount; i++)
                        {
                            targetPoints.AddPoint(sourcePoints.get_Point(i));
                        }
                    }

                    return compatibleGeometry;
                }
            }
            catch
            {
                // 如果失败，返回原始几何体
            }

            return geometry;
        }

        /// <summary>
        /// 标识按钮响应函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFeatureIdentify_Click(object sender, EventArgs e)
        {
            //设置当前状态为标识要素
            mapOperatorType = MapOperatorType.IdentifyFeature;
        }
        /// <summary>
        /// 要素浏览
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFeatureBrowse_Click(object sender, EventArgs e)
        {
            ILayer layer = GetSelectedLayer();
            if (layer == null)
            {
                MessageBox.Show("请先在TOC中选择要素图层！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            IFeatureLayer featureLayer = layer as IFeatureLayer;
            if (featureLayer == null)
            {
                MessageBox.Show("选中的图层不是要素图层！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 打开浏览窗口
            using (FormFeatureBrowse browseForm = new FormFeatureBrowse(featureLayer))
            {
                browseForm.ShowDialog();
            }
        }

        private void queryMax_Click(object sender, EventArgs e)
        {
            try
            {
                ILayer layer = GetSelectedLayer();
                if (layer == null)
                {
                    MessageBox.Show("请先选择一个图层！");
                    return;
                }

                IFeatureLayer featureLayer = layer as IFeatureLayer;
                if (featureLayer == null || featureLayer.FeatureClass == null)
                {
                    MessageBox.Show("选择的图层不是有效的要素图层！");
                    return;
                }

                // 检查是否为面图层
                if (featureLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                {
                    MessageBox.Show("请选择一个面图层！");
                    return;
                }

                // 查询最大面要素
                IFeature maxFeature = FindMaxAreaFeature(featureLayer.FeatureClass);

                if (maxFeature != null)
                {
                    // 高亮显示最大面要素
                    HighlightFeature(featureLayer, maxFeature);
                    PrintPolygonInfo(maxFeature, featureLayer);
                    double area = GetFeatureArea(maxFeature);
                    MessageBox.Show($"找到最大面要素，面积为：{area:F2} 平方单位");
                }
                else
                {
                    MessageBox.Show("未找到面要素！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询最大面要素时出错：{ex.Message}");
            }
        }

        private void queryMin_Click(object sender, EventArgs e)
        {
            try
            {
                ILayer layer = GetSelectedLayer();
                if (layer == null)
                {
                    MessageBox.Show("请先选择一个图层！");
                    return;
                }

                IFeatureLayer featureLayer = layer as IFeatureLayer;
                if (featureLayer == null || featureLayer.FeatureClass == null)
                {
                    MessageBox.Show("选择的图层不是有效的要素图层！");
                    return;
                }

                // 检查是否为面图层
                if (featureLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                {
                    MessageBox.Show("请选择一个面图层！");
                    return;
                }

                // 查询最小面要素
                IFeature minFeature = FindMinAreaFeature(featureLayer.FeatureClass);

                if (minFeature != null)
                {
                    // 高亮显示最小面要素
                    HighlightFeature(featureLayer, minFeature);
                    PrintPolygonInfo(minFeature, featureLayer);
                    double area = GetFeatureArea(minFeature);
                    MessageBox.Show($"找到最小面要素，面积为：{area:F2} 平方单位");
                }
                else
                {
                    MessageBox.Show("未找到面要素！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询最小面要素时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 查询最大面积的面要素
        /// </summary>
        private IFeature FindMaxAreaFeature(IFeatureClass featureClass)
        {
            IFeature maxFeature = null;
            double maxArea = double.MinValue;

            IFeatureCursor featureCursor = featureClass.Search(null, true);
            IFeature feature = featureCursor.NextFeature();

            while (feature != null)
            {
                double area = GetFeatureArea(feature);
                if (area > maxArea)
                {
                    maxArea = area;
                    // 复制要素而不是直接引用
                    maxFeature = featureClass.GetFeature(feature.OID);
                }
                feature = featureCursor.NextFeature();
            }

            // 释放游标
            System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);

            return maxFeature;
        }

        /// <summary>
        /// 查询最小面积的面要素
        /// </summary>
        private IFeature FindMinAreaFeature(IFeatureClass featureClass)
        {
            IFeature minFeature = null;
            double minArea = double.MaxValue;

            IFeatureCursor featureCursor = featureClass.Search(null, true);
            IFeature feature = featureCursor.NextFeature();

            while (feature != null)
            {
                double area = GetFeatureArea(feature);
                if (area < minArea)
                {
                    minArea = area;
                    minFeature = featureClass.GetFeature(feature.OID); ;
                }
                feature = featureCursor.NextFeature();
            }

            // 释放游标
            System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);

            return minFeature;
        }

        /// <summary>
        /// 获取面要素的面积
        /// </summary>
        private double GetFeatureArea(IFeature feature)
        {
            if (feature.Shape == null)
                return 0;

            IArea area = feature.Shape as IArea;
            if (area != null)
            {
                return area.Area;
            }
            return 0;
        }

        /// <summary>
        /// 高亮显示要素
        /// </summary>
        private void HighlightFeature(IFeatureLayer featureLayer, IFeature feature)
        {
            // 获取地图控件
            IMapControl3 mapControl = (IMapControl3)axMapControl1.Object; // 这里需要替换为您的实际地图控件

            // 清除之前的选择
            mapControl.Map.ClearSelection();

            // 选择要素
            featureLayer.Selectable = true;
            mapControl.Map.SelectFeature(featureLayer, feature);

            // 刷新地图显示
            mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
        }

        private void filterWrongHeightPoint_Click(object sender, EventArgs e)
        {
            try
            {
                ILayer layer = GetSelectedLayer();
                if (layer == null)
                {
                    MessageBox.Show("请先选择一个图层！");
                    return;
                }

                IFeatureLayer featureLayer = layer as IFeatureLayer;
                if (featureLayer == null || featureLayer.FeatureClass == null)
                {
                    MessageBox.Show("选择的图层不是有效的要素图层！");
                    return;
                }

                // 检查是否为点图层
                if (featureLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                {
                    MessageBox.Show("请选择一个点图层！");
                    return;
                }

                // 获取高程字段（假设字段名为"Elevation"或"高程"）
                string elevationFieldName = GetElevationFieldName(featureLayer.FeatureClass);
                if (string.IsNullOrEmpty(elevationFieldName))
                {
                    MessageBox.Show("未找到高程字段！请确保图层包含高程字段。");
                    return;
                }

                // 设置参数（可以改为从界面输入）
                int neighborCount = 10; // N近邻数量
                double stdDevMultiple = 3.0; // 标准差倍数

                // 执行异常值检测和过滤
                int removedCount = DetectAndRemoveHeightOutliers(featureLayer, elevationFieldName, neighborCount, stdDevMultiple);

                MessageBox.Show($"高程点异常值检测完成！共删除 {removedCount} 个异常点。");

                // 刷新地图
                RefreshMap();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"高程点异常值检测时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取高程字段名称
        /// </summary>
        private string GetElevationFieldName(IFeatureClass featureClass)
        {
            // 常见的高程字段名称
            string[] possibleFieldNames = { "Elevation", "高程", "HEIGHT", "Height", "ELEV", "Z", "ALTITUDE" };

            for (int i = 0; i < featureClass.Fields.FieldCount; i++)
            {
                string fieldName = featureClass.Fields.get_Field(i).Name;
                foreach (string possibleName in possibleFieldNames)
                {
                    if (fieldName.Equals(possibleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return fieldName;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 检测并删除高程异常值
        /// </summary>
        private int DetectAndRemoveHeightOutliers(IFeatureLayer featureLayer, string elevationFieldName, int neighborCount, double stdDevMultiple)
        {
            IFeatureClass featureClass = featureLayer.FeatureClass;
            int removedCount = 0;

            // 获取所有高程点
            List<IFeature> allFeatures = GetAllFeatures(featureClass);
            if (allFeatures.Count == 0) return 0;

            // 构建空间索引用于快速邻近搜索
            ISpatialFilter spatialFilter = new SpatialFilterClass();
            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

            // 存储需要删除的要素OID
            List<int> featuresToDelete = new List<int>();

            // 遍历每个要素进行异常值检测
            foreach (IFeature targetFeature in allFeatures)
            {
                try
                {
                    if (targetFeature == null) continue;

                    // 获取目标点的高程值
                    double targetElevation = GetFeatureElevation(targetFeature, elevationFieldName);
                    if (double.IsNaN(targetElevation)) continue;

                    // 获取N近邻
                    List<IFeature> neighbors = FindNearestNeighbors(targetFeature, allFeatures, neighborCount);
                    if (neighbors.Count < 3) continue; // 至少需要3个点才能计算标准差

                    // 计算近邻的高程统计信息
                    double avg, stdDev;
                    CalculateElevationStatistics(neighbors, elevationFieldName, out avg, out stdDev);

                    // 使用3倍标准差法判断异常值
                    if (IsOutlier(targetElevation, avg, stdDev, stdDevMultiple))
                    {
                        featuresToDelete.Add(targetFeature.OID);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"处理要素OID{targetFeature.OID}时出错：{ex.Message}");
                }
            }

            // 删除异常值要素
            if (featuresToDelete.Count > 0)
            {
                removedCount = DeleteFeatures(featureClass, featuresToDelete);
            }

            return removedCount;
        }

        /// <summary>
        /// 获取所有要素
        /// </summary>
        private List<IFeature> GetAllFeatures(IFeatureClass featureClass)
        {
            List<IFeature> features = new List<IFeature>();
            IFeatureCursor featureCursor = null;

            try
            {
                featureCursor = featureClass.Search(null, true);
                IFeature feature = featureCursor.NextFeature();

                while (feature != null)
                {
                    features.Add(feature);
                    feature = featureCursor.NextFeature();
                }
            }
            finally
            {
                if (featureCursor != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
            }

            return features;
        }

        /// <summary>
        /// 查找最近邻的N个要素
        /// </summary>
        private List<IFeature> FindNearestNeighbors(IFeature targetFeature, List<IFeature> allFeatures, int neighborCount)
        {
            // 计算所有点到目标点的距离
            var distances = new List<Tuple<double, IFeature>>();
            IPoint targetPoint = targetFeature.Shape as IPoint;

            foreach (IFeature feature in allFeatures)
            {
                if (feature.OID == targetFeature.OID) continue; // 排除自身

                IPoint point = feature.Shape as IPoint;
                if (point != null)
                {
                    double distance = CalculateDistance(targetPoint, point);
                    distances.Add(new Tuple<double, IFeature>(distance, feature));
                }
            }

            // 按距离排序并取前N个
            return distances.OrderBy(d => d.Item1)
                           .Take(neighborCount)
                           .Select(d => d.Item2)
                           .ToList();
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        private double CalculateDistance(IPoint point1, IPoint point2)
        {
            double dx = point1.X - point2.X;
            double dy = point1.Y - point2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 获取要素的高程值
        /// </summary>
        private double GetFeatureElevation(IFeature feature, string elevationFieldName)
        {
            try
            {
                int fieldIndex = feature.Fields.FindField(elevationFieldName);
                if (fieldIndex == -1) return double.NaN;

                IFeatureClass featureClass = (IFeatureClass)feature.Class;
                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = $"{featureClass.OIDFieldName} = {feature.OID}";


                IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);

                IFeature resultFeature = featureCursor.NextFeature();
                if (resultFeature != null)
                {
                    object value = resultFeature.get_Value(fieldIndex);
                    if (value == null || value == DBNull.Value) return double.NaN;
                    return Convert.ToDouble(value);
                }
                

                return double.NaN;
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// 计算高程统计信息（均值和标准差）
        /// </summary>
        private void CalculateElevationStatistics(List<IFeature> features, string elevationFieldName, out double average, out double stdDev)
        {
            List<double> elevations = new List<double>();

            foreach (IFeature feature in features)
            {
                double elevation = GetFeatureElevation(feature, elevationFieldName);
                if (!double.IsNaN(elevation))
                {
                    elevations.Add(elevation);
                }
            }

            if (elevations.Count == 0)
            {
                average = 0;
                stdDev = 0;
                return;
            }

            // 计算均值
            average = elevations.Average();

            // 计算标准差
            double sumOfSquares = 0;
            foreach (double elevation in elevations)
            {
                sumOfSquares += Math.Pow(elevation - average, 2);
            }
            stdDev = Math.Sqrt(sumOfSquares / elevations.Count);
        }

        /// <summary>
        /// 判断是否为异常值
        /// </summary>
        private bool IsOutlier(double value, double average, double stdDev, double multiple)
        {
            if (stdDev == 0) return false; // 如果标准差为0，所有值都相同，没有异常值

            double deviation = Math.Abs(value - average);
            return deviation > (multiple * stdDev);
        }

        /// <summary>
        /// 删除要素
        /// </summary>
        private int DeleteFeatures(IFeatureClass featureClass, List<int> oidsToDelete)
        {
            int deletedCount = 0;
            IWorkspaceEdit workspaceEdit = null;

            try
            {
                // 获取工作空间并开始编辑
                IDataset dataset = (IDataset)featureClass;
                workspaceEdit = (IWorkspaceEdit)dataset.Workspace;

                bool inEditMode = workspaceEdit.IsBeingEdited();
                if (!inEditMode)
                {
                    workspaceEdit.StartEditing(true);
                    workspaceEdit.StartEditOperation();
                }

                // 删除要素
                foreach (int oid in oidsToDelete)
                {
                    IFeature feature = featureClass.GetFeature(oid);
                    if (feature != null)
                    {
                        feature.Delete();
                        deletedCount++;
                    }
                }

                if (!inEditMode)
                {
                    workspaceEdit.StopEditOperation();
                    workspaceEdit.StopEditing(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除要素时出错：{ex.Message}");
                // 回滚编辑
                if (workspaceEdit != null && workspaceEdit.IsBeingEdited())
                {
                    workspaceEdit.AbortEditOperation();
                    workspaceEdit.StopEditing(false);
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// 刷新地图显示
        /// </summary>
        private void RefreshMap()
        {
            try
            {
                if (axMapControl1 != null)
                {
                    axMapControl1.ActiveView.Refresh();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新地图时出错：{ex.Message}");
            }
        }

        private void startPolylineBufferQuery_Click(object sender, EventArgs e)
        {
            try
            {
                // 设置当前操作类型
                m_currentOperator = MapOperatorType.QueryByPolylineBuffer;

                // 设置鼠标样式
                axMapControl1.MousePointer = esriControlsMousePointer.esriPointerCrosshair;

                MessageBox.Show("请在地图上绘制多义线进行缓冲区查询（单击开始，移动绘制，双击结束）");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动多义线缓冲区查询时出错：{ex.Message}");
                m_currentOperator = MapOperatorType.Default;
            }
        }
        private void HandlePolylineBufferMouseDown(IMapControlEvents2_OnMouseDownEvent e)
        {
            try
            {
                // 使用 TrackNew 方法绘制多义线
                IRubberBand rubberBand = new RubberLineClass();
                IGeometry geometry = rubberBand.TrackNew(axMapControl1.ActiveView.ScreenDisplay, null);

                if (geometry != null && geometry is IPolyline polyline && polyline.Length > 0)
                {
                    // 直接执行查询
                    ExecutePolylineBufferQuery(polyline);
                }
                else
                {
                    MessageBox.Show("绘制多义线失败或线长度为零！");
                }

                // 重置操作类型
                CleanupPolylineBufferQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"绘制多义线时出错：{ex.Message}");
                CleanupPolylineBufferQuery();
            }
        }

        private void axMapControl1_OnDoubleClick(object sender, IMapControlEvents2_OnDoubleClickEvent e)
        {
            if (m_currentOperator == MapOperatorType.QueryByPolylineBuffer)
            {
            }
        }
        private void ExecutePolylineBufferQuery(IPolyline polyline)
        {
            try
            {
                // 获取缓冲区距离
                double bufferDistance = GetBufferDistanceFromUI();
                if (bufferDistance <= 0) bufferDistance = 50; // 默认50米

                // 创建缓冲区
                IGeometry buffer = CreateBuffer(polyline, bufferDistance);
                if (buffer == null)
                {
                    MessageBox.Show("创建缓冲区失败！");
                    return;
                }

                // 显示缓冲区图形
                DisplayBufferGraphic(buffer);

                // 查询相交的建筑要素
                ILayer buildingLayer = GetBuildingLayer();
                if (buildingLayer == null)
                {
                    MessageBox.Show("未找到建筑图层！");
                    return;
                }

                List<IFeature> intersectedBuildings = FindIntersectedBuildings(buildingLayer, buffer);

                // 显示结果
                DisplayQueryResults(intersectedBuildings, buildingLayer);

                MessageBox.Show($"找到 {intersectedBuildings.Count} 个与缓冲区相交的建筑");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行多义线缓冲区查询时出错：{ex.Message}");
            }
        }

        private double GetBufferDistanceFromUI()
        {
            // 这里可以添加从界面获取缓冲区距离的逻辑
            // 例如从文本框、输入框等获取
            // 暂时返回默认值
            return 50;
        }

        private IGeometry CreateBuffer(IPolyline polyline, double distance)
        {
            try
            {
                ITopologicalOperator topologicalOperator = polyline as ITopologicalOperator;
                if (topologicalOperator == null) return null;

                IGeometry buffer = topologicalOperator.Buffer(distance);
                return buffer;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建缓冲区时出错：{ex.Message}");
                return null;
            }
        }
        private ILayer GetBuildingLayer()
        {
            // 方法1：自动识别建筑图层
            ILayer layer = FindBuildingLayerByNames();
            if (layer != null) return layer;

            // 方法2：使用当前选中图层
            return GetSelectedLayer();
        }

        private ILayer FindBuildingLayerByNames()
        {
            if (axMapControl1 == null || axMapControl1.Map == null) return null;

            string[] buildingLayerNames = { "建筑", "建筑物", "Building", "Buildings", "房屋", "房子", "building", "buildings" };

            for (int i = 0; i < axMapControl1.Map.LayerCount; i++)
            {
                ILayer layer = axMapControl1.Map.get_Layer(i);
                IFeatureLayer featureLayer = layer as IFeatureLayer;

                if (featureLayer != null && featureLayer.FeatureClass != null)
                {
                    // 检查是否为面图层（建筑通常是面要素）
                    if (featureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        // 检查图层名称
                        string layerName = layer.Name;
                        foreach (string buildingName in buildingLayerNames)
                        {
                            if (layerName.IndexOf(buildingName, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return layer;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private List<IFeature> FindIntersectedBuildings(ILayer buildingLayer, IGeometry buffer)
        {
            List<IFeature> results = new List<IFeature>();
            IFeatureCursor featureCursor = null;

            try
            {
                IFeatureLayer featureLayer = buildingLayer as IFeatureLayer;
                if (featureLayer?.FeatureClass == null) return results;

                // 创建空间查询
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = buffer;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = featureLayer.FeatureClass.ShapeFieldName;

                // 执行查询
                featureCursor = featureLayer.FeatureClass.Search(spatialFilter, true);
                IFeature feature = featureCursor.NextFeature();

                while (feature != null)
                {
                    results.Add(feature);
                    feature = featureCursor.NextFeature();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询相交建筑时出错：{ex.Message}");
            }
            finally
            {
                if (featureCursor != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
            }

            return results;
        }

        private void DisplayBufferGraphic(IGeometry buffer)
        {
            try
            {
                m_graphicsContainer = axMapControl1.Map as IGraphicsContainer;
                if (m_graphicsContainer == null) return;

                // 清除之前的缓冲区图形
                ClearBufferGraphic();

                // 创建新的缓冲区图形元素
                IFillShapeElement bufferElement = new PolygonElementClass();
                bufferElement.Symbol = (IFillSymbol)GetBufferSymbol();

                IElement element = bufferElement as IElement;
                element.Geometry = buffer;

                m_bufferElement = element;
                m_graphicsContainer.AddElement(element, 0);
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示缓冲区图形时出错：{ex.Message}");
            }
        }

        private ISymbol GetBufferSymbol()
        {
            // 创建缓冲区符号（半透明红色）
            ISimpleFillSymbol fillSymbol = new SimpleFillSymbolClass();
            fillSymbol.Color = GetRGBColor(255, 0, 0, 100); // 半透明红色
            fillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;

            ISimpleLineSymbol outlineSymbol = new SimpleLineSymbolClass();
            outlineSymbol.Color = GetRGBColor(255, 0, 0, 255); // 红色边框
            outlineSymbol.Width = 2;
            outlineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;

            fillSymbol.Outline = outlineSymbol;
            return fillSymbol as ISymbol;
        }

        private IColor GetRGBColor(int red, int green, int blue, int alpha = 255)
        {
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = red;
            rgbColor.Green = green;
            rgbColor.Blue = blue;
            return rgbColor as IColor;
        }

        private void DisplayQueryResults(List<IFeature> buildings, ILayer buildingLayer)
        {
            try
            {
                if (axMapControl1?.Map == null) return;

                // 清除之前的选择
                axMapControl1.Map.ClearSelection();

                // 选择查询到的建筑要素
                IFeatureLayer featureLayer = buildingLayer as IFeatureLayer;
                if (featureLayer != null)
                {
                    foreach (IFeature building in buildings)
                    {
                        axMapControl1.Map.SelectFeature(featureLayer, building);
                    }
                }

                // 刷新显示
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);

                // 缩放到选中要素
                //if (buildings.Count > 0)
                //{
                //    IEnvelope envelope = axMapControl1.Map.SelectionExtent;
                //    if (envelope != null && !envelope.IsEmpty)
                //    {
                //        envelope.Expand(1.2, 1.2, true); // 放大1.2倍
                //        axMapControl1.Extent = envelope;
                //    }
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示查询结果时出错：{ex.Message}");
            }
        }

        private void CleanupPolylineBufferQuery()
        {
            m_currentOperator = MapOperatorType.Default;
            axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;
        }

        private void ClearBufferGraphic()
        {
            try
            {
                if (m_graphicsContainer != null && m_bufferElement != null)
                {
                    m_graphicsContainer.DeleteElement(m_bufferElement);
                    m_bufferElement = null;
                    axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理缓冲区图形时出错：{ex.Message}");
            }
        }

        // 可以添加一个清除按钮
        private void clearBufferGraphic_Click(object sender, EventArgs e)
        {
            ClearBufferGraphic();
            if (axMapControl1?.Map != null)
            {
                axMapControl1.Map.ClearSelection();
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
            }
        }

        private void startGetElevation_Click(object sender, EventArgs e)
        {
            try
            {
                // 设置当前操作类型
                m_currentOperator = MapOperatorType.GetClickPointElevation;
                mapOperatorType = MapOperatorType.GetClickPointElevation;

                // 设置鼠标样式
                axMapControl1.MousePointer = esriControlsMousePointer.esriPointerCrosshair;

                MessageBox.Show("请在地图上点击要查询高程的位置");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动高程查询时出错：{ex.Message}");
                m_currentOperator = MapOperatorType.Default;
            }
        }
        private void HandleGetElevationMouseDown(IMapControlEvents2_OnMouseDownEvent e)
        {
            try
            {
                if (e.button != 1) return; // 只处理左键点击

                // 获取点击位置
                IPoint clickPoint = axMapControl1.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);

                // 获取高程图层
                ILayer elevationLayer = GetSelectedLayer();
                if (elevationLayer == null)
                {
                    MessageBox.Show("未找到高程点图层！");
                    return;
                }

                // 计算内插高程
                double? elevation = CalculateInterpolatedElevation(clickPoint, elevationLayer, 8); // 使用8个最近点

                if (elevation.HasValue)
                {
                    // 显示结果
                    DisplayElevationResult(clickPoint, elevation.Value);
                }
                else
                {
                    MessageBox.Show("无法计算该位置的高程值！");
                }

                // 重置操作类型（保持在高程查询模式，可以连续点击）
                // 如果希望单次查询，可以取消注释下一行
                // CleanupElevationQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询高程时出错：{ex.Message}");
            }
        }

        private void CleanupElevationQuery()
        {
            m_currentOperator = MapOperatorType.Default;
            axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;
        }
        /// <summary>
        /// 使用反距离权重法计算内插高程
        /// </summary>
        /// <param name="targetPoint">目标点</param>
        /// <param name="elevationLayer">高程点图层</param>
        /// <param name="neighborCount">最近邻点数</param>
        /// <returns>内插高程值</returns>
        private double? CalculateInterpolatedElevation(IPoint targetPoint, ILayer elevationLayer, int neighborCount)
        {
            try
            {
                IFeatureLayer featureLayer = elevationLayer as IFeatureLayer;
                if (featureLayer?.FeatureClass == null) return null;

                // 获取高程字段
                string elevationFieldName = GetElevationFieldName(featureLayer.FeatureClass);
                if (string.IsNullOrEmpty(elevationFieldName)) return null;

                // 搜索最近的高程点
                List<Tuple<IFeature, double>> nearestPoints = FindNearestElevationPoints(
                    targetPoint, featureLayer.FeatureClass, elevationFieldName, neighborCount);

                if (nearestPoints.Count == 0) return null;

                // 使用反距离权重法计算高程
                return CalculateIDWElevation(targetPoint, nearestPoints, elevationFieldName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算内插高程时出错：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 搜索最近的高程点
        /// </summary>
        private List<Tuple<IFeature, double>> FindNearestElevationPoints(
            IPoint targetPoint, IFeatureClass elevationClass, string elevationFieldName, int neighborCount)
        {
            List<Tuple<IFeature, double>> nearestPoints = new List<Tuple<IFeature, double>>();
            IFeatureCursor featureCursor = null;

            try
            {
                // 搜索所有高程点
                featureCursor = elevationClass.Search(null, true);
                IFeature feature = featureCursor.NextFeature();

                // 计算所有点到目标点的距离
                var allDistances = new List<Tuple<IFeature, double>>();

                while (feature != null)
                {
                    if (feature.Shape is IPoint elevationPoint)
                    {
                        double distance = CalculateDistance(targetPoint, elevationPoint);
                        double elevation = GetFeatureElevation(feature, elevationFieldName);

                        if (!double.IsNaN(elevation) && distance > 0) // 排除零距离点
                        {
                            allDistances.Add(new Tuple<IFeature, double>(feature, distance));
                        }
                    }
                    feature = featureCursor.NextFeature();
                }

                // 按距离排序并取前N个
                nearestPoints = allDistances
                    .OrderBy(d => d.Item2)
                    .Take(neighborCount)
                    .ToList();
            }
            finally
            {
                if (featureCursor != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(featureCursor);
            }

            return nearestPoints;
        }

        /// <summary>
        /// 使用反距离权重法计算高程
        /// </summary>
        private double CalculateIDWElevation(IPoint targetPoint, List<Tuple<IFeature, double>> nearestPoints, string elevationFieldName)
        {
            double sumWeightedElevation = 0;
            double sumWeights = 0;
            double power = 2; // 反距离的幂，通常为2

            foreach (var pointData in nearestPoints)
            {
                IFeature feature = pointData.Item1;
                double distance = pointData.Item2;
                double elevation = GetFeatureElevation(feature, elevationFieldName);

                if (double.IsNaN(elevation)) continue;

                // 计算权重：1 / distance^power
                double weight = 1.0 / Math.Pow(distance, power);

                sumWeightedElevation += elevation * weight;
                sumWeights += weight;
            }

            // 避免除零
            if (sumWeights == 0) return double.NaN;

            return sumWeightedElevation / sumWeights;
        }
        /// <summary>
        /// 获取高程点图层
        /// </summary>
        private ILayer GetElevationLayer()
        {
            // 方法1：自动识别高程点图层
            ILayer layer = FindElevationLayerByNames();
            if (layer != null) return layer;

            // 方法2：使用当前选中图层
            return GetSelectedLayer();
        }

        /// <summary>
        /// 自动识别高程点图层
        /// </summary>
        private ILayer FindElevationLayerByNames()
        {
            if (axMapControl1 == null || axMapControl1.Map == null) return null;

            string[] elevationLayerNames = {
        "高程", "高程点", "Elevation", "DEM", "点", "Points",
        "高程点层", "地形点", "地形", "Terrain"
    };

            for (int i = 0; i < axMapControl1.Map.LayerCount; i++)
            {
                ILayer layer = axMapControl1.Map.get_Layer(i);
                IFeatureLayer featureLayer = layer as IFeatureLayer;

                if (featureLayer != null && featureLayer.FeatureClass != null)
                {
                    // 检查是否为点图层
                    if (featureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        // 检查图层名称
                        string layerName = layer.Name;
                        foreach (string elevationName in elevationLayerNames)
                        {
                            if (layerName.IndexOf(elevationName, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return layer;
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 显示高程查询结果
        /// </summary>
        private void DisplayElevationResult(IPoint clickPoint, double elevation)
        {
            try
            {
                // 创建结果显示文本
                string resultText = $"位置坐标:\nX: {clickPoint.X:F2}\nY: {clickPoint.Y:F2}\n内插高程: {elevation:F2} 米";

                // 显示消息框
                MessageBox.Show(resultText, "高程查询结果");

                // 在点击位置添加标记
                AddElevationMarker(clickPoint, elevation);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示结果时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 在点击位置添加高程标记
        /// </summary>
        private void AddElevationMarker(IPoint point, double elevation)
        {
            try
            {
                IGraphicsContainer graphicsContainer = axMapControl1.Map as IGraphicsContainer;
                if (graphicsContainer == null) return;

                // 创建标记点元素
                IMarkerElement markerElement = new MarkerElementClass();

                // 设置符号
                ISimpleMarkerSymbol markerSymbol = new SimpleMarkerSymbolClass();
                markerSymbol.Color = GetRGBColor(255, 0, 0); // 红色
                markerSymbol.Size = 8;
                markerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle;

                markerElement.Symbol = markerSymbol;

                // 设置几何
                IElement element = markerElement as IElement;
                element.Geometry = point;

                // 添加到图形容器
                graphicsContainer.AddElement(element, 0);

                // 添加文本标注
                AddElevationText(point, elevation, graphicsContainer);

                // 刷新显示
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加高程标记时出错：{ex.Message}");
            }
        }

        /// <summary>
        /// 添加高程文本标注
        /// </summary>
        private void AddElevationText(IPoint point, double elevation, IGraphicsContainer graphicsContainer)
        {
            try
            {
                ITextElement textElement = new TextElementClass();

                // 设置文本符号
                ITextSymbol textSymbol = new TextSymbolClass();
                textSymbol.Color = GetRGBColor(0, 0, 255); // 蓝色
                textSymbol.Size = 10;

                textElement.Symbol = textSymbol;
                textElement.Text = $"{elevation:F1}m";

                // 设置文本位置（稍微偏移）
                IPoint textPoint = new PointClass();
                textPoint.X = point.X + 20; // 向右偏移20个单位
                textPoint.Y = point.Y;

                IElement element = textElement as IElement;
                element.Geometry = textPoint;

                graphicsContainer.AddElement(element, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加高程文本时出错：{ex.Message}");
            }
        }

        private void startPolygonClickQuery_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.PolygonClickQuery;
        }

        private void startPolylineClickQuery_Click(object sender, EventArgs e)
        {
            mapOperatorType = MapOperatorType.PolylineClickQuery;
        }
    }
}