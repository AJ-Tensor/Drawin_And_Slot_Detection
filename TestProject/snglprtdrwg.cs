using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures;
using Tekla.Structures.Drawing;
using TSD = Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using TSG = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using TSM = Tekla.Structures.Model;
using TSMUI = Tekla.Structures.Model.UI;

namespace TestProject
{
    internal class Snglprtdrwg:System.Windows.Window
    {
        bool OpenDrawings = true;
        Model Model { get; set; } = new Model();
        DrawingHandler DrawingHandler { get; set; } = new DrawingHandler();
        public bool CreateFrontView = true;
        public bool CreateTopView = true;
        public bool CreateEndView = true;
        public bool Create3dView = true;

        public void DrawingCreation()
        {
            TransformationPlane current = Model.GetWorkPlaneHandler().GetCurrentTransformationPlane(); // We use global transformation
            try
            {

                Model.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane()); // We use global transformation
                TSMUI.Picker picker = new TSMUI.Picker();
                Tekla.Structures.Model.Part part = picker.PickObject(TSMUI.Picker.PickObjectEnum.PICK_ONE_PART, "pick one part") as Tekla.Structures.Model.Part;

                Identifier partId = part.Identifier;
                SinglePartDrawing singlePartDrawing = new SinglePartDrawing(partId);
                singlePartDrawing.Insert();

                if (OpenDrawings)
                    DrawingHandler.SetActiveDrawing(singlePartDrawing, true); // Open drawing in editor
                else
                    DrawingHandler.SetActiveDrawing(singlePartDrawing, false); // Open drawing in invisible mode. When drawing is opened in invisible mode, it must always be saved and closed.

                if (singlePartDrawing != null && OpenDrawings)
                    DrawingHandler.SetActiveDrawing(singlePartDrawing);

                Model.GetWorkPlaneHandler().SetCurrentTransformationPlane(current); // return original transformation
            }
            catch (Exception exception)
            {
                Model.GetWorkPlaneHandler().SetCurrentTransformationPlane(current); // return original transformation
                System.Windows.MessageBox.Show(exception.ToString());
            }
        }
        public void ModificationsInDrawing(Dominating_Corner dimensionCorner)
        {
            try
            {
                DrawingHandler DrawingHandler = new DrawingHandler();

                if (DrawingHandler.GetConnectionStatus())
                {
                    Drawing CurrentDrawing = DrawingHandler.GetActiveDrawing();
                    if (CurrentDrawing != null)
                    {
                        DeleteDefaultDimension(CurrentDrawing);
                        CreateNewDimensions(CurrentDrawing, dimensionCorner);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
            }
        }
        public enum Dominating_Corner
        {
            LeftBottomCorner, RightBottomCorner, LeftTopCorner, RightTopCorner,
        }
        private void CreateNewDimensions(Drawing currentDrawing, Dominating_Corner dimensionCorner)
        {
            

            DrawingObjectEnumerator drawingObjectEnumerator = currentDrawing.GetSheet().GetAllObjects();
            List<DrawingObject> dwgObjct = new List<DrawingObject>();

            foreach (DrawingObject currentObject in drawingObjectEnumerator)
            {
                dwgObjct.Add(currentObject);
            }

            TSD.Part platePart = dwgObjct.Where(x => x is TSD.Part).ToList().FirstOrDefault() as TSD.Part;
            Bolt plateBolt = dwgObjct.Where(x => x is Bolt).ToList().FirstOrDefault() as Bolt;

            TSM.ModelObject Plate = Model.SelectModelObject(platePart.ModelIdentifier);
            TSM.ModelObject boltobjct = Model.SelectModelObject(plateBolt.ModelIdentifier);

            View View = platePart.GetView() as View;
            TransformationPlane SavePlane = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane();
            Model.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane(View.DisplayCoordinateSystem));
            try
            {
                GeometricPlane nutralPlane = new GeometricPlane();
                Plate.Select();
                boltobjct.Select();
                Beam plateBeam = Plate as Beam;
                BoltArray boltArray = boltobjct as BoltArray;
                Point sp = plateBeam.StartPoint;
                Point ep = plateBeam.EndPoint;

                double width = Math.Abs(Projection.PointToPlane((Plate as Beam).GetSolid().MinimumPoint, nutralPlane).Y);
                double length = Distance.PointToPoint(sp, ep);

                Vector plateX = plateBeam.GetCoordinateSystem().AxisX.GetNormal();
                Vector platey = plateBeam.GetCoordinateSystem().AxisY.GetNormal();

                Point p00 = sp - platey * width;
                Point p01 = sp + platey * width;
                Point p10 = ep - platey * width;
                Point p11 = ep + platey * width;


                TSG.LineSegment leftLineseg = new TSG.LineSegment(p00, p01);
                TSG.LineSegment rightLineseg = new TSG.LineSegment(p10, p11);
                TSG.LineSegment bottomLineseg = new TSG.LineSegment(p00, p10);
                TSG.LineSegment topLineseg = new TSG.LineSegment(p01, p11);
                TSG.Line leftLine = new TSG.Line(leftLineseg);
                TSG.Line rightLine = new TSG.Line(rightLineseg);
                TSG.Line bottomLine = new TSG.Line(bottomLineseg);
                TSG.Line topLine = new TSG.Line(topLineseg);

                List<Point> boltPoints = new List<Point>();
                var positions = boltArray.BoltPositions;
                foreach (var item in positions)
                {
                    boltPoints.Add(item as Point);
                }
                LineSegment verticalLineSegment = new LineSegment();
                LineSegment horizontalLineSegment = new LineSegment();

                Point boltMarkPoint = new Point();
                Point plateMarkPoint = new Point(0.5 * (p01.X + p11.X), 0.5 * (p01.Y + p11.Y), 0.5 * (p01.Z + p11.Z)) + new Vector(0, 30, 0);

                if (dimensionCorner == Dominating_Corner.RightTopCorner)
                {
                    verticalLineSegment = rightLineseg;
                    horizontalLineSegment = topLineseg;
                    boltMarkPoint = p00 + new Vector(-100, -100, 0);
                }
                else if (dimensionCorner == Dominating_Corner.RightBottomCorner)
                {
                    verticalLineSegment = rightLineseg;
                    horizontalLineSegment = bottomLineseg;
                    boltMarkPoint = p01 + new Vector(-100, 100, 0);
                }
                else if (dimensionCorner == Dominating_Corner.LeftTopCorner)
                {
                    verticalLineSegment = leftLineseg;
                    horizontalLineSegment = topLineseg;
                    boltMarkPoint = p10 + new Vector(100, -100, 0);
                }
                else if (dimensionCorner == Dominating_Corner.LeftBottomCorner)
                {
                    verticalLineSegment = leftLineseg;
                    horizontalLineSegment = bottomLineseg;
                    boltMarkPoint = p11 + new Vector(100, 100, 0);
                }

                Mark plateMark = new Mark(platePart);
                plateMark.Attributes.Content.Clear();
                plateMark.Attributes.Content.Add(new TextElement(plateBeam.Profile.ProfileString));
                plateMark.InsertionPoint = plateMarkPoint;
                plateMark.Insert();

                Mark boltMark = new Mark(plateBolt);
                boltMark.Attributes.Content.Clear();
                boltMark.Attributes.Content.Add(new TextElement(boltArray.BoltSize + boltArray.BoltStandard));
                boltMark.InsertionPoint = boltMarkPoint;
                boltMark.Insert();
                PlacingDimensionBasedOnCorner(verticalLineSegment, horizontalLineSegment, boltPoints, platePart, currentDrawing, dimensionCorner);
            }
            finally
            {
                new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(SavePlane);
            }


        }

        private void PlacingDimensionBasedOnCorner(LineSegment verticalLineSegment, LineSegment horizontalLineSegment, List<Point> boltPoints, TSD.Part platePart, Drawing currentDrawing, Dominating_Corner dimensionCorner)
        {
            PointList horizontalPointList = new PointList();
            PointList majorhorizontalPointList = new PointList();
            PointList verticalPointList = new PointList();
            PointList majorverticalPointList = new PointList();
            PointList verticalPoints = new PointList();

            verticalPoints = FindBoltProjectedPoints(boltPoints, verticalLineSegment);
            verticalPointList.Add(verticalLineSegment.StartPoint);
            majorverticalPointList.Add(verticalLineSegment.StartPoint);
            verticalPointList.AddRange(verticalPoints);
            verticalPointList.Add(verticalLineSegment.EndPoint);
            majorverticalPointList.Add(verticalLineSegment.EndPoint);


            PointList horizontalPoints = new PointList();
            horizontalPoints = FindBoltProjectedPoints(boltPoints, horizontalLineSegment);
            horizontalPointList.Add(horizontalLineSegment.StartPoint);
            majorhorizontalPointList.Add(horizontalLineSegment.StartPoint);
            horizontalPointList.AddRange(horizontalPoints);
            horizontalPointList.Add(horizontalLineSegment.EndPoint);
            majorhorizontalPointList.Add(horizontalLineSegment.EndPoint);


            Vector horizontalDimensionVector = (dimensionCorner == Dominating_Corner.RightTopCorner || dimensionCorner == Dominating_Corner.LeftTopCorner) ? new Vector(0, 1, 0) : new Vector(0, -1, 0);
            Vector verticalDimentionVector = (dimensionCorner == Dominating_Corner.RightTopCorner || dimensionCorner == Dominating_Corner.RightBottomCorner) ? new Vector(1, 0, 0) : new Vector(-1, 0, 0);


            ViewBase ViewBase = platePart.GetView();
            StraightDimensionSet.StraightDimensionSetAttributes attr = new StraightDimensionSet.StraightDimensionSetAttributes(platePart);

            StraightDimensionSet xDimensions = new StraightDimensionSetHandler().CreateDimensionSet(ViewBase, horizontalPointList, horizontalDimensionVector, 100, attr);
            StraightDimensionSet majorxDimensions = new StraightDimensionSetHandler().CreateDimensionSet(ViewBase, majorhorizontalPointList, horizontalDimensionVector, 200, attr);
            StraightDimensionSet yDimensions = new StraightDimensionSetHandler().CreateDimensionSet(ViewBase, verticalPointList, verticalDimentionVector, 100, attr);
            StraightDimensionSet majoryDimensions = new StraightDimensionSetHandler().CreateDimensionSet(ViewBase, majorverticalPointList, verticalDimentionVector, 200, attr);



            currentDrawing.CommitChanges();

        }

        private PointList FindBoltProjectedPoints(List<Point> boltPoints, TSG.LineSegment lineSegment)
        {
            PointList points = new PointList();
            foreach (Point item in boltPoints)
            {
                TSG.Line relevantLine = new TSG.Line(lineSegment.StartPoint, lineSegment.EndPoint);
                Point temppt = Projection.PointToLine(item, relevantLine);
                if (!CheckPointContainsIntheList(points, temppt))
                {
                    points.Add(temppt);
                }
            }
            return points;
        }

        private bool CheckPointContainsIntheList(PointList points, Point temppt)
        {
            foreach (Point point in points)
            {
                if (temppt.X == point.X && temppt.Y == point.Y && temppt.Z == point.Z)
                {
                    return true;
                }
            }
            return false;
        }

        private void DeleteDefaultDimension(Drawing currentDrawing)
        {
            #region Delete Existing parts
            List<View> frontViews = new List<View>();
            List<View> otherView = new List<View>();
            DrawingObjectEnumerator viewCollection = currentDrawing.GetSheet().GetAllViews();
            List<TSD.Part> tsdParts = new List<TSD.Part>();
            foreach (DrawingObject dwgObj in viewCollection)
            {
                if (dwgObj is View)
                {
                    View currentView = (View)dwgObj;
                    if (currentView.ViewType == View.ViewTypes.FrontView)
                    {
                        frontViews.Add(currentView);
                    }
                    else
                    {
                        otherView.Add(currentView);
                    }
                }

            }
            foreach (View currentView in otherView)
            {
                currentView.Delete();
            }
            View frontView = frontViews.FirstOrDefault();
            DrawingObjectEnumerator allObjects = frontView.GetAllObjects();
            List<DrawingObject> drawingObjects = new List<DrawingObject>();
            List<StraightDimensionSet> straightDimensionSets = new List<StraightDimensionSet>();
            List<Mark> marks = new List<Mark>();
            while (allObjects.MoveNext())
            {
                DrawingObject currentObject = allObjects.Current;
                if (currentObject is StraightDimensionSet)
                {
                    straightDimensionSets.Add(currentObject as StraightDimensionSet);
                }
                else if (currentObject is Mark)
                {
                    marks.Add(currentObject as Mark);
                }
            }
            foreach (StraightDimensionSet item in straightDimensionSets)
            {
                item.Delete();
            }
            foreach (Mark mark in marks)
            {
                mark.Delete();
            }
            currentDrawing.CommitChanges();
            #endregion
        }

        public void DimensionCreation()
        {
            try
            {
                DrawingHandler DrawingHandler = new DrawingHandler();

                if (DrawingHandler.GetConnectionStatus())
                {
                    Drawing CurrentDrawing = DrawingHandler.GetActiveDrawing();
                    if (CurrentDrawing != null)
                    {
                        DrawingObjectEnumerator DrawingObjectEnumerator = CurrentDrawing.GetSheet().GetAllObjects(typeof(TSD.Part));

                        foreach (TSD.Part myPart in DrawingObjectEnumerator)
                        {
                            View View = myPart.GetView() as View;
                            TransformationPlane SavePlane = new Model().GetWorkPlaneHandler().GetCurrentTransformationPlane();
                            new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane(View.DisplayCoordinateSystem));

                            Identifier Identifier = myPart.ModelIdentifier;
                            TSM.ModelObject ModelSideObject = new Model().SelectModelObject(Identifier);

                            PointList PointList = new PointList();
                            Beam myBeam = new Beam();
                            if (ModelSideObject.GetType().Equals(typeof(Beam)))
                            {
                                myBeam.Identifier = Identifier;
                                myBeam.Select();

                                PointList.Add(myBeam.StartPoint);
                                PointList.Add(myBeam.EndPoint);
                            }

                            ViewBase ViewBase = myPart.GetView();
                            StraightDimensionSet.StraightDimensionSetAttributes attr = new StraightDimensionSet.StraightDimensionSetAttributes(myPart);

                            if (myBeam.StartPoint.X != myBeam.EndPoint.X)
                            {
                                StraightDimensionSet XDimensions = new StraightDimensionSetHandler().CreateDimensionSet(ViewBase, PointList, new Vector(0, -100, 0), 200, attr);
                                CurrentDrawing.CommitChanges();
                            }

                            new Model().GetWorkPlaneHandler().SetCurrentTransformationPlane(SavePlane);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
            }
        }
    }
}
