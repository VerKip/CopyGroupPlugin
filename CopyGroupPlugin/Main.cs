using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

namespace CopyGroupPlugin
{
    [Transaction(TransactionMode.Manual)]

    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                GroupPickFilter groupPickfilter = new GroupPickFilter();    
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickfilter,
                    "Выберите группу объектов");
                Element element = doc.GetElement(reference);
                Group group = element as Group;

                //найти центр группы
                XYZ groupCenter = GetElementCenter(group);

                //Определить комнату, в которой находится выбранная исходная группа объектов
                Room room = GetRoomByPoint(doc, groupCenter);
                //Найти центр этой комнаты
                XYZ roomCenter = GetElementCenter(room);

                //Определить смещение центра группы от центра комнаты
                XYZ offset = groupCenter - roomCenter;


                //string str2 = "Выберите точку";
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                //Определить комнату, по кот. щелкнул пользователь
                Room targetRoom = GetRoomByPoint(doc, point);

                //Определить центр выбранной комнаты
                XYZ targetRoomCenter = GetElementCenter(targetRoom);

                //Найти точку для вставки группы
                XYZ targetPoint = targetRoomCenter + offset;

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(targetPoint, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;   
            }
            catch (Exception ex)
            {
                message = ex.Message;   
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        //Метод, который по элементу вычисляет его центр на основе bounding box
        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        //Метод, определяющий комнату по точке выбранной
        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }    
                }
            }
            return null;
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
