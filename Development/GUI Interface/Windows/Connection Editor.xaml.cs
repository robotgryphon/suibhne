﻿using Newtonsoft.Json.Linq;
using Nini.Config;
using Ostenvighx.Suibhne.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;
using System.Xml;

namespace Ostenvighx.Suibhne.Gui.Windows {
    /// <summary>
    /// Interaction logic for Service_Connection_Editor.xaml
    /// </summary>
    public partial class ConnectionEditor : Window {

        private ServiceItem item;
        private Dictionary<String, FrameworkElement> fields;

        public ConnectionEditor(ServiceItem si) {
            this.item = si;
            InitializeComponent();
        }

        public override void EndInit() {
            base.EndInit();

            this.ServiceNameArea.Content = item.Name;
            LoadFields();
        }

        private void LoadFields() {
            IniConfigSource config = null;
            this.connContainer.Children.Clear();
            this.fields = new Dictionary<string, FrameworkElement>();

            if(File.Exists(Core.ConfigDirectory + "Services/" + item.Identifier + "/service.ini")) {
                config = new IniConfigSource(Core.ConfigDirectory + "Services/" + item.Identifier + "/service.ini");
            }

            try {
                String sectionConfigFilename = Core.ConfigDirectory + @"Connectors\" + item.ServiceType + @"\service.json";
                JObject service = JObject.Parse(File.ReadAllText(sectionConfigFilename));

                foreach (JToken sectionFull in service.Children()) {

                    Expander sect = new Expander();
                    sect.Margin = new Thickness(10);
                    sect.Header = ((JProperty)sectionFull).Name;

                    StackPanel sectPanel = new StackPanel();
                    sect.Content = sectPanel;

                    JProperty configSection = (JProperty)sectionFull;
                    if (!configSection.HasValues)
                        continue;

                    foreach (JToken section in configSection.Children()) {

                        // Got entire section piece
                        JObject sectionParts = (JObject)section;

                        // Now loop through parts, see if there's anything. Fields should be in here..
                        // If no children to section, it's empty and we should skip it.
                        if (!sectionParts.HasValues)
                            continue;

                        if (sectionParts["fields"] == null)
                            continue;

                        if(sectionParts["summary"] != null) {
                            TextBlock summaryBlock = new TextBlock();
                            summaryBlock.Text = sectionParts["summary"].ToString();
                            summaryBlock.Foreground = new SolidColorBrush(Colors.DarkGray);
                            summaryBlock.FontStyle = FontStyles.Italic;
                            sectPanel.Children.Add(summaryBlock);
                        }

                        JObject fields = (JObject) sectionParts["fields"];

                        foreach (JToken field in fields.Children()) {
                            JProperty fieldsSection = (JProperty) field;
                            foreach (JToken fieldToken in fieldsSection.Children()) {
                                Grid fieldWrapper = new Grid();
                                ColumnDefinition Label = new ColumnDefinition();
                                Label.Width = new GridLength(2, GridUnitType.Star);
                                fieldWrapper.ColumnDefinitions.Add(Label);

                                ColumnDefinition Input = new ColumnDefinition();
                                Input.Width = new GridLength(8, GridUnitType.Star);
                                fieldWrapper.ColumnDefinitions.Add(Input);

                                Label fieldLabel = new Label();
                                fieldLabel.SetValue(Grid.ColumnProperty, 0);
                                fieldWrapper.Children.Add(fieldLabel);

                                JObject f = (JObject)fieldToken;
                                fieldLabel.Content = f["label"].ToString();

                                FrameworkElement ui = null;
                                String fieldValue = null;
                                if (config != null && config.Configs[(configSection as JProperty).Name] != null) {
                                    IConfig sectConfig = config.Configs[(configSection as JProperty).Name];
                                    if (sectConfig.Contains((field as JProperty).Name)) {
                                        fieldValue = sectConfig.Get((field as JProperty).Name);
                                    }
                                }

                                #region Element Parse
                                switch (f["type"].ToString().ToLower()) {
                                    case "text":
                                    case "integer":
                                    case "int":
                                    case "number":
                                        ui = new TextBox();
                                        if (f["default"] != null)
                                            (ui as TextBox).Text = f["default"].ToString();

                                        if (fieldValue != null)
                                            (ui as TextBox).Text = fieldValue;
                                        break;

                                    case "checkbox":
                                    case "bool":
                                    case "boolean":
                                    case "yesno":
                                        ui = new CheckBox();
                                        try {
                                            if (
                                                f["default"] != null && 
                                                Boolean.Parse(f["default"].ToString())  ) {
                                                (ui as CheckBox).IsChecked = true;
                                            }

                                            if (fieldValue != null)
                                                (ui as CheckBox).IsChecked = Boolean.Parse(fieldValue);

                                        }
                                        catch (Exception) { }

                                        
                                        break;

                                    case "password":
                                        ui = new PasswordBox();
                                        if (fieldValue != null)
                                            (ui as PasswordBox).Password = fieldValue;
                                        break;
                                }
                                #endregion

                                if (ui != null) {
                                    this.fields.Add((configSection as JProperty).Name + ":::" + (field as JProperty).Name, ui);

                                    ui.SetValue(Grid.ColumnProperty, 1);
                                    ui.SetValue(MarginProperty, new Thickness(0, 4, 0, 4));
                                    fieldWrapper.Children.Add(ui);
                                }

                                sectPanel.Children.Add(fieldWrapper);
                            }
                        }
                        

                    }

                    this.connContainer.Children.Add(sect);

                }
            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        private void ReloadFromDisk(object sender, RoutedEventArgs e) {
            LoadFields();
        }

        private void WriteToDisk(object sender, RoutedEventArgs rea) {
            IniConfigSource config = new IniConfigSource(Core.ConfigDirectory + "Services/" + item.Identifier + "/service.ini");
            MessageBoxResult confirm = MessageBox.Show("By agreeing, you allow the configuration file at " + config.SavePath +
                " to be completely overwritten.", "Overwrite?", MessageBoxButton.OKCancel);

            if (confirm == MessageBoxResult.Cancel)
                return;

            String sectionConfigFilename = Core.ConfigDirectory + @"Connectors\" + item.ServiceType + @"\service.json";
            JObject service = JObject.Parse(File.ReadAllText(sectionConfigFilename));
            try {
                foreach (JToken serviceSection in service.Children()) {
                    IConfig sectionInConfig = config.Configs[(serviceSection as JProperty).Name];
                    if (sectionInConfig == null)
                        sectionInConfig = config.AddConfig((serviceSection as JProperty).Name);

                    JObject serviceSectionAsObject = (JObject) serviceSection.First;
                    if (serviceSectionAsObject["fields"] == null) {
                        Debug.WriteLine("Skipping service section write: " + (serviceSection as JProperty).Name + "; no fields to write.");
                        continue;
                    }

                    JToken fieldsToken = serviceSectionAsObject["fields"];

                    Debug.Write("Working on section '" + (serviceSection as JProperty).Name + "': ");
                    foreach (JToken field in (serviceSectionAsObject["fields"] as JObject).Children()) {
                        Debug.Write((field as JProperty).Name + "; ");
                        FrameworkElement fieldInForm = this.fields[(serviceSection as JProperty).Name + ":::" + (field as JProperty).Name];

                        object value = "";
                        switch(((field as JProperty).First as JObject)["type"].ToString().ToLower()) {
                            case "text":
                                value = (fieldInForm as TextBox).Text;
                                break;

                            case "password":
                                value = (fieldInForm as PasswordBox).Password;
                                break;

                            case "int":
                            case "integer":
                            case "number":
                                try { value = int.Parse((fieldInForm as TextBox).Text); }
                                catch (FormatException) { value = 0; }
                                break;

                            case "bool":
                            case "checkbox":
                            case "boolean":
                            case "yesno":
                                try { value = (fieldInForm as CheckBox).IsChecked; }
                                catch (FormatException) { }
                                break;
                        }

                        sectionInConfig.Set((field as JProperty).Name, value); 
                    }

                    Debug.WriteLine("");
                    Debug.Flush();
                }

                config.Save();
            }

            catch(Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}
