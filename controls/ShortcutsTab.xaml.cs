// Version: 1.0.0.362
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThmdPlayer.Core.shortcuts;

namespace ThmdPlayer.Core.controls
{
    /// <summary>
    /// Logika interakcji dla klasy ShortcutsTab.xaml
    /// </summary>
    public partial class ShortcutsTab : UserControl
    {
        private List<ShortcutsTabItem> _shortcutsTabItem = new List<ShortcutsTabItem>();

        public ShortcutsTab()
        {
            InitializeComponent();
        }

        public void AddShortcut(string name, string shortcut, string description, string icon)
        {
            ShortcutsTabItem item = new ShortcutsTabItem(name, shortcut, description, icon);
            _shortcutsTabItem.Add(item);
            UpdateShortcuts();
        }

        /// <summary>
        /// Updates the shortcuts displayed in the ShortcutsTab.
        /// 
        /// <Grid Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="1" Grid.RowSpan="1">
        ///     <TextBlock Text = "S" FontSize="28" FontWeight="Bold" Foreground="Red" TextAlignment="Left" VerticalAlignment="Center"/>
        /// </Grid>
        /// <Grid Grid.Row="2" Grid.Column= "1" Grid.ColumnSpan= "1" Grid.RowSpan= "1" >
        ///     < TextBlock Text= "hortcuts panel" FontSize= "16" FontWeight= "Bold" Foreground= "Gray" VerticalAlignment= "Bottom" />
        /// </ Grid >
        /// < Grid Grid.Row= "2" Grid.Column= "2" Grid.ColumnSpan= "2" Grid.RowSpan= "1" >
        ///     < TextBlock Text= "key: H" FontSize= "24" FontWeight= "Bold" Foreground= "Red" TextAlignment= "Center" VerticalAlignment= "Center" />
        /// </ Grid >
        /// 
        /// </summary>

        private void UpdateShortcuts()
        {
            _grid.Children.Clear();

            _grid.Children.Add(new TextBlock
            {
                Text = "S",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Red,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            });
            _grid.Children.Add(new TextBlock
            {
                Text = "hortcuts panel",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Bottom
            });
            _grid.Children.Add(new TextBlock
            {
                Text = "key: H",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Red,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            var shortcuts_cell = new ShortcutsCell();
            shortcuts_cell.Children.Clear();
            foreach (var item in _shortcutsTabItem)
            {
                shortcuts_cell.Children.Add(item);
            }
        }
    }

    public class  ShortcutsCell : Grid
    {
        private int _row = 0;
        private int _column = 0;
        public int Column { get { return _column; } set { _column = value; this.ColumnDefinitions.Add(new ColumnDefinition()); Grid.SetColumn(this, _column); } }
        public int Row
        {
            get { return _row; }
            set
            {
                _row = value;
                this.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(this, _row);
            }
        }
        public ShortcutsCell()
        {
            this.Background = Brushes.Transparent;
            this.Margin = new Thickness(0);
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.RowDefinitions.Add(new RowDefinition());
            this.RowDefinitions.Add(new RowDefinition());
            this.RowDefinitions.Add(new RowDefinition());
            this.RowDefinitions.Add(new RowDefinition());
        }
    }
}
