using System.Windows.Documents;
using System.Windows.Media;

namespace System.Windows.Controls.Behaviors.DragDrop
{
    public class DragAdorner : Adorner
    {
        private readonly ContentPresenter ContentPresenter;
        private readonly AdornerLayer AdornerLayer;

        #region Location
        private Double Left
        { get; set; }

        private Double Top
        { get; set; }
        #endregion

        #region Constructor
        public DragAdorner(Object DragDropData, DataTemplate DragDropTemplate, UIElement AdornedElement, AdornerLayer AdornerLayer)
            : base(AdornedElement)
        {
            this.AdornerLayer = AdornerLayer;
            ContentPresenter = new ContentPresenter
            {
                Content = DragDropData,
                ContentTemplate = DragDropTemplate,
                Opacity = 0.75
            };
            this.AdornerLayer.Add(this);
        }

        public void Detach()
        { AdornerLayer.Remove(this); }
        #endregion

        public void SetPosition(Double Left, Double Top)
        {
            this.Left = Left - 1;
            this.Top = Top + 13;
            if (!Equals(AdornerLayer, null))
            { try { AdornerLayer.Update(AdornedElement); } catch { } }
        }

        #region Overrides
        protected override Size MeasureOverride(Size constraint)
        {
            ContentPresenter.Measure(constraint);
            return ContentPresenter.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            ContentPresenter.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(Int32 index)
        { return ContentPresenter; }

        protected override Int32 VisualChildrenCount
        { get { return 1; } }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup GeneralTransformGroup = new GeneralTransformGroup();
            GeneralTransformGroup.Children.Add(base.GetDesiredTransform(transform));
            GeneralTransformGroup.Children.Add(new TranslateTransform(Left, Top));
            return GeneralTransformGroup;
        }
        #endregion
    }
}
