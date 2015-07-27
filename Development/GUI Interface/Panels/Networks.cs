using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xaml;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class NetworkPanel {

        public Grid Panel;

        public NetworkPanel() {
            this.Panel = new Grid();
        }

        public Grid GetPanel() {

            ColumnDefinition sidebar = new ColumnDefinition();
            sidebar.Width = new GridLength(220);

            Panel.ColumnDefinitions.Add(sidebar);
            Panel.ColumnDefinitions.Add(new ColumnDefinition());

            #region Sidebar
            StackPanel sidebarContainer = new StackPanel();
            StackPanel tools = new StackPanel();

            tools.Height = 50;
            tools.Background = new SolidColorBrush(Colors.Sienna);

            tools.Width = sidebarContainer.Width;
            Panel.Children.Add(sidebarContainer);
            Button addBtn = new Button();
            addBtn.Content = "+ Add New Network";
            addBtn.Margin = new Thickness(10, 4, 10, 4);
            addBtn.Height = tools.Height - 10;
            addBtn.Background = new SolidColorBrush(Colors.Transparent);
            addBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            addBtn.SetValue(Grid.ColumnProperty, 0);
            addBtn.VerticalAlignment = VerticalAlignment.Center;
            addBtn.BorderThickness = new Thickness(0);
            addBtn.Foreground = new SolidColorBrush(Colors.White);
            tools.Children.Add(addBtn);

            sidebarContainer.Children.Add(tools);

            foreach (NetworkBot b in Core.Networks.Values) {
                Label l = new Label();
                l.Content = b.FriendlyName;

                sidebarContainer.Children.Add(l);
            }
            #endregion

            #region Configuration Loading
            StackPanel configPanel = new StackPanel();
            configPanel.CanVerticallyScroll = true;
            configPanel.VerticalAlignment = VerticalAlignment.Top;

            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.Content = configPanel;
            sv.SetValue(Grid.ColumnProperty, 1);
            

            Label stackHeader = new Label();
            configPanel.Children.Add(stackHeader);
            stackHeader.FontSize = 30;
            stackHeader.Content = "Editing Network: Localhost";
            stackHeader.Foreground = new SolidColorBrush(Colors.White);
            stackHeader.Background = new SolidColorBrush(Colors.Sienna);
            stackHeader.Height = 50;

            String file = Environment.CurrentDirectory + @"\IRC.xml";
            try {
                String stackText = File.ReadAllText(file);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(stackText);

                foreach (XmlNode node in xml.ChildNodes[1].ChildNodes) {
                    ParseNode(configPanel, node);
                }
            }

            catch (Exception e) { 
                Console.WriteLine(e);
            }

            Panel.Children.Add(sv);
            #endregion

            return Panel;

        }

        private void ParseNode(StackPanel configPanel, XmlNode n) {
            switch (n.Name.ToLower()) {
                case "field":
                    WrapPanel fieldPanel = new WrapPanel();
                    if (n.Attributes["Type"] == null) return;

                    if (n.Attributes["Label"] != null) {
                        Label fieldLabel = new Label();
                        fieldLabel.Content = n.Attributes["Label"];
                        fieldLabel.Width = 120;
                        fieldPanel.Children.Add(fieldLabel);
                    }

                    switch (n.Attributes["Type"].Value.ToLower()) {
                        case "text":
                            TextBox tb = new TextBox();
                            tb.Height = 20;
                            tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                            tb.MinWidth = 400;
                            tb.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(tb);
                            break;

                        case "checkbox":
                            CheckBox cb = new CheckBox();
                            cb.Margin = new Thickness(0, 5, 0, 5);
                            cb.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(cb);
                            break;

                        case "password":
                            PasswordBox p = new PasswordBox();
                            p.Height = 20;
                            p.HorizontalAlignment = HorizontalAlignment.Stretch;
                            p.MinWidth = 400;
                            p.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(p);
                            break;

                        case "select":
                            ComboBox select = new ComboBox();
                            select.Width = 400;
                            select.Name = n.Attributes["Key"].Value;
                            foreach (XmlNode childNode in n.ChildNodes) {
                                ComboBoxItem cbi = new ComboBoxItem();
                                cbi.Content = childNode.InnerText;
                                
                                select.Items.Add(cbi);
                            }

                            fieldPanel.Children.Add(select);
                            break;

                        default:
                            // Unsupported type
                            break;

                    }
                    configPanel.Children.Add(fieldPanel);
                    break;

                case "section":
                    Label l = new Label();
                    l.Content = n.Attributes["Name"].Value;
                    l.FontSize = 20;
                    l.Margin = new Thickness(0, 20, 40, 6);
                    l.BorderThickness = new Thickness(0, 0, 0, 1);
                    l.BorderBrush = new SolidColorBrush(Colors.SlateBlue);
                    configPanel.Children.Add(l);

                    foreach (XmlNode childNode in n.ChildNodes) {
                        ParseNode(configPanel, childNode);
                    }
                    break;

                case "text":
                    Label b = new Label();
                    if (n.InnerText != "")
                        b.Content = n.InnerText;

                    if (n.Attributes["Bold"] != null)
                        b.SetValue(Label.FontWeightProperty, FontWeights.Bold);

                    configPanel.Children.Add(b);
                    break;

                default:
                    Console.WriteLine("Unsupported node type: " + n.Name);
                    break;
            }
        }
    }
}
