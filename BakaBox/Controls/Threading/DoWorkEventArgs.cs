using System.ComponentModel;

namespace BakaBox.Controls.Threading
{
    public sealed class DoWorkEventArgs<T> : DoWorkEventArgs
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.Custom.Generic.DoWorkEventArgs class.
        /// </summary>
        /// <param name="argument">Specifies an argument for an asynchronous operation.</param>
        public DoWorkEventArgs(T argument)
            : base(argument)
        {
            Argument = argument;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets a value that represents the argument of an asynchronous operation.
        /// </summary>
        public new T Argument
        {
            get;
            private set;
        }
        #endregion
    }
}
