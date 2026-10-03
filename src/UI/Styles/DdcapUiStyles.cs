using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Markup;

namespace DDCAP.AltayCTF
{
    public static class DdcapUiStyles
    {
        public static Style CreateDarkColumnHeaderStyle()
        {
            Style headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 20))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 220, 220))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontFamilyProperty, new FontFamily("Consolas")));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 11.0));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(38, 38, 38))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(6, 4, 6, 4)));

            ControlTemplate template = new ControlTemplate(typeof(DataGridColumnHeader));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(DataGridColumnHeader.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(DataGridColumnHeader.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(DataGridColumnHeader.BorderThicknessProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(DataGridColumnHeader.PaddingProperty));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);

            template.VisualTree = border;
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.TemplateProperty, template));
            return headerStyle;
        }

        public static Style CreateDarkCellStyle()
        {
            Style cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new Binding("Foreground") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1) }));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(4, 2, 4, 2)));

            // Trigger on cell selection: deep tactical crimson/maroon #381212 instead of Aero blue #0078D7
            Trigger selTrigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(56, 18, 18))));
            selTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.White));
            cellStyle.Triggers.Add(selTrigger);

            return cellStyle;
        }

        public static Style CreateDarkRowStyle()
        {
            Style rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0)));
            rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, new SolidColorBrush(Color.FromRgb(212, 212, 212))));

            // Trigger on row selection: deep tactical crimson/maroon #381212 instead of Aero blue #0078D7
            Trigger selTrigger = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(DataGridRow.BackgroundProperty, new SolidColorBrush(Color.FromRgb(56, 18, 18))));
            selTrigger.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Brushes.White));
            rowStyle.Triggers.Add(selTrigger);
            return rowStyle;
        }

        public static Style CreateDarkTabItemStyle()
        {
            Style tabStyle = new Style(typeof(TabItem));
            tabStyle.Setters.Add(new Setter(TabItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(18, 18, 18))));
            tabStyle.Setters.Add(new Setter(TabItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(160, 160, 160))));
            tabStyle.Setters.Add(new Setter(TabItem.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(38, 38, 38))));
            tabStyle.Setters.Add(new Setter(TabItem.BorderThicknessProperty, new Thickness(1, 1, 1, 0)));
            tabStyle.Setters.Add(new Setter(TabItem.PaddingProperty, new Thickness(14, 6, 14, 6)));
            tabStyle.Setters.Add(new Setter(TabItem.FontFamilyProperty, new FontFamily("Consolas")));
            tabStyle.Setters.Add(new Setter(TabItem.FontSizeProperty, 11.0));
            tabStyle.Setters.Add(new Setter(TabItem.FontWeightProperty, FontWeights.Bold));

            ControlTemplate template = new ControlTemplate(typeof(TabItem));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "TabBorder");
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1, 1, 1, 0));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(38, 38, 38)));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(18, 18, 18)));
            border.SetValue(Border.MarginProperty, new Thickness(0, 0, 2, 0));

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(ContentPresenter.MarginProperty, new Thickness(12, 5, 12, 5));
            border.AppendChild(content);

            template.VisualTree = border;

            // Trigger when selected: dark #222222 background, #DC2626 red top/sides border, bright white text
            Trigger selTrigger = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(34, 34, 34)), "TabBorder"));
            selTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(220, 38, 38)), "TabBorder"));
            selTrigger.Setters.Add(new Setter(TabItem.ForegroundProperty, Brushes.White));
            template.Triggers.Add(selTrigger);

            // Trigger when mouse over unselected: slightly lighter dark #202020
            Trigger hoverTrigger = new Trigger { Property = TabItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(28, 28, 28)), "TabBorder"));
            template.Triggers.Add(hoverTrigger);

            tabStyle.Setters.Add(new Setter(TabItem.TemplateProperty, template));
            return tabStyle;
        }

        public static Style CreateDarkGridViewColumnHeaderStyle()
        {
            Style headerStyle = new Style(typeof(GridViewColumnHeader));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 26))));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.ForegroundProperty, new SolidColorBrush(Color.FromRgb(255, 200, 0))));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.FontFamilyProperty, new FontFamily("Consolas")));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.FontSizeProperty, 11.0));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(50, 50, 62))));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.PaddingProperty, new Thickness(6, 4, 6, 4)));

            ControlTemplate template = new ControlTemplate(typeof(GridViewColumnHeader));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(GridViewColumnHeader.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(GridViewColumnHeader.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(GridViewColumnHeader.BorderThicknessProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(GridViewColumnHeader.PaddingProperty));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);

            template.VisualTree = border;
            headerStyle.Setters.Add(new Setter(GridViewColumnHeader.TemplateProperty, template));
            return headerStyle;
        }

        public static Style CreateDarkListViewItemStyle()
        {
            Style itemStyle = new Style(typeof(ListViewItem));
            itemStyle.Setters.Add(new Setter(ListViewItem.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(ListViewItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 220, 220))));
            itemStyle.Setters.Add(new Setter(ListViewItem.BorderBrushProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(ListViewItem.BorderThicknessProperty, new Thickness(1)));
            itemStyle.Setters.Add(new Setter(ListViewItem.PaddingProperty, new Thickness(6, 4, 6, 4)));
            itemStyle.Setters.Add(new Setter(ListViewItem.FontFamilyProperty, new FontFamily("Consolas")));
            itemStyle.Setters.Add(new Setter(ListViewItem.FontSizeProperty, 12.0));
            itemStyle.Setters.Add(new Setter(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));

            ControlTemplate template = new ControlTemplate(typeof(ListViewItem));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "ItemBorder");
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ListViewItem.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(ListViewItem.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(ListViewItem.BorderThicknessProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(ListViewItem.PaddingProperty));

            FrameworkElementFactory rowPresenter = new FrameworkElementFactory(typeof(GridViewRowPresenter));
            border.AppendChild(rowPresenter);
            template.VisualTree = border;

            // Hover trigger: dark slate grey with lighter border
            Trigger hoverTrigger = new Trigger { Property = ListViewItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(35, 35, 46)), "ItemBorder"));
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(70, 70, 95)), "ItemBorder"));
            template.Triggers.Add(hoverTrigger);

            // Selected trigger: deep cyberpunk emerald background with vibrant neon green border & text
            Trigger selTrigger = new Trigger { Property = ListViewItem.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 55, 38)), "ItemBorder"));
            selTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0, 255, 150)), "ItemBorder"));
            selTrigger.Setters.Add(new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0, 255, 150))));
            selTrigger.Setters.Add(new Setter(ListViewItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0, 255, 150))));
            template.Triggers.Add(selTrigger);

            itemStyle.Setters.Add(new Setter(ListViewItem.TemplateProperty, template));
            return itemStyle;
        }

        public static void ApplyDarkListViewStyle(ListView lv)
        {
            if (lv == null) return;
            lv.Background = new SolidColorBrush(Color.FromRgb(20, 20, 25));
            lv.Foreground = Brushes.White;
            lv.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 62));
            lv.BorderThickness = new Thickness(1);
            lv.FontFamily = new FontFamily("Consolas");

            lv.Resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(Color.FromRgb(20, 55, 38));
            lv.Resources[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(Color.FromRgb(0, 255, 150));
            lv.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(16, 42, 29));
            lv.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = new SolidColorBrush(Color.FromRgb(0, 230, 130));
            lv.Resources[typeof(GridViewColumnHeader)] = CreateDarkGridViewColumnHeaderStyle();

            lv.ItemContainerStyle = CreateDarkListViewItemStyle();
        }

        public static Style CreateDarkComboBoxItemStyle(Color? selectedBg = null, Color? selectedFg = null)
        {
            Color selBgColor = selectedBg.HasValue ? selectedBg.Value : Color.FromRgb(25, 55, 40);
            Color selFgColor = selectedFg.HasValue ? selectedFg.Value : Color.FromRgb(0, 255, 150);

            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(24, 24, 30))));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(230, 230, 230))));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BorderBrushProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BorderThicknessProperty, new Thickness(1)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(8, 5, 8, 5)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.FontFamilyProperty, new FontFamily("Consolas")));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.FontSizeProperty, 12.0));

            ControlTemplate template = new ControlTemplate(typeof(ComboBoxItem));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "Bd");
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ComboBoxItem.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(ComboBoxItem.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(ComboBoxItem.BorderThicknessProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(ComboBoxItem.PaddingProperty));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            // Highlighted (mouse hover in dropdown)
            Trigger highlightTrigger = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlightTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 60)), "Bd"));
            highlightTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(75, 75, 100)), "Bd"));
            highlightTrigger.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White));
            template.Triggers.Add(highlightTrigger);

            // Selected
            Trigger selTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(selBgColor), "Bd"));
            selTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(selFgColor), "Bd"));
            selTrigger.Setters.Add(new Setter(TextElement.ForegroundProperty, new SolidColorBrush(selFgColor)));
            template.Triggers.Add(selTrigger);

            itemStyle.Setters.Add(new Setter(ComboBoxItem.TemplateProperty, template));
            return itemStyle;
        }

        public static void ApplyDarkComboBoxStyle(ComboBox cb, Color? bg = null, Color? fg = null, Color? selBg = null, Color? selFg = null)
        {
            if (cb == null) return;
            Color bgColor = bg ?? Color.FromRgb(24, 24, 30);
            Color fgColor = fg ?? Color.FromRgb(235, 235, 235);
            
            try
            {
                string xaml = string.Format(@"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBox'>
                    <Setter Property='Background' Value='#{0:X2}{1:X2}{2:X2}'/>
                    <Setter Property='Foreground' Value='#{3:X2}{4:X2}{5:X2}'/>
                    <Setter Property='BorderBrush' Value='#373744'/>
                    <Setter Property='BorderThickness' Value='1'/>
                    <Setter Property='FontFamily' Value='Consolas'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ComboBox'>
                                <Grid>
                                    <ToggleButton Name='ToggleButton' BorderBrush='{{TemplateBinding BorderBrush}}' Background='{{TemplateBinding Background}}' Foreground='{{TemplateBinding Foreground}}' BorderThickness='{{TemplateBinding BorderThickness}}' IsChecked='{{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={{RelativeSource TemplatedParent}}}}' ClickMode='Press'>
                                        <ToggleButton.Template>
                                            <ControlTemplate TargetType='ToggleButton'>
                                                <Border Background='{{TemplateBinding Background}}' BorderBrush='{{TemplateBinding BorderBrush}}' BorderThickness='{{TemplateBinding BorderThickness}}'>
                                                    <Path x:Name='Arrow' Fill='{{TemplateBinding Foreground}}' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,5,0' Data='M 0 0 L 4 4 L 8 0 Z'/>
                                                </Border>
                                            </ControlTemplate>
                                        </ToggleButton.Template>
                                    </ToggleButton>
                                    <ContentPresenter Name='ContentSite' IsHitTestVisible='False' Content='{{TemplateBinding SelectionBoxItem}}' ContentTemplate='{{TemplateBinding SelectionBoxItemTemplate}}' ContentTemplateSelector='{{TemplateBinding ItemTemplateSelector}}' Margin='8,3,23,3' VerticalAlignment='Center' HorizontalAlignment='Left' />
                                    <TextBox x:Name='PART_EditableTextBox' HorizontalAlignment='Left' VerticalAlignment='Center' Margin='8,3,23,3' Focusable='True' Background='Transparent' Foreground='{{TemplateBinding Foreground}}' Visibility='Hidden' IsReadOnly='{{TemplateBinding IsReadOnly}}' BorderThickness='0'/>
                                    <Popup Name='Popup' Placement='Bottom' IsOpen='{{TemplateBinding IsDropDownOpen}}' AllowsTransparency='True' Focusable='False' PopupAnimation='Slide'>
                                        <Grid Name='DropDown' SnapsToDevicePixels='True' MinWidth='{{TemplateBinding ActualWidth}}' MaxHeight='{{TemplateBinding MaxDropDownHeight}}'>
                                            <Border x:Name='DropDownBorder' Background='#18181E' BorderThickness='1' BorderBrush='#555568'/>
                                            <ScrollViewer Margin='1' SnapsToDevicePixels='True'>
                                                <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained' />
                                            </ScrollViewer>
                                        </Grid>
                                    </Popup>
                                </Grid>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsEditable' Value='True'>
                                        <Setter Property='IsTabStop' Value='False'/>
                                        <Setter TargetName='PART_EditableTextBox' Property='Visibility' Value='Visible'/>
                                        <Setter TargetName='ContentSite' Property='Visibility' Value='Hidden'/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>", bgColor.R, bgColor.G, bgColor.B, fgColor.R, fgColor.G, fgColor.B);
                cb.Style = (Style)XamlReader.Parse(xaml);
            }
            catch { }
            
            cb.ItemContainerStyle = CreateDarkComboBoxItemStyle(selBg, selFg);
        }

        public static void ApplyDarkButtonStyle(Button btn, Color bg, Color fg, Color borderCol)
        {
            if (btn == null) return;
            try
            {
                byte hR = (byte)Math.Min(255, bg.R + 20);
                byte hG = (byte)Math.Min(255, bg.G + 20);
                byte hB = (byte)Math.Min(255, bg.B + 20);
                
                string xaml = string.Format(@"
                <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>
                    <Setter Property='Background' Value='#{0:X2}{1:X2}{2:X2}'/>
                    <Setter Property='Foreground' Value='#{3:X2}{4:X2}{5:X2}'/>
                    <Setter Property='BorderBrush' Value='#{6:X2}{7:X2}{8:X2}'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Button'>
                                <Border x:Name='Bd' Background='{{TemplateBinding Background}}' BorderBrush='{{TemplateBinding BorderBrush}}' BorderThickness='{{TemplateBinding BorderThickness}}' Padding='{{TemplateBinding Padding}}'>
                                    <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                                </Border>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsMouseOver' Value='True'>
                                        <Setter TargetName='Bd' Property='Background' Value='#{9:X2}{10:X2}{11:X2}'/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>", bg.R, bg.G, bg.B, fg.R, fg.G, fg.B, borderCol.R, borderCol.G, borderCol.B, hR, hG, hB);
                btn.Style = (Style)XamlReader.Parse(xaml);
            }
            catch { }
        }

        public static ContextMenu CreateDarkContextMenu(Color? borderCol = null)
        {
            Color borderColor = borderCol.HasValue ? borderCol.Value : Color.FromRgb(220, 38, 38);

            var cmTemplate = new ControlTemplate(typeof(ContextMenu));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(18, 18, 22)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(borderColor));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(3));

            var itemsPresenterFactory = new FrameworkElementFactory(typeof(ItemsPresenter));
            borderFactory.AppendChild(itemsPresenterFactory);
            cmTemplate.VisualTree = borderFactory;

            return new ContextMenu
            {
                Template = cmTemplate,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };
        }

        public static MenuItem CreateDarkMenuItem(string header, Brush fgBrush, RoutedEventHandler onClick, Color? hoverBg = null)
        {
            Color hoverColor = hoverBg.HasValue ? hoverBg.Value : Color.FromRgb(40, 40, 52);

            var miTemplate = new ControlTemplate(typeof(MenuItem));
            var miBorder = new FrameworkElementFactory(typeof(Border));
            miBorder.Name = "ItemBorder";
            miBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(18, 18, 22)));
            miBorder.SetValue(Border.PaddingProperty, new Thickness(12, 6, 12, 6));

            var miPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            miPresenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            miPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            miBorder.AppendChild(miPresenter);
            miTemplate.VisualTree = miBorder;

            var trig = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            trig.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverColor), "ItemBorder"));
            miTemplate.Triggers.Add(trig);

            var mi = new MenuItem
            {
                Header = header,
                Foreground = fgBrush ?? Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                Template = miTemplate,
                Cursor = Cursors.Hand
            };
            if (onClick != null) mi.Click += onClick;
            return mi;
        }
        public static Style CreateDarkTextBoxStyle()
        {
            Style style = new Style(typeof(TextBox));
            style.Setters.Add(new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 20))));
            style.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(64, 64, 64))));
            style.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(4, 2, 4, 2)));
            style.Setters.Add(new Setter(TextBox.FontFamilyProperty, new FontFamily("Consolas")));
            style.Setters.Add(new Setter(TextBox.CaretBrushProperty, Brushes.Cyan));
            style.Setters.Add(new Setter(TextBox.SelectionBrushProperty, new SolidColorBrush(Color.FromRgb(20, 55, 38))));
            
            Trigger hover = new Trigger { Property = TextBox.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(100, 100, 100))));
            style.Triggers.Add(hover);
            
            Trigger focus = new Trigger { Property = TextBox.IsKeyboardFocusWithinProperty, Value = true };
            focus.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0, 255, 150))));
            style.Triggers.Add(focus);

            return style;
        }

        public static Style CreateDarkCheckBoxStyle()
        {
            Style style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(CheckBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(212, 212, 212))));
            style.Setters.Add(new Setter(CheckBox.FontFamilyProperty, new FontFamily("Consolas")));
            style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center));

            ControlTemplate template = new ControlTemplate(typeof(CheckBox));
            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Bd";
            border.SetValue(Border.WidthProperty, 14.0);
            border.SetValue(Border.HeightProperty, 14.0);
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 20)));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(64, 64, 64)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            border.SetValue(Border.MarginProperty, new Thickness(0, 0, 6, 0));

            FrameworkElementFactory checkMark = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            checkMark.Name = "CheckMark";
            checkMark.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M 3 7 L 6 10 L 11 4"));
            checkMark.SetValue(System.Windows.Shapes.Path.StrokeProperty, Brushes.Cyan);
            checkMark.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 2.0);
            checkMark.SetValue(System.Windows.Shapes.Path.VisibilityProperty, Visibility.Hidden);
            checkMark.SetValue(System.Windows.Shapes.Path.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkMark.SetValue(System.Windows.Shapes.Path.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(checkMark);

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            stackPanel.AppendChild(border);
            stackPanel.AppendChild(content);
            template.VisualTree = stackPanel;

            Trigger checkedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
            checkedTrigger.Setters.Add(new Setter(System.Windows.Shapes.Path.VisibilityProperty, Visibility.Visible, "CheckMark"));
            template.Triggers.Add(checkedTrigger);

            Trigger hoverTrigger = new Trigger { Property = CheckBox.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(100, 100, 100)), "Bd"));
            template.Triggers.Add(hoverTrigger);

            style.Setters.Add(new Setter(CheckBox.TemplateProperty, template));
            return style;
        }
        public static void ApplyGlobalDarkScrollBarStyle(Window window)
        {
            if (window == null) return;
            try
            {
                string xaml = @"
                <ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Style x:Key='DarkScrollBarThumb' TargetType='Thumb'>
                        <Setter Property='Background' Value='#444444'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='Thumb'>
                                    <Border Background='{TemplateBinding Background}' CornerRadius='3' Margin='2'/>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                        <Style.Triggers>
                            <Trigger Property='IsMouseOver' Value='True'>
                                <Setter Property='Background' Value='#666666'/>
                            </Trigger>
                            <Trigger Property='IsDragging' Value='True'>
                                <Setter Property='Background' Value='#00FF96'/>
                            </Trigger>
                        </Style.Triggers>
                    </Style>

                    <Style TargetType='ScrollBar'>
                        <Setter Property='Background' Value='#111111'/>
                        <Setter Property='BorderBrush' Value='#222222'/>
                        <Setter Property='Template'>
                            <Setter.Value>
                                <ControlTemplate TargetType='ScrollBar'>
                                    <Grid Background='{TemplateBinding Background}'>
                                        <Track x:Name='PART_Track' IsDirectionReversed='true'>
                                            <Track.Thumb>
                                                <Thumb Style='{StaticResource DarkScrollBarThumb}'/>
                                            </Track.Thumb>
                                        </Track>
                                    </Grid>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                </ResourceDictionary>";
                var dict = (ResourceDictionary)XamlReader.Parse(xaml);
                window.Resources.MergedDictionaries.Add(dict);
            }
            catch { }
        }
    }
}
