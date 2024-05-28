using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit2Gltf.glTF;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Revit2Gltf
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class Export : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            if (!(doc.ActiveView is View3D))
            {
                TaskDialog.Show("提示", "当前视图不支持导出，请切换至3D视图");
                return Result.Cancelled;
            }


            var mainWindow = new MainWindow();
            if (mainWindow.ShowDialog() == true)
            {
                var stopWatch = new Stopwatch();
                //测量运行时间
                stopWatch.Start();
                var setting = new glTFSetting
                {
                    useDraco = (bool)mainWindow.useDraco.IsChecked,
                    fileName = mainWindow.fileName.Text,
                    exportProperty = false
                    // exportProperty = (bool)mainWindow.exportProperty.IsChecked
                };
                var context = new glTFExportContext(doc, setting);
                var exporter = new CustomExporter(doc, context)
                {
                    IncludeGeometricObjects = false,
                    ShouldStopOnError = true
                };
                context.bIsMainRvtExport = true;
                exporter.Export(new List<ElementId>() { doc.ActiveView.Id });

                context.bIsMainRvtExport = false;
                // FamilyExport(doc, context, setting);
                
                stopWatch.Stop();


                var mainDialog = new TaskDialog("Revit2GLTF")
                {
                    MainContent = "success! time is:" + stopWatch.Elapsed.TotalSeconds + "s"
                    // MainContent = "success! time is:" + stopWatch.Elapsed.TotalSeconds + "s" + "\n" +
                    //  "<a href=\"https://cowboy1997.github.io/Revit2GLTF/threejs/index?\">" + "open your glb model</a>"
                };
                ;
                mainDialog.Show();

            }
            return Result.Succeeded;
        }

        private void FamilyExport(Document doc, glTFExportContext context, glTFSetting setting)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);// 过滤器
            List<Element> elements = collector.OfClass(typeof(FamilySymbol)).ToList();
            //遍历list中的元素
            foreach (Element element in elements)
            {
                FamilySymbol familySymbol = element as FamilySymbol;
                if (familySymbol != null && context.combinedFamilyElementList.Contains(familySymbol.UniqueId))
                {
                    if (familySymbol.Family.IsEditable)
                    {
                        var familyDoc = doc.EditFamily(familySymbol.Family);
                        if (familyDoc != null)
                        {
                            FilteredElementCollector familyFilterCollector = new FilteredElementCollector(familyDoc);
                            foreach (var view3d in familyFilterCollector.OfClass(typeof(View3D)))
                            {
                                if (view3d != null && view3d.Name.Contains("三维"))
                                {
                                    try
                                    {   var familyContext = new glTFExportContext(familyDoc, setting);
                                        var familyExporter = new CustomExporter(familyDoc, familyContext)
                                        {
                                            IncludeGeometricObjects = false,
                                            ShouldStopOnError = true
                                        };
                                        familyExporter.Export(new List<ElementId>() { familyDoc.ActiveView.Id });
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        throw;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            context.combinedFamilyElementList.Clear();
        }
    }
}

