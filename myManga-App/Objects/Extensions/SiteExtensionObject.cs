using myMangaSiteExtension.Interfaces;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace myManga_App.Objects.Extensions
{
    public sealed class SiteExtensionObject : DependencyObject
    {
        #region Constructors
        public ISiteExtension SiteExtension
        { get; private set; }

        public SiteExtensionObject(ISiteExtension SiteExtension, Boolean Enabled)
            : base()
        {
            Update(SiteExtension, Enabled);
        }

        public void Update(ISiteExtension SiteExtension, Boolean Enabled)
        {
            this.SiteExtension = SiteExtension;
            Name = SiteExtension.SiteExtensionDescriptionAttribute.Name;
            this.Enabled = Enabled;
            IsAuthenticated = SiteExtension.IsAuthenticated;
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(SiteExtension.ExtensionIcon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        public override string ToString()
        {
            return String.Format(
                "[SiteExtensionObject][{0}]{1}",
                Enabled ? "Enabled" : "Disabled",
                Name);
        }
        #endregion

        #region Name
        private static readonly DependencyPropertyKey NamePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "Name",
            typeof(String),
            typeof(SiteExtensionObject),
            null);
        private static readonly DependencyProperty NameProperty = NamePropertyKey.DependencyProperty;

        public String Name
        {
            get { return (String)GetValue(NameProperty); }
            private set { SetValue(NamePropertyKey, value); }
        }
        #endregion

        #region Icon
        private static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(ImageSource),
            typeof(SiteExtensionObject),
            null);

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        #endregion

        #region Enabled
        private static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(Boolean),
            typeof(SiteExtensionObject),
            new PropertyMetadata(false));

        public Boolean Enabled
        {
            get { return (Boolean)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }
        #endregion

        #region IsAuthenticated
        private static readonly DependencyPropertyKey IsAuthenticatedPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsAuthenticated",
            typeof(Boolean),
            typeof(SiteExtensionObject),
            null);
        private static readonly DependencyProperty IsAuthenticatedProperty = IsAuthenticatedPropertyKey.DependencyProperty;

        public Boolean IsAuthenticated
        {
            get { return (Boolean)GetValue(IsAuthenticatedProperty); }
            private set { SetValue(IsAuthenticatedPropertyKey, value); }
        }
        #endregion
    }
}
