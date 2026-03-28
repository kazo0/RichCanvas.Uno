using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace RichCanvas.Gestures
{
    /// <summary>
    /// Matches a pointer gesture combined with one or more keyboard keys being held down.
    /// </summary>
    /// <remarks>
    /// [WPF Migration] Replaces the custom MouseKeyGesture class from WPF which combined MouseGesture + KeyGesture.
    /// </remarks>
    public class PointerKeyGesture : InputGesture
    {
        private readonly PointerGesture _pointerGesture;
        private readonly VirtualKey[] _keys;

        /// <summary>
        /// Gets or sets the pointer gesture component.
        /// </summary>
        public PointerGesture PointerGesture => _pointerGesture;

        /// <summary>
        /// Gets or sets the required keys.
        /// </summary>
        public VirtualKey[] Keys => _keys;

        /// <summary>
        /// Creates a new <see cref="PointerKeyGesture"/>.
        /// </summary>
        public PointerKeyGesture(PointerGesture pointerGesture, params VirtualKey[] keys)
        {
            _pointerGesture = pointerGesture;
            _keys = keys;
        }

        /// <inheritdoc/>
        public override bool Matches(object sender, PointerRoutedEventArgs? e)
        {
            if (e == null) return false;

            // Check all required keys are held
            for (int i = 0; i < _keys.Length; i++)
            {
                if (!InputHelper.IsKeyDown(_keys[i]))
                    return false;
            }

            return _pointerGesture.Matches(sender, e);
        }

        /// <inheritdoc/>
        public override bool Matches(object sender, KeyRoutedEventArgs e)
        {
            // Check if the key event corresponds to one of our keys
            for (int i = 0; i < _keys.Length; i++)
            {
                if (e.Key == _keys[i])
                {
                    // Check all keys are down
                    for (int j = 0; j < _keys.Length; j++)
                    {
                        if (!InputHelper.IsKeyDown(_keys[j]))
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
