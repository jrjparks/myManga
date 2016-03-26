using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace myManga_App.Objects.Extensions
{
    [Flags]
    public enum AuthenticationState
    {
        Ignored = 0x00,
        Invalid = 0x01,
        Valid = 0x02,
        Required = 0x04
    }

    public class ExtensionObject : DependencyObject
    {
        #region Constructors
        protected readonly App App = App.Current as App;

        public IExtension Extension
        { get; private set; }

        public ExtensionObject(IExtension Extension, Boolean Enabled, Boolean LoadIcon = true) : base()
        {
            Update(Extension, Enabled, LoadIcon);
        }

        public void Update(IExtension Extension, Boolean Enabled, Boolean LoadIcon = true)
        {
            this.Enabled = Enabled;
            Update(Extension);
        }

        public void Update(IExtension Extension, Boolean LoadIcon=true)
        {
            this.Extension = Extension;

            Name = String.Format("{0} ({1})", Extension.ExtensionDescriptionAttribute.Name, Extension.ExtensionDescriptionAttribute.Language);

            // Load the extension icon to an ImageSource for WPF
            Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                Extension.ExtensionIcon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // Detect AuthenticationState
            if(Extension.IsAuthenticated)
            { AuthenticationState = AuthenticationState.Valid; }
            else
            {
                if(Extension.ExtensionDescriptionAttribute.RequiresAuthentication)
                {
                    if(App.UserAuthentication.UserPluginAuthentications.Count(upao => Equals(upao.PluginName, Name)) > 0)
                    { AuthenticationState = AuthenticationState.Invalid; }
                    else
                    { AuthenticationState = AuthenticationState.Required; }
                }
                else
                { AuthenticationState = AuthenticationState.Ignored; }
            }

            AuthenticationState = Extension.IsAuthenticated ?
                    AuthenticationState.Valid :         // Extension is Authenticated? Awesome.
                    (Extension.ExtensionDescriptionAttribute.RequiresAuthentication ?
                        AuthenticationState.Required :   // Extension is not Authenticated and needs to be? Boo!
                        AuthenticationState.Ignored     // What ever...
                    );
        }

        public override string ToString()
        {
            return String.Format(
                "[ExtensionObject][{0}]{1} - Auth:{2}",
                Enabled ? "Enabled" : "Disabled",
                Name,
                Enum.GetName(typeof(AuthenticationState), AuthenticationState));
        }
        #endregion

        #region Name
        public static readonly DependencyPropertyKey NamePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "Name",
            typeof(String),
            typeof(ExtensionObject),
            null);
        public static readonly DependencyProperty NameProperty = NamePropertyKey.DependencyProperty;

        public String Name
        {
            get { return (String)GetValue(NameProperty); }
            private set { SetValue(NamePropertyKey, value); }
        }
        #endregion

        #region Icon
        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(ImageSource),
            typeof(ExtensionObject),
            null);

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        #endregion

        #region Enabled
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(Boolean),
            typeof(ExtensionObject),
            new PropertyMetadata(false));

        public Boolean Enabled
        {
            get { return (Boolean)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }
        #endregion

        #region AuthenticationState
        public static readonly DependencyPropertyKey AuthenticationStatePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "AuthenticationState",
            typeof(AuthenticationState),
            typeof(ExtensionObject),
            new PropertyMetadata(AuthenticationState.Invalid));
        public static readonly DependencyProperty AuthenticationStateProperty = AuthenticationStatePropertyKey.DependencyProperty;

        public AuthenticationState AuthenticationState
        {
            get { return (AuthenticationState)GetValue(AuthenticationStateProperty); }
            private set { SetValue(AuthenticationStatePropertyKey, value); }
        }
        #endregion
    }
}
