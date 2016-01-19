using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Controls.Behaviors.DragDrop
{
    public class InsertAdorner : Adorner
    {
        private readonly Boolean IsSeparatorHorizontal;
        private readonly AdornerLayer AdornerLayer;
        private static readonly Pen Pen;
        private static readonly PathGeometry Triangle;

        public Boolean IsInFirstHalf
        { get; set; }

        #region Constructor
        /// <summary>
        /// Create the pen and triangle in a static constructor and freeze them to improve performance.
        /// </summary>
        static InsertAdorner()
        {
            Pen = new Pen { Brush = Brushes.Gray, Thickness = 2 };
            Pen.Freeze();

            LineSegment firstLineSegment = new LineSegment(new Point(0, -5), false);
            firstLineSegment.Freeze();
            LineSegment secondLineSegment = new LineSegment(new Point(0, 5), false);
            secondLineSegment.Freeze();

            PathFigure pathFigure = new PathFigure { StartPoint = new Point(5, 0) };
            pathFigure.Segments.Add(firstLineSegment);
            pathFigure.Segments.Add(secondLineSegment);
            pathFigure.Freeze();

            Triangle = new PathGeometry();
            Triangle.Figures.Add(pathFigure);
            Triangle.Freeze();
        }

        public InsertAdorner(Boolean IsSeparatorHorizontal, Boolean IsInFirstHalf, UIElement AdornedElement, AdornerLayer AdornerLayer)
            : base(AdornedElement)
        {
            this.IsSeparatorHorizontal = IsSeparatorHorizontal;
            this.IsInFirstHalf = IsInFirstHalf;
            this.AdornerLayer = AdornerLayer;
            IsHitTestVisible = false;
            this.AdornerLayer.Add(this);
        }

        public void Detach()
        { AdornerLayer.Remove(this); }
        #endregion

        #region Overrides
        protected override void OnRender(DrawingContext drawingContext)
        {
            Point Start, End;
            CalculateStartAndEndPoint(out Start, out End);
            drawingContext.DrawLine(Pen, Start, End);

            if (IsSeparatorHorizontal)
            {
                DrawTriangle(drawingContext, Start, 0);
                DrawTriangle(drawingContext, End, 180);
            }
            else
            {
                DrawTriangle(drawingContext, Start, 90);
                DrawTriangle(drawingContext, End, -90);
            }
        }
        #endregion

        private void DrawTriangle(DrawingContext DrawingContext, Point Origin, Double Angle)
        {
            DrawingContext.PushTransform(new TranslateTransform(Origin.X, Origin.Y));
            DrawingContext.PushTransform(new RotateTransform(Angle));

            DrawingContext.DrawGeometry(Pen.Brush, null, Triangle);

            DrawingContext.Pop();
            DrawingContext.Pop();
        }

        private void CalculateStartAndEndPoint(out Point Start, out Point End)
        {
            Start = new Point();
            End = new Point();

            Double Width = AdornedElement.RenderSize.Width,
                Height = AdornedElement.RenderSize.Height;

            if (IsSeparatorHorizontal)
            {
                End.X = Width;
                if (!IsInFirstHalf)
                {
                    Start.Y = Height;
                    End.Y = Height;
                }
            }
            else
            {
                End.Y = Height;
                if (!IsInFirstHalf)
                {
                    Start.X = Width;
                    End.X = Width;
                }
            }
        }
    }
}
