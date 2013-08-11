using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using BakaBox.Extensions;

namespace BakaBox.Touch
{
    public static class Manipulation
    {
        [Flags]
        public enum ManipulationOption
        {
            None = 0x1,
            Scale = 0x2,
            Rotate = 0x4,
            Translate = 0x8,
            All = Scale | Rotate | Translate
        }

        public enum TransformType
        {
            Render = 0x1,
            Layout = 0x2
        }

        public static Boolean Manipulate(
            ref FrameworkElement Element,
            ManipulationDelta DeltaManipulation,
            ManipulationVelocities VelocitiesManipulation)
        { return Manipulate(ref Element, DeltaManipulation, VelocitiesManipulation, ManipulationOption.All); }
        public static Boolean Manipulate(
            ref FrameworkElement Element,
            ManipulationDelta DeltaManipulation,
            ManipulationVelocities VelocitiesManipulation,
            ManipulationOption ManipulationOptions)
        { return Manipulate(ref Element, DeltaManipulation, VelocitiesManipulation, ManipulationOptions, TransformType.Layout); }
        public static Boolean Manipulate(
            ref FrameworkElement Element,
            ManipulationDelta DeltaManipulation,
            ManipulationVelocities VelocitiesManipulation, 
            ManipulationOption ManipulationOptions,
            TransformType TransformType)
        {
            if (Element != null &&
                ManipulationOptions.Missing(ManipulationOption.None))
            {
                Matrix TransformMatrix;
                if (TransformType == Manipulation.TransformType.Layout)
                    TransformMatrix = Element.LayoutTransform.Value;
                else
                    TransformMatrix = Element.RenderTransform.Value;

                Point Center = new Point(Element.ActualWidth / 2, Element.ActualHeight / 2);
                Center = TransformMatrix.Transform(Center);

                //this will be a Zoom.
                if (ManipulationOptions.Has(ManipulationOption.Scale))
                    TransformMatrix.ScaleAt(
                        DeltaManipulation.Scale.X, 
                        DeltaManipulation.Scale.Y , 
                        Center.X, Center.Y);
                // Rotation 
                if (ManipulationOptions.Has(ManipulationOption.Rotate))
                    TransformMatrix.RotateAt(
                        DeltaManipulation.Rotation,
                        Center.X, Center.Y);
                //Translation (pan) 
                if (ManipulationOptions.Has(ManipulationOption.Translate))
                    TransformMatrix.Translate(
                        DeltaManipulation.Translation.X,
                        DeltaManipulation.Translation.Y);

                if (TransformType == Manipulation.TransformType.Layout)
                    Element.LayoutTransform = new MatrixTransform(TransformMatrix);
                else
                    Element.RenderTransform = new MatrixTransform(TransformMatrix);

                return true;
            }
            return false;
        }

        private static Double Inverse(Double x)
        {
            if (x == 0) return 0;
            return 1 / x;
        }
    }
}
