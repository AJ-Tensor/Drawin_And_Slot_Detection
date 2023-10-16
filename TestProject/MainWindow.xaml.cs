using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using TSMUI = Tekla.Structures.Model.UI;

namespace TestProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Model Model = new Model();
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Snglprtdrwg.Dominating_Corner dimensionCorner = Snglprtdrwg.Dominating_Corner.LeftTopCorner;
            string caseInput = DimensionLocation.Text;
            switch (caseInput)
            {
                case "Left Top":
                    dimensionCorner = Snglprtdrwg.Dominating_Corner.LeftTopCorner;
                    break;
                case "Right Top":
                    dimensionCorner = Snglprtdrwg.Dominating_Corner.RightTopCorner;
                    break;
                case "Left Bottom":
                    dimensionCorner = Snglprtdrwg.Dominating_Corner.LeftBottomCorner;
                    break;
                case "Right Bottom":
                    dimensionCorner = Snglprtdrwg.Dominating_Corner.RightBottomCorner;
                    break;
                default:
                    dimensionCorner = Snglprtdrwg.Dominating_Corner.LeftTopCorner;
                    break;

            }
            Snglprtdrwg snglprtdrwg = new Snglprtdrwg();
            snglprtdrwg.DrawingCreation();
            snglprtdrwg.ModificationsInDrawing(dimensionCorner);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Model.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane()); // We use global transformation
            TSMUI.Picker picker = new TSMUI.Picker();
            var part = picker.PickObject(TSMUI.Picker.PickObjectEnum.PICK_ONE_BOLTGROUP, "pick one part");

            BoltArray bolt = part as BoltArray;
            WorkPlaneHandler workPlaneHandler = Model.GetWorkPlaneHandler();
            TransformationPlane globalTranPlane = workPlaneHandler.GetCurrentTransformationPlane();
            workPlaneHandler.SetCurrentTransformationPlane(new TransformationPlane(bolt.GetCoordinateSystem()));

            try
            {
                bolt.Select();
                var part1 = bolt.PartToBeBolted;
                var part2 = bolt.PartToBoltTo;
                part1.Select();
                part2.Select();
                List<Part> partList = new List<Part>();
                partList.Add(part1);
                partList.Add(part2);
                DetectSlotlocation(partList, bolt);
            }
            finally
            {
                workPlaneHandler.SetCurrentTransformationPlane(globalTranPlane);
            }
            new Tekla.Structures.Model.UI.GraphicsDrawer().DrawText(bolt.GetCoordinateSystem().Origin, "o", new TSMUI.Color());
            new Tekla.Structures.Model.UI.GraphicsDrawer().DrawText(bolt.GetCoordinateSystem().Origin + bolt.GetCoordinateSystem().AxisX, "X", new TSMUI.Color());
            new Tekla.Structures.Model.UI.GraphicsDrawer().DrawText(bolt.GetCoordinateSystem().Origin + bolt.GetCoordinateSystem().AxisY, "Y", new TSMUI.Color());
        }

        private void DetectSlotlocation(List<Part> partList, BoltArray bolt)
        {
            partList = partList.OrderBy(x => FindMidPoint(x.GetSolid().MinimumPoint, x.GetSolid().MaximumPoint).Z).ToList();
            if (bolt.Hole1 == true)
            {
                System.Windows.MessageBox.Show(partList.Last().Profile.ProfileString.ToString());
            }
            if (bolt.Hole2 == true)
            {
                System.Windows.MessageBox.Show(partList.First().Profile.ProfileString.ToString());
            }
        }
        private Tekla.Structures.Geometry3d.Point FindMidPoint(Tekla.Structures.Geometry3d.Point minPt, Tekla.Structures.Geometry3d.Point maxPt)
        {
            return new Tekla.Structures.Geometry3d.Point(0.5 * (minPt.X + maxPt.X), 0.5 * (minPt.Y + maxPt.Y), 0.5 * (minPt.Z + maxPt.Z));
        }
    }
}
