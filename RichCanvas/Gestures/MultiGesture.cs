using Microsoft.UI.Xaml.Input;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Defines a multiple gestures combination that can be used to match an input.
    /// </summary>
    public class MultiGesture : InputGesture
    {
        private readonly InputGesture[] _gestures;

        /// <summary>Constructs an instance of a <see cref="MultiGesture"/>.</summary>
        /// <param name="gestures">The input gestures.</param>
        public MultiGesture(params InputGesture[] gestures)
        {
            _gestures = gestures;
        }

        /// <inheritdoc />
        public override bool Matches(object sender, PointerRoutedEventArgs? e)
        {
            for (int i = 0; i < _gestures.Length; i++)
            {
                if (!_gestures[i].Matches(sender, e))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc />
        public override bool Matches(object sender, KeyRoutedEventArgs e)
        {
            for (int i = 0; i < _gestures.Length; i++)
            {
                if (!_gestures[i].Matches(sender, e))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
