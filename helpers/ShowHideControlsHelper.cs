// Version: 1.0.0.676
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ThmdPlayer.Core.helpers
{
    /// <summary>
    /// Helper class to show and hide controls with a delay
    /// </summary>
    public class ShowHideControlsHelper
    {
        /// <summary>
        /// Shows the control after a specified time delay.
        /// </summary>
        /// <param name="control">Element</param>
        /// <param name="time">Time to show</param>
        /// <returns></returns>
        public async static Task Show(Control control, TimeSpan time) 
        {
            await Task.Delay(time);
            control.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Hides the control after a specified time delay.
        /// </summary>
        /// <param name="control">Element</param>
        /// <param name="time">Time to hide</param>
        /// <returns></returns>
        public async static Task Hide(Control control, TimeSpan time)
        {
            await Task.Delay(time);
            control.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
