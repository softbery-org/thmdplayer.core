// Version: 1.0.0.663
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using ThmdPlayer.Core.controls;

namespace ThmdPlayer.Core.helpers
{
    /// <summary>
    /// Helper class for controlling visibility of controls using storyboards.
    /// </summary>
    public static class StoryboardHelper
    {
        /// <summary>
        /// Hides the control using the provided storyboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="storyboard"></param>
        /// <returns></returns>
        public static async Task HideByStoryboard(this Control sender, Storyboard storyboard)
        {
            if (storyboard != null)
                await sender.Hide(storyboard);
        }

        /// <summary>
        /// Shows the control using the provided storyboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="storyboard"></param>
        /// <returns></returns>
        public static async Task ShowByStoryboard(this Control sender, Storyboard storyboard)
        {
            if (storyboard != null)
                await sender.Show(storyboard);
        }

        private static async Task Hide(this Control sender, Storyboard storyboard)
        {
            var task = Task.Run((() =>
            {
                try
                {
                    sender.Dispatcher.Invoke(() =>
                    {
                        if (sender.IsVisible)
                        {
                            if (!sender.IsMouseOver)
                            {
                                sender.Cursor = Cursors.None;

                                storyboard.AutoReverse = false;
                                storyboard.Begin(sender, HandoffBehavior.Compose, false);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(Core.Logs.LogLevel.Error, "Console", $"{ex.Message}");
                    Logger.Log.Log(Core.Logs.LogLevel.Error, "File", $"{ex.Message}");
                }
            }));
            await Task.FromResult(task).Result;
        }

        private static async Task Show(this Control sender, Storyboard storyboard)
        {
            var task = Task.Run((() =>
            {
                try
                {
                    sender.Dispatcher.Invoke(() =>
                    {
                        sender.Visibility = Visibility.Visible;
                        sender.Cursor = Cursors.Arrow;

                        storyboard.AutoReverse = false;
                        storyboard.Begin(sender, HandoffBehavior.Compose, false);
                    });
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(Core.Logs.LogLevel.Error, "Console", $"{ex.Message}");
                    Logger.Log.Log(Core.Logs.LogLevel.Error, "File", $"{ex.Message}");
                }
            }));
            await Task.FromResult(task).Result;
        }
    }
}
