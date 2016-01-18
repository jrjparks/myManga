using System.Collections;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace System.Windows.Controls.Behaviors.DragDrop
{
    public class Helper
    {
        private readonly DataFormat DragDropFormat = DataFormats.GetDataFormat("DragDropItemsControl");


        #region Mouse
        private Point InitialMousePosition
        { get; set; }

        private Vector InitialMouseOffset
        { get; set; }
        #endregion

        #region DragDropTemplate
        public static readonly DependencyProperty DragDropTemplateProperty = DependencyProperty.RegisterAttached(
            "DragDropTemplate",
            typeof(DataTemplate),
            typeof(Helper),
            null);

        public static DataTemplate GetDragDropTemplate(DependencyObject d)
        { return (DataTemplate)d.GetValue(DragDropTemplateProperty); }

        public static void SetDragDropTemplate(DependencyObject d, DataTemplate value)
        { d.SetValue(DragDropTemplateProperty, value); }
        #endregion

        #region Source
        private ItemsControl SourceItemsControl
        { get; set; }

        private FrameworkElement SourceFrameworkElement
        { get; set; }

        #region IsDragSource Property
        public static readonly DependencyProperty IsDragSourceProperty = DependencyProperty.RegisterAttached(
            "IsDragSource",
            typeof(Boolean),
            typeof(Helper),
            new UIPropertyMetadata(false, IsDragSourceChanged));

        public static Boolean GetIsDragSource(DependencyObject d)
        { return (Boolean)d.GetValue(IsDragSourceProperty); }

        public static void SetIsDragSource(DependencyObject d, Boolean value)
        { d.SetValue(IsDragSourceProperty, value); }

        private static void IsDragSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl source = d as ItemsControl;
            if (!Equals(source, null))
            {
                if (Equals(e.NewValue, true))
                {
                    source.PreviewMouseLeftButtonDown += Instance.DragSource_PreviewMouseLeftButtonDown;
                    source.PreviewMouseLeftButtonUp += Instance.DragSource_PreviewMouseLeftButtonUp;
                    source.PreviewMouseMove += Instance.DragSource_PreviewMouseMove;
                }
                else
                {
                    source.PreviewMouseLeftButtonDown -= Instance.DragSource_PreviewMouseLeftButtonDown;
                    source.PreviewMouseLeftButtonUp -= Instance.DragSource_PreviewMouseLeftButtonUp;
                    source.PreviewMouseMove -= Instance.DragSource_PreviewMouseMove;
                }
            }
        }
        #endregion

        #region IsDragSource Events
        private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SourceItemsControl = (ItemsControl)sender;
            Visual Visual = e.OriginalSource as Visual;

            TopWindow = Window.GetWindow(SourceItemsControl);
            InitialMousePosition = e.GetPosition(TopWindow);

            SourceFrameworkElement = SourceItemsControl.ContainerFromElement(Visual) as FrameworkElement;
            if (!Equals(SourceFrameworkElement, null))
            { DragData = SourceFrameworkElement.DataContext; }
        }

        private void DragSource_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        { DragData = null; }

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (DragData != null)
            {
                // Only drag when user moved the mouse by a reasonable amount.
                if (IsMovementBigEnough(InitialMousePosition, e.GetPosition(TopWindow)))
                {
                    InitialMouseOffset = InitialMousePosition - SourceFrameworkElement.TranslatePoint(new Point(0, 0), TopWindow);

                    DataObject data = new DataObject(DragDropFormat.Name, DragData);

                    // Adding events to the window to make sure dragged adorner comes up when mouse is not over a drop target.
                    bool previousAllowDrop = TopWindow.AllowDrop;
                    TopWindow.AllowDrop = true;
                    TopWindow.DragEnter += TopWindow_DragEnter;
                    TopWindow.DragOver += TopWindow_DragOver;
                    TopWindow.DragLeave += TopWindow_DragLeave;

                    DragDropEffects effects = System.Windows.DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);

                    // Without this call, there would be a problem in the following scenario: Click on a data item, and drag
                    // the mouse very fast outside of the window. When doing this really fast, for some reason I don't get 
                    // the Window leave event, and the dragged adorner is left behind.
                    // With this call, the dragged adorner will disappear when we release the mouse outside of the window,
                    // which is when the DoDragDrop synchronous method returns.
                    RemoveDraggedAdorner();

                    TopWindow.AllowDrop = previousAllowDrop;
                    TopWindow.DragEnter -= TopWindow_DragEnter;
                    TopWindow.DragOver -= TopWindow_DragOver;
                    TopWindow.DragLeave -= TopWindow_DragLeave;

                    DragData = null;
                }
            }
        }
        #endregion

        #endregion

        #region Target
        private ItemsControl TargetItemsControl
        { get; set; }

        private FrameworkElement TargetFrameworkElement
        { get; set; }

        #region IsDropTarget Property
        public static readonly DependencyProperty IsDropTargetProperty = DependencyProperty.RegisterAttached(
            "IsDropTarget",
            typeof(Boolean),
            typeof(Helper),
            new UIPropertyMetadata(false, IsDropTargetChanged));

        public static Boolean GetIsDropTarget(DependencyObject d)
        { return (Boolean)d.GetValue(IsDropTargetProperty); }

        public static void SetIsDropTarget(DependencyObject d, Boolean value)
        { d.SetValue(IsDropTargetProperty, value); }

        private static void IsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl source = d as ItemsControl;
            if (!Equals(source, null))
            {
                if (Equals(e.NewValue, true))
                {
                    source.AllowDrop = true;
                    source.PreviewDrop += Instance.DropTarget_PreviewDrop;
                    source.PreviewDragEnter += Instance.DropTarget_PreviewDragEnter;
                    source.PreviewDragOver += Instance.DropTarget_PreviewDragOver;
                    source.PreviewDragLeave += Instance.DropTarget_PreviewDragLeave;
                }
                else
                {
                    source.AllowDrop = false;
                    source.PreviewDrop -= Instance.DropTarget_PreviewDrop;
                    source.PreviewDragEnter -= Instance.DropTarget_PreviewDragEnter;
                    source.PreviewDragOver -= Instance.DropTarget_PreviewDragOver;
                    source.PreviewDragLeave -= Instance.DropTarget_PreviewDragLeave;
                }
            }
        }
        #endregion

        #region IsDropTarget Events
        private void DropTarget_PreviewDrop(object sender, DragEventArgs e)
        {
            Object DragItem = e.Data.GetData(DragDropFormat.Name);
            int indexRemoved = -1;

            if (!Equals(DragItem, null))
            {
                if ((e.Effects & DragDropEffects.Move) != 0)
                {
                    indexRemoved = RemoveItemFromItemsControl(SourceItemsControl, DragItem);
                }
                // This happens when we drag an item to a later position within the same ItemsControl.
                if (indexRemoved != -1 && SourceItemsControl == TargetItemsControl && indexRemoved < InsertionIndex)
                {
                    InsertionIndex--;
                }
                InsertItemInItemsControl(TargetItemsControl, DragItem, InsertionIndex);

                RemoveDraggedAdorner();
                RemoveInsertionAdorner();
            }
            e.Handled = true;
        }

        private void DropTarget_PreviewDragEnter(object sender, DragEventArgs e)
        {
            TargetItemsControl = (ItemsControl)sender;
            Object DragItem = e.Data.GetData(DragDropFormat.Name);

            DecideDropTarget(e);
            if (!Equals(DragItem, null))
            {
                Point position = e.GetPosition(TopWindow);
                ShowDraggedAdorner(position);
                CreateInsertionAdorner();
            }
            e.Handled = true;
        }

        private void DropTarget_PreviewDragOver(object sender, DragEventArgs e)
        {
            Object DragItem = e.Data.GetData(DragDropFormat.Name);

            DecideDropTarget(e);
            if (!Equals(DragItem, null))
            {
                // Dragged Adorner is only updated here - it has already been created in DragEnter.
                Point position = e.GetPosition(TopWindow);

                ShowDraggedAdorner(position);
                UpdateInsertionAdornerPosition();
            }
            e.Handled = true;
        }

        private void DropTarget_PreviewDragLeave(object sender, DragEventArgs e)
        {
            // Dragged Adorner is only created once on DragEnter + every time we enter the window. 
            // It's only removed once on the DragDrop, and every time we leave the window. (so no need to remove it here)
            Object DragItem = e.Data.GetData(DragDropFormat.Name);

            if (!Equals(DragItem, null))
            {
                RemoveInsertionAdorner();
            }
            e.Handled = true;
        }
        #endregion

        #endregion

        #region Window Events
        private void TopWindow_DragEnter(object sender, DragEventArgs e)
        {
            ShowDraggedAdorner(e.GetPosition(TopWindow));
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void TopWindow_DragOver(object sender, DragEventArgs e)
        {
            ShowDraggedAdorner(e.GetPosition(TopWindow));
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void TopWindow_DragLeave(object sender, DragEventArgs e)
        {
            RemoveDraggedAdorner();
            e.Handled = true;
        }
        #endregion

        #region Drag Data
        private Object DragData
        { get; set; }

        private DragAdorner DragAdorner
        { get; set; }

        private InsertAdorner InsertAdorner
        { get; set; }

        private Window TopWindow
        { get; set; }
        #endregion

        #region Status
        private Boolean HasVerticalOrientation
        { get; set; }

        private Int32 InsertionIndex
        { get; set; }

        private Boolean IsInFirstHalf
        { get; set; }
        #endregion

        #region Instance
        private static Helper _Instance;
        private static Object SyncObj = new Object();
        public static Helper Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new Helper(); }
                    }
                }
                return _Instance;
            }
        }
        #endregion

        #region Methods
        private void ShowDraggedAdorner(Point currentPosition)
        {
            if (Equals(DragAdorner, null))
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(SourceItemsControl);
                DragAdorner = new DragAdorner(DragData, GetDragDropTemplate(SourceItemsControl), SourceFrameworkElement, adornerLayer);
            }

            Double left = currentPosition.X - InitialMousePosition.X + InitialMouseOffset.X;
            Double top = currentPosition.Y - InitialMousePosition.Y + InitialMouseOffset.Y;
            DragAdorner.SetPosition(left, top);
        }

        private void RemoveDraggedAdorner()
        {
            if (!Equals(DragAdorner, null))
            {
                DragAdorner.Detach();
                DragAdorner = null;
            }
        }

        private void CreateInsertionAdorner()
        {
            if (!Equals(TargetFrameworkElement, null))
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(TargetFrameworkElement);
                InsertAdorner = new InsertAdorner(HasVerticalOrientation, IsInFirstHalf, TargetFrameworkElement, adornerLayer);
            }
        }

        private void UpdateInsertionAdornerPosition()
        {
            if (!Equals(InsertAdorner, null))
            {
                InsertAdorner.IsInFirstHalf = IsInFirstHalf;
                InsertAdorner.InvalidateVisual();
            }
        }

        private void RemoveInsertionAdorner()
        {
            if (!Equals(InsertAdorner, null))
            {
                InsertAdorner.Detach();
                InsertAdorner = null;
            }
        }

        private void DecideDropTarget(DragEventArgs e)
        {
            int targetItemsControlCount = TargetItemsControl.Items.Count;
            object draggedItem = e.Data.GetData(DragDropFormat.Name);

            if (IsDropDataTypeAllowed(draggedItem))
            {
                if (targetItemsControlCount > 0)
                {
                    HasVerticalOrientation = GetHasVerticalOrientation(TargetItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement);
                    TargetFrameworkElement = TargetItemsControl.ContainerFromElement((DependencyObject)e.OriginalSource) as FrameworkElement;

                    if (!Equals(TargetFrameworkElement, null))
                    {
                        Point positionRelativeToItemContainer = e.GetPosition(TargetFrameworkElement);
                        IsInFirstHalf = GetIsInFirstHalf(TargetFrameworkElement, positionRelativeToItemContainer, HasVerticalOrientation);
                        InsertionIndex = TargetItemsControl.ItemContainerGenerator.IndexFromContainer(TargetFrameworkElement);

                        if (!IsInFirstHalf)
                        { InsertionIndex++; }
                    }
                    else
                    {
                        TargetFrameworkElement = TargetItemsControl.ItemContainerGenerator.ContainerFromIndex(targetItemsControlCount - 1) as FrameworkElement;
                        IsInFirstHalf = false;
                        InsertionIndex = targetItemsControlCount;
                    }
                }
                else
                {
                    TargetFrameworkElement = null;
                    InsertionIndex = 0;
                }
            }
            else
            {
                TargetFrameworkElement = null;
                InsertionIndex = -1;
                e.Effects = DragDropEffects.None;
            }
        }

        private bool IsDropDataTypeAllowed(Object DragItem)
        {
            bool isDropDataTypeAllowed;
            IEnumerable collectionSource = TargetItemsControl.ItemsSource;
            if (!Equals(DragItem, null))
            {
                if (!Equals(collectionSource, null))
                {
                    Type DragItemType = DragItem.GetType();
                    Type collectionType = collectionSource.GetType();

                    Type genericIListType = collectionType.GetInterface("IList`1");
                    if (!Equals(genericIListType, null))
                    {
                        Type[] genericArguments = genericIListType.GetGenericArguments();
                        isDropDataTypeAllowed = genericArguments[0].IsAssignableFrom(DragItemType);
                    }
                    else if (typeof(IList).IsAssignableFrom(collectionType))
                    {
                        isDropDataTypeAllowed = true;
                    }
                    else
                    {
                        isDropDataTypeAllowed = false;
                    }
                }
                else // the ItemsControl's ItemsSource is not data bound.
                {
                    isDropDataTypeAllowed = true;
                }
            }
            else
            {
                isDropDataTypeAllowed = false;
            }
            return isDropDataTypeAllowed;
        }
        #endregion

        #region Static Methods
        private static void InsertItemInItemsControl(ItemsControl ItemsControl, Object ItemToInsert, Int32 InsertionIndex)
        {
            if (!Equals(ItemToInsert, null))
            {
                IEnumerable itemsSource = ItemsControl.ItemsSource;

                if (Equals(itemsSource, null))
                {
                    if (!ItemsControl.Items.Contains(ItemToInsert))
                        ItemsControl.Items.Insert(InsertionIndex, ItemToInsert);
                }
                // Is the ItemsSource IList or IList<T>? If so, insert the dragged item in the list.
                else if (itemsSource is IList)
                {
                    if (!((IList)itemsSource).Contains(ItemToInsert))
                        ((IList)itemsSource).Insert(InsertionIndex, ItemToInsert);
                }
                else
                {
                    Type type = itemsSource.GetType();
                    Type genericIListType = type.GetInterface("IList`1");
                    if (genericIListType != null)
                    {
                        type.GetMethod("Insert").Invoke(itemsSource, new[] { InsertionIndex, ItemToInsert });
                    }
                }
            }
        }

        private static int RemoveItemFromItemsControl(ItemsControl ItemsControl, Object ItemToRemove)
        {
            int indexToBeRemoved = -1;
            if (!Equals(ItemToRemove, null))
            {
                indexToBeRemoved = ItemsControl.Items.IndexOf(ItemToRemove);

                if (!Equals(indexToBeRemoved, -1))
                {
                    IEnumerable itemsSource = ItemsControl.ItemsSource;
                    if (Equals(itemsSource, null))
                    {
                        if (indexToBeRemoved >= 0 && indexToBeRemoved < ItemsControl.Items.Count)
                            ItemsControl.Items.RemoveAt(indexToBeRemoved);
                    }
                    // Is the ItemsSource IList or IList<T>? If so, remove the item from the list.
                    else if (itemsSource is IList)
                    {
                        IList list = (IList)itemsSource;
                        if (indexToBeRemoved >= 0 && indexToBeRemoved < list.Count)
                            list.RemoveAt(indexToBeRemoved);
                    }
                    else
                    {
                        Type type = itemsSource.GetType();
                        Type genericIListType = type.GetInterface("IList`1");
                        if (genericIListType != null)
                        {
                            type.GetMethod("RemoveAt").Invoke(itemsSource, new object[] { indexToBeRemoved });
                        }
                    }
                }
            }
            return indexToBeRemoved;
        }

        private static bool IsMovementBigEnough(Point InitialMousePosition, Point CurrentPosition)
        {
            return (Math.Abs(CurrentPosition.X - InitialMousePosition.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(CurrentPosition.Y - InitialMousePosition.Y) >= SystemParameters.MinimumVerticalDragDistance);
        }

        private static Boolean GetIsInFirstHalf(FrameworkElement Container, Point ClickedPoint, bool HasVerticalOrientation)
        {
            if (HasVerticalOrientation)
            {
                return ClickedPoint.Y < Container.ActualHeight / 2;
            }
            return ClickedPoint.X < Container.ActualWidth / 2;
        }

        private static Boolean GetHasVerticalOrientation(FrameworkElement FrameworkElement)
        {
            Boolean hasVerticalOrientation = true;

            if (!Equals(FrameworkElement, null))
            {
                Panel panel = VisualTreeHelper.GetParent(FrameworkElement) as Panel;
                StackPanel stackPanel;
                WrapPanel wrapPanel;

                if (!Equals((stackPanel = panel as StackPanel), null))
                {
                    hasVerticalOrientation = Equals(stackPanel.Orientation, Orientation.Vertical);
                }
                else if (!Equals((wrapPanel = panel as WrapPanel), null))
                {
                    hasVerticalOrientation = Equals(wrapPanel.Orientation, Orientation.Vertical);
                }
                // You can add support for more panel types here.
            }
            return hasVerticalOrientation;
        }
        #endregion
    }
}
