using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class About : PanelBase {

        public override Panel GetPanel() {

            Grid About = new Grid();

            RowDefinition header = new RowDefinition();
            header.Height = new GridLength(50);
            About.RowDefinitions.Add(header);

            About.RowDefinitions.Add(new RowDefinition());

            ColumnDefinition Main = new ColumnDefinition();
            Main.Width = new System.Windows.GridLength(8, System.Windows.GridUnitType.Star);
            About.ColumnDefinitions.Add(Main);

            About.ColumnDefinitions.Add(new ColumnDefinition());

            About.Margin = new System.Windows.Thickness(10);
            About.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;

            StackPanel options = new StackPanel();
            options.SetValue(Grid.ColumnProperty, 1);
            About.Children.Add(options);

            // TODO: Add update check
            Button b = new Button();
            b.Content = "Update";
            b.SetValue(Button.IsEnabledProperty, false);
            options.Children.Add(b);

            Label l = new Label();
            l.SetValue(Grid.ColumnProperty, 0);
            l.SetValue(Grid.RowProperty, 0);
            l.FontSize = 30;
            l.BorderBrush = new SolidColorBrush(Colors.Crimson);
            l.BorderThickness = new System.Windows.Thickness(0, 0, 0, 1);
            l.Foreground = new SolidColorBrush(Colors.SlateGray);
            l.Content = "About Suibhne";

            About.Children.Add(l);

            RichTextBox rtb = new RichTextBox();

            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.Content = rtb;
            sv.VerticalAlignment = VerticalAlignment.Stretch;
            sv.SetValue(Grid.RowProperty, 1);
            sv.Margin = new Thickness(0, 10, 0, 10);
            sv.Padding = new Thickness(0);

            rtb.SetValue(Grid.RowProperty, 1);
            rtb.SetValue(Grid.ColumnProperty, 0);
            rtb.BorderThickness = new Thickness(0);
            rtb.SetValue(RichTextBox.IsReadOnlyProperty, true);
            rtb.Document.Blocks.Clear();


            Paragraph version = AddBoldText("Version: ", Core.SystemVersion.ToString());
            version.FontSize = 16;
            rtb.Document.Blocks.Add(version);

            Paragraph buildDate = AddBoldText("Build Date: ", "July 27, 2015");
            buildDate.FontSize = 16;
            buildDate.Margin = new Thickness(0, 0, 0, 20);

            rtb.Document.Blocks.Add(buildDate);

            try {
                JArray creds = JArray.Parse(File.ReadAllText(Environment.CurrentDirectory + @"\credits.json"));
                foreach (JToken j in creds.ToList()) {
                    JObject creditLine = (JObject)j;

                    Paragraph line = new Paragraph();
                    line.Margin = new Thickness(0);

                    if (creditLine["text"] != null) {
                        foreach (JObject inline in ((JArray)creditLine["text"])) {
                            if (inline["bold"] != null && inline["bold"].ToString() == "true") {
                                Bold inlineBold = new Bold();
                                inlineBold.Inlines.Add(inline["content"].ToString());
                                line.Inlines.Add(inlineBold);
                            } else {
                                line.Inlines.Add(inline["content"].ToString());
                            }
                        }
                    }

                    if (creditLine["size"] != null) {
                        line.FontSize = creditLine["size"].ToObject<int>();
                    }

                    rtb.Document.Blocks.Add(line);
                }
            }

            catch (Exception) { }

            About.Children.Add(sv);
            
            return About;
            
        }

        private Paragraph AddBoldText(string bold, string otherText = "") {
            Paragraph boldPara = new Paragraph();
            boldPara.Margin = new Thickness(0);
            boldPara.Padding = new Thickness(0);

            Bold boldObj = new Bold();
            boldObj.Inlines.Add(bold);
            boldPara.Inlines.Add(boldObj);
            
            if(otherText != "")
                boldPara.Inlines.Add(otherText);

            return boldPara;
        }
    }
}
